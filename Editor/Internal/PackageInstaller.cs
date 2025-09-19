#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Tiko.AudioSystem.EditorTools
{
    internal static class PackageInstaller
    {
        // Anchor để tìm root package path
        private sealed class _Anchor : ScriptableObject { }

        private static string GetThisPackageRoot()
        {
            // Lấy đường dẫn asset của file .cs hiện tại → suy ra root
            var ms = MonoScript.FromScriptableObject(ScriptableObject.CreateInstance<_Anchor>());
            var scriptPath = AssetDatabase.GetAssetPath(ms);
            // scriptPath: ".../AudioSystem/Editor/Internal/PackageInstaller.cs"
            var idx = scriptPath.LastIndexOf("/Editor/");
            var root = idx > 0 ? scriptPath.Substring(0, idx) : Path.GetDirectoryName(scriptPath).Replace("\\", "/");
            // root: ".../AudioSystem"
            return root;
        }

        public static void InstallFromInstallerFolder()
        {
            var root = GetThisPackageRoot();
            var installerDir = Path.Combine(root, "Installer").Replace("\\", "/");

            if (!Directory.Exists(installerDir))
            {
                EditorUtility.DisplayDialog("Installer", $"Không tìm thấy thư mục: {installerDir}", "OK");
                return;
            }

            var packages = Directory.GetFiles(installerDir, "*.unitypackage", SearchOption.AllDirectories);
            if (packages.Length == 0)
            {
                EditorUtility.DisplayDialog("Installer", "Không tìm thấy file .unitypackage nào trong Installer.", "OK");
                return;
            }

            foreach (var pkg in packages)
            {
                AssetDatabase.ImportPackage(pkg, true);
            }

            EditorUtility.DisplayDialog("Installer", $"Đã import {packages.Length} unitypackage.", "OK");
        }

    }
}
#endif
