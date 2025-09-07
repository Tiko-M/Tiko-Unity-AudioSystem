using UnityEngine;

namespace Tiko.AudioSystem
{
    public struct AudioHandle
    {
        internal int id;
        internal AudioSource source;

        public bool IsValid => source != null && id != 0;
        public bool IsPlaying => IsValid && source.isPlaying;

        public void FadeOut(float duration)
        {
            if (!IsValid) return;
            // AudioManager.Instance.FadeOutHandle(this, duration);
        }
        public void Stop()
        {
            // if (IsValid) AudioManager.Instance.StopSFX(this);
        }

        public void SetVolume(float volume01)
        {
            if (!IsValid) return;
            source.volume = Mathf.Clamp01(volume01);
        }

        public void SetPitch(float pitch)
        {
            if (!IsValid) return;
            source.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
        }
    }
}