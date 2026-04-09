#if !DISABLE_SW_TMP_MANAGER
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if TMP_PRESENT || true
using TMPro;
#endif

namespace SWTools
{
    /// <summary>
    /// 프로젝트 내 TMP_FontAsset을 모두 찾아 Atlas 사이즈, 글리프 수,
    /// Fallback 체인, Character Set 정보를 요약해주는 창입니다.
    /// 각 항목을 확장하면 Fallback 트리와 메모리 사용량을 확인할 수 있습니다.
    /// </summary>
    public class SWTMPFontAssetManagerWindow : EditorWindow
    {
        #region 필드
        private Vector2 scrollPosition;

        private List<FontAssetEntry> entries = new();
        private string searchFilter = "";
        private SortMode sortMode = SortMode.Name;
        private bool sortDescending = false;

        private readonly Dictionary<string, bool> foldoutStates = new();

        private long totalAtlasBytes;
        private int totalGlyphs;

        private enum SortMode
        {
            Name,
            AtlasSize,
            GlyphCount,
            AtlasMemory,
        }

        private class FontAssetEntry
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
        #endregion // 필드

        [MenuItem("SWTools/TMP Font Asset Manager")]
        public static void ShowWindow()
        {
            SWTMPFontAssetManagerWindow window = GetWindow<SWTMPFontAssetManagerWindow>();
            window.titleContent = new GUIContent("SW TMP Fonts",
                EditorGUIUtility.FindTexture("d_Text Icon"));
            window.minSize = new Vector2(400, 400);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshFontAssets();
        }

        /// <summary>
        /// 프로젝트에서 모든 TMP_FontAsset을 찾아 캐시합니다.
        /// </summary>
        private void RefreshFontAssets()
        {
            entries.Clear();
            totalAtlasBytes = 0;
            totalGlyphs = 0;

            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (asset == null) continue;

                FontAssetEntry entry = new()
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

                // Atlas 텍스처 바이트 추정: width * height * (채널 수 ~= 1 for SDF)
                // SDF는 보통 R8이므로 width*height 바이트로 근사.
                entry.estimatedBytes = (long)entry.atlasWidth * entry.atlasHeight;

                entries.Add(entry);
                totalAtlasBytes += entry.estimatedBytes;
                totalGlyphs += entry.glyphCount;
            }

            SortEntries();
        }

        private void SortEntries()
        {
            entries.Sort((a, b) =>
            {
                int cmp = sortMode switch
                {
                    SortMode.Name => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase),
                    SortMode.AtlasSize => (a.atlasWidth * a.atlasHeight).CompareTo(b.atlasWidth * b.atlasHeight),
                    SortMode.GlyphCount => a.glyphCount.CompareTo(b.glyphCount),
                    SortMode.AtlasMemory => a.estimatedBytes.CompareTo(b.estimatedBytes),
                    _ => 0,
                };
                return sortDescending ? -cmp : cmp;
            });
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawSummary();
            EditorGUILayout.Space(5);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            string filter = searchFilter.Trim().ToLowerInvariant();

            foreach (FontAssetEntry entry in entries)
            {
                if (!string.IsNullOrEmpty(filter) &&
                    !entry.name.ToLowerInvariant().Contains(filter))
                {
                    continue;
                }

                DrawEntry(entry);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                RefreshFontAssets();
            }

            GUILayout.Space(5);
            GUILayout.Label("정렬:", GUILayout.Width(40));
            SortMode newMode = (SortMode)EditorGUILayout.EnumPopup(sortMode,
                EditorStyles.toolbarPopup, GUILayout.Width(100));
            if (newMode != sortMode)
            {
                sortMode = newMode;
                SortEntries();
            }

            bool newDesc = GUILayout.Toggle(sortDescending, "내림차순",
                EditorStyles.toolbarButton, GUILayout.Width(70));
            if (newDesc != sortDescending)
            {
                sortDescending = newDesc;
                SortEntries();
            }

            GUILayout.FlexibleSpace();

            GUILayout.Label("검색:", GUILayout.Width(40));
            searchFilter = GUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField,
                GUILayout.Width(150));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSummary()
        {
            DrawHeader("Summary");
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Font Asset 수", entries.Count);
                EditorGUILayout.IntField("총 글리프 수", totalGlyphs);
                EditorGUILayout.LongField("추정 총 Atlas 메모리 (KB)", totalAtlasBytes / 1024);
            }
        }

        private void DrawEntry(FontAssetEntry entry)
        {
            if (!foldoutStates.TryGetValue(entry.path, out bool open))
            {
                open = false;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            string label = $"{entry.name}   ({entry.atlasWidth}x{entry.atlasHeight}, glyphs: {entry.glyphCount})";
            open = EditorGUILayout.Foldout(open, label, true);
            foldoutStates[entry.path] = open;

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

        /// <summary>
        /// 순환 참조를 방지하며 Fallback 체인을 들여쓰기로 그립니다.
        /// </summary>
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