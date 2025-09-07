// ============================================================================
// File: Runtime/Managers/Music.cs
// Namespace: Tiko.AudioSystem
// Purpose: Static facade for BGM control via BgmController.
// ============================================================================
using System;
using UnityEngine;

namespace Tiko.AudioSystem
{
    public static class Music
    {
        private static BGMController _cached;

        public static void SetController(BGMController controller) => _cached = controller; // why: allow explicit bootstrap

        private static BGMController Controller
        {
            get
            {
                if (_cached != null) return _cached;
                _cached = UnityEngine.Object.FindFirstObjectByType<BGMController>();
                if (_cached == null)
                {
                    Debug.LogWarning("[Music] No BgmController in scene. Add one from 'AudioSystem/BGM Controller'.");
                }
                return _cached;
            }
        }

        public static void Play<TEnum>(TEnum key, float? crossfade = null) where TEnum : struct, Enum
        {
            var ctrl = Controller; if (ctrl == null) return;
            ctrl.PlayBgmGeneric(key, crossfade);
        }

        public static void PlayInt(int key, float? crossfade = null)
        {
            var ctrl = Controller; if (ctrl == null) return;
            ctrl.PlayBgmInt(key, crossfade);
        }

        public static void Stop(float fadeOut = 0.5f)
        {
            var ctrl = Controller; if (ctrl == null) return;
            ctrl.StopBgm(fadeOut);
        }

        public static void Pause() { Controller?.PauseBgm(); }
        public static void Resume() { Controller?.ResumeBgm(); }
    }
}
