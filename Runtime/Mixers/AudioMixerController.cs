using UnityEngine;
using UnityEngine.Audio;

namespace Tiko.AudioSystem
{
    [AddComponentMenu("AudioSystem/Audio Mixer Controller")]
    [DefaultExecutionOrder(-95)]
    public sealed class AudioMixerController : MonoBehaviour
    {
        [Header("Mixer")]
        public AudioMixer mixer;
        [Tooltip("Exposed parameter name for Master volume in dB.")] public string masterParam = "MasterVolume";
        [Tooltip("Exposed parameter name for BGM volume in dB.")] public string bgmParam = "BgmVolume";
        [Tooltip("Exposed parameter name for SFX volume in dB.")] public string sfxParam = "SfxVolume";

        [Header("Persistence")]
        public bool saveToPlayerPrefs = true;
        public string prefsPrefix = "audio.vol."; // keys: audio.vol.master/bg m/sfx

        private const float MutedDb = -80f; // Unity's common mute floor

        private void Awake()
        {
            if (mixer == null)
            {
                Debug.LogWarning("[AudioMixerController] Mixer not assigned.");
                return;
            }
            LoadAndApplyAll();
            AudioSettings.SetController(this); // allow static facade access
        }

        // ------------- External API (instance) -------------
        public void SetVolume(AudioBus bus, float linear01)
        {
            float db = LinearToDb(Mathf.Clamp01(linear01));
            string param = GetParam(bus);
            if (!string.IsNullOrEmpty(param)) mixer.SetFloat(param, db);
            if (saveToPlayerPrefs) Save(bus, linear01);
        }

        public float GetVolume(AudioBus bus)
        {
            float db;
            string param = GetParam(bus);
            if (!string.IsNullOrEmpty(param) && mixer.GetFloat(param, out db))
            {
                return DbToLinear(db);
            }
            return 1f;
        }

        public void Mute(AudioBus bus, bool mute)
        {
            string param = GetParam(bus);
            if (string.IsNullOrEmpty(param)) return;
            if (mute)
            {
                mixer.SetFloat(param, MutedDb);
            }
            else
            {
                // restore from prefs or default 1f
                SetVolume(bus, Load(bus, 1f));
            }
        }

        public void LoadAndApplyAll()
        {
            SetVolume(AudioBus.Master, Load(AudioBus.Master, 1f));
            SetVolume(AudioBus.Bgm, Load(AudioBus.Bgm, 1f));
            SetVolume(AudioBus.Sfx, Load(AudioBus.Sfx, 1f));
        }

        // ------------- Helpers -------------
        private string GetParam(AudioBus bus)
        {
            switch (bus)
            {
                case AudioBus.Master: return masterParam;
                case AudioBus.Bgm: return bgmParam;
                case AudioBus.Sfx: return sfxParam;
                default: return null;
            }
        }

        private void Save(AudioBus bus, float linear)
        {
            PlayerPrefs.SetFloat(prefsPrefix + Key(bus), Mathf.Clamp01(linear));
        }

        private float Load(AudioBus bus, float fallback)
        {
            return PlayerPrefs.GetFloat(prefsPrefix + Key(bus), fallback);
        }

        private static string Key(AudioBus bus)
        {
            switch (bus)
            {
                case AudioBus.Master: return "master";
                case AudioBus.Bgm: return "bgm";
                case AudioBus.Sfx: return "sfx";
                default: return "unknown";
            }
        }

        private static float LinearToDb(float v)
        {
            if (v <= 0.0001f) return MutedDb;
            return Mathf.Log10(v) * 20f; // 1.0 -> 0 dB, 0.5 -> -6.02 dB
        }

        private static float DbToLinear(float db)
        {
            return Mathf.Pow(10f, db / 20f);
        }
    }
}
