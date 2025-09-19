#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tiko.AudioSystem.EditorTools
{
    public sealed partial class AudioLibraryWindow
    {
        private string _enumSearch = string.Empty;

        private struct Row { public int key; public string label; public int fullIndex; }

        private void DrawLeftList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(380)))
            {
                DrawEnumEditorInline();
            }
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
                        BuildEnumCache();
                        SelectFirstKey();
                    }
                }
            }
        }

        private void DrawEnumEditorInline()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(_mode == Mode.SFX ? "Enum (ESFX)" : "Enum (EBGM)", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("+", GUILayout.Width(28)))
                    {
                        int next = _workItems.Count;
                        _workItems.Add(new EnumCodegenUtility.EnumItem { name = "NewKey", value = next });
                    }
                    if (GUILayout.Button("Apply Changes", GUILayout.Width(120)))
                    {
                        var enumName = _mode == Mode.SFX ? "ESFX" : "EBGM";
                        ApplyEnumChanges_Inline(enumName);
                    }
                }

                EditorGUILayout.LabelField("Keys", EditorStyles.boldLabel);
                _enumSearch = EditorGUILayout.TextField(_enumSearch);

                using (var sv = new EditorGUILayout.ScrollViewScope(_scrollLeft, GUILayout.MaxHeight(220)))
                {
                    _scrollLeft = sv.scrollPosition;

                    for (int i = 0; i < _workItems.Count; i++)
                    {
                        var it = _workItems[i];
                        bool pass = string.IsNullOrEmpty(_enumSearch) || it.name.IndexOf(_enumSearch, StringComparison.OrdinalIgnoreCase) >= 0;
                        if (!pass) continue;

                        bool isSel = (_selectedKey == it.value);
                        var style = isSel ? _rowStyleSelected : _rowStyle;

                        using (new EditorGUILayout.HorizontalScope(style))
                        {
                            GUILayout.Label($"[{i}]", GUILayout.Width(28));
                            string newName = EditorGUILayout.TextField(it.name);

                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Delete", GUILayout.Width(60)))
                            {
                                _workItems.RemoveAt(i);
                                ReindexWorkItems_Inline();
                                if (_selectedIndex >= _workItems.Count) SetSelectionByIndex(Mathf.Max(0, _workItems.Count - 1));
                                i--;
                                continue;
                            }

                            if (newName != it.name)
                            {
                                _workItems[i] = new EnumCodegenUtility.EnumItem { name = newName, value = it.value };
                                var keyVal = it.value;
                                int idx = Array.IndexOf(_enumKeys, keyVal);
                                if (idx >= 0) SetSelection(keyVal, idx);
                            }
                        }
                    }
                }
            }
        }

        private void ReindexWorkItems_Inline()
        {
            for (int i = 0; i < _workItems.Count; i++)
                _workItems[i] = new EnumCodegenUtility.EnumItem { name = _workItems[i].name, value = i };
        }

        private void ApplyEnumChanges_Inline(string enumName)
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < _workItems.Count; i++)
            {
                var nm = _workItems[i].name;
                if (!EnumCodegenUtility.IsValidIdentifier(nm))
                {
                    EditorUtility.DisplayDialog("Invalid name", $"'{nm}' is not a valid C# identifier.", "OK");
                    return;
                }
                if (!seen.Add(nm))
                {
                    EditorUtility.DisplayDialog("Duplicate name", $"'{nm}' appears multiple times.", "OK");
                    return;
                }
            }

            ReindexWorkItems_Inline();

            if (string.IsNullOrEmpty(_enumFilePath))
            {
                string p = EditorUtility.SaveFilePanelInProject($"Create {enumName}.cs", enumName, "cs", "");
                if (string.IsNullOrEmpty(p)) return;
                var code = EnumCodegenUtility.GenerateEnumCs("Tiko.AudioSystem", enumName, _workItems);
                if (!EnumCodegenUtility.TryWriteEnumFile(p, code)) return;
                _enumFilePath = p;
            }
            else
            {
                var code = EnumCodegenUtility.GenerateEnumCs("Tiko.AudioSystem", enumName, _workItems);
                if (!EnumCodegenUtility.TryWriteEnumFile(_enumFilePath, code)) return;
            }

            AssetDatabase.Refresh();

            var lib = GetCurrentLib();
            if (lib != null)
            {
                Undo.RecordObject(lib, "Audio Library Update From Enum");
                lib.AddMissingFromEnum();
                lib.SortByEnumOrder();
                EditorUtility.SetDirty(lib);
                BindCurrentSerializedObject();
            }

            BuildEnumCache();
            if (!EnsureSelectionValid()) SelectFirstKey();
        }
    }
}
#endif
