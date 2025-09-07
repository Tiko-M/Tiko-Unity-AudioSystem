// ============================================================================
// File: Runtime/Mixers/AudioSettings.cs
// Namespace: Tiko.AudioSystem
// Purpose: Static convenience API to control mixer volumes via AudioMixerController.
// ============================================================================
using UnityEngine;

namespace Tiko.AudioSystem
{
    public static class AudioSettings
    {
        private static AudioMixerController _controller;

        public static void SetController(AudioMixerController controller)
        {
            // why: scene bootstrap can assign explicitly to avoid Find* scan
            _controller = controller;
        }

        private static AudioMixerController Controller
        {
            get
            {
                if (_controller != null) return _controller;
                _controller = UnityEngine.Object.FindFirstObjectByType<AudioMixerController>();
                if (_controller == null)
                {
                    Debug.LogWarning("[AudioSettings] No AudioMixerController found in scene.");
                }
                return _controller;
            }
        }

        public static void SetVolume(AudioBus bus, float linear01) => Controller?.SetVolume(bus, linear01);
        public static float GetVolume(AudioBus bus) => Controller != null ? Controller.GetVolume(bus) : 1f;
        public static void Mute(AudioBus bus, bool mute) => Controller?.Mute(bus, mute);
    }
}
