using UnityEngine;

namespace AudioSystem
{
    [System.Serializable]
    public class AudioData
    {
        public string name;
        public EAudio key;
        public AudioClip[] clips;

        [Range(0f, 1f)] public float volume = 1f;
        public bool loop = false;
        [Tooltip("Khoảng cách giữa 2 lần phát tối thiểu (giây).")]
        public float minInterval = 0f;

        [Header("Biến thể & Kiểm soát")]
        [Tooltip("Pitch random trong khoảng [x, y].")]
        public Vector2 pitchRange = new Vector2(1f, 1f);

        public bool useUnscaledCooldown = false;

        public AudioClip GetRandomClip()
        {
            if (clips == null || clips.Length == 0) return null;
            if (clips.Length == 1) return clips[0];
            int idx = Random.Range(0, clips.Length);
            return clips[idx];
        }

        public float GetRandomPitch()
        {
            return Random.Range(pitchRange.x, pitchRange.y);
        }
    }
}
