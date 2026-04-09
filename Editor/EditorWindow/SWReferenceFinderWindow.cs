using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SWTools
{
    /// <summary>
    /// 선택한 에셋을 참조하는 다른 에셋/씬/프리팹을 역방향으로 찾아주는 윈도우입니다.
    /// Unity 기본 "Select Dependencies"의 반대 방향 기능을 제공합니다.
    /// </summary>
    public class SWReferenceFinderWindow : EditorWindow
    {
        #region 필드
        private const string INCLUDE_PACKAGES_KEY = "SWTools.ReferenceFinder.IncludePackages";
        private const string SEARCH_EXTENSIONS_KEY = "SWTools.ReferenceFinder.SearchExtensions";

        // 검색 대상 확장자 (이 파일들 내부의 참조를 스캔)
        // .unity, .prefab, .asset, .mat, .controller, .playable, .spriteatlas 등
        private static readonly string[] defaultSearchExtensions =
        {
            ".unity", ".prefab", ".asset", ".mat", ".controller",
            ".overrideController", ".playable", ".spriteatlas", ".anim",
            ".mask", ".preset", ".shadergraph", ".shadervariants", ".guiskin",
            ".physicMaterial", ".physicsMaterial2D", ".fontsettings", ".mixer",
        };

        private Object targetAsset;
        private bool includePackages = false;
        private string searchExtensionsRaw;

        // 역참조 인덱스 캐시: "참조된 에셋 GUID" -> "참조하는 에셋 경로 리스트"
        private Dictionary<string, List<string>> reverseIndex;
        private double lastIndexBuildTime;
        private int indexedAssetCount;

        // 현재 검색 결과
        private List<string> currentResults = new();
        private Vector2 resultsScroll;

        // 진행 상황
        private bool isBuilding;

        // 일괄 선택용
        private readonly HashSet<string> selectedResults = new();

        // 필터 (결과 내 검색)
        private string resultFilter = "";

        // 결과 타입별 아이콘 캐시
        private readonly Dictionary<string, GUIContent> guiContentCache = new();
        #endregion // 필드

        [MenuItem("SWTools/Reference Finder %#r")]
        public static void ShowWindow()
        {
            SWReferenceFinderWindow window = GetWindow<SWReferenceFinderWindow>();
            window.titleContent = new GUIContent("Reference Finder", EditorGUIUtility.FindTexture("d_Search Icon"));
            window.minSize = new Vector2(380, 400);
            window.Show();
        }

        /// <summary>
        /// 프로젝트 창에서 우클릭 > 해당 에셋을 타겟으로 설정하여 창을 엽니다.
        /// </summary>
        [MenuItem("Assets/SWTools/Find References In Project", false, 25)]
        private static void FindReferencesForSelected()
        {
            Object selected = Selection.activeObject;
            if (selected == null) return;

            SWReferenceFinderWindow window = GetWindow<SWReferenceFinderWindow>();
            window.titleContent = new GUIContent("Reference Finder", EditorGUIUtility.FindTexture("d_Search Icon"));
            window.targetAsset = selected;
            window.Show();
            window.FindReferences();
        }

        [MenuItem("Assets/SWTools/Find References In Project", true)]
        private static bool FindReferencesForSelectedValidate()
        {
            return Selection.activeObject != null && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(Selection.activeObject));
        }

        private void OnEnable()
        {
            includePackages = EditorPrefs.GetBool(INCLUDE_PACKAGES_KEY, false);
            searchExtensionsRaw = EditorPrefs.GetString(SEARCH_EXTENSIONS_KEY, string.Join(",", defaultSearchExtensions));
        }

        private void OnDisable()
        {
            EditorPrefs.SetBool(INCLUDE_PACKAGES_KEY, includePackages);
            EditorPrefs.SetString(SEARCH_EXTENSIONS_KEY, searchExtensionsRaw);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(5);

            DrawTargetSection();

            EditorGUILayout.Space(10);
            DrawIndexSection();

            EditorGUILayout.Space(10);
            DrawResultsSection();
        }

        #region Target 섹션
        private void DrawTargetSection()
        {
            DrawHeader("Target Asset");

            EditorGUI.BeginChangeCheck();
            targetAsset = EditorGUILayout.ObjectField("찾을 에셋", targetAsset, typeof(Object), false);
            if (EditorGUI.EndChangeCheck())
            {
                // 타겟이 바뀌면 결과 초기화
                currentResults.Clear();
                selectedResults.Clear();
            }

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = targetAsset != null;
            if (GUILayout.Button("참조 찾기", GUILayout.Height(28)))
            {
                FindReferences();
            }
            GUI.enabled = true;

            if (GUILayout.Button("선택한 에셋으로", GUILayout.Width(120), GUILayout.Height(28)))
            {
                if (Selection.activeObject != null)
                {
                    targetAsset = Selection.activeObject;
                    FindReferences();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (targetAsset != null)
            {
                string path = AssetDatabase.GetAssetPath(targetAsset);
                EditorGUILayout.LabelField("Path", path, EditorStyles.miniLabel);
            }
        }
        #endregion // Target 섹션

        #region Index 섹션
        private void DrawIndexSection()
        {
            DrawHeader("Index");

            EditorGUILayout.BeginHorizontal();
            includePackages = EditorGUILayout.ToggleLeft("Packages 포함", includePackages, GUILayout.Width(120));
            if (GUILayout.Button("인덱스 재구축", GUILayout.Height(20)))
            {
                BuildReverseIndex();
            }
            if (GUILayout.Button("캐시 지우기", GUILayout.Height(20)))
            {
                reverseIndex = null;
                indexedAssetCount = 0;
                currentResults.Clear();
                selectedResults.Clear();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("검색 대상 확장자 (쉼표 구분)", EditorStyles.miniLabel);
            searchExtensionsRaw = EditorGUILayout.TextField(searchExtensionsRaw);

            if (GUILayout.Button("확장자 기본값으로", GUILayout.Height(18)))
            {
                searchExtensionsRaw = string.Join(",", defaultSearchExtensions);
            }

            if (reverseIndex != null)
            {
                EditorGUILayout.HelpBox(
                    $"인덱스: {indexedAssetCount}개 파일 스캔됨, {reverseIndex.Count}개 고유 GUID 참조됨\n" +
                    $"최근 빌드 시간: {lastIndexBuildTime:F2}초",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("아직 인덱스가 없습니다. \"참조 찾기\" 또는 \"인덱스 재구축\"을 누르세요.", MessageType.None);
            }
        }
        #endregion // Index 섹션

        #region 결과 섹션
        private void DrawResultsSection()
        {
            DrawHeader($"Results ({currentResults.Count})");

            if (currentResults.Count == 0)
            {
                EditorGUILayout.HelpBox("검색 결과가 없습니다.", MessageType.None);
                return;
            }

            // 결과 내 필터
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("필터", GUILayout.Width(30));
            resultFilter = EditorGUILayout.TextField(resultFilter);
            if (GUILayout.Button("✕", GUILayout.Width(22)))
            {
                resultFilter = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            // 일괄 액션 버튼
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("모두 선택", GUILayout.Height(20)))
            {
                foreach (string p in GetFilteredResults()) selectedResults.Add(p);
            }
            if (GUILayout.Button("선택 해제", GUILayout.Height(20)))
            {
                selectedResults.Clear();
            }
            GUI.enabled = selectedResults.Count > 0;
            if (GUILayout.Button($"Project에서 선택 ({selectedResults.Count})", GUILayout.Height(20)))
            {
                SelectInProject(selectedResults);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            resultsScroll = EditorGUILayout.BeginScrollView(resultsScroll);

            foreach (string path in GetFilteredResults())
            {
                DrawResultRow(path);
            }

            EditorGUILayout.EndScrollView();
        }

        private IEnumerable<string> GetFilteredResults()
        {
            if (string.IsNullOrEmpty(resultFilter)) return currentResults;
            return currentResults.Where(p => p.IndexOf(resultFilter, System.StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void DrawResultRow(string path)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            bool wasSelected = selectedResults.Contains(path);
            bool nowSelected = EditorGUILayout.Toggle(wasSelected, GUILayout.Width(16));
            if (nowSelected != wasSelected)
            {
                if (nowSelected) selectedResults.Add(path);
                else selectedResults.Remove(path);
            }

            if (!guiContentCache.TryGetValue(path, out GUIContent content))
            {
                Texture icon = AssetDatabase.GetCachedIcon(path);
                content = new GUIContent(Path.GetFileName(path), icon, path);
                guiContentCache[path] = content;
            }

            GUILayout.Label(content, GUILayout.ExpandWidth(true), GUILayout.Height(18));

            if (GUILayout.Button("◎", GUILayout.Width(22), GUILayout.Height(18)))
            {
                Object obj = AssetDatabase.LoadMainAssetAtPath(path);
                if (obj != null) EditorGUIUtility.PingObject(obj);
            }

            if (GUILayout.Button("Open", GUILayout.Width(45), GUILayout.Height(18)))
            {
                Object obj = AssetDatabase.LoadMainAssetAtPath(path);
                if (obj != null) AssetDatabase.OpenAsset(obj);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void SelectInProject(IEnumerable<string> paths)
        {
            List<Object> objs = new();
            foreach (string p in paths)
            {
                Object o = AssetDatabase.LoadMainAssetAtPath(p);
                if (o != null) objs.Add(o);
            }
            Selection.objects = objs.ToArray();
        }
        #endregion // 결과 섹션

        #region 인덱스 빌드 / 검색 로직
        /// <summary>
        /// 현재 타겟 에셋을 참조하는 에셋들을 찾습니다.
        /// 인덱스가 없으면 먼저 빌드합니다.
        /// </summary>
        private void FindReferences()
        {
            if (targetAsset == null) return;

            if (reverseIndex == null)
            {
                BuildReverseIndex();
            }

            if (reverseIndex == null) return;

            string targetPath = AssetDatabase.GetAssetPath(targetAsset);
            string targetGuid = AssetDatabase.AssetPathToGUID(targetPath);

            currentResults.Clear();
            selectedResults.Clear();
            guiContentCache.Clear();

            if (reverseIndex.TryGetValue(targetGuid, out List<string> refs))
            {
                // 자기 자신은 제외하고 정렬
                currentResults.AddRange(refs.Where(p => p != targetPath).Distinct().OrderBy(p => p));
            }

            Debug.Log($"[SWReferenceFinder] '{targetPath}'를 참조하는 에셋 {currentResults.Count}개를 찾았습니다.");
        }

        /// <summary>
        /// 프로젝트 내 모든 검색 대상 에셋에 대해 의존성을 스캔하여 역방향 인덱스를 만듭니다.
        /// </summary>
        private void BuildReverseIndex()
        {
            if (isBuilding) return;
            isBuilding = true;

            double startTime = EditorApplication.timeSinceStartup;

            try
            {
                HashSet<string> extSet = ParseExtensions(searchExtensionsRaw);

                // 모든 에셋 GUID 가져오기
                string[] allGuids = AssetDatabase.FindAssets("");
                List<string> pathsToScan = new(allGuids.Length);

                foreach (string guid in allGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(path)) continue;
                    if (!path.StartsWith("Assets/") && !(includePackages && path.StartsWith("Packages/"))) continue;

                    string ext = Path.GetExtension(path).ToLowerInvariant();
                    if (!extSet.Contains(ext)) continue;

                    pathsToScan.Add(path);
                }

                reverseIndex = new Dictionary<string, List<string>>(pathsToScan.Count * 2);
                indexedAssetCount = pathsToScan.Count;

                for (int i = 0; i < pathsToScan.Count; i++)
                {
                    string path = pathsToScan[i];

                    if (i % 50 == 0)
                    {
                        float progress = (float)i / pathsToScan.Count;
                        if (EditorUtility.DisplayCancelableProgressBar(
                            "Reference Finder",
                            $"의존성 스캔 중... ({i}/{pathsToScan.Count})\n{path}",
                            progress))
                        {
                            reverseIndex = null;
                            indexedAssetCount = 0;
                            return;
                        }
                    }

                    // recursive=false: 직접 의존성만 인덱싱 (속도+정확도 균형)
                    string[] deps = AssetDatabase.GetDependencies(path, false);
                    foreach (string dep in deps)
                    {
                        if (dep == path) continue;
                        string depGuid = AssetDatabase.AssetPathToGUID(dep);
                        if (string.IsNullOrEmpty(depGuid)) continue;

                        if (!reverseIndex.TryGetValue(depGuid, out List<string> list))
                        {
                            list = new List<string>();
                            reverseIndex[depGuid] = list;
                        }
                        list.Add(path);
                    }
                }

                lastIndexBuildTime = EditorApplication.timeSinceStartup - startTime;
                Debug.Log($"[SWReferenceFinder] 인덱스 빌드 완료: {indexedAssetCount}개 파일, {reverseIndex.Count}개 고유 GUID, {lastIndexBuildTime:F2}초");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                isBuilding = false;
            }
        }

        private HashSet<string> ParseExtensions(string raw)
        {
            HashSet<string> set = new(System.StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(raw)) return set;

            string[] tokens = raw.Split(new[] { ',', ';', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (string t in tokens)
            {
                string token = t.Trim();
                if (string.IsNullOrEmpty(token)) continue;
                if (!token.StartsWith(".")) token = "." + token;
                set.Add(token.ToLowerInvariant());
            }
            return set;
        }
        #endregion // 인덱스 빌드 / 검색 로직

        /// <summary>
        /// 섹션 헤더를 그립니다.
        /// </summary>
        private void DrawHeader(string title)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 1f));
            EditorGUILayout.Space(3);
        }
    }
}