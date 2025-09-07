using System;
using UnityEngine;


namespace Tiko.AudioSystem
{
    [Serializable]
    public sealed class AudioCue
    {
        [Range(0f, 2f)] public float volume = 1f;
        [Range(0f, 1f)] public float volumeRandom = 0f; // why: lightweight natural variation

        [Range(0.1f, 3f)] public float pitch = 1f;
        [Range(0f, 1f)] public float pitchRandom = 0f; // why: avoid robotic repetition

        public bool loop = false;
        [Range(0f, 1f)] public float spatialBlend = 0f; // 0=2D, 1=3D
        [Min(0f)] public float dopplerLevel = 0f;
        [Range(0f, 360f)] public float spread = 0f;

        [Min(0)] public int maxInstances = 0; // 0 = unlimited; enforced in runtime slice
        [Min(0)] public int cooldownMs = 0; // enforced in runtime slice


        /// <summary>Apply randomization.</summary>
        public void ApplyToSource(AudioSource source)
        {
            // why: keep runtime deterministic yet varied
            var vRand = (UnityEngine.Random.value * 2f - 1f) * volumeRandom; // [-r, +r]
            var pRand = (UnityEngine.Random.value * 2f - 1f) * pitchRandom;


            source.volume = Mathf.Clamp01(volume + vRand);
            source.pitch = Mathf.Clamp(pitch + pRand, 0.1f, 3f);
            source.loop = loop;
            source.spatialBlend = spatialBlend;
            source.dopplerLevel = dopplerLevel;
            source.spread = spread;
        }
    }
}