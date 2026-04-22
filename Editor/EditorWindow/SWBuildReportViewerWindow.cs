using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SWTools
{
    /// <summary>
    /// 사용자가 직접 로드한 BuildReport를 파싱해 어떤 에셋이 빌드 사이즈를
    /// 얼마나 차지하는지 정렬·그룹핑해 보여주고, 두 리포트 간 비교 기능도 제공합니다.
    /// </summary>
    public class SWBuildReportViewerWindow : EditorWindow
    {
        #region 필드
        // 슬롯 A (기본 / Base)
        private BuildReport reportA;
        private bool hasDataA;
        private ReportData dataA = new();

        // 슬롯 B (비교 대상 / Compare)
        private BuildReport reportB;
        private bool hasDataB;
        private ReportData dataB = new();

        private Vector2 scrollPosition;
        private string searchFilter = "";

        // 탭바
        private int selectedTab = 0;
        private static readonly string[] tabNames = { "일반", "비교" };

        private NormalSubMode normalSubMode = NormalSubMode.AllAssets;
        private SortMode sortMode = SortMode.Size;
        private bool sortDescending = true;

        // 비교 결과
        private List<DiffEntry> diffEntries = new();
        private DiffSortMode diffSortMode = DiffSortMode.SizeDelta;
        private bool diffSortDescending = true;
        private DiffFilter diffFilter = DiffFilter.All;

        private enum NormalSubMode { AllAssets, ByCategory }
        private enum SortMode { Size, Name, Category }
        private enum DiffSortMode { SizeDelta, SizeA, SizeB, Name }
        private enum DiffFilter { All, Changed, Added, Removed }
        private enum DiffStatus { Unchanged, Changed, Added, Removed }

        private class ReportData
        {
            public List<AssetEntry> entries = new();
            public Dictionary<string, AssetEntry> entryByPath = new();
            public Dictionary<string, long> categoryTotals = new();
            public long totalSize;
            public int totalCount;
            public string targetLabel = "(없음)";
            public string timeLabel = "(없음)";
            public string sourceLabel = "(로드되지 않음)";

            public void Clear()
            {
                entries.Clear();
                entryByPath.Clear();
                categoryTotals.Clear();
                totalSize = 0;
                totalCount = 0;
                targetLabel = "(없음)";
                timeLabel = "(없음)";
                sourceLabel = "(로드되지 않음)";
            }
        }

        private class AssetEntry
        {
            public string path;
            public string name;
            public long size;
            public string category;
        }

        private class DiffEntry
        {
            public string path;
            public string name;
            public string category;
            public long sizeA;
            public long sizeB;
            public long delta;
            public DiffStatus status;
        }
        #endregion // 필드

        [MenuItem("SWTools/Build Report Viewer")]
        public static void ShowWindow()
        {
            SWBuildReportViewerWindow window = GetWindow<SWBuildReportViewerWindow>();
            SWEditorUtils.SetupWindow(window, "SW Build Report", "d_BuildSettings.SelectedIcon", 600, 450);
            window.Show();
        }

        #region 로드 로직
        private void LoadReportFromFileDialog(bool isSlotA)
        {
            string path = EditorUtility.OpenFilePanel(
                "BuildReport 파일 선택", "", "buildreport");
            if (string.IsNullOrEmpty(path)) return;

            LoadReportFromPath(path, isSlotA);
        }

        private void LoadReportFromPath(string sourcePath, bool isSlotA)
        {
            if (!File.Exists(sourcePath))
            {
                Debug.LogWarning($"[SWTools] 파일이 존재하지 않습니다: {sourcePath}");
                return;
            }

            string tempAssetPath = isSlotA
                ? "Assets/SWTools_ReportA.buildreport"
                : "Assets/SWTools_ReportB.buildreport";

            try
            {
                File.Copy(sourcePath, tempAssetPath, true);
                AssetDatabase.ImportAsset(tempAssetPath);
                BuildReport loaded = AssetDatabase.LoadAssetAtPath<BuildReport>(tempAssetPath);

                if (loaded == null)
                {
                    Debug.LogWarning("[SWTools] BuildReport 로드 실패: " + sourcePath);
                    return;
                }

                ReportData data = ParseReport(loaded, sourcePath);

                if (isSlotA)
                {
                    reportA = null;
                    hasDataA = true;
                    dataA = data;
                    SortEntries(dataA);
                }
                else
                {
                    reportB = null;
                    hasDataB = true;
                    dataB = data;
                    SortEntries(dataB);
                }

                if (hasDataA && hasDataB)
                {
                    BuildDiff();
                }
            }
            finally
            {
                if (File.Exists(tempAssetPath))
                {
                    AssetDatabase.DeleteAsset(tempAssetPath);
                }
            }
        }

        private void LoadReportFromAsset(BuildReport asset, bool isSlotA)
        {
            if (asset == null)
            {
                if (isSlotA) { reportA = null; hasDataA = false; dataA.Clear(); }
                else { reportB = null; hasDataB = false; dataB.Clear(); }
                diffEntries.Clear();
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(asset);
            ReportData data = ParseReport(asset, assetPath);

            if (isSlotA)
            {
                reportA = asset;
                hasDataA = true;
                dataA = data;
                SortEntries(dataA);
            }
            else
            {
                reportB = asset;
                hasDataB = true;
                dataB = data;
                SortEntries(dataB);
            }

            if (hasDataA && hasDataB)
            {
                BuildDiff();
            }
        }

        private ReportData ParseReport(BuildReport report, string sourceLabel)
        {
            ReportData data = new()
            {
                targetLabel = report.summary.platform.ToString(),
                timeLabel = report.summary.buildEndedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                sourceLabel = string.IsNullOrEmpty(sourceLabel) ? "(Unknown)" : sourceLabel,
            };

            foreach (PackedAssets packed in report.packedAssets)
            {
                foreach (PackedAssetInfo info in packed.contents)
                {
                    string path = info.sourceAssetPath;
                    if (string.IsNullOrEmpty(path)) continue;

                    string category = GetCategoryFromPath(path);

                    if (data.entryByPath.TryGetValue(path, out AssetEntry existing))
                    {
                        existing.size += (long)info.packedSize;
                    }
                    else
                    {
                        AssetEntry entry = new()
                        {
                            path = path,
                            name = Path.GetFileName(path),
                            size = (long)info.packedSize,
                            category = category,
                        };
                        data.entries.Add(entry);
                        data.entryByPath[path] = entry;
                    }

                    data.totalSize += (long)info.packedSize;
                    data.totalCount++;

                    if (!data.categoryTotals.ContainsKey(category))
                        data.categoryTotals[category] = 0;
                    data.categoryTotals[category] += (long)info.packedSize;
                }
            }

            return data;
        }

        private string GetCategoryFromPath(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".png" or ".jpg" or ".jpeg" or ".tga" or ".psd" or ".exr" or ".tif" => "Texture",
                ".fbx" or ".obj" or ".blend" or ".dae" => "Mesh",
                ".wav" or ".mp3" or ".ogg" or ".aif" => "Audio",
                ".anim" or ".controller" => "Animation",
                ".mat" => "Material",
                ".shader" or ".shadergraph" or ".compute" => "Shader",
                ".prefab" => "Prefab",
                ".asset" => "ScriptableObject",
                ".unity" => "Scene",
                ".ttf" or ".otf" => "Font",
                ".cs" or ".dll" => "Script",
                _ => "Other",
            };
        }
        #endregion // 로드 로직

        #region 정렬 & Diff
        private void SortEntries(ReportData data)
        {
            data.entries.Sort((a, b) =>
            {
                int cmp = sortMode switch
                {
                    SortMode.Size => a.size.CompareTo(b.size),
                    SortMode.Name => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase),
                    SortMode.Category => string.Compare(a.category, b.category, System.StringComparison.Ordinal),
                    _ => 0,
                };
                return sortDescending ? -cmp : cmp;
            });
        }

        private void BuildDiff()
        {
            diffEntries.Clear();
            if (!hasDataA || !hasDataB) return;

            HashSet<string> allPaths = new();
            foreach (var e in dataA.entries) allPaths.Add(e.path);
            foreach (var e in dataB.entries) allPaths.Add(e.path);

            foreach (string path in allPaths)
            {
                dataA.entryByPath.TryGetValue(path, out AssetEntry a);
                dataB.entryByPath.TryGetValue(path, out AssetEntry b);

                DiffEntry d = new()
                {
                    path = path,
                    name = Path.GetFileName(path),
                    category = (a ?? b).category,
                    sizeA = a?.size ?? 0,
                    sizeB = b?.size ?? 0,
                };
                d.delta = d.sizeB - d.sizeA;

                if (a == null) d.status = DiffStatus.Added;
                else if (b == null) d.status = DiffStatus.Removed;
                else if (d.delta != 0) d.status = DiffStatus.Changed;
                else d.status = DiffStatus.Unchanged;

                diffEntries.Add(d);
            }

            SortDiff();
        }

        private void SortDiff()
        {
            diffEntries.Sort((x, y) =>
            {
                int cmp = diffSortMode switch
                {
                    DiffSortMode.SizeDelta => x.delta.CompareTo(y.delta),
                    DiffSortMode.SizeA => x.sizeA.CompareTo(y.sizeA),
                    DiffSortMode.SizeB => x.sizeB.CompareTo(y.sizeB),
                    DiffSortMode.Name => string.Compare(x.name, y.name, System.StringComparison.OrdinalIgnoreCase),
                    _ => 0,
                };
                return diffSortDescending ? -cmp : cmp;
            });
        }
        #endregion // 정렬 & Diff

        #region GUI
        private void OnGUI()
        {
            // SWEditorUtils 탭바 사용
            int newTab = SWEditorUtils.DrawTabBar(selectedTab, tabNames);
            if (newTab != selectedTab)
            {
                selectedTab = newTab;
                if (selectedTab == 1 && hasDataA && hasDataB)
                {
                    BuildDiff();
                }
            }

            DrawReportSlots();
            DrawToolbar();
            EditorGUILayout.Space(5);

            if (selectedTab == 0)
            {
                DrawNormalTab();
            }
            else
            {
                DrawCompareTab();
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (selectedTab == 0)
            {
                GUILayout.Label("보기:", GUILayout.Width(40));
                NormalSubMode newSub = (NormalSubMode)EditorGUILayout.EnumPopup(normalSubMode,
                    EditorStyles.toolbarPopup, GUILayout.Width(110));
                if (newSub != normalSubMode)
                {
                    normalSubMode = newSub;
                }

                GUILayout.Space(10);

                if (normalSubMode == NormalSubMode.AllAssets)
                {
                    GUILayout.Label("정렬:", GUILayout.Width(40));
                    SortMode newMode = (SortMode)EditorGUILayout.EnumPopup(sortMode,
                        EditorStyles.toolbarPopup, GUILayout.Width(90));
                    if (newMode != sortMode)
                    {
                        sortMode = newMode;
                        SortEntries(dataA);
                    }
                    bool newDesc = GUILayout.Toggle(sortDescending, "내림차순",
                        EditorStyles.toolbarButton, GUILayout.Width(70));
                    if (newDesc != sortDescending)
                    {
                        sortDescending = newDesc;
                        SortEntries(dataA);
                    }
                }
            }
            else // Compare
            {
                GUILayout.Label("필터:", GUILayout.Width(40));
                diffFilter = (DiffFilter)EditorGUILayout.EnumPopup(diffFilter,
                    EditorStyles.toolbarPopup, GUILayout.Width(90));

                GUILayout.Label("정렬:", GUILayout.Width(40));
                DiffSortMode newDiffSort = (DiffSortMode)EditorGUILayout.EnumPopup(diffSortMode,
                    EditorStyles.toolbarPopup, GUILayout.Width(100));
                if (newDiffSort != diffSortMode)
                {
                    diffSortMode = newDiffSort;
                    SortDiff();
                }
                bool newDesc = GUILayout.Toggle(diffSortDescending, "내림차순",
                    EditorStyles.toolbarButton, GUILayout.Width(70));
                if (newDesc != diffSortDescending)
                {
                    diffSortDescending = newDesc;
                    SortDiff();
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("검색:", GUILayout.Width(40));
            searchFilter = SWEditorUtils.DrawSearchField(searchFilter, 150f);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawReportSlots()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawReportSlot("Report A", ref reportA, dataA, hasDataA, true);

            if (selectedTab == 1)
            {
                EditorGUILayout.Space(2);
                DrawReportSlot("Report B", ref reportB, dataB, hasDataB, false);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawReportSlot(string label, ref BuildReport slot, ReportData data, bool hasData, bool isSlotA)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, EditorStyles.boldLabel, GUILayout.Width(65));

            EditorGUI.BeginChangeCheck();
            BuildReport newSlot = (BuildReport)EditorGUILayout.ObjectField(
                slot, typeof(BuildReport), false);
            if (EditorGUI.EndChangeCheck())
            {
                LoadReportFromAsset(newSlot, isSlotA);
            }

            if (GUILayout.Button("Load...", GUILayout.Width(70)))
            {
                LoadReportFromFileDialog(isSlotA);
            }
            if (GUILayout.Button("Clear", GUILayout.Width(55)))
            {
                LoadReportFromAsset(null, isSlotA);
            }
            EditorGUILayout.EndHorizontal();

            if (hasData)
            {
                if (slot == null)
                {
                    EditorGUILayout.LabelField(
                        $"   (외부 파일) {data.sourceLabel}",
                        EditorStyles.miniLabel);
                }
                EditorGUILayout.LabelField(
                    $"   {data.targetLabel}  |  {data.timeLabel}  |  " +
                    $"{data.totalCount} assets  |  {SWEditorUtils.FormatBytes(data.totalSize)}",
                    EditorStyles.miniLabel);
            }
        }

        private void DrawNormalTab()
        {
            if (!hasDataA)
            {
                SWEditorUtils.DrawEmptyNotice(
                    "상단의 'Report A' 슬롯에 .buildreport 파일을 지정하거나\n" +
                    "'Load...' 버튼을 눌러 파일을 선택하세요.");
                return;
            }

            DrawSummary(dataA, "Report A Summary");
            EditorGUILayout.Space(5);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            if (normalSubMode == NormalSubMode.AllAssets)
            {
                DrawAllAssets(dataA);
            }
            else
            {
                DrawByCategory(dataA);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawSummary(ReportData data, string title)
        {
            SWEditorUtils.DrawHeader(title);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Source", data.sourceLabel);
                EditorGUILayout.TextField("Target", data.targetLabel);
                EditorGUILayout.TextField("Built At", data.timeLabel);
                EditorGUILayout.IntField("Asset Count", data.totalCount);
                EditorGUILayout.TextField("Total Size", SWEditorUtils.FormatBytes(data.totalSize));
            }
        }

        private void DrawAllAssets(ReportData data)
        {
            string filter = searchFilter.Trim().ToLowerInvariant();

            foreach (AssetEntry entry in data.entries)
            {
                if (!SWEditorUtils.MatchesFilter(entry.path, filter))
                {
                    continue;
                }
                DrawAssetRow(entry);
            }
        }

        private void DrawByCategory(ReportData data)
        {
            List<KeyValuePair<string, long>> sortedCategories = new(data.categoryTotals);
            sortedCategories.Sort((a, b) => b.Value.CompareTo(a.Value));

            foreach (var kv in sortedCategories)
            {
                float pct = data.totalSize > 0 ? (float)kv.Value / data.totalSize : 0f;

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.Label(kv.Key, EditorStyles.boldLabel, GUILayout.Width(130));
                GUILayout.Label(SWEditorUtils.FormatBytes(kv.Value), GUILayout.Width(90));
                GUILayout.Label($"{pct * 100f:F1}%", GUILayout.Width(55));

                Rect barRect = GUILayoutUtility.GetRect(0, 14, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(barRect, new Color(0.15f, 0.15f, 0.15f, 1f));
                Rect fill = new(barRect.x, barRect.y, barRect.width * pct, barRect.height);
                EditorGUI.DrawRect(fill, new Color(0.4f, 0.7f, 0.9f, 1f));

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawAssetRow(AssetEntry entry)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(SWEditorUtils.FormatBytes(entry.size), GUILayout.Width(80));
            GUILayout.Label($"[{entry.category}]", GUILayout.Width(110));
            GUILayout.Label(new GUIContent(entry.name, entry.path), GUILayout.ExpandWidth(true));

            if (SWEditorUtils.SmallButton("Ping", 40f))
            {
                SWEditorUtils.PingAssetAtPath(entry.path);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCompareTab()
        {
            if (!hasDataA || !hasDataB)
            {
                SWEditorUtils.DrawEmptyNotice(
                    "비교할 두 개의 .buildreport 파일을 Report A / Report B 슬롯에 지정하세요.");
                return;
            }

            long deltaTotal = dataB.totalSize - dataA.totalSize;
            string sign = deltaTotal >= 0 ? "+" : "";

            SWEditorUtils.DrawHeader("Compare Summary (B - A)");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                $"A: {dataA.totalCount} assets, {SWEditorUtils.FormatBytes(dataA.totalSize)}");
            EditorGUILayout.LabelField(
                $"B: {dataB.totalCount} assets, {SWEditorUtils.FormatBytes(dataB.totalSize)}");

            Color prev = GUI.contentColor;
            GUI.contentColor = deltaTotal > 0 ? new Color(1f, 0.5f, 0.5f)
                             : deltaTotal < 0 ? new Color(0.5f, 1f, 0.5f)
                             : Color.white;
            EditorGUILayout.LabelField(
                $"Δ Total: {sign}{SWEditorUtils.FormatBytes(System.Math.Abs(deltaTotal))}  ({sign}{deltaTotal} bytes)",
                EditorStyles.boldLabel);
            GUI.contentColor = prev;
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("상태", GUILayout.Width(60));
            GUILayout.Label("A", GUILayout.Width(70));
            GUILayout.Label("B", GUILayout.Width(70));
            GUILayout.Label("Δ", GUILayout.Width(80));
            GUILayout.Label("[Category] 에셋", GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            string filter = searchFilter.Trim().ToLowerInvariant();
            foreach (DiffEntry d in diffEntries)
            {
                if (!PassesDiffFilter(d)) continue;
                if (!SWEditorUtils.MatchesFilter(d.path, filter))
                {
                    continue;
                }
                DrawDiffRow(d);
            }

            EditorGUILayout.EndScrollView();
        }

        private bool PassesDiffFilter(DiffEntry d)
        {
            return diffFilter switch
            {
                DiffFilter.All => true,
                DiffFilter.Changed => d.status == DiffStatus.Changed,
                DiffFilter.Added => d.status == DiffStatus.Added,
                DiffFilter.Removed => d.status == DiffStatus.Removed,
                _ => true,
            };
        }

        private void DrawDiffRow(DiffEntry d)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            Color prev = GUI.contentColor;
            string statusLabel;
            switch (d.status)
            {
                case DiffStatus.Added:
                    GUI.contentColor = new Color(0.5f, 1f, 0.6f);
                    statusLabel = "ADD";
                    break;
                case DiffStatus.Removed:
                    GUI.contentColor = new Color(1f, 0.5f, 0.5f);
                    statusLabel = "DEL";
                    break;
                case DiffStatus.Changed:
                    GUI.contentColor = d.delta > 0
                        ? new Color(1f, 0.7f, 0.4f)
                        : new Color(0.6f, 0.9f, 1f);
                    statusLabel = d.delta > 0 ? "▲" : "▼";
                    break;
                default:
                    statusLabel = "=";
                    break;
            }
            GUILayout.Label(statusLabel, EditorStyles.boldLabel, GUILayout.Width(60));
            GUI.contentColor = prev;

            GUILayout.Label(SWEditorUtils.FormatBytes(d.sizeA), GUILayout.Width(70));
            GUILayout.Label(SWEditorUtils.FormatBytes(d.sizeB), GUILayout.Width(70));

            string deltaSign = d.delta >= 0 ? "+" : "";
            GUI.contentColor = d.delta > 0 ? new Color(1f, 0.6f, 0.6f)
                            : d.delta < 0 ? new Color(0.6f, 1f, 0.6f)
                            : Color.white;
            GUILayout.Label($"{deltaSign}{SWEditorUtils.FormatBytes(System.Math.Abs(d.delta))}", GUILayout.Width(80));
            GUI.contentColor = prev;

            GUILayout.Label(new GUIContent($"[{d.category}] {d.name}", d.path),
                GUILayout.ExpandWidth(true));

            if (SWEditorUtils.SmallButton("Ping", 40f))
            {
                SWEditorUtils.PingAssetAtPath(d.path);
            }
            EditorGUILayout.EndHorizontal();
        }
        #endregion // GUI
    }
}