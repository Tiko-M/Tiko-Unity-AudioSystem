#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AudioSystem.EditorTools
{
    internal static class AudioLibrarySync
    {
        public static void SyncLibrary(AudioImportConfig cfg, Dictionary<string, string> locationToKey)
        {
            if (cfg == null) return;

            var lib = cfg.targetLibrary;
            if (lib == null)
            {
                string cfgPath = AssetDatabase.GetAssetPath(cfg);
                string dir = System.IO.Path.GetDirectoryName(cfgPath);
                string targetPath = $"{dir}/AudioLibrary.asset";
                lib = ScriptableObject.CreateInstance<AudioLibrary>();
                AssetDatabase.CreateAsset(lib, targetPath);
                AssetDatabase.SaveAssets();
                cfg.targetLibrary = lib;
                EditorUtility.SetDirty(cfg);
                Debug.Log($"[AudioLibrarySync] Tạo mới AudioLibrary: {targetPath}");
            }

            // Build danh sách MỚI từ scan (Remove Missing = ON)
            var newData = new List<(EAudio key, string token, AudioClip[] clips)>();

            foreach (var kv in locationToKey)
            {
                string location = kv.Key;
                string token = kv.Value;

                if (!System.Enum.TryParse<EAudio>(token, out var key))
                {
                    Debug.LogWarning($"[AudioLibrarySync] Bỏ qua: không parse được enum '{token}'");
                    continue;
                }

                AudioClip[] clips;
                if (AssetDatabase.IsValidFolder(location))
                {
                    var folderNorm = location.Replace("\\", "/");
                    var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { location });
                    clips = guids
                        .Select(g => AssetDatabase.GUIDToAssetPath(g))
                        .Where(p => System.IO.Path.GetDirectoryName(p).Replace("\\", "/") == folderNorm) // chỉ clip trực tiếp
                        .Select(p => AssetDatabase.LoadAssetAtPath<AudioClip>(p))
                        .Where(a => a != null)
                        .ToArray();
                }
                else
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(location);
                    clips = (clip != null) ? new[] { clip } : System.Array.Empty<AudioClip>();
                }

                newData.Add((key, token, clips));
            }

            // Ghi list bằng SerializedObject
            var so = new SerializedObject(lib);
            var listProp = so.FindProperty("audioList");
            if (listProp == null)
            {
                Debug.LogError("[AudioLibrarySync] Không tìm thấy field 'audioList' trong AudioLibrary.");
                return;
            }

            listProp.ClearArray();
            listProp.arraySize = newData.Count;

            for (int i = 0; i < newData.Count; i++)
            {
                var el = listProp.GetArrayElementAtIndex(i);

                var keyProp = el.FindPropertyRelative("key");
                var clipsProp = el.FindPropertyRelative("clips");
                var nameProp = el.FindPropertyRelative("name"); // <-- field bạn mới thêm

                if (keyProp != null)
                    keyProp.intValue = (int)newData[i].key;

                // key
                if (keyProp != null)
                {
                    var names = keyProp.enumDisplayNames;
                    int idx = System.Array.IndexOf(names, newData[i].key.ToString());
                    keyProp.enumValueIndex = (idx >= 0) ? idx : (int)newData[i].key;
                }

                // name (nếu có)
                if (nameProp != null)
                    nameProp.stringValue = newData[i].token;

                // clips
                if (clipsProp != null)
                {
                    var clips = newData[i].clips ?? System.Array.Empty<AudioClip>();
                    clipsProp.arraySize = clips.Length;
                    for (int c = 0; c < clips.Length; c++)
                        clipsProp.GetArrayElementAtIndex(c).objectReferenceValue = clips[c];
                }

                // defaults nhẹ cho field khác (nếu có)
                var volProp = el.FindPropertyRelative("volume");
                var minIntProp = el.FindPropertyRelative("minInterval");
                var pitchRangeProp = el.FindPropertyRelative("pitchRange");
                var maxInstProp = el.FindPropertyRelative("maxInstances");
                var priorityProp = el.FindPropertyRelative("priority");

                if (volProp != null && Mathf.Approximately(volProp.floatValue, 0f)) volProp.floatValue = 1f;
                if (minIntProp != null && minIntProp.floatValue < 0f) minIntProp.floatValue = 0f;
                if (pitchRangeProp != null)
                {
                    var x = pitchRangeProp.FindPropertyRelative("x");
                    var y = pitchRangeProp.FindPropertyRelative("y");
                    if (x != null && y != null && Mathf.Approximately(x.floatValue, 0f) && Mathf.Approximately(y.floatValue, 0f))
                    { x.floatValue = 1f; y.floatValue = 1f; }
                }
                if (maxInstProp != null && maxInstProp.intValue < 0) maxInstProp.intValue = 0;
                if (priorityProp != null && priorityProp.intValue == 0) priorityProp.intValue = 128;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();

            int totalClips = newData.Sum(d => d.clips?.Length ?? 0);
            Debug.Log($"[AudioLibrarySync] Đồng bộ xong. Entry: {newData.Count}, Tổng clips gán: {totalClips}");
        }
    }
}
#endif
