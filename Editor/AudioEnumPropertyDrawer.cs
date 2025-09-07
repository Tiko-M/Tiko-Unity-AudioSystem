#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace Tiko.AudioSystem.Editor
{
    [CustomPropertyDrawer(typeof(AudioEnumPreviewAttribute))]
    public class AudioEnumPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // why: keep default enum UX while adding a small preview affordance
            var enumRect = position;
            enumRect.width -= 24f;
            EditorGUI.PropertyField(enumRect, property, label, true);


            var buttonRect = new Rect(enumRect.xMax + 2f, position.y, 22f, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(buttonRect, "▶"))
            {
                TryPreview(property);
            }
        }


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }


        private static void TryPreview(SerializedProperty enumProp)
        {
            if (enumProp.propertyType != SerializedPropertyType.Enum) return;


            var obj = enumProp.serializedObject.targetObject;
            var enumType = obj.GetType().GetField(enumProp.propertyPath,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)?.FieldType;
            if (enumType == null || !enumType.IsEnum) return;


            // find a library asset that binds to this enum
            var guids = AssetDatabase.FindAssets("t:EnumLibraryBase");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var lib = AssetDatabase.LoadAssetAtPath<Tiko.AudioSystem.EnumLibraryBase>(path);
                if (lib == null) continue;
                var bound = lib.ResolveEnumType();
                if (bound == enumType)
                {
                    // map selected enum value to entry and play first clip
                    int key = enumProp.enumValueIndex >= 0 ? (int)Convert.ChangeType(
                    Enum.ToObject(enumType, enumProp.intValue), typeof(int)) : enumProp.intValue;


                    if (lib.TryGet(key, out var entry) && entry.clips != null && entry.clips.Count > 0)
                    {
                        EditorAudioPreviewUtility.Play(entry.clips[0]);
                    }
                    return;
                }
            }
        }
    }
}
#endif