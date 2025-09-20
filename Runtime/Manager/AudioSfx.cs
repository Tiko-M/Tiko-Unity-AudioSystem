using System;
using UnityEngine;

namespace Tiko.AudioSystem
{
    public static class AudioSfx
    {
        private static AudioManager _am;
        private static AudioManager AM
        {
            get
            {
                if (_am != null) return _am;
                _am = UnityEngine.Object.FindFirstObjectByType<AudioManager>();
                if (_am == null) Debug.LogWarning("[AudioSfx] No AudioManager in scene.");
                return _am;
            }
        }

        public static void StopAll(float fadeOut = 0f) => AM?.StopAllSfx(fadeOut);

        public static void Stop<TEnum>(TEnum key, float fadeOut = 0f) where TEnum : struct, Enum
            => AM?.StopSfxByKey(key, fadeOut);

        public static void StopInt(int key, float fadeOut = 0f)
            => AM?.StopSfxByInt(key, fadeOut);

        public static void Play<TEnum>(TEnum key, Vector3? worldPos = null, float volumeScale = 1f) where TEnum : struct, Enum
                => AM?.PlaySfxGeneric(key, worldPos, volumeScale);
        public static void PlayFollow<TEnum>(TEnum key, Transform follow, float volumeScale = 1f) where TEnum : struct, Enum
            => AM?.PlaySfxFollow(key, follow, volumeScale);
    }
}
