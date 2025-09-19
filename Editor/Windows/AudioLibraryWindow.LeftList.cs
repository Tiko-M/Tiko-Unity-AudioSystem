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
                        var newName = EditorGUILayout.DelayedTextField(it.name);
                        if (newName != it.name && EnumCodegenUtility.IsValidIdentifier(newName))
                        {
                            it.name = newName;
                            _workItems[i] = it;
                        }
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("−", GUILayout.Width(28)))
                        {
                            if (i >= 0 && i < _workItems.Count)
                            {
                                _workItems.RemoveAt(i);
                                GUIUtility.ExitGUI();
                            }
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

        private void ApplyEnumChanges_Inline(string enumName)
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < _workItems.Count; i++)
            {
                var nm = _workItems[i].name;
                if (!EnumCodegenUtility.IsValidIdentifier(nm))
                { EditorUtility.DisplayDialog("Invalid name", $"'{nm}' is not a valid C# identifier.", "OK"); return; }
                if (!seen.Add(nm))
                { EditorUtility.DisplayDialog("Duplicate name", $"'{nm}' appears multiple times.", "OK"); return; }
            }


            for (int i = 0; i < _workItems.Count; i++)
                _workItems[i] = new EnumCodegenUtility.EnumItem { name = _workItems[i].name, value = i };


            EnumCodegenUtility.WriteEnum(enumName, _workItems);

            var lib = GetCurrentLib();
            if (lib != null)
            {
                Undo.RecordObject(lib, "Audio Library Update From Enum");

                var newNames = new List<string>(_workItems.Count);
                var newValues = new List<int>(_workItems.Count);
                foreach (var it in _workItems)
                {
                    newNames.Add(it.name);
                    newValues.Add(it.value);
                }


                var entries = lib.Entries as List<EnumLibraryBase.Entry>;

                for (int i = 0; i < entries.Count; i++)
                {
                    var e = entries[i];
                    int idx = newValues.IndexOf(e.key);
                    if (idx >= 0)
                        e.keyName = newNames[idx];
                }

                lib.ReCheckAll();
                EditorUtility.SetDirty(lib);
                AssetDatabase.SaveAssets();
            }


            AssetDatabase.Refresh();

            BuildEnumCache();
            BindCurrentSerializedObject();
            if (!EnsureSelectionValid()) SelectFirstKey();
            Repaint();
        }

    }
}
#endif
