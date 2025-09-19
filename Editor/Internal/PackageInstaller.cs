#if UNITY_EDITOR
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEditor.PackageManager.Requests;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Tiko.AudioSystem.EditorTools
{
    internal static class PackageInstaller
    {
        // Anchor để tìm root package path
        private sealed class _Anchor : ScriptableObject { }

        private static string GetThisPackageRoot()
        {
            // Lấy asset path của chính file script này
            var anchor = ScriptableObject.CreateInstance<_Anchor>();
            var ms = MonoScript.FromScriptableObject(anchor);
            var scriptAssetPath = AssetDatabase.GetAssetPath(ms);   // ví dụ: "Packages/com.tiko.audiosystem/Editor/Internal/PackageInstaller.cs"
            ScriptableObject.DestroyImmediate(anchor);

            // Dò package info từ asset path → có resolvedPath tuyệt đối trên ổ đĩa
            var pi = PackageInfo.FindForAssetPath(scriptAssetPath);
            if (pi != null && !string.IsNullOrEmpty(pi.resolvedPath))
                return pi.resolvedPath.Replace("\\", "/");

            // Fallback (hiếm khi cần)
            var idx = scriptAssetPath.LastIndexOf("/Editor/");
            var rootRel = idx > 0 ? scriptAssetPath.Substring(0, idx) : Path.GetDirectoryName(scriptAssetPath).Replace("\\", "/");
            // convert relative ("Packages/...") → absolute
            var projectRoot = Directory.GetParent(Application.dataPath).FullName.Replace("\\", "/");
            return Path.GetFullPath(Path.Combine(projectRoot, rootRel)).Replace("\\", "/");
        }


        public static void InstallFromInstallerFolder()
        {
            var root = GetThisPackageRoot(); // absolute path tới gói
            var installerDir = Path.Combine(root, "Installer").Replace("\\", "/");

            if (!Directory.Exists(installerDir))
            {
                EditorUtility.DisplayDialog("Installer", $"Không tìm thấy thư mục: {installerDir}", "OK");
                return;
            }

            // Tìm tất cả *.unitypackage trong Installer (đệ quy)
            var unitypackages = Directory.GetFiles(installerDir, "*.unitypackage", SearchOption.AllDirectories);
            if (unitypackages == null || unitypackages.Length == 0)
            {
                EditorUtility.DisplayDialog("Installer", "Không tìm thấy file .unitypackage nào trong Installer.", "OK");
                return;
            }

            // Tuỳ chọn: cho người dùng chọn gói nào muốn import (đơn giản hoá: import hết)
            int imported = 0;
            foreach (var pkgAbsPath in unitypackages)
            {
                // true = hiện dialog “Import Unity Package” cho người dùng duyệt; false = im lặng
                AssetDatabase.ImportPackage(pkgAbsPath, /*interactive*/ true);
                imported++;
            }

            EditorUtility.DisplayDialog("Installer", $"Đã import {imported} unitypackage từ:\n{installerDir}", "OK");
        }


    }
}
#endif
