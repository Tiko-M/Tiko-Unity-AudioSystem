// ============================================================================
// File: Runtime/Managers/BgmController.cs
// Namespace: Tiko.AudioSystem
// Purpose: Two-track BGM playback with crossfade, reading from EnumLibraryBase.
// Note: Keeps independence from AudioManager to avoid touching previous scripts.
// ============================================================================
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace Tiko.AudioSystem
{
    [AddComponentMenu("Tiko/AudioSystem/BGM Controller")]
    [DefaultExecutionOrder(-90)]
    public sealed class BGMController : MonoBehaviour
    {
        [Header("Library")]
        [Tooltip("Library bound to your Bgm enum (or a BoundEnumLibrary bound to the enum).")]
        public EnumLibraryBase bgmLibrary;

        [Header("Routing & Defaults")]
        public AudioMixerGroup defaultBgmBus;
        [Min(0f)] public float defaultCrossfade = 0.6f;
        [Range(0f, 1f)] public float targetVolume = 1f; // master for BGM track volumes

        private AudioSource _a; // active or next
        private AudioSource _b; // the other track
        private AudioSource _current;
        private Coroutine _fadeRoutine;
        private int? _currentKey;

        private void Awake()
        {
            EnsureTracks();
        }

        private void EnsureTracks()
        {
            if (_a != null && _b != null) return;
            _a = CreateTrack("BGM A");
            _b = CreateTrack("BGM B");
            _current = _a;
        }

        private AudioSource CreateTrack(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f; // why: bgm is 2D by default
            src.loop = true;       // will be overridden by cue
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            if (defaultBgmBus != null) src.outputAudioMixerGroup = defaultBgmBus;
            src.volume = 0f;
            return src;
        }

        public void PlayBgmGeneric<TEnum>(TEnum key, float? crossfade = null) where TEnum : struct, Enum
        {
            if (bgmLibrary == null)
            {
                Debug.LogWarning("[BgmController] No BGM library assigned.");
                return;
            }
            var bound = bgmLibrary.ResolveEnumType();
            if (bound != typeof(TEnum))
            {
                Debug.LogWarning($"[BgmController] Library is bound to {bound?.Name ?? "<null>"} but used with {typeof(TEnum).Name}.");
                return;
            }
            PlayBgmInt(Convert.ToInt32(key), crossfade);
        }

        public void PlayBgmInt(int key, float? crossfade = null)
        {
            if (bgmLibrary == null) { Debug.LogWarning("[BgmController] No BGM library."); return; }
            if (!bgmLibrary.TryGet(key, out var entry)) { Debug.LogWarning($"[BgmController] Key {key} not found."); return; }
            if (entry.clips == null || entry.clips.Count == 0) { Debug.LogWarning($"[BgmController] Key {key} has no clips."); return; }

            if (_currentKey.HasValue && _currentKey.Value == key && _current != null && _current.isPlaying)
                return; // same bgm already playing

            var clip = entry.clips[UnityEngine.Random.Range(0, entry.clips.Count)];
            var next = (_current == _a) ? _b : _a;

            // Configure next track
            next.Stop();
            next.clip = clip;
            entry.cue.ApplyToSource(next);
            next.outputAudioMixerGroup = defaultBgmBus;
            next.volume = 0f; // start muted for crossfade
            next.Play();

            var dur = Mathf.Max(0f, crossfade ?? defaultCrossfade);
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(Crossfade(_current, next, dur));
            _current = next;
            _currentKey = key;
        }

        public void StopBgm(float fadeOut = 0.5f)
        {
            if (_fadeRoutine != null) { StopCoroutine(_fadeRoutine); _fadeRoutine = null; }
            if (_current == null) return;
            StartCoroutine(FadeOutAndStop(_current, Mathf.Max(0f, fadeOut)));
            _currentKey = null;
        }

        public void PauseBgm() { _a?.Pause(); _b?.Pause(); }
        public void ResumeBgm() { _a?.UnPause(); _b?.UnPause(); }

        private IEnumerator Crossfade(AudioSource from, AudioSource to, float seconds)
        {
            if (from == null && to == null) yield break;
            if (seconds <= 0f)
            {
                if (from != null) from.Stop();
                if (to != null) to.volume = targetVolume;
                yield break;
            }

            float t = 0f;
            float fromStart = from != null ? from.volume : 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                if (from != null) from.volume = Mathf.Lerp(fromStart, 0f, k);
                if (to != null) to.volume = Mathf.Lerp(0f, targetVolume, k);
                yield return null;
            }
            if (from != null) from.Stop();
            if (to != null) to.volume = targetVolume;
        }

        private IEnumerator FadeOutAndStop(AudioSource src, float seconds)
        {
            if (src == null) yield break;
            float start = src.volume;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                src.volume = Mathf.Lerp(start, 0f, k);
                yield return null;
            }
            src.Stop();
            src.volume = 0f;
        }
    }
}
