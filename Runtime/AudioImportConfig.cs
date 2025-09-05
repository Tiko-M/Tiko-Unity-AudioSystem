using System.Collections.Generic;
using UnityEngine;

namespace AudioSystem
{
    [CreateAssetMenu(menuName = "Audio/Audio Import Config", fileName = "AudioImportConfig")]
    public class AudioImportConfig : ScriptableObject
    {
        [Header("Nguồn dữ liệu")]
        public string cuesRoot = "Assets/_ProjectAssets/6.Audio";

        [Header("Sinh mã")]
        public string enumFilePath = "Assets/_ProjectAssets/1.Core/AudioSystem/Scripts/EAudio.cs";

        [Header("Thư viện")]
        public AudioLibrary targetLibrary;

        [Header("Bộ lọc")]
        public List<string> excludeFolders = new List<string> { "_bak", "_ignore" };
    }
}