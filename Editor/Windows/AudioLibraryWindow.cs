#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tiko.AudioSystem.Editor
{
    public sealed class AudioLibraryWindow : EditorWindow
    {
        private enum Mode { SFX, BGM }

        [MenuItem("Tiko/Audio/Library", priority = 1)]
        private static void Open() => GetWindow<AudioLibraryWindow>("Audio Library");

        private Mode _mode = Mode.SFX;
        private string _search = string.Empty;
        private Vector2 _scrollLeft;
        private Vector2 _scrollRight;

        // selection caches
        private int _selectedKey = -1;      // selected enum int value
        private int _selectedIndex = -1;    // index in full enum array (not filtered list)

        // current libs/so
        private SfxLibrary _sfxLib;
        private BGMLibrary _bgmLib;
        private SerializedObject _so; // of current lib

        // enum cache for current mode
        private string[] _enumNames = Array.Empty<string>();
        private int[] _enumKeys = Array.Empty<int>();

        // styles
        private GUIStyle _rowStyle;
        private GUIStyle _rowStyleSelected;

        private void OnEnable()
        {
            BuildStyles();
            RefreshLibraries();
            EnsureSync(explicitSort: true);
            BuildEnumCache();
            SelectFirstKey();
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

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var newMode = (Mode)GUILayout.Toolbar((int)_mode, new[] { new GUIContent("SFX"), new GUIContent("BGM") }, EditorStyles.toolbarButton);
                if (newMode != _mode)
                {
                    _mode = newMode;
                    BindCurrentSerializedObject();
                    EnsureSync(explicitSort: true);
                    BuildEnumCache();
                    SelectFirstKey();
                }

                GUILayout.Space(8);
                GUI.SetNextControlName("SearchField");
                _search = GUILayout.TextField(_search, EditorStyles.toolbarTextField, GUILayout.Width(220));

                GUILayout.Space(8);

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    RefreshLibraries();
                    EnsureSync(explicitSort: true);
                    BuildEnumCache();
                    if (!EnsureSelectionValid()) SelectFirstKey();
                }
                if (GUILayout.Button("Sync", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    EnsureSync(explicitSort: true);
                }
            }
        }

        private struct Row { public int key; public string label; public int fullIndex; }

        private void DrawLeftList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(380)))
            {
                var lib = GetCurrentLib();
                if (lib == null)
                {
                    DrawCreateLibBlock();
                    return;
                }

                // build filtered rows
                var rows = new List<Row>(_enumKeys.Length);
                for (int i = 0; i < _enumKeys.Length; i++)
                {
                    var label = $"[{_enumKeys[i]}] {_enumNames[i]}";
                    if (!string.IsNullOrEmpty(_search) && label.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    rows.Add(new Row { key = _enumKeys[i], label = label, fullIndex = i });
                }

                _scrollLeft = EditorGUILayout.BeginScrollView(_scrollLeft);

                foreach (var row in rows)
                {
                    bool isSel = row.key == _selectedKey;
                    var style = isSel ? _rowStyleSelected : _rowStyle;
                    // full-row button for easy hit target
                    if (GUILayout.Button(row.label, style))
                    {
                        SetSelection(row.key, row.fullIndex);
                    }

                    // double-click to preview first clip
                    var e = Event.current;
                    if (isSel && e.type == EventType.MouseDown && e.clickCount == 2 && GUILayoutUtility.GetLastRect().Contains(e.mousePosition))
                    {
                        PreviewFirstClip(row.key);
                        e.Use();
                    }
                }

                if (rows.Count == 0)
                {
                    EditorGUILayout.HelpBox("No enum values match your search.", MessageType.Info);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawRightInspector()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                var lib = GetCurrentLib();
                if (lib == null)
                {
                    EditorGUILayout.HelpBox($"No {(_mode == Mode.SFX ? "SfxLibrary" : "BGMLibrary")} asset found.", MessageType.Info);
                    return;
                }

                if (_so == null) BindCurrentSerializedObject();

                EditorGUILayout.LabelField(_mode == Mode.SFX ? "SFX Clips" : "BGM Clips", EditorStyles.boldLabel);
                EditorGUILayout.Space(2);

                if (_selectedKey < 0)
                {
                    EditorGUILayout.HelpBox("Select a key from the list.", MessageType.Info);
                    return;
                }

                if (!TryGetEntryProperty(_selectedKey, out var entryProp))
                {
                    EditorGUILayout.HelpBox("Entry not found. Click Sync.", MessageType.Warning);
                    if (GUILayout.Button("Sync Now", GUILayout.Width(100))) EnsureSync(explicitSort: true);
                    return;
                }

                _so.Update();
                var nameProp = entryProp.FindPropertyRelative("keyName");
                var clipsProp = entryProp.FindPropertyRelative("clips");

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"[{_selectedKey}] {nameProp.stringValue}", EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("▶ Preview", GUILayout.Width(90))) PreviewFirstClip(_selectedKey);
                        if (GUILayout.Button("■ Stop", GUILayout.Width(70))) EditorAudioPreviewUtility.StopAll();
                    }

                    EditorGUILayout.Space(2);

                    // List of clips
                    if (clipsProp.isArray)
                    {
                        for (int i = 0; i < clipsProp.arraySize; i++)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                var elem = clipsProp.GetArrayElementAtIndex(i);
                                EditorGUILayout.PropertyField(elem, GUIContent.none);
                                if (GUILayout.Button("X", GUILayout.Width(24)))
                                {
                                    clipsProp.DeleteArrayElementAtIndex(i);
                                }
                            }
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("+ Add Clip", GUILayout.Width(110)))
                        {
                            clipsProp.arraySize++;
                        }
                        if (GUILayout.Button("Remove Nulls", GUILayout.Width(110)))
                        {
                            RemoveNullClips(clipsProp);
                        }
                        if (GUILayout.Button("Clear All", GUILayout.Width(90)))
                        {
                            if (EditorUtility.DisplayDialog("Clear Clips", "Remove all assigned clips for this key?", "Clear", "Cancel"))
                                clipsProp.ClearArray();
                        }
                    }
                }

                if (GUI.changed)
                {
                    _so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(lib);
                }
            }
        }

        // ----------------- helpers -----------------
        private EnumLibraryBase GetCurrentLib() => _mode == Mode.SFX ? (EnumLibraryBase)_sfxLib : (EnumLibraryBase)_bgmLib;

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

        private void EnsureSync(bool explicitSort = false)
        {
            var lib = GetCurrentLib();
            if (lib == null) return;
            Undo.RecordObject(lib, "Audio Library Sync");
            lib.AddMissingFromEnum();
            if (explicitSort) lib.SortByEnumOrder();
            EditorUtility.SetDirty(lib);
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
            if (_selectedIndex < 0 || _selectedIndex >= _enumKeys.Length) return false;
            if (_enumKeys.Length == 0) return false;
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

        private void SetSelection(int key, int fullIndex)
        {
            _selectedKey = key;
            _selectedIndex = fullIndex;
            Repaint();
        }

        private void PreviewFirstClip(int key)
        {
            if (!TryGetEntryProperty(key, out var entryProp)) return;
            var clipsProp = entryProp.FindPropertyRelative("clips");
            var clip = GetFirstClip(clipsProp);
            EditorAudioPreviewUtility.Play(clip);
        }

        private void DrawCreateLibBlock()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField(_mode == Mode.SFX ? "SFX Library" : "BGM Library", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("No library asset found. Create one to start assigning clips.", MessageType.Info);
                if (GUILayout.Button("Create", GUILayout.Width(100)))
                {
                    var path = EditorUtility.SaveFilePanelInProject(
                        _mode == Mode.SFX ? "Create SfxLibrary" : "Create BgmLibrary",
                        _mode == Mode.SFX ? "SfxLibrary" : "BgmLibrary",
                        "asset", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (_mode == Mode.SFX)
                        {
                            var asset = ScriptableObject.CreateInstance<SfxLibrary>();
                            AssetDatabase.CreateAsset(asset, path);
                            AssetDatabase.SaveAssets();
                            _sfxLib = asset;
                        }
                        else
                        {
                            var asset = ScriptableObject.CreateInstance<BGMLibrary>();
                            AssetDatabase.CreateAsset(asset, path);
                            AssetDatabase.SaveAssets();
                            _bgmLib = asset;
                        }
                        BindCurrentSerializedObject();
                        EnsureSync(explicitSort: true);
                        BuildEnumCache();
                        SelectFirstKey();
                    }
                }
            }
        }

        private bool TryGetEntryProperty(int key, out SerializedProperty entryProp)
        {
            entryProp = null;
            if (_so == null) return false;
            var entries = _so.FindProperty("entries");
            if (entries == null || !entries.isArray) return false;
            for (int i = 0; i < entries.arraySize; i++)
            {
                var e = entries.GetArrayElementAtIndex(i);
                var k = e.FindPropertyRelative("key");
                if (k != null && k.intValue == key) { entryProp = e; return true; }
            }
            return false;
        }

        private static AudioClip GetFirstClip(SerializedProperty clipsProp)
        {
            if (clipsProp == null || !clipsProp.isArray || clipsProp.arraySize == 0) return null;
            var elem = clipsProp.GetArrayElementAtIndex(0);
            return elem.objectReferenceValue as AudioClip;
        }

        private static void RemoveNullClips(SerializedProperty clipsProp)
        {
            if (clipsProp == null || !clipsProp.isArray) return;
            for (int i = clipsProp.arraySize - 1; i >= 0; i--)
            {
                var elem = clipsProp.GetArrayElementAtIndex(i);
                if (elem.objectReferenceValue == null) clipsProp.DeleteArrayElementAtIndex(i);
            }
        }

        private static T FindAsset<T>() where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length == 0) return null;
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private void BuildStyles()
        {
            _rowStyle = new GUIStyle(EditorStyles.label)
            {
                padding = new RectOffset(8, 8, 4, 4),
                alignment = TextAnchor.MiddleLeft
            };
            _rowStyle.hover = new GUIStyleState { background = MakeTex(1, 1, new Color(0.5f, 0.5f, 0.5f, EditorGUIUtility.isProSkin ? 0.12f : 0.08f)) };

            _rowStyleSelected = new GUIStyle(_rowStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState { textColor = EditorStyles.boldLabel.normal.textColor, background = MakeTex(1, 1, new Color(0.24f, 0.48f, 0.90f, 0.20f)) }
            };
        }

        private static Texture2D MakeTex(int w, int h, Color c)
        {
            var tex = new Texture2D(w, h);
            var cols = new Color[w * h];
            for (int i = 0; i < cols.Length; i++) cols[i] = c;
            tex.SetPixels(cols); tex.Apply();
            return tex;
        }

        private void HandleKeyboard()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;

            // Focus search: Ctrl/Cmd+F
            if ((e.control || e.command) && e.keyCode == KeyCode.F)
            {
                EditorGUI.FocusTextInControl("SearchField");
                e.Use();
                return;
            }

            if (_enumKeys.Length == 0) return;

            if (e.keyCode == KeyCode.UpArrow)
            {
                SetSelectionByIndex(Mathf.Clamp(_selectedIndex - 1, 0, _enumKeys.Length - 1));
                e.Use();
            }
            else if (e.keyCode == KeyCode.DownArrow)
            {
                SetSelectionByIndex(Mathf.Clamp(_selectedIndex + 1, 0, _enumKeys.Length - 1));
                e.Use();
            }
            else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Space)
            {
                if (_selectedKey >= 0) { PreviewFirstClip(_selectedKey); e.Use(); }
            }
        }

        private void FocusClips()
        {
            // No-op placeholder: IMGUI doesn't have per-control focus here,
            // but we keep the method for future inline clip list focus.
        }
    }
}
#endif
