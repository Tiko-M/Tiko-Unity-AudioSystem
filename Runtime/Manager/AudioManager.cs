using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;


namespace Tiko.AudioSystem
{
    [AddComponentMenu("Tiko/AudioSystem/Audio Manager")]
    [DefaultExecutionOrder(-100)]
    public sealed class AudioManager : MonoBehaviour
    {
        [Header("Libraries")]
        [Tooltip("Library bound to your SFX enum. Drag the asset created from EnumLibrary<YourEnum>.")]
        public EnumLibraryBase sfxLibrary;


        [Header("Fallback Routing")]
        public AudioMixerGroup defaultSfxBus;


        [Header("Pooling")]
        [Min(0)] public int poolInitial = 8;
        [Min(1)] public int poolCapacity = 32; // hard cap to avoid runaway


        private readonly Queue<AudioSource> _idle = new Queue<AudioSource>();
        private readonly HashSet<AudioSource> _busy = new HashSet<AudioSource>();


        private readonly Dictionary<int, int> _playingPerKey = new Dictionary<int, int>();
        private readonly Dictionary<int, float> _cooldownUntil = new Dictionary<int, float>();


        private Transform _oneShotRoot;


        private void Awake()
        {
            _oneShotRoot = new GameObject("OneShotRoot").transform;
            _oneShotRoot.SetParent(transform, false);
            WarmPool(poolInitial);
        }


        private void OnDestroy()
        {
            StopAllCoroutines();
        }


        private void WarmPool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _idle.Enqueue(CreateSource());
            }
        }


        private AudioSource CreateSource()
        {
            var go = new GameObject("OneShotSource");
            go.transform.SetParent(_oneShotRoot, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            if (defaultSfxBus != null) src.outputAudioMixerGroup = defaultSfxBus;
            go.SetActive(false);
            return src;
        }

        private AudioSource Rent()
        {
            if (_idle.Count > 0) return _idle.Dequeue();
            if (_busy.Count + _idle.Count < poolCapacity) return CreateSource();
            // drop oldest: naive but safe under cap
            var e = _busy.GetEnumerator();
            if (e.MoveNext())
            {
                var src = e.Current;
                src.Stop();
                _busy.Remove(src);
                return src;
            }
            return CreateSource();
        }

        private void Return(AudioSource src)
        {
            if (src == null) return;
            src.clip = null;
            src.transform.SetParent(_oneShotRoot, false);
            src.gameObject.SetActive(false);
            _busy.Remove(src);
            _idle.Enqueue(src);
        }

        /// <summary>
        /// Generic entry point when you have a typed enum and a matching typed library assigned to sfxLibrary.
        /// </summary>
        public void PlaySfxGeneric<TEnum>(TEnum key, Vector3? worldPos = null, float volumeScale = 1f) where TEnum : struct, Enum
        {
            if (sfxLibrary == null)
            {
                Debug.LogWarning("[AudioManager] No SFX library assigned.");
                return;
            }
            var bound = sfxLibrary.ResolveEnumType();
            if (bound != typeof(TEnum))
            {
                Debug.LogWarning($"[AudioManager] SFX library is bound to {bound?.Name ?? "<null>"} but used with {typeof(TEnum).Name}.");
                return;
            }
            PlaySfxInt(Convert.ToInt32(key), worldPos, volumeScale);
        }

        /// <summary>
        /// Int-based variant so you can call without a compile-time enum type.
        /// </summary>
        public void PlaySfxInt(int key, Vector3? worldPos = null, float volumeScale = 1f)
        {
            if (sfxLibrary == null)
            {
                Debug.LogWarning("[AudioManager] No SFX library assigned.");
                return;
            }
            if (!sfxLibrary.TryGet(key, out var entry))
            {
                Debug.LogWarning($"[AudioManager] SFX key {key} not found in library.");
                return;
            }
            if (entry.clips == null || entry.clips.Count == 0)
            {
                Debug.LogWarning($"[AudioManager] SFX key {key} has no clips.");
                return;
            }


            // cooldown check
            if (entry.cue.cooldownMs > 0)
            {
                var now = Time.unscaledTime;
                if (_cooldownUntil.TryGetValue(key, out var until) && now < until)
                    return;
                _cooldownUntil[key] = now + entry.cue.cooldownMs / 1000f;
            }


            // maxInstances check
            if (entry.cue.maxInstances > 0)
            {
                _playingPerKey.TryGetValue(key, out var cnt);
                if (cnt >= entry.cue.maxInstances) return;
            }


            var clip = entry.clips[UnityEngine.Random.Range(0, entry.clips.Count)];
            if (clip == null) return;


            var src = Rent();
            src.gameObject.SetActive(true);


            if (worldPos.HasValue)
            {
                src.transform.position = worldPos.Value;
            }


            src.clip = clip;
            entry.cue.ApplyToSource(src);
            src.outputAudioMixerGroup = defaultSfxBus;


            // scale final volume after cue application
            src.volume = Mathf.Clamp01(src.volume * Mathf.Max(0f, volumeScale));


            _busy.Add(src);
            _playingPerKey.TryGetValue(key, out var current);
            _playingPerKey[key] = current + 1;


            src.Play();
            StartCoroutine(ReturnWhenDone(src, key));
        }

        private IEnumerator ReturnWhenDone(AudioSource src, int key)
        {
            if (src == null || src.clip == null)
            {
                yield break;
            }
            // only manage one-shots in slice 2
            if (src.loop)
            {
                // safety: stop after a generous cap to avoid leaks in editor play mode
                yield return new WaitForSecondsRealtime(10f);
                src.Stop();
            }
            else
            {
                var dur = Mathf.Max(0.01f, src.clip.length / Mathf.Max(0.1f, Mathf.Abs(src.pitch)));
                yield return new WaitForSecondsRealtime(dur + 0.05f);
            }


            _playingPerKey.TryGetValue(key, out var cnt);
            if (cnt > 1) _playingPerKey[key] = cnt - 1; else _playingPerKey.Remove(key);
            Return(src);
        }
    }
}