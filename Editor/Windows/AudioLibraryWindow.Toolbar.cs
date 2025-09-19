#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Tiko.AudioSystem.EditorTools
{
    public sealed partial class AudioLibraryWindow
    {
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var newMode = (Mode)GUILayout.Toolbar(
                    (int)_mode,
                    new[] { new GUIContent("SFX"), new GUIContent("BGM") },
                    EditorStyles.toolbarButton);

                if (newMode != _mode)
                {
                    _mode = newMode;
                    _workItems.Clear();
                    _enumNames = Array.Empty<string>();
                    _enumKeys = Array.Empty<int>();

                    BuildEnumCache();           // NEW: rebuild enum list theo mode
                    BindCurrentSerializedObject();
                    Repaint();
                }

                if (GUILayout.Button(EditorGUIUtility.IconContent("_Popup"), EditorStyles.toolbarButton, GUILayout.Width(24)))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Enum Path: " + AudioEditorSettings.EnumPath), false, () => { });
                    menu.AddItem(new GUIContent("Set Enum Path..."), false, () =>
                    {
                        var selected = EditorUtility.OpenFolderPanel("Select Enum Folder", "Assets", "");
                        if (!string.IsNullOrEmpty(selected) && selected.Contains("Assets"))
                        {
                            var rel = "Assets" + selected.Split(new[] { "Assets" }, System.StringSplitOptions.None)[1];
                            AudioEditorSettings.EnumPath = rel.Replace("\\", "/");
                            BuildEnumCache();
                            Repaint();
                        }
                    });
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Library Path: " + AudioEditorSettings.LibraryPath), false, () => { });
                    menu.AddItem(new GUIContent("Set Library Path..."), false, () =>
                    {
                        var selected = EditorUtility.OpenFolderPanel("Select Library Folder", "Assets", "");
                        if (!string.IsNullOrEmpty(selected) && selected.Contains("Assets"))
                        {
                            var rel = "Assets" + selected.Split(new[] { "Assets" }, System.StringSplitOptions.None)[1];
                            AudioEditorSettings.LibraryPath = rel.Replace("\\", "/");
                            EnsureLibrariesExist();
                            BindCurrentSerializedObject();
                            Repaint();
                        }
                    });
                    menu.ShowAsContext();
                }

                GUILayout.FlexibleSpace();
            }
        }

        private void HandleKeyboard()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;

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
                if (_selectedKey >= 0) { PreviewRandomClip(_selectedKey); e.Use(); }
            }
        }
    }
}
#endif
