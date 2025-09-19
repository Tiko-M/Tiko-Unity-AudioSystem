// ============================================================================
// File: Editor/Internal/EnumCodegenUtility.cs
// Namespace: Tiko.AudioSystem.Editor
// Purpose: Helper for AudioLibraryWindow – find/read/write ESFX/EBGM enum files.
// Note: Editor-only. Parser targets simple one-line entries: NAME or NAME = int.
// ============================================================================
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Tiko.AudioSystem.EditorTools
{
    internal static class EnumCodegenUtility
    {
        internal struct EnumItem { public string name; public int value; }

        // Try locate enum file by script name (e.g., ESFX.cs / EBGM.cs)
        public static bool TryFindEnumFile(string enumName, out string path)
        {
            path = null;
            var hint = enumName + ".cs";

            // NEW: ưu tiên thư mục mặc định
            var preferred = System.IO.Path.Combine(AudioEditorSettings.EnumPath, hint).Replace("\\", "/");
            if (System.IO.File.Exists(preferred))
            {
                path = preferred;
                return true;
            }

            // Priority: MonoScript with exact filename
            foreach (var guid in AssetDatabase.FindAssets($"t:MonoScript {enumName}"))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.EndsWith(hint, StringComparison.OrdinalIgnoreCase)) { path = p; return true; }
            }
            // Fallback: any text asset with the filename
            foreach (var guid in AssetDatabase.FindAssets($"t:TextAsset {enumName}"))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.EndsWith(hint, StringComparison.OrdinalIgnoreCase)) { path = p; return true; }
            }
            return false;
        }

        // Parse target enum body into items (robust to comments & missing '=')
        public static bool TryReadEnum(string enumCsText, string enumName, out List<EnumItem> items)
        {
            items = new List<EnumItem>();
            if (string.IsNullOrEmpty(enumCsText)) return false;

            // locate `enum <Name> { ... }` ignoring attributes/modifiers
            var header = new Regex($@"\benum\s+{Regex.Escape(enumName)}\s*\", RegexOptions.Multiline);
            var m = header.Match(enumCsText);
            if (!m.Success) return false;

            int start = m.Index + m.Length;
            int depth = 1;
            int i = start;
            while (i < enumCsText.Length && depth > 0)
            {
                char c = enumCsText[i++];
                if (c == '{') depth++;
                else if (c == '}') depth--;
            }
            int end = i - 1;
            if (depth != 0 || end <= start) return false;

            string body = enumCsText.Substring(start, end - start);
            // strip block & line comments
            body = Regex.Replace(body, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            body = Regex.Replace(body, @"//.*?$", string.Empty, RegexOptions.Multiline);

            // split by commas at top level
            foreach (var raw in body.Split(','))
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                var m2 = Regex.Match(line, @"^([A-Za-z_][A-Za-z0-9_]*)\s*(=\s*(-?\d+))?\s*$");
                if (!m2.Success) continue;
                var name = m2.Groups[1].Value;
                if (!IsValidIdentifier(name)) continue;

                int value;
                if (m2.Groups[3].Success && int.TryParse(m2.Groups[3].Value, out value))
                {
                    items.Add(new EnumItem { name = name, value = value });
                }
                else
                {
                    int last = (items.Count > 0) ? items[items.Count - 1].value : -1;
                    items.Add(new EnumItem { name = name, value = last + 1 });
                }
            }
            return true;
        }

        public static bool IsValidIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (!(char.IsLetter(name[0]) || name[0] == '_')) return false;
            for (int i = 1; i < name.Length; i++)
            {
                char c = name[i];
                if (!(char.IsLetterOrDigit(c) || c == '_')) return false;
            }
            return true;
        }

        public static string WriteEnum(string enumName, List<EnumItem> items)
        {
            var dir = AudioEditorSettings.EnumPath;
            AssetPathUtil.EnsureFolder(dir);
            var path = System.IO.Path.Combine(dir, enumName + ".cs").Replace("\\", "/");

            var sb = new StringBuilder();
            sb.AppendLine("namespace Tiko.AudioSystem");
            sb.AppendLine("{");
            sb.AppendLine($"    public enum {enumName}");
            sb.AppendLine("    {");
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                sb.AppendLine($"        {it.name} = {it.value},");
            }
            sb.AppendLine("    }");
            sb.AppendLine("}");

            System.IO.File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            AssetDatabase.ImportAsset(path);
            return path;
        }

    }
}
#endif
