#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Tiko.AudioSystem.EditorTools
{
    public sealed partial class AudioLibraryWindow
    {
        private void BuildStyles()
        {
            _rowStyle = new GUIStyle(EditorStyles.label)
            {
                padding = new RectOffset(8, 8, 4, 4),
                alignment = TextAnchor.MiddleLeft
            };
            _rowStyle.hover = new GUIStyleState
            {
                background = MakeTex(1, 1, new Color(0.5f, 0.5f, 0.5f, EditorGUIUtility.isProSkin ? 0.12f : 0.08f))
            };

            _rowStyleSelected = new GUIStyle(_rowStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState
                {
                    textColor = EditorStyles.boldLabel.normal.textColor,
                    background = MakeTex(1, 1, new Color(0.24f, 0.48f, 0.90f, 0.20f))
                }
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
    }
}
#endif
