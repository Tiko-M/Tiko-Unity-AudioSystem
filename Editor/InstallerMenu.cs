#if UNITY_EDITOR
using UnityEditor;

namespace Tiko.AudioSystem.EditorTools
{
    internal static class InstallerMenu
    {
        [MenuItem("Tiko/InstallTemplate", priority = 2)]
        private static void InstallTemplate()
        {
            PackageInstaller.InstallFromInstallerFolder();
        }
    }
}
#endif
