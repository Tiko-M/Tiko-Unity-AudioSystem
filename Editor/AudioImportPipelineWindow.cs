#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tiko.AudioSystem.EditorTools
{
    public class AudioImportPipelineWindow : EditorWindow
    {
        // ===== Settings lưu trong EditorPrefs =====
        private const string kCuesRootKey = "Tiko.Audio.CuesRoot";

        // Mặc định bạn yêu cầu
        private const string kDefaultEnumPath = "Assets/Tiko/Tiko.AudioSystem/Scripts/EAudio.cs";
        private const string kDefaultLibPath = "Assets/Tiko/Tiko.AudioSystem/Resources/AudioLibrary.asset";

        // Session keys dùng cho post-reload
        private const string kPendingSyncKey = "AudioPipeline_PendingSync";
        private const string kLibPathKey = "Tiko.Audio.LibraryPath";


        // UI state
        private bool _defaultExpanded = true; // mặc định: mở hết

        private string _cuesRoot;
        private string _enumPath;
        private string _libAssetPath;
        private string _search = "";
        private Vector2 _scroll;
        private readonly Dictionary<string, bool> _foldouts = new();

        [MenuItem("Tools/Audio/Import Pipeline")]
        public static void Open() => GetWindow<AudioImportPipelineWindow>("Audio Import Pipeline");

        private void OnEnable()
        {
            _cuesRoot = EditorPrefs.GetString(kCuesRootKey, "");
            _enumPath = kDefaultEnumPath;
            _libAssetPath = kDefaultLibPath;
        }

        private void OnGUI()
        {
            // Header
            EditorGUILayout.LabelField("Audio Import Pipeline", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Enum: {_enumPath}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Library: {_libAssetPath}", EditorStyles.miniLabel);
            EditorGUILayout.Space();

            // ===== Cues Root (bắt buộc kéo-thả) =====
            var currentFolderObj = string.IsNullOrEmpty(_cuesRoot) ? null : AssetDatabase.LoadAssetAtPath<DefaultAsset>(_cuesRoot);
            var newFolderObj = (DefaultAsset)EditorGUILayout.ObjectField("Cues Root", currentFolderObj, typeof(DefaultAsset), false);
            if (newFolderObj != null)
            {
                var p = AssetDatabase.GetAssetPath(newFolderObj);
                if (AssetDatabase.IsValidFolder(p) && _cuesRoot != p)
                {
                    _cuesRoot = p;
                    EditorPrefs.SetString(kCuesRootKey, _cuesRoot);
                }
            }

            bool hasValidRoot = !string.IsNullOrEmpty(_cuesRoot) && AssetDatabase.IsValidFolder(_cuesRoot);
            if (!hasValidRoot)
            {
                EditorGUILayout.HelpBox("Chưa chọn thư mục audio gốc (Cues Root). Hãy kéo-thả một folder trong Assets vào ô trên để tiếp tục.", MessageType.Warning);
                return; // chặn toàn bộ thao tác khi chưa có root
            }

            // ===== Library asset (Ensure) =====
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Ensure Library", GUILayout.Width(130)))
                {
                    var lib = EnsureLibraryAsset(ref _libAssetPath);
                    if (lib != null) Selection.activeObject = lib;
                }
            }

            EditorGUILayout.Space();

            // ===== Generate Enum + Sync =====
            if (GUILayout.Button("Generate Enum + Sync Library"))
            {
                if (!AssetDatabase.IsValidFolder(_cuesRoot))
                {
                    Debug.LogWarning($"[AudioPipeline] Cues Root không hợp lệ: '{_cuesRoot}'. Hãy chọn lại.");
                    return;
                }

                var lib = EnsureLibraryAsset(ref _libAssetPath); // đảm bảo có Library (Resources)
                if (lib == null)
                {
                    Debug.LogError("[AudioPipeline] Không tạo được AudioLibrary. Dừng.");
                    return;
                }

                // Scan -> Generate enum -> Refresh -> post-reload Sync
                var keys = AudioScanUtils.ScanCueFolders_PathOnly(_cuesRoot, out var _);      // <-- yêu cầu bạn cập nhật chữ ký hàm này
                AudioEnumGenerator.GenerateEnum_PathOnly(_enumPath, keys);                    // <-- và hàm này

                // Lưu thông tin cho post-reload
                SessionState.SetBool(kPendingSyncKey, true);
                SessionState.SetString(kCuesRootKey, _cuesRoot);         // Root để rescan sau reload
                SessionState.SetString(kLibPathKey, _libAssetPath);
                AssetDatabase.Refresh();
                Debug.Log("[AudioPipeline] Enum generated. Waiting for scripts to reload before syncing library...");
            }

            EditorGUILayout.Space();
            DrawLibraryInspector(); // phần UI xem/sửa clips trong Library
        }

        // ======= Library UI =======
        private void DrawLibraryInspector()
        {
            var lib = AssetDatabase.LoadAssetAtPath<AudioLibrary>(_libAssetPath);
            EditorGUILayout.LabelField("Audio Library – Key & Clips", EditorStyles.boldLabel);

            if (lib == null)
            {
                EditorGUILayout.HelpBox("Library chưa tồn tại. Bấm 'Ensure Library' để tạo.", MessageType.Info);
                return;
            }

            var so = new SerializedObject(lib);
            var listProp = so.FindProperty("audioList");
            if (listProp == null)
            {
                EditorGUILayout.HelpBox("Không tìm thấy 'audioList' trong AudioLibrary.", MessageType.Warning);
                return;
            }

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
                    _defaultExpanded = false;
                    _foldouts.Clear(); // xoá cache để toàn bộ dùng mặc định mới = collapsed
                }
                if (GUILayout.Button("Expand All", EditorStyles.toolbarButton, GUILayout.Width(90)))
                {
                    _defaultExpanded = true;
                    _foldouts.Clear(); // xoá cache để toàn bộ dùng mặc định mới = expanded
                }

            }

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

                var keyProp = elem.FindPropertyRelative("key");     // string
                var clipsProp = elem.FindPropertyRelative("clips");   // AudioClip[]
                if (keyProp == null || clipsProp == null) continue;

                string keyName = keyProp.stringValue ?? "";
                if (!string.IsNullOrEmpty(_search) &&
                    keyName.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0) continue;

                int clipCount = Mathf.Max(0, clipsProp.arraySize);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    // Header row
                    Rect headerRect = GUILayoutUtility.GetRect(1, 24, GUILayout.ExpandWidth(true));
                    bool expanded = _foldouts.TryGetValue(keyName, out var ex) ? ex : _defaultExpanded;

                    if (GUI.Button(headerRect, GUIContent.none, GUIStyle.none))
                    {
                        expanded = !expanded; GUI.changed = true;
                    }

                    string headerText = string.IsNullOrEmpty(keyName) ? "<EMPTY>" : keyName;
                    var foldoutStyle = new GUIStyle(EditorStyles.foldoutHeader) { fontStyle = FontStyle.Bold };
                    EditorGUI.Foldout(headerRect, expanded, headerText, true, foldoutStyle);

                    string pillText = $"{clipCount} clip{(clipCount == 1 ? "" : "s")}";
                    float pillW = Mathf.Max(38f, 8f + pillText.Length * 7f);
                    var pillRect = new Rect(headerRect.xMax - (pillW + 8f), headerRect.y + 3f, pillW, headerRect.height - 6f);
                    var pillBg = EditorGUIUtility.isProSkin ? new Color(0.19f, 0.38f, 0.90f, 0.35f) : new Color(0.13f, 0.36f, 0.82f, 0.27f);
                    var pillFg = EditorGUIUtility.isProSkin ? new Color(0.67f, 0.83f, 1f, 1f) : new Color(0.10f, 0.22f, 0.40f, 1f);
                    DrawPillRect(pillRect, pillText, pillBg, pillFg);

                    _foldouts[keyName] = expanded;
                    if (!expanded) continue;

                    EditorGUI.indentLevel++;
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField("Key", keyName);
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
                                // Preview
                                if (GUILayout.Button("▶", GUILayout.Width(28)))
                                {
                                    var clip = clipElem.objectReferenceValue as AudioClip;
                                    if (clip != null) EditorAudioPreview.Play(clip);
                                }
                                if (GUILayout.Button("■", GUILayout.Width(28)))
                                {
                                    EditorAudioPreview.StopAll();
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

        // ===== Helpers =====
        private static AudioLibrary EnsureLibraryAsset(ref string libAssetPath)
        {
            if (string.IsNullOrEmpty(libAssetPath)) libAssetPath = kDefaultLibPath;
            var dir = System.IO.Path.GetDirectoryName(libAssetPath)?.Replace("\\", "/");
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
                CreateFolders(dir);

            var lib = AssetDatabase.LoadAssetAtPath<AudioLibrary>(libAssetPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<AudioLibrary>();
                AssetDatabase.CreateAsset(lib, libAssetPath);
                AssetDatabase.SaveAssets();
            }
            return lib;
        }

        private static void CreateFolders(string assetPath)
        {
            var parts = assetPath.Split('/');
            string cur = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{cur}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }

    // ===== Post-reload: chạy Sync sau khi biên dịch xong =====
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

            // Lấy dữ liệu đã lưu
            var libPath = SessionState.GetString("Tiko.Audio.LibraryPath", "");
            var cuesRoot = SessionState.GetString("Tiko.Audio.CuesRoot", "");
            var lib = AssetDatabase.LoadAssetAtPath<AudioLibrary>(libPath);
            if (lib == null)
            {
                ClearFlags();
                EditorApplication.update -= TryRunPendingSync;
                Debug.LogWarning("[AudioPipeline] Library không tồn tại để sync.");
                return;
            }

            var keys = AudioScanUtils.ScanCueFolders_PathOnly(cuesRoot, out var map);
            AudioLibrarySync.SyncLibrary_LibAndMap(lib, map); // <-- cập nhật chữ ký như đã trao đổi

            Debug.Log("[AudioPipeline] Sync Library done after scripts reloaded.");
            ClearFlags();
            EditorApplication.update -= TryRunPendingSync;
        }

        private static void ClearFlags()
        {
            SessionState.EraseBool("AudioPipeline_PendingSync");
            SessionState.EraseString("Tiko.Audio.LibraryPath");
            SessionState.EraseString("Tiko.Audio.CuesRoot");
        }
    }

    // ===== Editor preview helper (AudioUtil) =====
    internal static class EditorAudioPreview
    {
        private static readonly System.Reflection.MethodInfo _playMethod;
        private static readonly System.Reflection.MethodInfo _stopAllMethod;

        static EditorAudioPreview()
        {
            var audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            _playMethod = audioUtil.GetMethod("PlayPreviewClip",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                null, new Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null)
                ?? audioUtil.GetMethod("PlayPreviewClip",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic,
                    null, new Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
            _stopAllMethod = audioUtil.GetMethod("StopAllPreviewClips",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                ?? audioUtil.GetMethod("StopAllPreviewClips",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        }

        public static void Play(AudioClip clip, bool loop = false)
        {
            if (clip == null || _playMethod == null) return;
            StopAll();
            _playMethod.Invoke(null, new object[] { clip, 0, loop });
        }

        public static void StopAll()
        {
            if (_stopAllMethod == null) return;
            _stopAllMethod.Invoke(null, null);
        }
    }
}
#endif
