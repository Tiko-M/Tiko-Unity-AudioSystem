#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Tiko.AudioSystem.EditorTools
{
    public sealed partial class AudioLibraryWindow
    {
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

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(_mode == Mode.SFX ? "SFX Clips" : "BGM Clips", EditorStyles.boldLabel);
                if (GUILayout.Button("■ Stop", GUILayout.Width(70))) EditorAudioPreviewUtility.StopAll();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(2);

                if (_selectedKey < 0)
                {
                    EditorGUILayout.HelpBox("Select a key from the list.", MessageType.Info);
                    return;
                }

                if (!TryGetEntryProperty(_selectedKey, out var entryProp))
                {
                    EditorGUILayout.HelpBox("Entry not found. Re-apply enum changes để cập nhật entries.", MessageType.Warning);
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
                        // Preview RANDOM một clip trong enum đang chọn
                        if (GUILayout.Button("▶ Preview", GUILayout.Width(90))) PreviewRandomClip(_selectedKey);
                    }

                    EditorGUILayout.Space(2);

                    // Danh sách clips + nút ▶ cạnh từng clip
                    if (clipsProp.isArray)
                    {
                        for (int i = 0; i < clipsProp.arraySize; i++)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                var elem = clipsProp.GetArrayElementAtIndex(i);
                                EditorGUILayout.PropertyField(elem, GUIContent.none);

                                // Preview clip đơn
                                if (GUILayout.Button("▶", GUILayout.Width(24)))
                                {
                                    var c = elem.objectReferenceValue as AudioClip;
                                    EditorAudioPreviewUtility.StopAll();
                                    EditorAudioPreviewUtility.Play(c);
                                }

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

        private void PreviewRandomClip(int key)
        {
            if (!TryGetEntryProperty(key, out var entryProp)) return;
            var clipsProp = entryProp.FindPropertyRelative("clips");
            int count = clipsProp != null && clipsProp.isArray ? clipsProp.arraySize : 0;
            if (count <= 0) { EditorAudioPreviewUtility.Play(null); return; }

            int idx = Random.Range(0, count);
            var elem = clipsProp.GetArrayElementAtIndex(idx);
            var clip = elem.objectReferenceValue as AudioClip;

            EditorAudioPreviewUtility.StopAll();
            EditorAudioPreviewUtility.Play(clip);
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
    }
}
#endif
