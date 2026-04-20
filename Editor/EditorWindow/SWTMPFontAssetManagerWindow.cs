#if !DISABLE_SW_TMP_MANAGER
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

#if TMP_PRESENT || true
using TMPro;
#endif

namespace SWTools
{
    /// <summary>
    /// TMP 폰트 통합 매니저.
    /// - Quick Swap : 폰트를 등록해두고 선택된 TMP 오브젝트에 원클릭 교체 + 기본 폰트 자동 적용
    /// - Presets    : 등록된 폰트별 Material Preset 목록 조회 및 적용
    /// - Browser    : 프로젝트 내 모든 TMP_FontAsset Atlas/글리프/Fallback 요약
    /// </summary>
    public class SWTMPFontAssetManagerWindow : EditorWindow
    {
        #region 필드 - 공통
        private int selectedTab = 0;
        private static readonly string[] tabNames = { "Quick Swap", "Presets", "Browser" };
        #endregion

        #region 필드 - Quick Swap 탭
        private const string DEFAULT_FONT_GUID_KEY = "SWTools.TMP.DefaultFontGuid";
        private const string REGISTERED_FONTS_KEY = "SWTools.TMP.RegisteredFontGuids";

        private TMP_FontAsset defaultFont;
        private string defaultFontGuid = "";

        private List<string> registeredFontGuids = new();
        private readonly List<RegisteredFontCache> registeredFontCaches = new();
        private bool needsFontCacheRebuild = true;

        private Vector2 swapScroll;

        private class RegisteredFontCache
        {
            public string guid;
            public string path;
            public TMP_FontAsset asset;
            public GUIContent content;
            public bool exists;
        }
        #endregion

        #region 필드 - Presets 탭
        private int presetSelectedFontIndex = -1;
        private List<Material> cachedPresets = new();
        private Vector2 presetScroll;
        #endregion

        #region 필드 - Browser 탭
        private Vector2 browserScroll;
        private List<BrowserEntry> browserEntries = new();
        private string browserSearchFilter = "";
        private BrowserSortMode browserSortMode = BrowserSortMode.Name;
        private bool browserSortDescending = false;
        private readonly Dictionary<string, bool> browserFoldouts = new();

        private long totalAtlasBytes;
        private int totalGlyphs;

        private enum BrowserSortMode { Name, AtlasSize, GlyphCount, AtlasMemory }

        private class BrowserEntry
        {
            public TMP_FontAsset asset;
            public string path;
            public string name;
            public int atlasWidth;
            public int atlasHeight;
            public int glyphCount;
            public int characterCount;
            public long estimatedBytes;
            public int fallbackCount;
        }
        #endregion

        // ────────────────────────────────────────────
        // Hierarchy 자동 적용용 콜백 (static)
        // ────────────────────────────────────────────
        private static TMP_FontAsset s_autoApplyFont;

        [InitializeOnLoadMethod]
        private static void RegisterHierarchyCallback()
        {
            ObjectFactory.componentWasAdded -= OnComponentAdded;
            ObjectFactory.componentWasAdded += OnComponentAdded;

            // 저장된 기본 폰트 로드
            LoadStaticDefaultFont();
        }

        private static void LoadStaticDefaultFont()
        {
            string guid = EditorPrefs.GetString(DEFAULT_FONT_GUID_KEY, "");
            if (string.IsNullOrEmpty(guid)) { s_autoApplyFont = null; return; }

            string path = AssetDatabase.GUIDToAssetPath(guid);
            s_autoApplyFont = string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }

        /// <summary>
        /// TMP_Text 계열 컴포넌트가 새로 추가될 때 기본 폰트를 자동 적용합니다.
        /// </summary>
        private static void OnComponentAdded(Component component)
        {
            if (s_autoApplyFont == null) return;
            if (component is TMP_Text tmpText)
            {
                tmpText.font = s_autoApplyFont;
                EditorUtility.SetDirty(tmpText);
            }
        }

        // ────────────────────────────────────────────

        [MenuItem("SWTools/TMP Font Asset Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<SWTMPFontAssetManagerWindow>();
            window.titleContent = new GUIContent("SW TMP Fonts",
                EditorGUIUtility.FindTexture("d_Text Icon"));
            window.minSize = new Vector2(420, 450);
            window.Show();
        }

        private void OnEnable()
        {
            LoadDefaultFont();
            LoadRegisteredFonts();
            needsFontCacheRebuild = true;
            RefreshBrowser();
        }

        private void OnDisable()
        {
            SaveDefaultFont();
            SaveRegisteredFonts();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(5);
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(25));
            EditorGUILayout.Space(10);

            switch (selectedTab)
            {
                case 0: DrawQuickSwapTab(); break;
                case 1: DrawPresetsTab(); break;
                case 2: DrawBrowserTab(); break;
            }
        }

        // =====================================================================
        //  Quick Swap 탭
        // =====================================================================
        #region Quick Swap 탭

        private void DrawQuickSwapTab()
        {
            // ── 기본 폰트 ──
            DrawHeader("기본 폰트 (자동 적용)");

            EditorGUILayout.HelpBox(
                "여기에 폰트를 지정하면 앞으로 생성되는 모든 TextMeshPro 컴포넌트에\n" +
                "해당 폰트가 자동 적용됩니다.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            defaultFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
                "Default Font", defaultFont, typeof(TMP_FontAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                SaveDefaultFont();
                s_autoApplyFont = defaultFont;
            }

            if (defaultFont != null)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("씬 내 모든 TMP에 적용", GUILayout.Height(24)))
                {
                    ApplyFontToAllInScene(defaultFont);
                }
                if (GUILayout.Button("기본 폰트 해제", GUILayout.Height(24)))
                {
                    defaultFont = null;
                    s_autoApplyFont = null;
                    SaveDefaultFont();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);

            // ── 등록된 폰트 목록 ──
            DrawHeader("등록된 폰트");
            DrawFontDropArea();
            EditorGUILayout.Space(3);

            if (needsFontCacheRebuild) RebuildFontCache();

            if (registeredFontCaches.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "등록된 폰트가 없습니다.\n위 영역에 TMP_FontAsset을 드래그하거나 '선택 항목 추가' 버튼을 사용하세요.",
                    MessageType.Info);
            }
            else
            {
                DrawRegisteredFontList();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("선택 항목 추가", GUILayout.Height(22)))
            {
                AddSelectionAsFonts();
            }
            if (GUILayout.Button("없는 항목 정리", GUILayout.Height(22)))
            {
                CleanMissingFonts();
            }
            EditorGUILayout.EndHorizontal();

            // ── 선택된 TMP 오브젝트 정보 ──
            EditorGUILayout.Space(10);
            DrawHeader("선택된 오브젝트");
            DrawSelectedTMPInfo();
        }

        private void DrawFontDropArea()
        {
            Rect dropRect = GUILayoutUtility.GetRect(0, 32, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "여기에 TMP_FontAsset을 드래그해서 등록", EditorStyles.helpBox);

            Event evt = Event.current;
            if (!dropRect.Contains(evt.mousePosition)) return;

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (Object obj in DragAndDrop.objectReferences)
                    {
                        if (obj is TMP_FontAsset fa) RegisterFont(fa);
                    }
                    evt.Use();
                }
            }
        }

        private void DrawRegisteredFontList()
        {
            swapScroll = EditorGUILayout.BeginScrollView(swapScroll, GUILayout.MaxHeight(300));

            for (int i = 0; i < registeredFontCaches.Count; i++)
            {
                var cache = registeredFontCaches[i];

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                // 아이콘
                Texture icon = cache.asset != null ? AssetPreview.GetMiniThumbnail(cache.asset) : null;
                if (icon != null)
                {
                    Rect iconRect = GUILayoutUtility.GetRect(24, 24, GUILayout.Width(24), GUILayout.Height(24));
                    GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                }

                // 이름
                if (!cache.exists) GUI.color = Color.red;
                GUILayout.Label(cache.content, GUILayout.ExpandWidth(true));
                GUI.color = Color.white;

                // ★ 기본 폰트로 지정
                bool isDefault = defaultFont != null && cache.asset == defaultFont;
                GUI.backgroundColor = isDefault ? Color.yellow : Color.white;
                if (GUILayout.Button(isDefault ? "★" : "☆", GUILayout.Width(24), GUILayout.Height(20)))
                {
                    if (isDefault)
                    {
                        defaultFont = null;
                        s_autoApplyFont = null;
                    }
                    else
                    {
                        defaultFont = cache.asset;
                        s_autoApplyFont = cache.asset;
                    }
                    SaveDefaultFont();
                }
                GUI.backgroundColor = Color.white;

                // 적용 버튼
                GUI.enabled = cache.exists;
                if (GUILayout.Button("적용", GUILayout.Width(40), GUILayout.Height(20)))
                {
                    ApplyFontToSelection(cache.asset);
                }
                GUI.enabled = true;

                // Ping
                if (GUILayout.Button("Ping", GUILayout.Width(40), GUILayout.Height(20)))
                {
                    if (cache.asset != null)
                    {
                        EditorGUIUtility.PingObject(cache.asset);
                        Selection.activeObject = cache.asset;
                    }
                }

                // 순서
                GUI.enabled = i > 0;
                if (GUILayout.Button("▲", GUILayout.Width(22), GUILayout.Height(20)))
                {
                    SwapFont(i, i - 1);
                    GUIUtility.ExitGUI();
                }
                GUI.enabled = i < registeredFontCaches.Count - 1;
                if (GUILayout.Button("▼", GUILayout.Width(22), GUILayout.Height(20)))
                {
                    SwapFont(i, i + 1);
                    GUIUtility.ExitGUI();
                }
                GUI.enabled = true;

                // 삭제
                if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(20)))
                {
                    registeredFontGuids.RemoveAt(i);
                    SaveRegisteredFonts();
                    needsFontCacheRebuild = true;
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSelectedTMPInfo()
        {
            var selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                EditorGUILayout.HelpBox("Hierarchy에서 TextMeshPro가 있는 오브젝트를 선택하세요.", MessageType.None);
                return;
            }

            int tmpCount = 0;
            TMP_FontAsset firstFont = null;
            bool mixed = false;

            foreach (var go in selected)
            {
                var tmps = go.GetComponentsInChildren<TMP_Text>(true);
                foreach (var t in tmps)
                {
                    tmpCount++;
                    if (firstFont == null) firstFont = t.font;
                    else if (t.font != firstFont) mixed = true;
                }
            }

            if (tmpCount == 0)
            {
                EditorGUILayout.HelpBox("선택된 오브젝트에 TMP_Text 컴포넌트가 없습니다.", MessageType.None);
                return;
            }

            string fontInfo = mixed ? "(혼합)" : (firstFont != null ? firstFont.name : "(없음)");
            EditorGUILayout.LabelField($"TMP 컴포넌트 {tmpCount}개  |  현재 폰트: {fontInfo}");
        }

        /// <summary>
        /// 선택된 오브젝트(및 자식)의 모든 TMP_Text에 폰트를 적용합니다.
        /// </summary>
        private void ApplyFontToSelection(TMP_FontAsset font)
        {
            if (font == null) return;

            var selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                EditorUtility.DisplayDialog("알림", "Hierarchy에서 오브젝트를 선택한 뒤 다시 시도하세요.", "확인");
                return;
            }

            int count = 0;
            Undo.SetCurrentGroupName("TMP Font Swap");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (var go in selected)
            {
                var tmps = go.GetComponentsInChildren<TMP_Text>(true);
                foreach (var t in tmps)
                {
                    Undo.RecordObject(t, "TMP Font Swap");
                    t.font = font;
                    EditorUtility.SetDirty(t);
                    count++;
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log($"[SWTools] '{font.name}' 폰트를 {count}개 TMP 컴포넌트에 적용했습니다.");
        }

        /// <summary>
        /// 씬 내 모든 TMP_Text에 폰트를 적용합니다.
        /// </summary>
        private void ApplyFontToAllInScene(TMP_FontAsset font)
        {
            if (font == null) return;

            if (!EditorUtility.DisplayDialog("확인",
                $"현재 씬의 모든 TextMeshPro 컴포넌트에 '{font.name}' 폰트를 적용하시겠습니까?",
                "적용", "취소"))
                return;

            var allTmps = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Undo.SetCurrentGroupName("TMP Font Apply All");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (var t in allTmps)
            {
                Undo.RecordObject(t, "TMP Font Apply All");
                t.font = font;
                EditorUtility.SetDirty(t);
            }

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log($"[SWTools] 씬 내 {allTmps.Length}개 TMP 컴포넌트에 '{font.name}' 적용 완료.");
        }

        private void RegisterFont(TMP_FontAsset font)
        {
            if (font == null) return;
            string path = AssetDatabase.GetAssetPath(font);
            if (string.IsNullOrEmpty(path)) return;

            string guid = AssetDatabase.AssetPathToGUID(path);
            if (registeredFontGuids.Contains(guid))
            {
                Debug.Log($"[SWTools] 이미 등록된 폰트: {font.name}");
                return;
            }

            registeredFontGuids.Add(guid);
            SaveRegisteredFonts();
            needsFontCacheRebuild = true;
        }

        private void AddSelectionAsFonts()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is TMP_FontAsset fa) RegisterFont(fa);
            }
        }

        private void CleanMissingFonts()
        {
            int before = registeredFontGuids.Count;
            registeredFontGuids.RemoveAll(guid =>
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                return string.IsNullOrEmpty(path);
            });
            int removed = before - registeredFontGuids.Count;
            SaveRegisteredFonts();
            needsFontCacheRebuild = true;
            Debug.Log($"[SWTools] 없는 폰트 {removed}개 정리됨.");
        }

        private void SwapFont(int from, int to)
        {
            string item = registeredFontGuids[from];
            registeredFontGuids.RemoveAt(from);
            registeredFontGuids.Insert(to, item);
            SaveRegisteredFonts();
            needsFontCacheRebuild = true;
        }

        private void RebuildFontCache()
        {
            registeredFontCaches.Clear();
            foreach (string guid in registeredFontGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                bool exists = !string.IsNullOrEmpty(path);
                TMP_FontAsset asset = exists ? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path) : null;
                string displayName = asset != null ? asset.name : "(없음)";

                registeredFontCaches.Add(new RegisteredFontCache
                {
                    guid = guid,
                    path = path,
                    asset = asset,
                    content = new GUIContent(displayName, path),
                    exists = exists && asset != null,
                });
            }
            needsFontCacheRebuild = false;
        }

        #region 저장 / 로드
        private void SaveDefaultFont()
        {
            if (defaultFont != null)
            {
                string path = AssetDatabase.GetAssetPath(defaultFont);
                defaultFontGuid = AssetDatabase.AssetPathToGUID(path);
            }
            else
            {
                defaultFontGuid = "";
            }
            EditorPrefs.SetString(GetProjectKey(DEFAULT_FONT_GUID_KEY), defaultFontGuid);
        }

        private void LoadDefaultFont()
        {
            defaultFontGuid = EditorPrefs.GetString(GetProjectKey(DEFAULT_FONT_GUID_KEY), "");
            if (!string.IsNullOrEmpty(defaultFontGuid))
            {
                string path = AssetDatabase.GUIDToAssetPath(defaultFontGuid);
                defaultFont = string.IsNullOrEmpty(path) ? null
                    : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            }
            else
            {
                defaultFont = null;
            }
            s_autoApplyFont = defaultFont;
        }

        private void SaveRegisteredFonts()
        {
            string joined = string.Join("|", registeredFontGuids);
            EditorPrefs.SetString(GetProjectKey(REGISTERED_FONTS_KEY), joined);
        }

        private void LoadRegisteredFonts()
        {
            string joined = EditorPrefs.GetString(GetProjectKey(REGISTERED_FONTS_KEY), "");
            registeredFontGuids.Clear();
            if (!string.IsNullOrEmpty(joined))
            {
                foreach (string g in joined.Split('|'))
                {
                    if (!string.IsNullOrEmpty(g)) registeredFontGuids.Add(g);
                }
            }
        }

        private string GetProjectKey(string key)
        {
            return $"{key}.{Application.dataPath.GetHashCode()}";
        }
        #endregion
        #endregion // Quick Swap 탭

        // =====================================================================
        //  Presets 탭
        // =====================================================================
        #region Presets 탭

        private void DrawPresetsTab()
        {
            DrawHeader("폰트 선택");

            if (registeredFontCaches.Count == 0 && needsFontCacheRebuild) RebuildFontCache();

            if (registeredFontCaches.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Quick Swap 탭에서 폰트를 먼저 등록하세요.\n등록된 폰트의 Material Preset을 여기서 확인할 수 있습니다.",
                    MessageType.Info);
                return;
            }

            // 폰트 선택 버튼 행
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < registeredFontCaches.Count; i++)
            {
                var cache = registeredFontCaches[i];
                if (!cache.exists) continue;

                bool isSelected = (presetSelectedFontIndex == i);
                GUI.backgroundColor = isSelected ? new Color(0.45f, 0.65f, 0.9f) : Color.white;

                if (GUILayout.Button(cache.asset.name, GUILayout.Height(24)))
                {
                    presetSelectedFontIndex = i;
                    RefreshPresets();
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            if (presetSelectedFontIndex < 0 || presetSelectedFontIndex >= registeredFontCaches.Count)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("위에서 폰트를 선택하면 해당 폰트의 Material Preset 목록이 표시됩니다.", MessageType.None);
                return;
            }

            var selectedCache = registeredFontCaches[presetSelectedFontIndex];
            if (!selectedCache.exists || selectedCache.asset == null) return;

            EditorGUILayout.Space(10);
            DrawHeader($"Material Presets - {selectedCache.asset.name}");

            // 폰트 기본 정보
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Font Asset", selectedCache.asset, typeof(TMP_FontAsset), false);
                EditorGUILayout.IntField("Atlas", selectedCache.asset.atlasWidth);
                EditorGUILayout.IntField("Glyphs",
                    selectedCache.asset.glyphTable != null ? selectedCache.asset.glyphTable.Count : 0);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            if (cachedPresets.Count == 0)
            {
                EditorGUILayout.HelpBox("이 폰트에 Material Preset이 없습니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"{cachedPresets.Count}개 Preset", EditorStyles.boldLabel);

            presetScroll = EditorGUILayout.BeginScrollView(presetScroll);

            foreach (var mat in cachedPresets)
            {
                if (mat == null) continue;

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                // 머티리얼 미리보기
                Texture preview = AssetPreview.GetAssetPreview(mat);
                if (preview != null)
                {
                    Rect previewRect = GUILayoutUtility.GetRect(36, 36, GUILayout.Width(36), GUILayout.Height(36));
                    GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit);
                }

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(mat.name, EditorStyles.boldLabel);

                // Shader 이름
                if (mat.shader != null)
                {
                    EditorGUILayout.LabelField($"Shader: {mat.shader.name}", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndVertical();

                // 적용 버튼
                if (GUILayout.Button("적용", GUILayout.Width(40), GUILayout.Height(28)))
                {
                    ApplyPresetToSelection(selectedCache.asset, mat);
                }
                if (GUILayout.Button("Ping", GUILayout.Width(40), GUILayout.Height(28)))
                {
                    EditorGUIUtility.PingObject(mat);
                    Selection.activeObject = mat;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 선택된 폰트의 Material Preset 목록을 갱신합니다.
        /// TMP는 같은 폴더에 동일 폰트를 참조하는 Material들을 Preset으로 취급합니다.
        /// </summary>
        private void RefreshPresets()
        {
            cachedPresets.Clear();
            if (presetSelectedFontIndex < 0 || presetSelectedFontIndex >= registeredFontCaches.Count)
                return;

            var cache = registeredFontCaches[presetSelectedFontIndex];
            if (!cache.exists || cache.asset == null) return;

            // TMP_FontAsset의 material은 기본 머티리얼
            Material baseMat = cache.asset.material;
            if (baseMat != null) cachedPresets.Add(baseMat);

            // 같은 폴더에서 동일 폰트를 참조하는 머티리얼 검색
            string fontPath = AssetDatabase.GetAssetPath(cache.asset);
            string folder = System.IO.Path.GetDirectoryName(fontPath);

            if (!string.IsNullOrEmpty(folder))
            {
                string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { folder });
                foreach (string guid in matGuids)
                {
                    string matPath = AssetDatabase.GUIDToAssetPath(guid);
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat == null || mat == baseMat) continue;

                    // TMP 머티리얼인지 확인: shader 이름에 TextMeshPro가 포함되어야 함
                    if (mat.shader == null) continue;
                    if (!mat.shader.name.Contains("TextMeshPro") && !mat.shader.name.Contains("TMP"))
                        continue;

                    cachedPresets.Add(mat);
                }
            }

            // 프로젝트 전체에서 추가 검색 (다른 폴더에 있을 수도 있으므로)
            string[] allMatGuids = AssetDatabase.FindAssets("t:Material");
            foreach (string guid in allMatGuids)
            {
                string matPath = AssetDatabase.GUIDToAssetPath(guid);

                // 이미 추가된 것은 스킵
                if (!string.IsNullOrEmpty(folder) && matPath.StartsWith(folder)) continue;

                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null || cachedPresets.Contains(mat)) continue;
                if (mat.shader == null) continue;
                if (!mat.shader.name.Contains("TextMeshPro") && !mat.shader.name.Contains("TMP"))
                    continue;

                // 머티리얼 이름이 폰트 이름을 포함하는지 확인 (TMP 규칙)
                if (mat.name.StartsWith(cache.asset.name))
                {
                    cachedPresets.Add(mat);
                }
            }
        }

        /// <summary>
        /// 선택된 오브젝트에 폰트와 Material Preset을 적용합니다.
        /// </summary>
        private void ApplyPresetToSelection(TMP_FontAsset font, Material preset)
        {
            var selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                EditorUtility.DisplayDialog("알림", "Hierarchy에서 오브젝트를 선택한 뒤 다시 시도하세요.", "확인");
                return;
            }

            int count = 0;
            Undo.SetCurrentGroupName("TMP Preset Apply");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (var go in selected)
            {
                var tmps = go.GetComponentsInChildren<TMP_Text>(true);
                foreach (var t in tmps)
                {
                    Undo.RecordObject(t, "TMP Preset Apply");
                    t.font = font;

                    // fontSharedMaterial이 preset과 호환되는지 확인 후 적용
                    if (preset != null && preset.shader == t.fontSharedMaterial?.shader)
                    {
                        t.fontSharedMaterial = preset;
                    }
                    else if (preset != null)
                    {
                        t.fontSharedMaterial = preset;
                    }

                    EditorUtility.SetDirty(t);
                    count++;
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log($"[SWTools] '{font.name}' + Preset '{preset.name}'을(를) {count}개 TMP에 적용했습니다.");
        }

        #endregion // Presets 탭

        // =====================================================================
        //  Browser 탭 (기존 기능)
        // =====================================================================
        #region Browser 탭

        private void RefreshBrowser()
        {
            browserEntries.Clear();
            totalAtlasBytes = 0;
            totalGlyphs = 0;

            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (asset == null) continue;

                BrowserEntry entry = new()
                {
                    asset = asset,
                    path = path,
                    name = asset.name,
                    atlasWidth = asset.atlasWidth,
                    atlasHeight = asset.atlasHeight,
                    glyphCount = asset.glyphTable != null ? asset.glyphTable.Count : 0,
                    characterCount = asset.characterTable != null ? asset.characterTable.Count : 0,
                    fallbackCount = asset.fallbackFontAssetTable != null ? asset.fallbackFontAssetTable.Count : 0,
                };
                entry.estimatedBytes = (long)entry.atlasWidth * entry.atlasHeight;

                browserEntries.Add(entry);
                totalAtlasBytes += entry.estimatedBytes;
                totalGlyphs += entry.glyphCount;
            }

            SortBrowser();
        }

        private void SortBrowser()
        {
            browserEntries.Sort((a, b) =>
            {
                int cmp = browserSortMode switch
                {
                    BrowserSortMode.Name => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase),
                    BrowserSortMode.AtlasSize => (a.atlasWidth * a.atlasHeight).CompareTo(b.atlasWidth * b.atlasHeight),
                    BrowserSortMode.GlyphCount => a.glyphCount.CompareTo(b.glyphCount),
                    BrowserSortMode.AtlasMemory => a.estimatedBytes.CompareTo(b.estimatedBytes),
                    _ => 0,
                };
                return browserSortDescending ? -cmp : cmp;
            });
        }

        private void DrawBrowserTab()
        {
            // 툴바
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                RefreshBrowser();
            }

            GUILayout.Space(5);
            GUILayout.Label("정렬:", GUILayout.Width(40));
            BrowserSortMode newMode = (BrowserSortMode)EditorGUILayout.EnumPopup(browserSortMode,
                EditorStyles.toolbarPopup, GUILayout.Width(100));
            if (newMode != browserSortMode)
            {
                browserSortMode = newMode;
                SortBrowser();
            }

            bool newDesc = GUILayout.Toggle(browserSortDescending, "내림차순",
                EditorStyles.toolbarButton, GUILayout.Width(70));
            if (newDesc != browserSortDescending)
            {
                browserSortDescending = newDesc;
                SortBrowser();
            }

            GUILayout.FlexibleSpace();

            GUILayout.Label("검색:", GUILayout.Width(40));
            browserSearchFilter = GUILayout.TextField(browserSearchFilter,
                EditorStyles.toolbarSearchField, GUILayout.Width(150));

            EditorGUILayout.EndHorizontal();

            // Summary
            DrawHeader("Summary");
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Font Asset 수", browserEntries.Count);
                EditorGUILayout.IntField("총 글리프 수", totalGlyphs);
                EditorGUILayout.LongField("추정 총 Atlas 메모리 (KB)", totalAtlasBytes / 1024);
            }

            EditorGUILayout.Space(5);

            // 리스트
            browserScroll = EditorGUILayout.BeginScrollView(browserScroll);

            string filter = browserSearchFilter.Trim().ToLowerInvariant();

            foreach (BrowserEntry entry in browserEntries)
            {
                if (!string.IsNullOrEmpty(filter) &&
                    !entry.name.ToLowerInvariant().Contains(filter))
                {
                    continue;
                }

                DrawBrowserEntry(entry);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawBrowserEntry(BrowserEntry entry)
        {
            if (!browserFoldouts.TryGetValue(entry.path, out bool open))
            {
                open = false;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            string label = $"{entry.name}   ({entry.atlasWidth}x{entry.atlasHeight}, glyphs: {entry.glyphCount})";
            open = EditorGUILayout.Foldout(open, label, true);
            browserFoldouts[entry.path] = open;

            if (GUILayout.Button("등록", GUILayout.Width(40), GUILayout.Height(18)))
            {
                RegisterFont(entry.asset);
            }
            if (GUILayout.Button("Ping", GUILayout.Width(40), GUILayout.Height(18)))
            {
                EditorGUIUtility.PingObject(entry.asset);
                Selection.activeObject = entry.asset;
            }
            EditorGUILayout.EndHorizontal();

            if (open)
            {
                EditorGUI.indentLevel++;
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("Asset", entry.asset, typeof(TMP_FontAsset), false);
                    EditorGUILayout.TextField("Path", entry.path);
                    EditorGUILayout.IntField("Atlas Width", entry.atlasWidth);
                    EditorGUILayout.IntField("Atlas Height", entry.atlasHeight);
                    EditorGUILayout.IntField("Glyph Count", entry.glyphCount);
                    EditorGUILayout.IntField("Character Count", entry.characterCount);
                    EditorGUILayout.LongField("Est. Memory (KB)", entry.estimatedBytes / 1024);
                    EditorGUILayout.IntField("Fallback Count", entry.fallbackCount);
                }

                if (entry.fallbackCount > 0)
                {
                    EditorGUILayout.LabelField("Fallback Chain", EditorStyles.miniBoldLabel);
                    DrawFallbackChain(entry.asset, 0, new HashSet<TMP_FontAsset>());
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFallbackChain(TMP_FontAsset asset, int depth, HashSet<TMP_FontAsset> visited)
        {
            if (asset == null || depth > 10) return;
            if (!visited.Add(asset))
            {
                EditorGUILayout.LabelField(new string(' ', depth * 2) + "↳ (순환 참조)");
                return;
            }

            if (asset.fallbackFontAssetTable == null) return;

            foreach (TMP_FontAsset fb in asset.fallbackFontAssetTable)
            {
                if (fb == null) continue;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(depth * 12 + 15);
                GUILayout.Label($"↳ {fb.name} ({fb.atlasWidth}x{fb.atlasHeight})");
                if (GUILayout.Button("Ping", GUILayout.Width(40), GUILayout.Height(16)))
                {
                    EditorGUIUtility.PingObject(fb);
                }
                EditorGUILayout.EndHorizontal();

                DrawFallbackChain(fb, depth + 1, visited);
            }
        }

        #endregion // Browser 탭

        // =====================================================================
        //  공통
        // =====================================================================
        private void DrawHeader(string title)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 1f));
            EditorGUILayout.Space(3);
        }
    }
}
#endif // !DISABLE_SW_TMP_MANAGER