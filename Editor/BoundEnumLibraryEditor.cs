// ============================================================================
// File: Editor/BoundEnumLibraryEditor.cs
// Namespace: Tiko.AudioSystem.Editor
// Purpose: Type picker UI for BoundEnumLibrary, then reuse EnumLibraryEditor UI.
// ============================================================================
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tiko.AudioSystem.Editor
{
    [CustomEditor(typeof(BoundEnumLibrary))]
    public sealed class BoundEnumLibraryEditor : UnityEditor.Editor
    {
        private BoundEnumLibrary _lib;
        private EnumLibraryEditor _inner;

        private readonly List<Type> _allEnums = new List<Type>();
        private string _filter = string.Empty;
        private int _popupIndex = -1;
        private GUIContent[] _popupOptions = Array.Empty<GUIContent>();

        private void OnEnable()
        {
            _lib = (BoundEnumLibrary)target;
            _inner = CreateEditor(target, typeof(EnumLibraryEditor)) as EnumLibraryEditor;
            BuildEnumList();
            UpdatePopupIndexFromCurrent();
        }

        public override void OnInspectorGUI()
        {
            DrawTypeSelector();
            EditorGUILayout.Space(6);
            _inner?.OnInspectorGUI();
        }

        private void DrawTypeSelector()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Bind Enum Type", EditorStyles.boldLabel);

                var current = _lib.ResolveEnumType();
                EditorGUILayout.LabelField("Current", current != null ? current.FullName : "<unassigned>");

                _filter = EditorGUILayout.TextField("Search", _filter);

                using (new EditorGUILayout.HorizontalScope())
                {
                    var last = _popupIndex;
                    _popupIndex = EditorGUILayout.Popup(new GUIContent("Pick"), _popupIndex, _popupOptions);

                    if (GUILayout.Button("Rebind & Sync", GUILayout.Width(120)))
                    {
                        if (_popupIndex >= 0 && _popupIndex < _allEnums.Count)
                        {
                            var chosen = _allEnums[_popupIndex];
                            Undo.RecordObject(_lib, "Bind Enum Type");
                            _lib.EditorBindEnumType(chosen);
                            _lib.RemoveOrphans();
                            _lib.AddMissingFromEnum();
                            _lib.SortByEnumOrder();
                            EditorUtility.SetDirty(_lib);
                        }
                    }
                }

                // filter changes should rebuild popup list
                if (GUI.changed && _filter != null)
                {
                    RebuildPopup();
                }
            }
        }

        private void BuildEnumList()
        {
            _allEnums.Clear();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch { continue; }

                foreach (var t in types)
                {
                    if (t != null && t.IsEnum)
                    {
                        _allEnums.Add(t);
                    }
                }
            }
            // Prefer project enums first for UX
            _allEnums.Sort((a, b) => Score(b).CompareTo(Score(a)));
            RebuildPopup();
        }

        private static int Score(Type t)
        {
            // Higher score for Assembly-CSharp*, then by name length (shorter first)
            var asm = t.Assembly.GetName().Name ?? string.Empty;
            int s = asm.StartsWith("Assembly-CSharp", StringComparison.Ordinal) ? 1000 : 0;
            s += Math.Max(0, 200 - t.FullName.Length);
            return s;
        }

        private void RebuildPopup()
        {
            var filtered = string.IsNullOrWhiteSpace(_filter)
                ? _allEnums
                : _allEnums.Where(t => t.FullName.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            _popupOptions = filtered.Select(t => new GUIContent($"{t.Assembly.GetName().Name} · {t.FullName}"))
                                     .ToArray();
            // keep index in range
            if (_popupIndex >= filtered.Count) _popupIndex = filtered.Count - 1;
            if (_popupIndex < -1) _popupIndex = -1;
        }

        private void UpdatePopupIndexFromCurrent()
        {
            var current = _lib.ResolveEnumType();
            if (current == null) { _popupIndex = -1; return; }

            for (int i = 0; i < _allEnums.Count; i++)
            {
                if (_allEnums[i] == current) { _popupIndex = i; break; }
            }
            if (_popupIndex == -1) RebuildPopup();
        }
    }
}
#endif
