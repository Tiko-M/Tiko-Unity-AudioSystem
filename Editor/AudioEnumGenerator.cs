#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Tiko.AudioSystem.EditorTools
{
    internal static class AudioEnumGenerator
    {
        private const string START = "// <AUTOGEN: DO NOT EDIT>";
        private const string END = "// </AUTOGEN: DO NOT EDIT>";

        /// <summary>
        /// Sinh/ghi enum vào file path chỉ định (tạo nếu chưa có), chèn keys vào vùng AUTOGEN.
        /// </summary>
        public static void GenerateEnum_PathOnly(string enumFilePath, IReadOnlyList<string> keys)
        {
            if (string.IsNullOrEmpty(enumFilePath))
            {
                Debug.LogError("[AudioEnumGenerator] enumFilePath rỗng");
                return;
            }

            // Đảm bảo thư mục tồn tại
            var dir = Path.GetDirectoryName(enumFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string content;
            if (File.Exists(enumFilePath))
            {
                content = File.ReadAllText(enumFilePath, Encoding.UTF8);
            }
            else
            {
                // Mặc định không namespace để dễ dùng ở Assets
                content =
@"public enum EAudio
{
    None = 0,
    // <AUTOGEN: DO NOT EDIT>
    // </AUTOGEN: DO NOT EDIT>
}
";
            }

            int iStart = content.IndexOf(START, StringComparison.Ordinal);
            int iEnd = content.IndexOf(END, StringComparison.Ordinal);
            if (iStart < 0 || iEnd < 0 || iEnd <= iStart)
            {
                Debug.LogError("[AudioEnumGenerator] Không tìm thấy vùng AUTOGEN trong EAudio.cs");
                return;
            }

            var sb = new StringBuilder();
            if (keys != null)
            {
                foreach (var k in keys)
                {
                    if (string.IsNullOrEmpty(k)) continue;
                    sb.AppendLine($"    {k},");
                }
            }

            string newContent = content.Substring(0, iStart + START.Length) + "\n" +
                                sb.ToString() + "\n" +
                                content.Substring(iEnd);

            File.WriteAllText(enumFilePath, newContent, Encoding.UTF8);
            AssetDatabase.ImportAsset(enumFilePath);
            Debug.Log($"[AudioEnumGenerator] Đã cập nhật {enumFilePath} ({(keys?.Count ?? 0)} keys).");
        }
    }
}
#endif
