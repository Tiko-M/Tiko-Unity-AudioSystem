#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;

namespace AudioSystem.EditorTools
{
    public class AudioImportPipelineWindow : EditorWindow
    {
        private AudioImportConfig _config;
        private Vector2 _scroll;

        // Trạng thái foldout theo GIÁ TRỊ THẬT của enum
        private readonly Dictionary<int, bool> _foldouts = new();

        // UI state
        [SerializeField] private string _search = "";

        [MenuItem("Tools/Tiko/AudioTools")]
        public static void Open() => GetWindow<AudioImportPipelineWindow>("Audio Import Pipeline");

        private void OnEnable()
        {
            if (_config == null)
            {
                var guids = AssetDatabase.FindAssets("t:AudioImportConfig");
                if (guids != null && guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _config = AssetDatabase.LoadAssetAtPath<AudioImportConfig>(path);
                }
            }
        }

        private void OnGUI()
        {

            EditorGUILayout.LabelField("Audio Import Pipeline", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _config = (AudioImportConfig)EditorGUILayout.ObjectField("Config", _config, typeof(AudioImportConfig), false);
            if (_config == null)
            {
                EditorGUILayout.HelpBox("Tạo AudioImportConfig (Create > Audio > Audio Import Config) rồi gán vào đây.", MessageType.Info);
                if (GUILayout.Button("Tạo nhanh Config trong Assets"))
                {
                    var cfg = ScriptableObject.CreateInstance<AudioImportConfig>();
                    string path = AssetDatabase.GenerateUniqueAssetPath("Assets/AudioImportConfig.asset");
                    AssetDatabase.CreateAsset(cfg, path);
                    AssetDatabase.SaveAssets();
                    _config = cfg;
                    Selection.activeObject = cfg;
                }
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            _config.cuesRoot = EditorGUILayout.TextField("Cues Root", _config.cuesRoot);
            _config.enumFilePath = EditorGUILayout.TextField("Enum File Path", _config.enumFilePath);
            _config.targetLibrary = (AudioLibrary)EditorGUILayout.ObjectField("Audio Library", _config.targetLibrary, typeof(AudioLibrary), false);

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate Enum + Sync Library"))
            {
                // 1) Generate enum ngay
                var keys = AudioScanUtils.ScanCueFolders(_config, out var _);
                AudioEnumGenerator.GenerateEnum(_config, keys);

                // 2) Yêu cầu refresh & chờ compile + reload
                var cfgPath = AssetDatabase.GetAssetPath(_config);
                SessionState.SetBool("AudioPipeline_PendingSync", true);
                SessionState.SetString("AudioPipeline_ConfigPath", cfgPath);
                AssetDatabase.Refresh();
                Debug.Log("[AudioPipeline] Enum generated. Waiting for scripts to reload before syncing library...");
            }


            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Audio Library – Key & Clips", EditorStyles.boldLabel);

            if (_config.targetLibrary == null)
            {
                EditorGUILayout.HelpBox("Chưa chọn Audio Library.", MessageType.Info);
            }
            else
            {
                DrawKeyAndClipsList(_config.targetLibrary);
            }

            if (GUI.changed) EditorUtility.SetDirty(_config);
        }
        private void DrawKeyAndClipsList(AudioLibrary lib)
        {
            var so = new SerializedObject(lib);
            var listProp = so.FindProperty("audioList");
            if (listProp == null)
            {
                EditorGUILayout.HelpBox("Không tìm thấy 'audioList' trong AudioLibrary.", MessageType.Warning);
                return;
            }

            // Toolbar (giữ nguyên nếu bạn đang dùng)
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Audio Library", GUILayout.Width(92));
                GUILayout.Space(6);
                _search = GUILayout.TextField(_search,
                    GUI.skin.FindStyle("ToolbarTextField") ?? EditorStyles.toolbarTextField,
                    GUILayout.Width(220));
                GUILayout.Space(6);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Collapse All", EditorStyles.toolbarButton, GUILayout.Width(110)))
                {
                    for (int i = 0; i < listProp.arraySize; i++)
                    {
                        var elem = listProp.GetArrayElementAtIndex(i);
                        var keyProp = elem?.FindPropertyRelative("key");
                        if (keyProp == null) continue;
                        _foldouts[keyProp.intValue] = false;
                    }
                }
            }

            // Header nhắc cột (Key là header, Clips ở body)
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Key", GUILayout.Width(260));
                GUILayout.Label("Clips (kéo-thả để thay)", GUILayout.ExpandWidth(true));
            }
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            for (int i = 0; i < listProp.arraySize; i++)
            {
                var elem = listProp.GetArrayElementAtIndex(i);
                if (elem == null) continue;

                var keyProp = elem.FindPropertyRelative("key");     // EAudio
                var clipsProp = elem.FindPropertyRelative("clips");   // AudioClip[]
                if (keyProp == null || clipsProp == null) continue;

                int keyVal = keyProp.intValue; // GIÁ TRỊ THẬT
                string keyName = keyProp.enumDisplayNames[keyProp.enumValueIndex];
                int clipCount = Mathf.Max(0, clipsProp.arraySize);

                bool anyNull = false;
                for (int c = 0; c < clipCount; c++)
                    anyNull |= clipsProp.GetArrayElementAtIndex(c).objectReferenceValue == null;
                if (!string.IsNullOrEmpty(_search) &&
                    keyName.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0) continue;

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    Rect headerRect = GUILayoutUtility.GetRect(1, 24, GUILayout.ExpandWidth(true));

                    bool expanded = _foldouts.TryGetValue(keyVal, out var ex) ? ex : true;
                    if (GUI.Button(headerRect, GUIContent.none, GUIStyle.none))
                    {
                        expanded = !expanded;
                        GUI.changed = true;
                    }

                    string headerText = keyName;
                    var foldoutStyle = new GUIStyle(EditorStyles.foldoutHeader) { fontStyle = FontStyle.Bold };
                    EditorGUI.Foldout(headerRect, expanded, headerText, true, foldoutStyle);

                    string pillText = $"{clipCount} clip{(clipCount == 1 ? "" : "s")}";
                    float pillW = Mathf.Max(38f, 8f + pillText.Length * 7f);
                    var pillRect = new Rect(headerRect.xMax - (pillW + 8f), headerRect.y + 3f, pillW, headerRect.height - 6f);
                    var pillBg = EditorGUIUtility.isProSkin ? new Color(0.19f, 0.38f, 0.90f, 0.35f) : new Color(0.13f, 0.36f, 0.82f, 0.27f);
                    var pillFg = EditorGUIUtility.isProSkin ? new Color(0.67f, 0.83f, 1f, 1f) : new Color(0.10f, 0.22f, 0.40f, 1f);
                    DrawPillRect(pillRect, pillText, pillBg, pillFg);

                    _foldouts[keyVal] = expanded;

                    if (!expanded) continue;

                    EditorGUI.indentLevel++;
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.EnumPopup("Key", (Enum)Enum.ToObject(typeof(EAudio), keyVal));
                    EditorGUI.EndDisabledGroup();

                    if (clipCount == 0)
                    {
                        EditorGUILayout.LabelField("— Không có clip —", EditorStyles.miniLabel);
                    }
                    else
                    {
                        for (int c = 0; c < clipCount; c++)
                        {
                            var clipElem = clipsProp.GetArrayElementAtIndex(c);
                            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                            {
                                GUILayout.Label($"Clip {c}", GUILayout.Width(56));
                                EditorGUILayout.PropertyField(clipElem, GUIContent.none, GUILayout.ExpandWidth(true));
                                if (GUILayout.Button("Clear", GUILayout.Width(56)))
                                {
                                    clipElem.objectReferenceValue = null;
                                }
                            }
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUILayout.EndScrollView();

            if (so.ApplyModifiedProperties())
                EditorUtility.SetDirty(lib);
        }

        private static void DrawPillRect(Rect rr, string text, Color bg, Color fg)
        {
            EditorGUI.DrawRect(rr, bg);
            var old = GUI.color; GUI.color = fg;
            GUI.Label(rr, text, new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            });
            GUI.color = old;
        }



    }

    [InitializeOnLoad]
    internal static class AudioPipelinePostReload
    {
        static AudioPipelinePostReload()
        {
            EditorApplication.update += TryRunPendingSync;
        }

        private static void TryRunPendingSync()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            if (!SessionState.GetBool("AudioPipeline_PendingSync", false)) return;
            var cfgPath = SessionState.GetString("AudioPipeline_ConfigPath", "");
            var cfg = AssetDatabase.LoadAssetAtPath<AudioImportConfig>(cfgPath);
            if (cfg == null) { ClearFlags(); return; }

            // Re-scan (lấy enum mới vừa compile) rồi sync
            var keys = AudioScanUtils.ScanCueFolders(cfg, out var map);
            AudioLibrarySync.SyncLibrary(cfg, map);

            Debug.Log("[AudioPipeline] Sync Library done after scripts reloaded.");
            ClearFlags();
            // Có thể bỏ đăng ký update nếu không cần chạy thường trực
            // EditorApplication.update -= TryRunPendingSync;
        }

        private static void ClearFlags()
        {
            SessionState.EraseBool("AudioPipeline_PendingSync");
            SessionState.EraseString("AudioPipeline_ConfigPath");
        }
    }
}
#endif
