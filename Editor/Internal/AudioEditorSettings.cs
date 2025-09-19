#if UNITY_EDITOR
using UnityEditor;

namespace Tiko.AudioSystem.EditorTools
{
    internal static class AudioEditorSettings
    {
        private const string KeyEnumPath = "Tiko.AudioSystem.EnumPath";
        private const string KeyLibraryPath = "Tiko.AudioSystem.LibraryPath";

        private const string DefaultEnumPath = "Assets/Tiko/Tiko.AudioSystem/Scripts/Enum";
        private const string DefaultLibraryPath = "Assets/Tiko/Tiko.AudioSystem/Libraries";

        public static string EnumPath
        {
            get => EditorPrefs.GetString(KeyEnumPath, DefaultEnumPath);
            set => EditorPrefs.SetString(KeyEnumPath, value);
        }

        public static string LibraryPath
        {
            get => EditorPrefs.GetString(KeyLibraryPath, DefaultLibraryPath);
            set => EditorPrefs.SetString(KeyLibraryPath, value);
        }
    }
}
#endif