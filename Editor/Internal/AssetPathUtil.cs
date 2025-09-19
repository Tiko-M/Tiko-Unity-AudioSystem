#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tiko.AudioSystem.EditorTools
{
    internal static class AssetPathUtil
    {
        public static void EnsureFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath)) return;
            var parts = assetFolderPath.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        public static T CreateScriptableIfMissing<T>(string folder, string filename) where T : ScriptableObject
        {
            EnsureFolder(folder);
            string path = $"{folder}/{filename}";
            var obj = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!obj)
            {
                obj = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(obj, path);
            }
            return obj;
        }
    }
}
#endif