#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace Tiko.AudioSystem.EditorTools
{
    public static class AudioSystemTemplateImporter
    {
        [MenuItem("Tools/Audio/Install Template to Assets")]
        public static void Install()
        {
            // Tìm root của package chứa script này (UPM)
            var asm = typeof(AudioSystemTemplateImporter).Assembly;
            var pkg = UnityEditor.PackageManager.PackageInfo.FindForAssembly(asm);
            if (pkg == null || string.IsNullOrEmpty(pkg.resolvedPath))
            {
                EditorUtility.DisplayDialog("Audio System", "Không tìm thấy root package (không chạy UPM?).", "OK");
                return;
            }

            // Đường dẫn tới unitypackage nhúng trong package
            var pkgFile = Path.Combine(pkg.resolvedPath, "Installer/AudioSystemTemplate.unitypackage");
            if (!File.Exists(pkgFile))
            {
                EditorUtility.DisplayDialog("Audio System", "Không tìm thấy AudioSystemTemplate.unitypackage trong package.", "OK");
                return;
            }

            // Import yên lặng (interactive=false). Asset sẽ được thả vào đúng "Assets/Tiko/AudioSystem/..."
            AssetDatabase.ImportPackage(pkgFile, /*interactive:*/ false);
            AssetDatabase.Refresh();

            // (Optional) set lại config nếu muốn:
            var cfg = AssetDatabase.LoadAssetAtPath<AudioSystem.AudioImportConfig>("Assets/AudioImportConfig.asset");
            var lib = AssetDatabase.LoadAssetAtPath<AudioSystem.AudioLibrary>("Assets/AudioLibrary.asset");
            var enumPath = "Assets/Tiko/AudioSystem/EAudio.cs";
            if (cfg != null)
            {
                cfg.enumFilePath = enumPath;   // enum ở Assets → ghi được
                cfg.targetLibrary = cfg.targetLibrary ? cfg.targetLibrary : lib;
                cfg.cuesRoot = "";             // ép user kéo folder audio cho chắc
                EditorUtility.SetDirty(cfg);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[AudioSystem] Imported template from unitypackage → Assets/Tiko/AudioSystem.");
        }
    }
#endif

}
