using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Tiko.AudioSystem
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private AudioLibrary library;
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;

        [Header("Exposed Mixer Params (dB)")]
        [SerializeField] private string exposedMaster = "MasterVol";
        [SerializeField] private string exposedMusic = "MusicVol";
        [SerializeField] private string exposedSFX = "SFXVol";

        [Header("Music")]
        [SerializeField] private AudioSource musicA;
        [SerializeField] private AudioSource musicB;
        [SerializeField, Range(0f, 1f)] private float musicVolume01 = 1f;
        [SerializeField, Range(0f, 1f)] private float timeFade = 1f;
        private bool musicAActive = true;

        [Header("SFX Pooling")]
        [SerializeField] private int initialSfxSources = 16;
        [SerializeField] private int maxSfxSources = 64;
        private readonly List<AudioSource> sfxPool = new List<AudioSource>();
        private readonly Queue<AudioSource> idleSfx = new Queue<AudioSource>();

        // Tracking
        private readonly Dictionary<string, int> activeByType = new Dictionary<string, int>();
        private readonly Dictionary<AudioSource, string> srcToType = new Dictionary<AudioSource, string>();
        private readonly Dictionary<string, float> lastPlayedTime = new Dictionary<string, float>();
        // V1.1
        private readonly Dictionary<AudioSource, float> _startedAt = new Dictionary<AudioSource, float>();



        private int _nextHandleId = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureMusicSources();
            BuildSfxPool(initialSfxSources);
            ApplyMixerVolumes();
        }

        #region Music
        private void EnsureMusicSources()
        {
            if (musicA == null)
            {
                var go = new GameObject("Music_A");
                go.transform.SetParent(transform);
                musicA = go.AddComponent<AudioSource>();
                musicA.playOnAwake = false;
                musicA.loop = true;
                musicA.outputAudioMixerGroup = musicGroup;
            }
            if (musicB == null)
            {
                var go = new GameObject("Music_B");
                go.transform.SetParent(transform);
                musicB = go.AddComponent<AudioSource>();
                musicB.playOnAwake = false;
                musicB.loop = true;
                musicB.outputAudioMixerGroup = musicGroup;
            }
        }

        public void PlayMusic(string key)
        {
            var data = library != null ? library.GetAudioData(key) : null;
            if (data == null)
            {
                Debug.LogWarning($"[AudioManager] Không tìm thấy AudioData cho {key}");
                return;
            }
            var clip = data.GetRandomClip();
            if (clip == null) { Debug.LogWarning($"[AudioManager] Clip rỗng cho {key}"); return; }

            var inactive = musicAActive ? musicB : musicA;
            inactive.clip = clip;
            inactive.volume = 0f;
            inactive.outputAudioMixerGroup = musicGroup;
            inactive.loop = true;
            inactive.Play();

            StopAllCoroutines();
            StartCoroutine(CrossfadeMusicRoutine(inactive, timeFade));
        }

        public void PauseMusic()
        {
            EnsureMusicSources();
            StopAllCoroutines();

            if (timeFade > 0f)
                StartCoroutine(PauseMusicRoutine(timeFade));
            else
            {
                if (musicA != null && musicA.isPlaying) musicA.Pause();
                if (musicB != null && musicB.isPlaying) musicB.Pause();
            }
        }

        public void ResumeMusic()
        {
            EnsureMusicSources();

            var active = musicAActive ? musicA : musicB;
            var inactive = musicAActive ? musicB : musicA;

            if (active == null || active.clip == null) return;

            if (inactive != null) inactive.volume = 0f;

            StopAllCoroutines();
            if (!active.isPlaying) active.UnPause();

            if (timeFade > 0f)
                StartCoroutine(ResumeMusicRoutine(active, timeFade));
            else
                active.volume = musicVolume01;
        }

        public void StopMusic()
        {
            EnsureMusicSources();
            StopAllCoroutines();

            if (timeFade > 0f)
                StartCoroutine(StopMusicRoutine(timeFade));
            else
            {
                if (musicA != null)
                {
                    musicA.volume = 0f;
                    musicA.Stop();
                    musicA.clip = null;
                }
                if (musicB != null)
                {
                    musicB.volume = 0f;
                    musicB.Stop();
                    musicB.clip = null;
                }
            }
        }


        private IEnumerator PauseMusicRoutine(float duration)
        {
            var a = musicA; var b = musicB;
            float a0 = (a != null) ? a.volume : 0f;
            float b0 = (b != null) ? b.volume : 0f;

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
                if (a != null) a.volume = Mathf.Lerp(a0, 0f, k);
                if (b != null) b.volume = Mathf.Lerp(b0, 0f, k);
                yield return null;
            }

            if (a != null && a.isPlaying) a.Pause();
            if (b != null && b.isPlaying) b.Pause();
        }

        private IEnumerator ResumeMusicRoutine(AudioSource active, float duration)
        {
            if (active == null) yield break;

            float start = active.volume; // có thể đang 0 từ lần pause trước
                                         // Track kia tắt tiếng
            var inactive = (active == musicA) ? musicB : musicA;
            if (inactive != null) inactive.volume = 0f;

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
                active.volume = Mathf.Lerp(start, musicVolume01, k);
                yield return null;
            }
            active.volume = musicVolume01;
        }

        private IEnumerator StopMusicRoutine(float duration)
        {
            var a = musicA; var b = musicB;
            float a0 = (a != null) ? a.volume : 0f;
            float b0 = (b != null) ? b.volume : 0f;

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
                if (a != null) a.volume = Mathf.Lerp(a0, 0f, k);
                if (b != null) b.volume = Mathf.Lerp(b0, 0f, k);
                yield return null;
            }

            if (a != null)
            {
                a.volume = 0f;
                a.Stop();
                a.clip = null;
            }
            if (b != null)
            {
                b.volume = 0f;
                b.Stop();
                b.clip = null;
            }
        }


        private IEnumerator CrossfadeMusicRoutine(AudioSource toActive, float duration)
        {
            var from = musicAActive ? musicA : musicB;
            var to = toActive;
            musicAActive = (to == musicA);

            if (!to.isPlaying) to.Play();
            if (!from.isPlaying) from.Play();

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
                to.volume = k * musicVolume01;
                from.volume = (1f - k) * musicVolume01;
                yield return null;
            }
            to.volume = musicVolume01;
            from.volume = 0f;
            from.Stop();
        }

        public void SetMusicVolume01(float v)
        {
            musicVolume01 = Mathf.Clamp01(v);
            SetExposed01(exposedMusic, v);
            (musicA ??= null)?.SetScheduledEndTime(AudioSettings.dspTime); // no-op safety
        }
        #endregion

        #region Mixer Volume Helpers
        public void SetMasterVolume01(float v) => SetExposed01(exposedMaster, v);
        public void SetSFXVolume01(float v) => SetExposed01(exposedSFX, v);

        private void ApplyMixerVolumes()
        {
            SetExposed01(exposedMaster, 1f);
            SetExposed01(exposedMusic, musicVolume01);
            SetExposed01(exposedSFX, 1f);
        }

        private void SetExposed01(string param, float v01)
        {
            if (mixer == null || string.IsNullOrEmpty(param)) return;
            float v = Mathf.Clamp01(v01);
            float dB = v > 0.0001f ? Mathf.Log10(v) * 20f : -80f; // 0..1 -> -80..0 dB
            mixer.SetFloat(param, dB);
        }
        #endregion

        #region SFX
        private void BuildSfxPool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var src = CreatePooledSfx();
                idleSfx.Enqueue(src);
            }
        }

        private AudioSource CreatePooledSfx()
        {
            var go = new GameObject("SFX_Source");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f;
            src.outputAudioMixerGroup = sfxGroup;
            sfxPool.Add(src);
            return src;
        }

        private AudioSource GetSfxSource()
        {
            if (idleSfx.Count > 0) return idleSfx.Dequeue();
            if (sfxPool.Count < maxSfxSources) return CreatePooledSfx();
            foreach (var s in sfxPool) if (!s.isPlaying) return s;
            return null;
        }

        private void ReleaseSfxSource(AudioSource src)
        {
            if (src == null) return;
            src.Stop();
            src.clip = null;
            src.transform.SetParent(transform);
            var attached = src.GetComponent<AttachedAudioSource>();
            if (attached) { attached.target = null; attached.enabled = false; }
            if (!idleSfx.Contains(src)) idleSfx.Enqueue(src);
        }

        public AudioHandle PlaySFX(string key) => PlaySFXInternal(key, null, null);
        public AudioHandle PlaySFXAt(string key, Vector3 position) => PlaySFXInternal(key, position, null);
        public AudioHandle PlaySFXFollow(string key, Transform target) => PlaySFXInternal(key, null, target);

        private AudioHandle PlaySFXInternal(string key, Vector3? position, Transform followTarget)
        {
            var data = library != null ? library.GetAudioData(key) : null;

            if (data.maxInstances > 0 && activeByType.TryGetValue(key, out var curCount) && curCount >= data.maxInstances)
            {
                AudioSource oldest = null;
                float oldestT = float.MaxValue;

                // Tìm source cùng key cũ nhất
                foreach (var kv in srcToType)
                {
                    if (!kv.Value.Equals(key)) continue;
                    if (_startedAt.TryGetValue(kv.Key, out var t) && t < oldestT)
                    {
                        oldestT = t;
                        oldest = kv.Key;
                    }
                }

                if (oldest != null)
                {
                    // giảm đếm & release
                    if (activeByType.TryGetValue(key, out var n)) activeByType[key] = Mathf.Max(0, n - 1);
                    srcToType.Remove(oldest);
                    _startedAt.Remove(oldest);
                    ReleaseSfxSource(oldest);
                }
                else
                {
                    // fallback (hiếm): không thể steal -> bỏ phát
                    return default;
                }
            }

            if (data == null)
            {
                Debug.LogWarning($"[AudioManager] Không tìm thấy AudioData cho {key}");
                return default;
            }

            float now = data.useUnscaledCooldown ? Time.unscaledTime : Time.time;
            if (data.minInterval > 0f && lastPlayedTime.TryGetValue(key, out float lastT))
            {
                if (now - lastT < data.minInterval) return default;
            }
            lastPlayedTime[key] = now;



            var src = GetSfxSource();
            if (src == null) return default;

            var clip = data.GetRandomClip();
            if (clip == null) return default;

            srcToType[src] = key;
            activeByType[key] = activeByType.TryGetValue(key, out var count) ? count + 1 : 1;

            src.clip = clip;
            src.loop = data.loop;
            src.outputAudioMixerGroup = sfxGroup; // luôn route qua SFX
            src.pitch = Mathf.Clamp(data.GetRandomPitch(), 0.1f, 3f);

            if (position.HasValue || followTarget != null)
            {
                if (followTarget != null)
                {
                    var att = src.GetComponent<AttachedAudioSource>();
                    if (att == null) att = src.gameObject.AddComponent<AttachedAudioSource>();
                    att.target = followTarget;
                    att.enabled = true;
                    src.transform.position = followTarget.position;
                }
                else
                {
                    src.transform.position = position ?? Vector3.zero;
                }
            }
            else
            {
                src.spatialBlend = 0f;
            }

            if (position.HasValue || followTarget != null)
            {
                src.spatialBlend = 1f;
                src.dopplerLevel = 0f;
                if (followTarget != null)
                {
                    var att = src.GetComponent<AttachedAudioSource>();
                    if (att == null) att = src.gameObject.AddComponent<AttachedAudioSource>();
                    att.target = followTarget;
                    att.enabled = true;
                    src.transform.position = followTarget.position;
                }
                else
                {
                    src.transform.position = position ?? Vector3.zero;
                }
            }
            else
            {
                src.spatialBlend = 0f;
            }

            src.outputAudioMixerGroup = data.overrideGroup != null ? data.overrideGroup : sfxGroup;
            src.priority = Mathf.Clamp(data.priority, 0, 256);

            src.volume = Mathf.Clamp01(data.volume);
            src.Play();
            _startedAt[src] = Time.unscaledTime;
            srcToType[src] = key;
            activeByType[key] = activeByType.TryGetValue(key, out var cnt) ? cnt + 1 : 1;


            var handle = new AudioHandle { id = _nextHandleId++, source = src };
            StartCoroutine(ReleaseAfter(src, key));
            return handle;
        }

        public void StopSFX(AudioHandle handle)
        {
            if (!handle.IsValid) return;
            var src = handle.source;
            if (srcToType.TryGetValue(src, out var key))
            {
                if (activeByType.TryGetValue(key, out var n)) activeByType[key] = Mathf.Max(0, n - 1);
                srcToType.Remove(src);
            }
            ReleaseSfxSource(src);
        }

        public void StopSFX(string key)
        {
            var toStop = new List<AudioSource>();
            foreach (var kv in srcToType)
            {
                if (kv.Value.Equals(key)) toStop.Add(kv.Key);
            }
            foreach (var s in toStop) ReleaseSfxSource(s);
            activeByType[key] = 0;
        }

        private IEnumerator ReleaseAfter(AudioSource src, string key)
        {
            var data = library != null ? library.GetAudioData(key) : null;
            float length = (src != null && src.clip != null) ? src.clip.length : 0f;
            float baseTtl = length / Mathf.Max(0.01f, src != null ? src.pitch : 1f);

            float ttl;
            if (src != null && src.loop)
            {
                if (data != null && data.maxDuration > 0f)
                    ttl = data.maxDuration;
                else
                    ttl = 600f;
            }
            else
            {
                ttl = baseTtl;
            }

            float end = Time.unscaledTime + Mathf.Max(0.01f, ttl);
            while (src != null && Time.unscaledTime < end && (src.isPlaying || src.loop))
            {
                yield return null;
            }

            if (srcToType.TryGetValue(src, out var type))
            {
                if (activeByType.TryGetValue(type, out var n)) activeByType[type] = Mathf.Max(0, n - 1);
                srcToType.Remove(src);
            }
            _startedAt.Remove(src);
            ReleaseSfxSource(src);

        }
        public void FadeOutHandle(AudioHandle handle, float duration)
        {
            if (!handle.IsValid) return;
            StartCoroutine(FadeOutThenRelease(handle.source, duration));
        }

        private IEnumerator FadeOutThenRelease(AudioSource src, float duration)
        {
            if (src == null) yield break;
            float t = 0f;
            float v0 = src.volume;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = (duration > 0f) ? Mathf.Clamp01(t / duration) : 1f;
                src.volume = Mathf.Lerp(v0, 0f, k);
                yield return null;
            }
            src.volume = 0f;

            if (srcToType.TryGetValue(src, out var type))
            {
                if (activeByType.TryGetValue(type, out var n)) activeByType[type] = Mathf.Max(0, n - 1);
                srcToType.Remove(src);
            }
            _startedAt.Remove(src);
            ReleaseSfxSource(src);
        }

        #endregion
    }


}
