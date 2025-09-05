using UnityEngine;

namespace Tiko.AudioSystem
{
    /// <summary>
    /// Component phụ gắn lên pooled AudioSource để bám theo 1 Transform trong khi phát.
    /// Tự tắt khi không còn phát.
    /// </summary>
    [DisallowMultipleComponent]
    public class AttachedAudioSource : MonoBehaviour
    {
        public Transform target;
        private AudioSource _src;

        private void Awake()
        {
            _src = GetComponent<AudioSource>();
        }

        private void LateUpdate()
        {
            if (target != null)
            {
                transform.position = target.position;
            }
            if (_src != null && !_src.isPlaying)
            {
                target = null;
                enabled = false;
            }
        }
    }
}