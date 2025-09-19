#if UNITY_EDITOR
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
                    BindCurrentSerializedObject();
                    BuildEnumCache();

                    // RESET & RELOAD LIST ENUM KHI ĐỔI TAB
                    _workItems.Clear();
                    _enumFilePath = null;
                    EnsureWorkItemsLoaded();

                    if (_workItems.Count > 0)
                    {
                        _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _workItems.Count - 1);
                        _selectedKey = _workItems[_selectedIndex].value;
                    }
                    else
                    {
                        _selectedIndex = -1;
                        _selectedKey = -1;
                    }

                    Repaint();
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
