using System;
using UnityEngine;

namespace Tiko.AudioSystem
{
    public static class Audio
    {
        private static AudioManager _cached;

        public static void SetManager(AudioManager manager) => _cached = manager; // why: explicit override in bootstraps

        private static AudioManager Instance
        {
            get
            {
                if (_cached != null) return _cached;
                _cached = UnityEngine.Object.FindFirstObjectByType<AudioManager>();
                if (_cached == null)
                {
                    Debug.LogWarning("[Audio] No AudioManager in scene. Add one from 'AudioSystem/Audio Manager'.");
                }
                return _cached;
            }
        }

        public static void Play<TEnum>(TEnum key, Vector3? worldPos = null, float volumeScale = 1f) where TEnum : struct, Enum
        {
            var inst = Instance; if (inst == null) return;
            inst.PlaySfxGeneric(key, worldPos, volumeScale);
        }

        public static void PlayInt(int key, Vector3? worldPos = null, float volumeScale = 1f)
        {
            var inst = Instance; if (inst == null) return;
            inst.PlaySfxInt(key, worldPos, volumeScale);
        }
    }
}
