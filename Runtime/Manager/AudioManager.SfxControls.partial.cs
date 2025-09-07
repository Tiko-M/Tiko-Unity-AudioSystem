// ============================================================================
// File: Runtime/Managers/AudioManager.SfxControls.partial.cs
// Namespace: Tiko.AudioSystem
// Requires: Change class declaration in AudioManager.cs to 'public sealed partial class AudioManager'
// Purpose: Stop-by-key / Stop-all / Fade-out / Follow-transform SFX helpers without touching old file body.
// ============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tiko.AudioSystem
{
    public sealed partial class AudioManager
    {
        // Track active fades to avoid stacking multiple coroutines on the same source.
        private readonly Dictionary<AudioSource, Coroutine> _activeFades = new Dictionary<AudioSource, Coroutine>();

        /// <summary>Stop all currently playing SFX. Optional fade out seconds.</summary>
        public void StopAllSfx(float fadeOut = 0f)
        {
            var snapshot = new List<AudioSource>(_busy);
            foreach (var src in snapshot)
            {
                if (src == null) continue;
                if (fadeOut > 0f) StartFade(src, fadeOut, stopAndReturn: true);
                else ImmediateStop(src);
            }
        }

        /// <summary>Stop SFX by enum key with optional fade-out.</summary>
        public void StopSfxByKey<TEnum>(TEnum key, float fadeOut = 0f) where TEnum : struct, Enum
        {
            if (sfxLibrary == null) return;
            var bound = sfxLibrary.ResolveEnumType();
            if (bound != typeof(TEnum)) return;
            StopSfxByInt(Convert.ToInt32(key), fadeOut);
        }

        /// <summary>Stop SFX by raw int key with optional fade-out.</summary>
        public void StopSfxByInt(int key, float fadeOut = 0f)
        {
            if (sfxLibrary == null) return;
            if (!sfxLibrary.TryGet(key, out var entry)) return;
            var set = new HashSet<AudioClip>(entry.clips);

            var snapshot = new List<AudioSource>(_busy);
            foreach (var src in snapshot)
            {
                if (src == null || src.clip == null) continue;
                if (!set.Contains(src.clip)) continue;
                if (fadeOut > 0f) StartFade(src, fadeOut, stopAndReturn: true);
                else ImmediateStop(src);
            }
        }

        /// <summary>Play SFX and keep the AudioSource following a Transform until completed.</summary>
        public void PlaySfxFollow<TEnum>(TEnum key, Transform follow, float volumeScale = 1f) where TEnum : struct, Enum
        {
            if (follow == null) { PlaySfxGeneric(key, null, volumeScale); return; }
            var bound = sfxLibrary != null ? sfxLibrary.ResolveEnumType() : null;
            if (sfxLibrary == null || bound != typeof(TEnum)) { Debug.LogWarning("[AudioManager] Follow: library not bound to enum."); return; }
            int intKey = Convert.ToInt32(key);

            if (!sfxLibrary.TryGet(intKey, out var entry) || entry.clips == null || entry.clips.Count == 0)
                return;

            if (entry.cue.cooldownMs > 0)
            {
                var now = Time.unscaledTime;
                if (_cooldownUntil.TryGetValue(intKey, out var until) && now < until) return;
                _cooldownUntil[intKey] = now + entry.cue.cooldownMs / 1000f;
            }
            if (entry.cue.maxInstances > 0)
            {
                _playingPerKey.TryGetValue(intKey, out var cnt);
                if (cnt >= entry.cue.maxInstances) return;
            }

            var clip = entry.clips[UnityEngine.Random.Range(0, entry.clips.Count)];
            if (clip == null) return;

            var src = Rent();
            src.gameObject.SetActive(true);
            src.transform.position = follow.position;
            src.clip = clip;
            entry.cue.ApplyToSource(src);
            src.outputAudioMixerGroup = defaultSfxBus;

            src.volume = Mathf.Clamp01(src.volume * Mathf.Max(0f, volumeScale));

            _busy.Add(src);
            _playingPerKey.TryGetValue(intKey, out var current);
            _playingPerKey[intKey] = current + 1;

            src.Play();
            StartCoroutine(FollowAndReturn(src, intKey, follow));
        }

        private IEnumerator FollowAndReturn(AudioSource src, int key, Transform follow)
        {
            while (src != null && src.isPlaying)
            {
                if (follow != null) src.transform.position = follow.position;
                yield return null;
            }
            _playingPerKey.TryGetValue(key, out var cnt);
            if (cnt > 1) _playingPerKey[key] = cnt - 1; else _playingPerKey.Remove(key);
            Return(src);
        }

        // -------------- Fade helpers --------------
        private void StartFade(AudioSource src, float seconds, bool stopAndReturn)
        {
            if (src == null) return;
            if (_activeFades.TryGetValue(src, out var co) && co != null) StopCoroutine(co);
            var routine = StartCoroutine(FadeOutOne(src, Mathf.Max(0f, seconds), stopAndReturn));
            _activeFades[src] = routine;
        }

        private IEnumerator FadeOutOne(AudioSource src, float seconds, bool stopAndReturn)
        {
            if (src == null) yield break;
            float start = src.volume;
            float t = 0f;
            while (t < seconds && src != null && src.isPlaying)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                src.volume = Mathf.Lerp(start, 0f, k);
                yield return null;
            }
            if (src != null)
            {
                src.Stop();
                src.volume = 0f;
                if (stopAndReturn) Return(src); // Return handles book-keeping
            }
            _activeFades.Remove(src);
        }

        private void ImmediateStop(AudioSource src)
        {
            if (src == null) return;
            if (_activeFades.TryGetValue(src, out var co) && co != null) StopCoroutine(co);
            _activeFades.Remove(src);
            src.Stop();
            Return(src);
        }
    }
}
