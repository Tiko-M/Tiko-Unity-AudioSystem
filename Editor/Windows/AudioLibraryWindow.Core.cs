#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tiko.AudioSystem.EditorTools
{
    public sealed partial class AudioLibraryWindow : EditorWindow
    {
        private enum Mode { SFX, BGM }

        [MenuItem("Tiko/Audio/Library", priority = 1)]
        private static void Open() => GetWindow<AudioLibraryWindow>("Audio Library");

        // ====== Shared State (dùng chung giữa các partial) ======
        private Mode _mode = Mode.SFX;

        // scrolls
        private Vector2 _scrollLeft;
        private Vector2 _scrollRight;

        // selection caches
        private int _selectedKey = -1;   // enum int value hiện chọn
        private int _selectedIndex = -1; // index trong mảng enum (không filter)

        // current libs/so
        private SfxLibrary _sfxLib;
        private BGMLibrary _bgmLib;
        private SerializedObject _so; // SerializedObject của lib đang active

        // enum cache cho mode hiện tại
        private string[] _enumNames = Array.Empty<string>();
        private int[] _enumKeys = Array.Empty<int>();

        // styles
        private GUIStyle _rowStyle;
        private GUIStyle _rowStyleSelected;

        // Enum working list & file
        private bool _enumFoldout = true;
        private List<EnumCodegenUtility.EnumItem> _workItems = new();
        private string _enumFilePath;
        private const string EnumNamespace = "Tiko.AudioSystem";
        private const string ESfx = "ESFX";
        private const string EBgm = "EBGM";

        // Search đặt TRONG tab Enum

        private void OnEnable()
        {
            BuildStyles();
            RefreshLibraries();
            BuildEnumCache();
            SelectFirstKey();
            EnsureLibrariesExist();
        }

        private void OnGUI()
        {
            HandleKeyboard();
            DrawToolbar();



            using (new EditorGUILayout.HorizontalScope())
            {
                DrawLeftList();
                GUILayout.Space(8);
                DrawRightInspector();
            }
        }

        // ====== Helpers dùng chung ======
        private EnumLibraryBase GetCurrentLib() =>
            _mode == Mode.SFX ? (EnumLibraryBase)_sfxLib : (EnumLibraryBase)_bgmLib;

        private void RefreshLibraries()
        {
            _sfxLib = FindAsset<SfxLibrary>();
            _bgmLib = FindAsset<BGMLibrary>();
            BindCurrentSerializedObject();
        }

        private void BindCurrentSerializedObject()
        {
            var lib = GetCurrentLib();
            _so = lib != null ? new SerializedObject(lib) : null;
        }

        private void BuildEnumCache()
        {
            var lib = GetCurrentLib();
            if (lib == null) { _enumNames = Array.Empty<string>(); _enumKeys = Array.Empty<int>(); return; }
            var t = lib.ResolveEnumType();
            if (t == null) { _enumNames = Array.Empty<string>(); _enumKeys = Array.Empty<int>(); return; }

            var names = Enum.GetNames(t);
            var vals = (Array)Enum.GetValues(t);
            _enumNames = names;
            _enumKeys = new int[vals.Length];
            for (int i = 0; i < vals.Length; i++) _enumKeys[i] = Convert.ToInt32(vals.GetValue(i));
        }

        private void SelectFirstKey()
        {
            if (_enumKeys.Length == 0) { _selectedKey = -1; _selectedIndex = -1; return; }
            SetSelectionByIndex(0);
        }

        private bool EnsureSelectionValid()
        {
            if (_enumKeys.Length == 0) return false;
            if (_selectedIndex < 0 || _selectedIndex >= _enumKeys.Length) return false;
            _selectedKey = _enumKeys[_selectedIndex];
            return true;
        }

        private void SetSelectionByIndex(int index)
        {
            index = Mathf.Clamp(index, 0, Math.Max(0, _enumKeys.Length - 1));
            if (_enumKeys.Length == 0) { _selectedIndex = -1; _selectedKey = -1; return; }
            _selectedIndex = index;
            _selectedKey = _enumKeys[index];
            Repaint();
        }

        private static T FindAsset<T>() where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length == 0) return null;
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private void EnsureLibrariesExist()
        {
            var folder = AudioEditorSettings.LibraryPath;
            AssetPathUtil.EnsureFolder(folder);

            // SFX
            _sfxLib = AssetDatabase.LoadAssetAtPath<SfxLibrary>($"{folder}/SFXLibrary.asset");
            if (!_sfxLib) _sfxLib = AssetPathUtil.CreateScriptableIfMissing<SfxLibrary>(folder, "SFXLibrary.asset");

            // BGM
            _bgmLib = AssetDatabase.LoadAssetAtPath<BGMLibrary>($"{folder}/BGMLibrary.asset");
            if (!_bgmLib) _bgmLib = AssetPathUtil.CreateScriptableIfMissing<BGMLibrary>(folder, "BGMLibrary.asset");
        }
    }
}
#endif
