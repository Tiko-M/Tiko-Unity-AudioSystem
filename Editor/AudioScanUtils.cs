#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tiko.AudioSystem.EditorTools
{
    internal static class AudioScanUtils
    {
        public static List<string> ScanCueFolders(AudioImportConfig cfg, out Dictionary<string, string> locationToKey)
        {
            locationToKey = new Dictionary<string, string>();
            var keys = new List<string>();
            if (cfg == null || string.IsNullOrEmpty(cfg.cuesRoot)) return keys;

            string root = cfg.cuesRoot.Replace("\\", "/");
            if (!AssetDatabase.IsValidFolder(root))
            {
                Debug.LogWarning($"[AudioScan] Không tìm thấy thư mục: {root}");
                return keys;
            }

            // Quét tất cả subfolder (đã lọc exclude) – KHÔNG coi root là cue
            var allFolders = GetAllFoldersRecursive(root, cfg.excludeFolders);

            var usedTokens = new HashSet<string>();
            var consumedClipPaths = new HashSet<string>(); // clip đã “thuộc” 1 cue theo folder

            // 1) MỌI subfolder -> 1 cue = tên folder (kể cả rỗng/1 clip)
            foreach (var folder in allFolders)
            {
                var folderNorm = folder.Replace("\\", "/");
                if (folderNorm == root) continue; // đừng coi root là cue
                if (IsInExcludedFolder(folderNorm, cfg.excludeFolders)) continue;

                var folderName = Path.GetFileName(folderNorm);
                string token = MakeUniqueToken(SanitizeToEnumToken(folderName), usedTokens);
                locationToKey[folderNorm] = token;
                keys.Add(token);

                // Đánh dấu các clip TRỰC TIẾP trong folder này đã được “tiêu thụ”
                var directClips = GetImmediateClipPaths(folderNorm);
                foreach (var p in directClips) consumedClipPaths.Add(p);
            }

            // 2) File rời còn lại (thường nằm trực tiếp trong root) -> 1 cue = tên file
            var allClipGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { root });
            foreach (var g in allClipGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g).Replace("\\", "/");
                if (consumedClipPaths.Contains(path)) continue;
                if (IsInExcludedFolder(path, cfg.excludeFolders)) continue;

                var name = Path.GetFileNameWithoutExtension(path);
                string token = MakeUniqueToken(SanitizeToEnumToken(name), usedTokens);
                locationToKey[path] = token;
                keys.Add(token);
            }

            keys.Sort(System.StringComparer.Ordinal);
            return keys;
        }

        // ===== Helpers =====

        // Lấy các clip TRỰC TIẾP trong folder (không lấy của subfolder)
        private static List<string> GetImmediateClipPaths(string folder)
        {
            var list = new List<string>();
            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
            var folderNorm = folder.Replace("\\", "/");
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g).Replace("\\", "/");
                if (Path.GetDirectoryName(p).Replace("\\", "/") == folderNorm)
                    list.Add(p);
            }
            return list;
        }

        // Duyệt đệ quy folder (lọc exclude ngay từ đầu)
        private static List<string> GetAllFoldersRecursive(string root, List<string> excludes)
        {
            var result = new List<string>();
            var q = new Queue<string>();
            q.Enqueue(root);
            while (q.Count > 0)
            {
                var f = q.Dequeue();
                result.Add(f);
                foreach (var c in AssetDatabase.GetSubFolders(f))
                {
                    var baseName = Path.GetFileName(c);
                    if (excludes != null && excludes.Any(ex => string.Equals(ex, baseName, System.StringComparison.OrdinalIgnoreCase)))
                        continue;
                    q.Enqueue(c);
                }
            }
            return result;
        }

        // Kiểm tra đường dẫn có chứa tên folder trong exclude không (case-insensitive)
        private static bool IsInExcludedFolder(string path, List<string> excludes)
        {
            if (excludes == null || excludes.Count == 0) return false;
            var segs = path.Replace("\\", "/").Split('/');
            foreach (var ex in excludes)
            {
                var name = (ex ?? "").Trim();
                if (string.IsNullOrEmpty(name)) continue;
                for (int i = 0; i < segs.Length; i++)
                    if (string.Equals(segs[i], name, System.StringComparison.OrdinalIgnoreCase))
                        return true;
            }
            return false;
        }

        private static string MakeUniqueToken(string baseToken, HashSet<string> used)
        {
            string token = baseToken;
            int i = 2;
            while (used.Contains(token))
            {
                token = baseToken + "_" + i;
                i++;
            }
            used.Add(token);
            return token;
        }

        public static string SanitizeToEnumToken(string name)
        {
            if (string.IsNullOrEmpty(name)) return "_Undefined";
            // a-zA-Z0-9 giữ nguyên; còn lại -> '_'
            var chars = name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
            var token = new string(chars);

            // Không thêm prefix; nếu bắt đầu bằng số → thêm '_' cho hợp lệ C#
            if (char.IsDigit(token[0])) token = "_" + token;

            // Gộp '__' thành '_' cho sạch
            while (token.Contains("__")) token = token.Replace("__", "_");
            return token;
        }
    }
}
#endif
