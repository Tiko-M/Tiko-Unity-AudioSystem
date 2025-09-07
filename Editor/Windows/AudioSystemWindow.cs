#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;

namespace Tiko.AudioSystem.Editor
{
    public sealed class AudioSystemWindow : EditorWindow
    {
        private Vector2 _scroll;

        // Cache
        private AudioManager _am;
        private BGMController _bgm;
        private AudioMixerController _mix;

        [MenuItem("Tiko/Audio/Window", priority = 0)]
        private static void Open() => GetWindow<AudioSystemWindow>("Tiko Audio System");

        private void OnEnable()
        {
            RefreshSceneRefs();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70))) RefreshSceneRefs();
                GUILayout.FlexibleSpace();
                GUILayout.Label(EditorSceneManager.GetActiveScene().name, EditorStyles.miniLabel);
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawOverview();
            EditorGUILayout.Space(8);
            DrawLibraries();
            EditorGUILayout.Space(8);
            DrawMixer();
            EditorGUILayout.Space(8);
            DrawSceneHelpers();

            EditorGUILayout.EndScrollView();
        }

        private void DrawOverview()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Audio Manager", ObjStatus(_am));
                EditorGUILayout.LabelField("BGM Controller", ObjStatus(_bgm));
                EditorGUILayout.LabelField("Mixer Controller", ObjStatus(_mix));

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Create Essentials")) CreateEssentials();
                    if (GUILayout.Button("Select Manager") && _am) Selection.activeObject = _am.gameObject;
                    if (GUILayout.Button("Select BGM") && _bgm) Selection.activeObject = _bgm.gameObject;
                    if (GUILayout.Button("Select Mixer") && _mix) Selection.activeObject = _mix.gameObject;
                }
            }
        }

        private void DrawLibraries()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Libraries", EditorStyles.boldLabel);
                var guids = AssetDatabase.FindAssets("t:EnumLibraryBase");
                if (guids.Length == 0) { EditorGUILayout.HelpBox("No EnumLibrary assets found.", MessageType.Info); return; }

                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var lib = AssetDatabase.LoadAssetAtPath<EnumLibraryBase>(path);
                    if (lib == null) continue;

                    using (new EditorGUILayout.VerticalScope("helpbox"))
                    {
                        var enumType = lib.ResolveEnumType();
                        EditorGUILayout.ObjectField("Asset", lib, typeof(EnumLibraryBase), false);
                        EditorGUILayout.LabelField("Enum", enumType != null ? enumType.FullName : "<unassigned>");
                        EditorGUILayout.LabelField("Entries", lib.Entries != null ? lib.Entries.Count.ToString() : "0");

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Add Missing"))
                            {
                                Undo.RecordObject(lib, "Add Missing Enum Entries");
                                lib.AddMissingFromEnum();
                                lib.SortByEnumOrder();
                                EditorUtility.SetDirty(lib);
                            }
                            if (GUILayout.Button("Clean Orphans"))
                            {
                                if (EditorUtility.DisplayDialog("Clean Orphans", "Remove entries that are not in the enum?", "Remove", "Cancel"))
                                {
                                    Undo.RecordObject(lib, "Remove Orphans");
                                    lib.RemoveOrphans();
                                    lib.SortByEnumOrder();
                                    EditorUtility.SetDirty(lib);
                                }
                            }
                            if (GUILayout.Button("Sort"))
                            {
                                Undo.RecordObject(lib, "Sort Entries");
                                lib.SortByEnumOrder();
                                EditorUtility.SetDirty(lib);
                            }
                            if (GUILayout.Button("Select")) Selection.activeObject = lib;
                        }
                    }
                }
            }
        }

        private void DrawMixer()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Mixer", EditorStyles.boldLabel);

                _mix = (AudioMixerController)EditorGUILayout.ObjectField("Controller", _mix, typeof(AudioMixerController), true);
                if (_mix == null)
                {
                    EditorGUILayout.HelpBox("Assign an AudioMixerController from the scene.", MessageType.Info);
                    return;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    var m = Mathf.Clamp01(_mix.GetVolume(AudioBus.Master));
                    var b = Mathf.Clamp01(_mix.GetVolume(AudioBus.Bgm));
                    var s = Mathf.Clamp01(_mix.GetVolume(AudioBus.Sfx));

                    EditorGUI.BeginChangeCheck();
                    m = EditorGUILayout.Slider("Master", m, 0f, 1f);
                    b = EditorGUILayout.Slider("BGM", b, 0f, 1f);
                    s = EditorGUILayout.Slider("SFX", s, 0f, 1f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_mix, "Mixer Volume Change");
                        _mix.SetVolume(AudioBus.Master, m);
                        _mix.SetVolume(AudioBus.Bgm, b);
                        _mix.SetVolume(AudioBus.Sfx, s);
                        EditorUtility.SetDirty(_mix);
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Mute BGM")) _mix.Mute(AudioBus.Bgm, true);
                    if (GUILayout.Button("Unmute BGM")) _mix.Mute(AudioBus.Bgm, false);
                    if (GUILayout.Button("Reset Volumes")) _mix.LoadAndApplyAll();
                }
            }
        }

        private void DrawSceneHelpers()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Scene Helpers", EditorStyles.boldLabel);

                _am = (AudioManager)EditorGUILayout.ObjectField("Audio Manager", _am, typeof(AudioManager), true);
                _bgm = (BGMController)EditorGUILayout.ObjectField("BGM Controller", _bgm, typeof(BGMController), true);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Find In Scene")) RefreshSceneRefs();
                    if (GUILayout.Button("Create Essentials")) CreateEssentials();
                }

                if (_am != null)
                {
                    _am.poolInitial = EditorGUILayout.IntField("Pool Initial", _am.poolInitial);
                    _am.poolCapacity = EditorGUILayout.IntField("Pool Capacity", _am.poolCapacity);
                    EditorGUILayout.ObjectField("SFX Library", _am.sfxLibrary, typeof(EnumLibraryBase), false);
                }

                if (_bgm != null)
                {
                    EditorGUILayout.ObjectField("BGM Library", _bgm.bgmLibrary, typeof(EnumLibraryBase), false);
                    _bgm.defaultCrossfade = EditorGUILayout.Slider("Crossfade", _bgm.defaultCrossfade, 0f, 5f);
                }
            }
        }

        // ----------------- helpers -----------------
        private void RefreshSceneRefs()
        {
            _am = FindFirstObjectByType<AudioManager>();
            _bgm = FindFirstObjectByType<BGMController>();
            _mix = FindFirstObjectByType<AudioMixerController>();
            Repaint();
        }

        private static string ObjStatus(UnityEngine.Object obj) => obj ? "OK" : "Missing";

        private static void CreateEssentials()
        {
            var root = new GameObject("Audio System");
            var am = root.AddComponent<AudioManager>();
            var bgm = root.AddComponent<BGMController>();
            var mix = root.AddComponent<AudioMixerController>();
            Undo.RegisterCreatedObjectUndo(root, "Create Audio Essentials");
            Selection.activeObject = root;
        }
    }
}
#endif
