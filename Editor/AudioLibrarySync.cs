#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tiko.AudioSystem.EditorTools
{
    internal static class AudioLibrarySync
    {
        /// <summary>
        /// Đồng bộ AudioLibrary từ map token -> locations (folder hoặc file).
        /// - Với location là folder: chỉ lấy AudioClip nằm TRỰC TIẾP trong folder đó (không lấy của subfolder).
        /// - Với location là file: lấy đúng clip đó.
        /// Ghi đè toàn bộ danh sách (remove-missing = ON).
        /// </summary>
        public static void SyncLibrary_LibAndMap(AudioLibrary lib, Dictionary<string, List<string>> tokenToLocations)
        {
            if (lib == null) { Debug.LogError("[AudioLibrarySync] Library null"); return; }
            if (tokenToLocations == null) tokenToLocations = new Dictionary<string, List<string>>();

            // Build danh sách MỚI từ scan
            var newData = new List<(string token, AudioClip[] clips)>();

            foreach (var kv in tokenToLocations)
            {
                string token = kv.Key;
                var locations = kv.Value ?? new List<string>();
                var bag = new List<AudioClip>();

                foreach (var loc in locations)
                {
                    if (AssetDatabase.IsValidFolder(loc))
                    {
                        var folderNorm = loc.Replace("\\", "/");
                        var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { loc });
                        var direct = guids
                            .Select(g => AssetDatabase.GUIDToAssetPath(g))
                            .Where(p => System.IO.Path.GetDirectoryName(p).Replace("\\", "/") == folderNorm) // chỉ clip trực tiếp
                            .Select(p => AssetDatabase.LoadAssetAtPath<AudioClip>(p))
                            .Where(a => a != null);
                        bag.AddRange(direct);
                    }
                    else
                    {
                        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(loc);
                        if (clip != null) bag.Add(clip);
                    }
                }

                newData.Add((token, bag.Distinct().ToArray()));
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

                var keyProp = el.FindPropertyRelative("key");   // string
                var nameProp = el.FindPropertyRelative("name");  // optional (nếu có)
                var clipsProp = el.FindPropertyRelative("clips"); // AudioClip[]

                // key/name
                if (keyProp != null) keyProp.stringValue = newData[i].token;
                if (nameProp != null) nameProp.stringValue = newData[i].token;

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
                    if (x != null && y != null &&
                        Mathf.Approximately(x.floatValue, 0f) && Mathf.Approximately(y.floatValue, 0f))
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
