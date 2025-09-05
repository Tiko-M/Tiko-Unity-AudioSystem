#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Tiko.AudioSystem.EditorTools
{
    internal static class AudioEnumGenerator
    {
        private const string START = "// <AUTOGEN: DO NOT EDIT>";
        private const string END = "// </AUTOGEN: DO NOT EDIT>";

        public static void GenerateEnum(AudioImportConfig cfg, IReadOnlyList<string> keys)
        {
            if (cfg == null) { Debug.LogError("[AudioEnumGenerator] Config null"); return; }
            string path = cfg.enumFilePath;
            if (string.IsNullOrEmpty(path)) { Debug.LogError("[AudioEnumGenerator] enumFilePath rỗng"); return; }

            string content;
            if (File.Exists(path))
                content = File.ReadAllText(path, Encoding.UTF8);
            else
            {
                content = "namespace AudioSystem\n{\n" +
                          "    public enum EAudio\n" +
                          "    {\n" +
                          "        None = 0,\n" +
                          $"        {START}\n" +
                          $"        {END}\n" +
                          "    }\n" +
                          "}\n";
                var dir = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            }

            int iStart = content.IndexOf(START, StringComparison.Ordinal);
            int iEnd = content.IndexOf(END, StringComparison.Ordinal);
            if (iStart < 0 || iEnd < 0 || iEnd <= iStart)
            {
                Debug.LogError("[AudioEnumGenerator] Không tìm thấy vùng AUTOGEN trong EAudio.cs");
                return;
            }

            var sb = new StringBuilder();
            foreach (var k in keys)
                sb.AppendLine($"        {k},");

            string newContent = content.Substring(0, iStart + START.Length) + "\n" +
                                sb.ToString() + "\n" +
                                content.Substring(iEnd);

            File.WriteAllText(path, newContent, Encoding.UTF8);
            AssetDatabase.ImportAsset(path);
            Debug.Log($"[AudioEnumGenerator] Đã cập nhật {path} ({keys.Count} keys).");
        }
    }
}
#endif