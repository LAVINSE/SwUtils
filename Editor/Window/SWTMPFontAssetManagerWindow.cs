#if !SW_TMP_MANAGER_DISABLE
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

#if TMP_PRESENT || true
using TMPro;
#endif

using SW.EditorTools.Util;

using SW.Util;

namespace SW.EditorTools.Window
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
        private static readonly string[] tabNames = { "Quick Swap", "Presets", "Browser", "성능" };
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

        /// <summary>
        /// 등록된 TMP 폰트의 표시용 캐시 정보입니다.
        /// </summary>
        private class RegisteredFontCache
        {
            /// <summary>폰트 에셋 GUID입니다.</summary>
            public string guid;
            /// <summary>폰트 에셋 경로입니다.</summary>
            public string path;
            /// <summary>캐시된 TMP 폰트 에셋입니다.</summary>
            public TMP_FontAsset asset;
            /// <summary>목록에 표시할 GUIContent입니다.</summary>
            public GUIContent content;
            /// <summary>폰트 에셋이 현재 존재하는지 여부입니다.</summary>
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

        /// <summary>
        /// TMP 폰트 브라우저에 표시할 폰트 요약 정보입니다.
        /// </summary>
        private class BrowserEntry
        {
            /// <summary>TMP 폰트 에셋입니다.</summary>
            public TMP_FontAsset asset;
            /// <summary>폰트 에셋 경로입니다.</summary>
            public string path;
            /// <summary>폰트 에셋 이름입니다.</summary>
            public string name;
            /// <summary>아틀라스 텍스처 너비입니다.</summary>
            public int atlasWidth;
            /// <summary>아틀라스 텍스처 높이입니다.</summary>
            public int atlasHeight;
            /// <summary>글리프 개수입니다.</summary>
            public int glyphCount;
            /// <summary>문자 개수입니다.</summary>
            public int characterCount;
            /// <summary>예상 아틀라스 메모리 사용량입니다.</summary>
            public long estimatedBytes;
            /// <summary>Fallback 폰트 개수입니다.</summary>
            public int fallbackCount;
        }
        #endregion

        #region 필드 - 성능 탭
        private TMP_FontAsset performanceFont;
        private Vector2 performanceScroll;
        private bool performanceIncludeFallbacks = true;

        /// <summary>
        /// TMP 폰트 성능 탭에서 표시할 분석 결과입니다.
        /// </summary>
        private class PerformanceSnapshot
        {
            /// <summary>분석 대상 TMP 폰트 에셋입니다.</summary>
            public TMP_FontAsset asset;
            /// <summary>분석 대상 TMP 폰트 에셋 경로입니다.</summary>
            public string path;
            /// <summary>아틀라스 텍스처 개수입니다.</summary>
            public int atlasTextureCount;
            /// <summary>아틀라스 전체 픽셀 면적입니다.</summary>
            public long atlasPixels;
            /// <summary>아틀라스 예상 메모리 사용량입니다.</summary>
            public long estimatedAtlasBytes;
            /// <summary>에디터가 계산한 런타임 텍스처 메모리 사용량입니다.</summary>
            public long runtimeTextureBytes;
            /// <summary>에디터가 계산한 저장 텍스처 메모리 사용량입니다.</summary>
            public long storageTextureBytes;
            /// <summary>글리프 개수입니다.</summary>
            public int glyphCount;
            /// <summary>문자 개수입니다.</summary>
            public int characterCount;
            /// <summary>직접 폴백 폰트 개수입니다.</summary>
            public int directFallbackCount;
            /// <summary>전체 폴백 폰트 개수입니다.</summary>
            public int totalFallbackCount;
            /// <summary>폴백 체인의 최대 깊이입니다.</summary>
            public int fallbackDepth;
            /// <summary>동적 아틀라스 사용 여부입니다.</summary>
            public bool isDynamic;
            /// <summary>같은 폴더에서 찾은 머티리얼 프리셋 개수입니다.</summary>
            public int materialPresetCount;
        }
        #endregion

        // Hierarchy 자동 적용용 콜백 (static)
        private static TMP_FontAsset s_autoApplyFont;

        [InitializeOnLoadMethod]
        private static void RegisterHierarchyCallback()
        {
            ObjectFactory.componentWasAdded -= OnComponentAdded;
            ObjectFactory.componentWasAdded += OnComponentAdded;
            LoadStaticDefaultFont();
        }

        private static void LoadStaticDefaultFont()
        {
            string guid = EditorPrefs.GetString(SWEditorUtils.GetProjectKey(DEFAULT_FONT_GUID_KEY), "");
            if (string.IsNullOrEmpty(guid)) { s_autoApplyFont = null; return; }

            s_autoApplyFont = SWEditorUtils.LoadAssetFromGuid<TMP_FontAsset>(guid);
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


        /// <summary>
        /// TMP Font Asset Manager 창을 엽니다.
        /// </summary>
        [MenuItem("SWTools/Utils/Asset/TMP Font Asset Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<SWTMPFontAssetManagerWindow>();
            SWEditorUtils.SetupWindow(window, "SW TMP Fonts", "d_Text Icon", 420, 450);
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
            selectedTab = SWEditorUtils.DrawTabBar(selectedTab, tabNames);

            switch (selectedTab)
            {
                case 0: DrawQuickSwapTab(); break;
                case 1: DrawPresetsTab(); break;
                case 2: DrawBrowserTab(); break;
                case 3: DrawPerformanceTab(); break;
            }
        }

        //  Quick Swap 탭
        #region Quick Swap 탭

        private void DrawQuickSwapTab()
        {
            SWEditorUtils.DrawHeader("기본 폰트 (자동 적용)");

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

            SWEditorUtils.DrawHeader("등록된 폰트");

            // SWEditorUtils 드래그 앤 드롭 사용
            var droppedFonts = SWEditorUtils.DrawDropArea<TMP_FontAsset>("여기에 TMP_FontAsset을 드래그해서 등록");
            if (droppedFonts != null)
            {
                foreach (var fa in droppedFonts) RegisterFont(fa);
            }

            EditorGUILayout.Space(3);

            if (needsFontCacheRebuild) RebuildFontCache();

            if (registeredFontCaches.Count == 0)
            {
                SWEditorUtils.DrawEmptyNotice(
                    "등록된 폰트가 없습니다.\n위 영역에 TMP_FontAsset을 드래그하거나 '선택 항목 추가' 버튼을 사용하세요.");
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

            EditorGUILayout.Space(10);
            SWEditorUtils.DrawHeader("선택된 오브젝트");
            DrawSelectedTMPInfo();
        }

        private void DrawRegisteredFontList()
        {
            swapScroll = EditorGUILayout.BeginScrollView(swapScroll, GUILayout.MaxHeight(300));

            for (int i = 0; i < registeredFontCaches.Count; i++)
            {
                var cache = registeredFontCaches[i];

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                // 아이콘
                Texture icon = SWEditorUtils.GetAssetIcon(cache.asset);
                if (icon != null)
                {
                    Rect iconRect = GUILayoutUtility.GetRect(24, 24, GUILayout.Width(24), GUILayout.Height(24));
                    GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                }

                // 이름
                using (new SWEditorUtils.GUIColorScope(cache.exists ? Color.white : SWEditorUtils.ErrorColor))
                {
                    GUILayout.Label(cache.content, GUILayout.ExpandWidth(true));
                }

                // ★ 기본 폰트로 지정
                bool isDefault = defaultFont != null && cache.asset == defaultFont;
                using (new SWEditorUtils.GUIBgColorScope(isDefault ? Color.yellow : Color.white))
                {
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
                }

                // 적용 버튼
                using (new SWEditorUtils.GUIEnabledScope(cache.exists))
                {
                    if (SWEditorUtils.SmallButton("적용", 40f))
                    {
                        ApplyFontToSelection(cache.asset);
                    }
                }

                // Ping
                if (SWEditorUtils.SmallButton("Ping", 40f))
                {
                    SWEditorUtils.PingAndSelect(cache.asset);
                }

                // 순서
                using (new SWEditorUtils.GUIEnabledScope(i > 0))
                {
                    if (GUILayout.Button("▲", GUILayout.Width(22), GUILayout.Height(20)))
                    {
                        SwapFont(i, i - 1);
                        GUIUtility.ExitGUI();
                    }
                }
                using (new SWEditorUtils.GUIEnabledScope(i < registeredFontCaches.Count - 1))
                {
                    if (GUILayout.Button("▼", GUILayout.Width(22), GUILayout.Height(20)))
                    {
                        SwapFont(i, i + 1);
                        GUIUtility.ExitGUI();
                    }
                }

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
                SWEditorUtils.DrawEmptyNotice("Hierarchy에서 TextMeshPro가 있는 오브젝트를 선택하세요.", MessageType.None);
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
                SWEditorUtils.DrawEmptyNotice("선택된 오브젝트에 TMP_Text 컴포넌트가 없습니다.", MessageType.None);
                return;
            }

            string fontInfo = mixed ? "(혼합)" : (firstFont != null ? firstFont.name : "(없음)");
            EditorGUILayout.LabelField($"TMP 컴포넌트 {tmpCount}개  |  현재 폰트: {fontInfo}");
        }

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
            SWLog.Log($"[SWTools] '{font.name}' 폰트를 {count}개 TMP 컴포넌트에 적용했습니다.");
        }

        private void ApplyFontToAllInScene(TMP_FontAsset font)
        {
            if (font == null) return;

            if (!EditorUtility.DisplayDialog("확인",
                $"현재 씬의 모든 TextMeshPro 컴포넌트에 '{font.name}' 폰트를 적용하시겠습니까?",
                "적용", "취소"))
                return;

#if UNITY_6000_4_OR_NEWER
            var allTmps = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);
#else
            var allTmps = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#endif

            Undo.SetCurrentGroupName("TMP Font Apply All");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (var t in allTmps)
            {
                Undo.RecordObject(t, "TMP Font Apply All");
                t.font = font;
                EditorUtility.SetDirty(t);
            }

            Undo.CollapseUndoOperations(undoGroup);
            SWLog.Log($"[SWTools] 씬 내 {allTmps.Length}개 TMP 컴포넌트에 '{font.name}' 적용 완료.");
        }

        private void RegisterFont(TMP_FontAsset font)
        {
            if (font == null) return;
            string guid = SWEditorUtils.GetAssetGuid(font);
            if (string.IsNullOrEmpty(guid)) return;

            if (registeredFontGuids.Contains(guid))
            {
                SWLog.Log($"[SWTools] 이미 등록된 폰트: {font.name}");
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
            SWLog.Log($"[SWTools] 없는 폰트 {removed}개 정리됨.");
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
                TMP_FontAsset asset = SWEditorUtils.LoadAssetFromGuid<TMP_FontAsset>(guid);
                string path = AssetDatabase.GUIDToAssetPath(guid);
                bool exists = asset != null;
                string displayName = exists ? asset.name : "(없음)";

                registeredFontCaches.Add(new RegisteredFontCache
                {
                    guid = guid,
                    path = path,
                    asset = asset,
                    content = new GUIContent(displayName, path),
                    exists = exists,
                });
            }
            needsFontCacheRebuild = false;
        }

        #region 저장 / 로드
        private void SaveDefaultFont()
        {
            defaultFontGuid = defaultFont != null ? SWEditorUtils.GetAssetGuid(defaultFont) ?? "" : "";
            SWEditorUtils.SavePref(DEFAULT_FONT_GUID_KEY, defaultFontGuid);
        }

        private void LoadDefaultFont()
        {
            defaultFontGuid = SWEditorUtils.LoadPref(DEFAULT_FONT_GUID_KEY, "");
            defaultFont = !string.IsNullOrEmpty(defaultFontGuid)
                ? SWEditorUtils.LoadAssetFromGuid<TMP_FontAsset>(defaultFontGuid)
                : null;
            s_autoApplyFont = defaultFont;
        }

        private void SaveRegisteredFonts()
        {
            SWEditorUtils.SaveList(REGISTERED_FONTS_KEY, registeredFontGuids);
        }

        private void LoadRegisteredFonts()
        {
            registeredFontGuids = SWEditorUtils.LoadList(REGISTERED_FONTS_KEY);
        }
        #endregion
        #endregion // Quick Swap 탭

        //  Presets 탭
        #region Presets 탭

        private void DrawPresetsTab()
        {
            SWEditorUtils.DrawHeader("폰트 선택");

            if (registeredFontCaches.Count == 0 && needsFontCacheRebuild) RebuildFontCache();

            if (registeredFontCaches.Count == 0)
            {
                SWEditorUtils.DrawEmptyNotice(
                    "Quick Swap 탭에서 폰트를 먼저 등록하세요.\n등록된 폰트의 Material Preset을 여기서 확인할 수 있습니다.");
                return;
            }

            // 폰트 선택 버튼 행
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < registeredFontCaches.Count; i++)
            {
                var cache = registeredFontCaches[i];
                if (!cache.exists) continue;

                bool isSelected = (presetSelectedFontIndex == i);
                using (new SWEditorUtils.GUIBgColorScope(isSelected ? new Color(0.45f, 0.65f, 0.9f) : Color.white))
                {
                    if (GUILayout.Button(cache.asset.name, GUILayout.Height(24)))
                    {
                        presetSelectedFontIndex = i;
                        RefreshPresets();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            if (presetSelectedFontIndex < 0 || presetSelectedFontIndex >= registeredFontCaches.Count)
            {
                EditorGUILayout.Space(10);
                SWEditorUtils.DrawEmptyNotice("위에서 폰트를 선택하면 해당 폰트의 Material Preset 목록이 표시됩니다.", MessageType.None);
                return;
            }

            var selectedCache = registeredFontCaches[presetSelectedFontIndex];
            if (!selectedCache.exists || selectedCache.asset == null) return;

            EditorGUILayout.Space(10);
            SWEditorUtils.DrawHeader($"Material Presets - {selectedCache.asset.name}");

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
                SWEditorUtils.DrawEmptyNotice("이 폰트에 Material Preset이 없습니다.");
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
                if (mat.shader != null)
                {
                    EditorGUILayout.LabelField($"Shader: {mat.shader.name}", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndVertical();

                if (GUILayout.Button("적용", GUILayout.Width(40), GUILayout.Height(28)))
                {
                    ApplyPresetToSelection(selectedCache.asset, mat);
                }
                if (GUILayout.Button("Ping", GUILayout.Width(40), GUILayout.Height(28)))
                {
                    SWEditorUtils.PingAndSelect(mat);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void RefreshPresets()
        {
            cachedPresets.Clear();
            if (presetSelectedFontIndex < 0 || presetSelectedFontIndex >= registeredFontCaches.Count)
                return;

            var cache = registeredFontCaches[presetSelectedFontIndex];
            if (!cache.exists || cache.asset == null) return;

            Material baseMat = cache.asset.material;
            if (baseMat != null) cachedPresets.Add(baseMat);

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
                    if (mat.shader == null) continue;
                    if (!mat.shader.name.Contains("TextMeshPro") && !mat.shader.name.Contains("TMP"))
                        continue;
                    cachedPresets.Add(mat);
                }
            }

            string[] allMatGuids = AssetDatabase.FindAssets("t:Material");
            foreach (string guid in allMatGuids)
            {
                string matPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(folder) && matPath.StartsWith(folder)) continue;

                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null || cachedPresets.Contains(mat)) continue;
                if (mat.shader == null) continue;
                if (!mat.shader.name.Contains("TextMeshPro") && !mat.shader.name.Contains("TMP"))
                    continue;

                if (mat.name.StartsWith(cache.asset.name))
                {
                    cachedPresets.Add(mat);
                }
            }
        }

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

                    if (preset != null)
                    {
                        t.fontSharedMaterial = preset;
                    }

                    EditorUtility.SetDirty(t);
                    count++;
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
            SWLog.Log($"[SWTools] '{font.name}' + Preset '{preset.name}'을(를) {count}개 TMP에 적용했습니다.");
        }

        #endregion // Presets 탭

        //  Browser 탭
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
            browserSearchFilter = SWEditorUtils.DrawSearchField(browserSearchFilter, 150f);

            EditorGUILayout.EndHorizontal();

            // Summary
            SWEditorUtils.DrawHeader("Summary");
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Font Asset 수", browserEntries.Count);
                EditorGUILayout.IntField("총 글리프 수", totalGlyphs);
                EditorGUILayout.TextField("추정 총 Atlas 메모리", SWEditorUtils.FormatBytes(totalAtlasBytes));
            }

            EditorGUILayout.Space(5);

            // 리스트
            browserScroll = EditorGUILayout.BeginScrollView(browserScroll);

            foreach (BrowserEntry entry in browserEntries)
            {
                if (!SWEditorUtils.MatchesFilter(entry.name, browserSearchFilter))
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

            if (SWEditorUtils.SmallButton("등록", 40f))
            {
                RegisterFont(entry.asset);
            }
            if (SWEditorUtils.SmallButton("Ping", 40f))
            {
                SWEditorUtils.PingAndSelect(entry.asset);
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
                    EditorGUILayout.TextField("Est. Memory", SWEditorUtils.FormatBytes(entry.estimatedBytes));
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
                if (SWEditorUtils.SmallButton("Ping", 40f))
                {
                    EditorGUIUtility.PingObject(fb);
                }
                EditorGUILayout.EndHorizontal();

                DrawFallbackChain(fb, depth + 1, visited);
            }
        }

        #endregion // Browser 탭
        //  성능 탭
        #region 성능 탭

        private void DrawPerformanceTab()
        {
            SWEditorUtils.DrawHeader("TMP 성능 확인");
            EditorGUILayout.HelpBox(
                "TMP_FontAsset을 넣으면 아틀라스 메모리, 글리프, 문자, 폴백 체인, 머티리얼 프리셋 비용을 확인합니다.",
                MessageType.Info);

            var droppedFonts = SWEditorUtils.DrawDropArea<TMP_FontAsset>("여기에 TMP_FontAsset을 드래그해서 성능 확인");
            if (droppedFonts != null && droppedFonts.Count > 0)
            {
                performanceFont = droppedFonts[0];
            }

            EditorGUI.BeginChangeCheck();
            performanceFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
                "TMP Font Asset", performanceFont, typeof(TMP_FontAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                GUI.FocusControl(null);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("선택 에셋 사용", GUILayout.Height(22)))
            {
                UseSelectedFontForPerformance();
            }
            using (new SWEditorUtils.GUIEnabledScope(defaultFont != null))
            {
                if (GUILayout.Button("기본 폰트 사용", GUILayout.Height(22)))
                {
                    performanceFont = defaultFont;
                }
            }
            if (GUILayout.Button("비우기", GUILayout.Height(22)))
            {
                performanceFont = null;
            }
            EditorGUILayout.EndHorizontal();

            performanceIncludeFallbacks = EditorGUILayout.ToggleLeft(
                "폴백 체인 포함", performanceIncludeFallbacks);

            EditorGUILayout.Space(6);

            if (performanceFont == null)
            {
                SWEditorUtils.DrawEmptyNotice("성능을 확인할 TMP_FontAsset을 넣어주세요.", MessageType.None);
                return;
            }

            PerformanceSnapshot snapshot = CreatePerformanceSnapshot(performanceFont, performanceIncludeFallbacks);

            performanceScroll = EditorGUILayout.BeginScrollView(performanceScroll);
            DrawPerformanceSummary(snapshot);
            EditorGUILayout.Space(8);
            DrawPerformanceAdvice(snapshot);
            EditorGUILayout.EndScrollView();
        }

        private void UseSelectedFontForPerformance()
        {
            if (Selection.activeObject is TMP_FontAsset selectedFont)
            {
                performanceFont = selectedFont;
                return;
            }

            if (Selection.activeGameObject == null) return;

            TMP_Text selectedText = Selection.activeGameObject.GetComponentInChildren<TMP_Text>(true);
            if (selectedText != null)
            {
                performanceFont = selectedText.font;
            }
        }

        private PerformanceSnapshot CreatePerformanceSnapshot(TMP_FontAsset asset, bool includeFallbacks)
        {
            List<Texture2D> atlasTextures = GetAtlasTextures(asset);
            long atlasPixels = 0;
            long runtimeTextureBytes = 0;
            long storageTextureBytes = 0;

            foreach (Texture2D texture in atlasTextures)
            {
                if (texture == null) continue;

                atlasPixels += (long)texture.width * texture.height;
                runtimeTextureBytes += GetRuntimeTextureMemoryBytes(texture);
                storageTextureBytes += GetStorageTextureMemoryBytes(texture);
            }

            FallbackStats fallbackStats = includeFallbacks
                ? CalculateFallbackStats(asset, new HashSet<TMP_FontAsset>(), 0)
                : new FallbackStats();

            return new PerformanceSnapshot
            {
                asset = asset,
                path = AssetDatabase.GetAssetPath(asset),
                atlasTextureCount = atlasTextures.Count,
                atlasPixels = atlasPixels,
                estimatedAtlasBytes = atlasPixels * 4L,
                runtimeTextureBytes = runtimeTextureBytes,
                storageTextureBytes = storageTextureBytes,
                glyphCount = asset.glyphTable != null ? asset.glyphTable.Count : 0,
                characterCount = asset.characterTable != null ? asset.characterTable.Count : 0,
                directFallbackCount = asset.fallbackFontAssetTable != null ? asset.fallbackFontAssetTable.Count : 0,
                totalFallbackCount = fallbackStats.count,
                fallbackDepth = fallbackStats.depth,
                isDynamic = asset.atlasPopulationMode == AtlasPopulationMode.Dynamic,
                materialPresetCount = CountMaterialPresets(asset),
            };
        }

        private long GetRuntimeTextureMemoryBytes(Texture2D texture)
        {
            if (texture == null) return 0L;

            long runtimeTextureBytes = Profiler.GetRuntimeMemorySizeLong(texture);
            return runtimeTextureBytes > 0L ? runtimeTextureBytes : EstimateTextureMemoryBytes(texture);
        }

        private long GetStorageTextureMemoryBytes(Texture2D texture)
        {
            if (texture == null) return 0L;

            string texturePath = AssetDatabase.GetAssetPath(texture);
            if (!string.IsNullOrEmpty(texturePath))
            {
                FileInfo textureFile = new FileInfo(texturePath);
                if (textureFile.Exists)
                {
                    return textureFile.Length;
                }
            }

            return EstimateTextureMemoryBytes(texture);
        }

        private long EstimateTextureMemoryBytes(Texture2D texture)
        {
            if (texture == null) return 0L;

            return (long)texture.width * texture.height * GetTextureFormatBytesPerPixel(texture.format);
        }

        private int GetTextureFormatBytesPerPixel(TextureFormat textureFormat)
        {
            switch (textureFormat.ToString())
            {
                case "Alpha8":
                case "R8":
                    return 1;
                case "R16":
                case "RG16":
                case "RGB565":
                case "RGBA4444":
                    return 2;
                case "RGB24":
                    return 3;
                case "RGBAHalf":
                    return 8;
                case "RGBAFloat":
                    return 16;
                case "RGBA32":
                case "ARGB32":
                case "BGRA32":
                default:
                    return 4;
            }
        }

        private List<Texture2D> GetAtlasTextures(TMP_FontAsset asset)
        {
            List<Texture2D> textures = new();
            if (asset == null) return textures;

            if (asset.atlasTextures != null)
            {
                foreach (Texture2D texture in asset.atlasTextures)
                {
                    if (texture != null && !textures.Contains(texture))
                    {
                        textures.Add(texture);
                    }
                }
            }

            if (asset.atlasTexture != null && !textures.Contains(asset.atlasTexture))
            {
                textures.Add(asset.atlasTexture);
            }

            return textures;
        }

        /// <summary>
        /// 대체 폰트 트리의 통계입니다.
        /// </summary>
        private struct FallbackStats
        {
            /// <summary>발견한 대체 폰트 수입니다.</summary>
            public int count;
            /// <summary>가장 깊은 대체 폰트 단계입니다.</summary>
            public int depth;
        }

        private FallbackStats CalculateFallbackStats(TMP_FontAsset asset, HashSet<TMP_FontAsset> visited, int depth)
        {
            if (asset == null || asset.fallbackFontAssetTable == null || depth > 10)
            {
                return new FallbackStats();
            }

            FallbackStats result = new();
            foreach (TMP_FontAsset fallback in asset.fallbackFontAssetTable)
            {
                if (fallback == null || !visited.Add(fallback)) continue;

                result.count++;
                result.depth = Mathf.Max(result.depth, depth + 1);

                FallbackStats child = CalculateFallbackStats(fallback, visited, depth + 1);
                result.count += child.count;
                result.depth = Mathf.Max(result.depth, child.depth);
            }

            return result;
        }

        private int CountMaterialPresets(TMP_FontAsset asset)
        {
            if (asset == null) return 0;

            int count = 0;
            Material baseMaterial = asset.material;
            if (baseMaterial != null) count++;

            string fontPath = AssetDatabase.GetAssetPath(asset);
            string folder = System.IO.Path.GetDirectoryName(fontPath);
            if (string.IsNullOrEmpty(folder)) return count;

            string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { folder });
            foreach (string guid in materialGuids)
            {
                string materialPath = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null || material == baseMaterial || material.shader == null) continue;
                if (!material.shader.name.Contains("TextMeshPro") && !material.shader.name.Contains("TMP")) continue;

                count++;
            }

            return count;
        }

        private void DrawPerformanceSummary(PerformanceSnapshot snapshot)
        {
            SWEditorUtils.DrawHeader("분석 결과");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Font Asset", snapshot.asset, typeof(TMP_FontAsset), false);
                EditorGUILayout.TextField("Path", snapshot.path);
                EditorGUILayout.TextField("Atlas Mode", snapshot.isDynamic ? "Dynamic" : "Static");
                EditorGUILayout.IntField("Atlas Texture Count", snapshot.atlasTextureCount);
                EditorGUILayout.TextField("Atlas Pixels", snapshot.atlasPixels.ToString("N0"));
                EditorGUILayout.TextField("Runtime Texture Memory", SWEditorUtils.FormatBytes(snapshot.runtimeTextureBytes));
                EditorGUILayout.TextField("Storage Texture Memory", SWEditorUtils.FormatBytes(snapshot.storageTextureBytes));
                EditorGUILayout.TextField("Estimated RGBA32 Memory", SWEditorUtils.FormatBytes(snapshot.estimatedAtlasBytes));
                EditorGUILayout.IntField("Glyph Count", snapshot.glyphCount);
                EditorGUILayout.IntField("Character Count", snapshot.characterCount);
                EditorGUILayout.IntField("Direct Fallback Count", snapshot.directFallbackCount);
                EditorGUILayout.IntField("Total Fallback Count", snapshot.totalFallbackCount);
                EditorGUILayout.IntField("Fallback Depth", snapshot.fallbackDepth);
                EditorGUILayout.IntField("Material Preset Count", snapshot.materialPresetCount);
            }
        }

        private void DrawPerformanceAdvice(PerformanceSnapshot snapshot)
        {
            SWEditorUtils.DrawHeader("점검 항목");

            bool hasWarning = false;
            hasWarning |= DrawPerformanceWarning(
                snapshot.runtimeTextureBytes >= 16L * 1024L * 1024L,
                "아틀라스 런타임 메모리가 16 MB 이상입니다. 모바일 대상이면 아틀라스 크기와 텍스처 개수를 줄이는 것을 검토하세요.");
            hasWarning |= DrawPerformanceWarning(
                snapshot.glyphCount >= 4000,
                "글리프가 4,000개 이상입니다. 실제 사용하는 문자 범위만 포함하면 로딩과 메모리 비용을 줄일 수 있습니다.");
            hasWarning |= DrawPerformanceWarning(
                snapshot.totalFallbackCount >= 6,
                "폴백 폰트가 많습니다. 폴백 체인이 길면 누락 문자 탐색 비용과 메모리 사용량이 함께 늘어날 수 있습니다.");
            hasWarning |= DrawPerformanceWarning(
                snapshot.fallbackDepth >= 3,
                "폴백 깊이가 3단계 이상입니다. 자주 쓰는 문자는 상위 폰트에 배치하는 것이 좋습니다.");
            hasWarning |= DrawPerformanceWarning(
                snapshot.isDynamic,
                "동적 아틀라스 폰트입니다. 런타임에 글리프가 추가되면 순간적인 텍스처 갱신 비용이 발생할 수 있습니다.");
            hasWarning |= DrawPerformanceWarning(
                snapshot.materialPresetCount >= 8,
                "머티리얼 프리셋이 많습니다. 서로 다른 프리셋을 동시에 많이 쓰면 드로우콜이 늘어날 수 있습니다.");

            if (!hasWarning)
            {
                EditorGUILayout.HelpBox("큰 성능 위험 신호가 보이지 않습니다.", MessageType.Info);
            }
        }

        private bool DrawPerformanceWarning(bool condition, string message)
        {
            if (!condition) return false;

            EditorGUILayout.HelpBox(message, MessageType.Warning);
            return true;
        }

        #endregion // 성능 탭
    }
}
#endif // !SW_TMP_MANAGER_DISABLE
