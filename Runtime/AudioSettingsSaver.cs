using UnityEngine;

namespace AudioSystem
{
    public class AudioSettingsSaver : MonoBehaviour
    {
        private const string KEY_MASTER = "Audio_Master";
        private const string KEY_MUSIC  = "Audio_Music";
        private const string KEY_SFX    = "Audio_SFX";

        private void Start()
        {
            LoadAndApply();
        }

        public static void Save(float master, float music, float sfx)
        {
            PlayerPrefs.SetFloat(KEY_MASTER, Mathf.Clamp01(master));
            PlayerPrefs.SetFloat(KEY_MUSIC,  Mathf.Clamp01(music));
            PlayerPrefs.SetFloat(KEY_SFX,    Mathf.Clamp01(sfx));
            PlayerPrefs.Save();
        }

        public static void Load(out float master, out float music, out float sfx)
        {
            master = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
            music  = PlayerPrefs.GetFloat(KEY_MUSIC,  1f);
            sfx    = PlayerPrefs.GetFloat(KEY_SFX,    1f);
        }

        public static void LoadAndApply()
        {
            Load(out var master, out var music, out var sfx);
            var mgr = AudioManager.Instance;
            if (mgr != null)
            {
                mgr.SetMasterVolume01(master);
                mgr.SetMusicVolume01(music);
                mgr.SetSFXVolume01(sfx);
            }
        }
    }
}