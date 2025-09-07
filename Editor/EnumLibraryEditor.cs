#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Tiko.AudioSystem;

namespace Tiko.AudioSystem.Editor
{
    [CustomEditor(typeof(EnumLibraryBase), true)]
    public class EnumLibraryEditor : UnityEditor.Editor
    {
        private EnumLibraryBase _lib;
        private string _search = string.Empty;

        private void OnEnable()
        {
            _lib = (EnumLibraryBase)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                var type = _lib.ResolveEnumType();
                EditorGUILayout.TextField("Enum Type", type != null ? type.FullName : "<unbound>");
            }

            EditorGUILayout.Space(4);
            DrawSyncControls();

            EditorGUILayout.Space(6);
            DrawEntries();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSyncControls()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Sync", EditorStyles.boldLabel);
                var diff = _lib.ComputeDiff();
                EditorGUILayout.LabelField($"Missing: {diff.missing.Count} | Orphans: {diff.orphans.Count}");

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add Missing"))
                    {
                        Undo.RecordObject(_lib, "Add Missing Enum Entries");
                        _lib.AddMissingFromEnum();
                        _lib.SortByEnumOrder();
                        EditorUtility.SetDirty(_lib);
                    }

                    if (GUILayout.Button("Clean Orphans"))
                    {
                        if (EditorUtility.DisplayDialog("Clean Orphans",
                                "Remove entries that no longer exist in the enum? This cannot be undone.",
                                "Remove", "Cancel"))
                        {
                            Undo.RecordObject(_lib, "Remove Orphans");
                            _lib.RemoveOrphans();
                            _lib.SortByEnumOrder();
                            EditorUtility.SetDirty(_lib);
                        }
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Sort By Enum Order"))
                    {
                        Undo.RecordObject(_lib, "Sort Entries");
                        _lib.SortByEnumOrder();
                        EditorUtility.SetDirty(_lib);
                    }

                    if (GUILayout.Button("Stop All Previews"))
                    {
                        EditorAudioPreviewUtility.StopAll();
                    }
                }
            }
        }

        private void DrawEntries()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Entries", EditorStyles.boldLabel);
                _search = EditorGUILayout.TextField("Search", _search);

                var entriesProp = serializedObject.FindProperty("entries");
                for (int i = 0; i < entriesProp.arraySize; i++)
                {
                    var eProp = entriesProp.GetArrayElementAtIndex(i);
                    var keyProp = eProp.FindPropertyRelative("key");
                    var nameProp = eProp.FindPropertyRelative("keyName");
                    var clipsProp = eProp.FindPropertyRelative("clips");
                    var cueProp = eProp.FindPropertyRelative("cue");

                    var visible = string.IsNullOrEmpty(_search)
                                  || nameProp.stringValue.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0
                                  || keyProp.intValue.ToString().Contains(_search);
                    if (!visible) continue;

                    using (new EditorGUILayout.VerticalScope("helpbox"))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField($"[{keyProp.intValue}] {nameProp.stringValue}", EditorStyles.boldLabel);
                            if (GUILayout.Button("Play First", GUILayout.Width(90)))
                            {
                                var first = GetFirstClip(clipsProp);
                                EditorAudioPreviewUtility.Play(first);
                            }
                        }

                        EditorGUILayout.PropertyField(nameProp, new GUIContent("Display Name"));
                        EditorGUILayout.PropertyField(clipsProp, new GUIContent("Clips"), true);
                        EditorGUILayout.PropertyField(cueProp, new GUIContent("Cue"), true);
                    }
                }
            }
        }

        private static AudioClip GetFirstClip(SerializedProperty clipsProp)
        {
            if (clipsProp == null) return null;
            if (clipsProp.isArray && clipsProp.arraySize > 0)
            {
                var elem = clipsProp.GetArrayElementAtIndex(0);
                return elem.objectReferenceValue as AudioClip;
            }
            return null;
        }
    }
}
#endif