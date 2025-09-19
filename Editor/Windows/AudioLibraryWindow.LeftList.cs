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

        private void DrawLeftList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(420)))
            {
                DrawEnumListUnified();
            }
        }

        private void DrawEnumListUnified()
        {
            EnsureWorkItemsLoaded();

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(_mode == Mode.SFX ? "Enum (ESFX)" : "Enum (EBGM)", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("+", GUILayout.Width(28)))
                    {
                        int nextVal = _workItems.Count > 0 ? _workItems[_workItems.Count - 1].value + 1 : 0;
                        _workItems.Add(new EnumCodegenUtility.EnumItem { name = "NewKey", value = nextVal });
                        _selectedIndex = _workItems.Count - 1;
                        _selectedKey = _workItems[_selectedIndex].value;
                    }
                    if (GUILayout.Button("Apply Changes", GUILayout.Width(120)))
                    {
                        var enumName = _mode == Mode.SFX ? "ESFX" : "EBGM";
                        ApplyEnumChanges_Inline(enumName);
                    }
                }

                _enumSearch = EditorGUILayout.TextField(_enumSearch);

                _scrollLeft = EditorGUILayout.BeginScrollView(_scrollLeft, GUILayout.MaxHeight(420));
                for (int i = 0; i < _workItems.Count; i++)
                {
                    var it = _workItems[i];
                    if (!string.IsNullOrEmpty(_enumSearch) &&
                        it.name.IndexOf(_enumSearch, StringComparison.OrdinalIgnoreCase) < 0) continue;

                    bool isSel = _selectedIndex == i;
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
                            if (_workItems.Count == 0) { _selectedIndex = -1; _selectedKey = -1; }
                            else { _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _workItems.Count - 1); _selectedKey = _workItems[_selectedIndex].value; }
                            i--;
                            continue;
                        }
                        if (newName != it.name)
                            _workItems[i] = new EnumCodegenUtility.EnumItem { name = newName, value = it.value };
                    }

                    var lastRect = GUILayoutUtility.GetLastRect();
                    if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
                    {
                        _selectedIndex = i;
                        _selectedKey = _workItems[i].value;
                        Repaint();
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void EnsureWorkItemsLoaded()
        {
            if (_workItems != null && _workItems.Count > 0) return;

            var lib = GetCurrentLib();
            if (lib != null)
            {
                var t = lib.ResolveEnumType();
                if (t != null)
                {
                    var names = Enum.GetNames(t);
                    var values = (Array)Enum.GetValues(t);
                    _workItems = new List<EnumCodegenUtility.EnumItem>(names.Length);
                    for (int i = 0; i < names.Length; i++)
                        _workItems.Add(new EnumCodegenUtility.EnumItem { name = names[i], value = Convert.ToInt32(values.GetValue(i)) });
                    _workItems.Sort((a, b) => a.value.CompareTo(b.value));
                }
            }

            if (_workItems == null) _workItems = new List<EnumCodegenUtility.EnumItem>();

            if (_workItems.Count > 0 && (_selectedIndex < 0 || _selectedIndex >= _workItems.Count))
            {
                _selectedIndex = 0;
                _selectedKey = _workItems[0].value;
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
                if (!EnumCodegenUtility.IsValidIdentifier(nm)) { EditorUtility.DisplayDialog("Invalid name", $"'{nm}' is not a valid C# identifier.", "OK"); return; }
                if (!seen.Add(nm)) { EditorUtility.DisplayDialog("Duplicate name", $"'{nm}' appears multiple times.", "OK"); return; }
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
