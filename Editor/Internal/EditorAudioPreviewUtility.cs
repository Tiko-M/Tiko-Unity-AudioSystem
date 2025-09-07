#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;


namespace Tiko.AudioSystem.Editor
{
    internal static class EditorAudioPreviewUtility
    {
        private static readonly Type AudioUtilType = Type.GetType("UnityEditor.AudioUtil, UnityEditor");
        private static readonly MethodInfo PlayMethod = AudioUtilType?.GetMethod(
        "PlayPreviewClip",
        BindingFlags.Static | BindingFlags.Public,
        null,
        new[] { typeof(AudioClip), typeof(int), typeof(bool) },
        null) ?? AudioUtilType?.GetMethod(
        "PlayClip",
        BindingFlags.Static | BindingFlags.Public,
        null,
        new[] { typeof(AudioClip), typeof(int), typeof(bool) },
        null);


        private static readonly MethodInfo StopAllMethod = AudioUtilType?.GetMethod(
        "StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public)
        ?? AudioUtilType?.GetMethod("StopAllClips", BindingFlags.Static | BindingFlags.Public);


        public static void Play(AudioClip clip)
        {
            if (clip == null || PlayMethod == null) return;
            PlayMethod.Invoke(null, new object[] { clip, 0, false });
        }


        public static void StopAll()
        {
            StopAllMethod?.Invoke(null, null);
        }
    }
}
#endif