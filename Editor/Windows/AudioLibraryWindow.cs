// ============================================================================
// File: Editor/Windows/TikoAudioLibraryWindow.cs
// Namespace: Tiko.AudioSystem.Editor
// Purpose: Manage EnumLibrary assets: bind enum (for BoundEnumLibrary), sync entries, quick preview.
// Menu: Tiko/Audio/Library Manager
// ============================================================================
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
        private Vector2 _scrollLeft;
        private Vector2 _scrollRight;

        private List<EnumLibraryBase> _libs = new();
        private EnumLibraryBase _selected;

        // enum picker cache
        private List<Type> _allEnums = new();
        private string _enumFilter = string.Empty;
        private GUIContent[] _enumOptions = Array.Empty<GUIContent>();
        private int _enumIndex = -1;

        [MenuItem("Tiko/Audio/Library Manager", priority = 1)]
        private static void Open() => GetWindow<AudioLibraryWindow>("Tiko Library Manager");

        private void OnEnable()
        {
            RefreshLibs();
            BuildEnumList();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawLibraryList();
                GUILayout.Space(6);
                DrawInspectorPane();
            }
        }

        private void DrawLibraryList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(340)))
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70))) RefreshLibs();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Create Bound Lib", EditorStyles.toolbarButton))
                    {
                        var asset = ScriptableObject.CreateInstance<BoundEnumLibrary>();
                        var path = EditorUtility.SaveFilePanelInProject("Create Bound Enum Library", "NewBoundEnumLibrary", "asset", "");
                        if (!string.IsNullOrEmpty(path))
                        {
                            AssetDatabase.CreateAsset(asset, path);
                            AssetDatabase.SaveAssets();
                            RefreshLibs();
                            _selected = asset;
                        }
                    }
                }

                _scrollLeft = EditorGUILayout.BeginScrollView(_scrollLeft);
                if (_libs.Count == 0)
                {
                    EditorGUILayout.HelpBox("No EnumLibrary assets found.", MessageType.Info);
                }
                else
                {
                    foreach (var lib in _libs)
                    {
                        using (new EditorGUILayout.VerticalScope("box"))
                        {
                            var bound = lib.ResolveEnumType();
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                if (GUILayout.Toggle(_selected == lib, GUIContent.none, GUILayout.Width(18))) _selected = lib;
                                EditorGUILayout.ObjectField(lib, typeof(EnumLibraryBase), false);
                            }
                            EditorGUILayout.LabelField("Enum", bound != null ? bound.FullName : "<unassigned>");
                            EditorGUILayout.LabelField("Entries", lib.Entries != null ? lib.Entries.Count.ToString() : "0");

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                if (GUILayout.Button("Select")) Selection.activeObject = lib;
                                if (GUILayout.Button("Add Missing")) { Undo.RecordObject(lib, "Add Missing"); lib.AddMissingFromEnum(); lib.SortByEnumOrder(); EditorUtility.SetDirty(lib); }
                                if (GUILayout.Button("Clean Orphans")) { Undo.RecordObject(lib, "Clean Orphans"); lib.RemoveOrphans(); lib.SortByEnumOrder(); EditorUtility.SetDirty(lib); }
                            }
                        }
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawInspectorPane()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                if (_selected == null)
                {
                    EditorGUILayout.HelpBox("Select a library on the left to inspect.", MessageType.Info);
                    return;
                }

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField("Selected Library", EditorStyles.boldLabel);
                    EditorGUILayout.ObjectField("Asset", _selected, typeof(EnumLibraryBase), false);
                    var bound = _selected.ResolveEnumType();
                    EditorGUILayout.LabelField("Enum", bound != null ? bound.FullName : "<unassigned>");

                    // Bind enum for BoundEnumLibrary
                    if (_selected is BoundEnumLibrary bel)
                    {
                        EditorGUILayout.Space(4);
                        EditorGUILayout.LabelField("Bind Enum Type", EditorStyles.miniBoldLabel);
                        _enumFilter = EditorGUILayout.TextField("Search", _enumFilter);
                        if (GUILayout.Button("Rebuild Enum List")) BuildEnumList();

                        // popup
                        var filtered = FilterEnums();
                        var options = filtered.Select(t => new GUIContent($"{t.Assembly.GetName().Name} · {t.FullName}")).ToArray();
                        if (options.Length == 0) EditorGUILayout.HelpBox("No enums match filter.", MessageType.Info);
                        else
                        {
                            _enumIndex = Mathf.Clamp(_enumIndex, 0, options.Length - 1);
                            _enumIndex = EditorGUILayout.Popup(new GUIContent("Pick"), _enumIndex, options);
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                if (GUILayout.Button("Bind & Sync", GUILayout.Width(120)))
                                {
                                    var chosen = filtered[_enumIndex];
                                    Undo.RecordObject(bel, "Bind Enum Type");
                                    bel.EditorBindEnumType(chosen);
                                    bel.RemoveOrphans();
                                    bel.AddMissingFromEnum();
                                    bel.SortByEnumOrder();
                                    EditorUtility.SetDirty(bel);
                                }
                                if (GUILayout.Button("Select Asset")) Selection.activeObject = bel;
                            }
                        }
                    }
                }

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField("Entries", EditorStyles.boldLabel);
                    _scrollRight = EditorGUILayout.BeginScrollView(_scrollRight);
                    var entries = _selected.Entries;
                    if (entries == null || entries.Count == 0)
                    {
                        EditorGUILayout.HelpBox("No entries. Use 'Add Missing'.", MessageType.Info);
                    }
                    else
                    {
                        foreach (var e in entries)
                        {
                            using (new EditorGUILayout.VerticalScope("helpbox"))
                            {
                                EditorGUILayout.LabelField($"[{e.key}] {e.keyName}", EditorStyles.boldLabel);
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    if (GUILayout.Button("▶ Play First", GUILayout.Width(110)))
                                    {
                                        var clip = (e.clips != null && e.clips.Count > 0) ? e.clips[0] : null;
                                        EditorAudioPreviewUtility.Play(clip);
                                    }
                                    if (GUILayout.Button("■ Stop All", GUILayout.Width(90)))
                                    {
                                        EditorAudioPreviewUtility.StopAll();
                                    }
                                    GUILayout.FlexibleSpace();
                                    if (GUILayout.Button("Select Clips"))
                                    {
                                        if (e.clips != null && e.clips.Count > 0)
                                            Selection.objects = e.clips.Where(c => c != null).Cast<UnityEngine.Object>().ToArray();
                                    }
                                }

                                // read-only view of clips (drag-drop editing in Inspector proper)
                                if (e.clips != null)
                                {
                                    foreach (var c in e.clips)
                                    {
                                        EditorGUILayout.ObjectField(c, typeof(AudioClip), false);
                                    }
                                }
                            }
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
        }

        private void RefreshLibs()
        {
            _libs.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:EnumLibraryBase"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var lib = AssetDatabase.LoadAssetAtPath<EnumLibraryBase>(path);
                if (lib != null) _libs.Add(lib);
            }
            if (_selected == null && _libs.Count > 0) _selected = _libs[0];
            Repaint();
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
                    if (t != null && t.IsEnum) _allEnums.Add(t);
                }
            }
            _allEnums.Sort((a, b) => Score(b).CompareTo(Score(a)));
            _enumIndex = 0;
        }

        private List<Type> FilterEnums()
        {
            if (string.IsNullOrWhiteSpace(_enumFilter)) return _allEnums;
            return _allEnums.Where(t => t.FullName.IndexOf(_enumFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }

        private static int Score(Type t)
        {
            var asm = t.Assembly.GetName().Name ?? string.Empty;
            int s = asm.StartsWith("Assembly-CSharp", StringComparison.Ordinal) ? 1000 : 0;
            s += Math.Max(0, 200 - (t.FullName?.Length ?? 0));
            return s;
        }
    }
}
#endif
