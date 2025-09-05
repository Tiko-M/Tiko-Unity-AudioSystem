using System.Collections.Generic;
using UnityEngine;

namespace Tiko.AudioSystem
{
    [CreateAssetMenu(menuName = "Audio/Audio Library", fileName = "AudioLibrary")]
    public class AudioLibrary : ScriptableObject
    {
        [SerializeField] private List<AudioData> audioList = new List<AudioData>();
        private Dictionary<string, AudioData> _map;

        private void OnEnable()
        {
            BuildMap();
        }

        [ContextMenu("Rebuild Map")]
        public void BuildMap()
        {
            _map = new Dictionary<string, AudioData>(audioList.Count);
            foreach (var data in audioList)
            {
                if (data == null) continue;
                if (_map.ContainsKey(data.key))
                {
                    Debug.LogWarning($"[AudioLibrary] Trùng key {data.key}, sẽ override entry trước đó.");
                }
                _map[data.key] = data;
            }
        }

        public AudioData GetAudioData(string key)
        {
            if (_map == null || _map.Count == 0) BuildMap();
            return _map != null && _map.TryGetValue(key, out var data) ? data : null;
        }

        [ContextMenu("Validate")]
        public void Validate()
        {
            var seen = new HashSet<string>();
            foreach (var d in audioList)
            {
                if (d == null) continue;
                if (!seen.Add(d.key)) Debug.LogWarning($"[AudioLibrary] Trùng key: {d.key}");
                if (d.clips == null || d.clips.Length == 0) Debug.LogWarning($"[AudioLibrary] Clip rỗng ở key: {d.key}");
                if (d.minInterval < 0) Debug.LogWarning($"[AudioLibrary] minInterval < 0 ở key: {d.key}");
            }
        }
    }
}