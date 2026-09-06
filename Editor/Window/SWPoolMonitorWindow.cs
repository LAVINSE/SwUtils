using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using SW.EditorTools.Util;

using SW.Pooling;

#if UNITY_6000_4_OR_NEWER
using SWObjectIdentifier = UnityEngine.EntityId;
#else
using SWObjectIdentifier = System.Int32;
#endif

namespace SW.EditorTools.Window
{
    /// <summary>
    /// SWPool의 프리팹별 생성량, 활성 수, 대기 수, 반환 예약을 확인하는 에디터 모니터 창입니다.
    /// </summary>
    /// <remarks>
    /// 프리팹별 상태를 검색할 수 있으며 활성 인스턴스가 있는 풀을 강조 표시합니다.
    /// </remarks>
    public class SWPoolMonitorWindow : EditorWindow
    {
        #region 필드
        private const double REPAINT_INTERVAL = 0.1;
        private const double SNAPSHOT_INTERVAL = 0.25;
        private const string SearchSessionKey = "SWTools.PoolMonitor.Search";
        private const string ExpandedSessionKey = "SWTools.PoolMonitor.Expanded";

        private Vector2 scrollPosition;
        private string searchText = string.Empty;
        private double lastRepaintTime;
        private double lastSnapshotTime;

        /// <summary>캐시된 SWPool 참조입니다. 파괴되면 다음 갱신 때 재검색합니다.</summary>
        private SWPool cachedPool;
        /// <summary>캐시된 스냅샷 목록입니다. SNAPSHOT_INTERVAL마다 갱신됩니다.</summary>
        private IReadOnlyList<SWPoolSnapshot> cachedSnapshots;
        /// <summary>펼쳐진 행의 프리팹 식별자 집합입니다.</summary>
        private readonly HashSet<SWObjectIdentifier> expandedObjectIdentifiers = new();

        private GUIStyle rightAlignedLabelStyle;
        private GUIStyle boldMiniLabelStyle;
        #endregion // 필드

        /// <summary>
        /// Pool Monitor 창을 엽니다.
        /// </summary>
        [MenuItem("SWTools/Debug/Pool/Pool Monitor Window")]
        public static void ShowWindow()
        {
            SWPoolMonitorWindow window = GetWindow<SWPoolMonitorWindow>();
            SWEditorUtils.SetupWindow(window, "SW Pool Monitor", "d_Prefab Icon", 420, 420);
            window.Show();
        }

        private void OnEnable()
        {
            searchText = SessionState.GetString(SearchSessionKey, string.Empty);
            LoadExpandedState();
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (!Application.isPlaying)
            {
                cachedPool = null;
                cachedSnapshots = null;
            }

            if (SWEditorUtils.ShouldRepaint(ref lastRepaintTime, REPAINT_INTERVAL))
                Repaint();
        }

        private void OnGUI()
        {
            EnsureStyles();
            RefreshSnapshotsIfNeeded();

            DrawToolbar();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawPoolSnapshots();
            EditorGUILayout.EndScrollView();
        }

        #region 갱신
        /// <summary>
        /// 풀 참조와 스냅샷을 갱신 주기에 맞춰 갱신합니다.
        /// </summary>
        private void RefreshSnapshotsIfNeeded()
        {
            double now = EditorApplication.timeSinceStartup;
            if (cachedSnapshots != null && now - lastSnapshotTime < SNAPSHOT_INTERVAL)
                return;

            lastSnapshotTime = now;

            if (cachedPool == null)
                cachedPool = FindAnyObjectByType<SWPool>();

            cachedSnapshots = cachedPool != null ? cachedPool.GetPoolSnapshots() : null;
        }

        /// <summary>
        /// 스타일 객체를 준비합니다.
        /// </summary>
        private void EnsureStyles()
        {
            rightAlignedLabelStyle ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
            boldMiniLabelStyle ??= new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleRight };
        }
        #endregion // 갱신

        #region 툴바
        /// <summary>
        /// 상단 검색과 선택 버튼을 그립니다.
        /// </summary>
        private void DrawToolbar()
        {
            SWEditorUtils.DrawHeader("Pool 상태");

            EditorGUILayout.BeginHorizontal();

            string newSearchText = GUILayout.TextField(searchText, EditorStyles.toolbarSearchField);
            if (newSearchText != searchText)
            {
                searchText = newSearchText;
                SessionState.SetString(SearchSessionKey, searchText);
            }

            using (new SWEditorUtils.GUIEnabledScope(cachedPool != null))
            {
                if (GUILayout.Button("풀 선택", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                {
                    Selection.activeObject = cachedPool;
                    EditorGUIUtility.PingObject(cachedPool);
                }

                if (GUILayout.Button("유휴 정리", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                {
                    int trimmedCount = cachedPool.TrimIdlePools();
                    ShowNotification(new GUIContent($"유휴 풀 {trimmedCount}개 정리"));
                    lastSnapshotTime = 0d;
                }
            }

            EditorGUILayout.EndHorizontal();

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("플레이 중에 생성된 풀 상태를 확인하는 창입니다.", MessageType.Info);
        }
        #endregion // 툴바

        #region 목록
        /// <summary>
        /// 풀 스냅샷 목록을 그립니다.
        /// </summary>
        private void DrawPoolSnapshots()
        {
            if (cachedPool == null)
            {
                SWEditorUtils.DrawEmptyNotice("현재 씬에 SWPool이 없습니다.", MessageType.Warning);
                return;
            }

            if (cachedSnapshots == null || cachedSnapshots.Count == 0)
            {
                SWEditorUtils.DrawEmptyNotice("아직 생성된 풀이 없습니다.", MessageType.Info);
                return;
            }

            DrawSummary(cachedSnapshots);
            DrawColumnHeader();

            int visibleCount = 0;
            for (int index = 0; index < cachedSnapshots.Count; index++)
            {
                SWPoolSnapshot snapshot = cachedSnapshots[index];
                if (!IsSearchMatched(snapshot)) continue;

                visibleCount++;
                DrawPoolRow(snapshot);
            }

            if (visibleCount == 0)
                SWEditorUtils.DrawEmptyNotice("검색 조건에 맞는 풀이 없습니다.", MessageType.Info);
        }

        /// <summary>
        /// 전체 풀 요약을 한 줄로 그립니다.
        /// </summary>
        /// <param name="snapshots">풀 상태 목록입니다.</param>
        private void DrawSummary(IReadOnlyList<SWPoolSnapshot> snapshots)
        {
            int activeCount = 0;
            int inactiveCount = 0;
            int delayedReleaseCount = 0;

            for (int index = 0; index < snapshots.Count; index++)
            {
                activeCount += snapshots[index].ActiveCount;
                inactiveCount += snapshots[index].InactiveCount;
                delayedReleaseCount += snapshots[index].DelayedReleaseCount;
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label($"풀 {snapshots.Count}", EditorStyles.boldLabel, GUILayout.Width(60f));
            GUILayout.Label($"활성 {activeCount}", GUILayout.Width(80f));
            GUILayout.Label($"대기 {inactiveCount}", GUILayout.Width(80f));
            GUILayout.Label($"지연 반환 {delayedReleaseCount}");
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 컬럼 제목 행을 그립니다.
        /// </summary>
        private void DrawColumnHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Space(16f);
            GUILayout.Label("프리팹", EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label("활성", boldMiniLabelStyle, GUILayout.Width(44f));
            GUILayout.Label("대기", boldMiniLabelStyle, GUILayout.Width(44f));
            GUILayout.Label("스폰/반환", boldMiniLabelStyle, GUILayout.Width(80f));
            GUILayout.Label("지연", boldMiniLabelStyle, GUILayout.Width(40f));
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 풀 하나를 한 줄 요약 + 펼침 상세로 그립니다.
        /// </summary>
        /// <param name="snapshot">풀 상태입니다.</param>
        private void DrawPoolRow(SWPoolSnapshot snapshot)
        {
            SWObjectIdentifier objectIdentifier = SWEditorObjectUtility.GetIdentifier(snapshot.Prefab);
            bool expanded = expandedObjectIdentifiers.Contains(objectIdentifier);
            bool hasActive = snapshot.ActiveCount > 0;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            bool newExpanded = GUILayout.Toggle(expanded, GUIContent.none, EditorStyles.foldout, GUILayout.Width(14f));
            if (newExpanded != expanded)
            {
                if (newExpanded) expandedObjectIdentifiers.Add(objectIdentifier);
                else expandedObjectIdentifiers.Remove(objectIdentifier);
                SaveExpandedState();
            }

            string prefabName = snapshot.Prefab != null ? snapshot.Prefab.name : "(Missing)";
            using (new SWEditorUtils.GUIColorScope(hasActive ? new Color(0.65f, 1f, 0.65f) : GUI.color))
            {
                if (GUILayout.Button(prefabName, EditorStyles.label))
                {
                    if (snapshot.Prefab != null)
                        EditorGUIUtility.PingObject(snapshot.Prefab);
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label(snapshot.ActiveCount.ToString(), rightAlignedLabelStyle, GUILayout.Width(44f));
            GUILayout.Label(snapshot.InactiveCount.ToString(), rightAlignedLabelStyle, GUILayout.Width(44f));
            GUILayout.Label($"{snapshot.SpawnCount}/{snapshot.ReleaseCount}", rightAlignedLabelStyle, GUILayout.Width(80f));
            GUILayout.Label(snapshot.DelayedReleaseCount.ToString(), rightAlignedLabelStyle, GUILayout.Width(40f));

            EditorGUILayout.EndHorizontal();

            if (newExpanded)
                DrawPoolDetails(snapshot);

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 펼쳐진 풀의 상세 정보를 그립니다.
        /// </summary>
        /// <param name="snapshot">풀 상태입니다.</param>
        private void DrawPoolDetails(SWPoolSnapshot snapshot)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("누적 생성", snapshot.CreatedCount.ToString());
            EditorGUILayout.LabelField("누적 파괴", snapshot.DestroyedCount.ToString());

            if (snapshot.PoolNames != null && snapshot.PoolNames.Count > 0)
                EditorGUILayout.LabelField("풀 이름", string.Join(", ", snapshot.PoolNames));

            if (snapshot.GroupNames != null && snapshot.GroupNames.Count > 0)
                EditorGUILayout.LabelField("그룹", string.Join(", ", snapshot.GroupNames));

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16f);

            using (new SWEditorUtils.GUIEnabledScope(snapshot.Prefab != null))
            {
                if (GUILayout.Button("프리팹 선택", GUILayout.Width(90f)))
                {
                    Selection.activeObject = snapshot.Prefab;
                    EditorGUIUtility.PingObject(snapshot.Prefab);
                }
            }

            using (new SWEditorUtils.GUIEnabledScope(Application.isPlaying && cachedPool != null && snapshot.Prefab != null))
            {
                if (GUILayout.Button("풀 비우기", GUILayout.Width(90f)))
                {
                    cachedPool.Clear(snapshot.Prefab);
                    lastSnapshotTime = 0d;
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// 검색어와 풀 상태가 일치하는지 확인합니다.
        /// </summary>
        /// <param name="snapshot">풀 상태입니다.</param>
        /// <returns>일치하면 true입니다.</returns>
        private bool IsSearchMatched(SWPoolSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(searchText)) return true;

            string prefabName = snapshot.Prefab != null ? snapshot.Prefab.name : string.Empty;
            if (SWEditorUtils.MatchesFilter(prefabName, searchText)) return true;

            if (snapshot.PoolNames != null)
            {
                for (int index = 0; index < snapshot.PoolNames.Count; index++)
                {
                    if (SWEditorUtils.MatchesFilter(snapshot.PoolNames[index], searchText))
                        return true;
                }
            }

            if (snapshot.GroupNames != null)
            {
                for (int index = 0; index < snapshot.GroupNames.Count; index++)
                {
                    if (SWEditorUtils.MatchesFilter(snapshot.GroupNames[index], searchText))
                        return true;
                }
            }

            return false;
        }
        #endregion // 목록

        #region 상태 유지
        /// <summary>
        /// 펼침 상태를 SessionState에 저장합니다.
        /// </summary>
        private void SaveExpandedState()
        {
#if UNITY_6000_4_OR_NEWER
            SWObjectIdentifier[] identifiers = new SWObjectIdentifier[expandedObjectIdentifiers.Count];
            expandedObjectIdentifiers.CopyTo(identifiers);
            SessionState.SetEntityIdArray(ExpandedSessionKey, identifiers);
#else
            SessionState.SetString(ExpandedSessionKey, string.Join(",", expandedObjectIdentifiers));
#endif
        }

        /// <summary>
        /// SessionState에서 펼침 상태를 복원합니다.
        /// </summary>
        private void LoadExpandedState()
        {
            expandedObjectIdentifiers.Clear();

#if UNITY_6000_4_OR_NEWER
            expandedObjectIdentifiers.UnionWith(SessionState.GetEntityIdArray(ExpandedSessionKey, System.Array.Empty<SWObjectIdentifier>()));
#else
            string saved = SessionState.GetString(ExpandedSessionKey, string.Empty);
            if (string.IsNullOrEmpty(saved)) return;

            string[] tokens = saved.Split(',');
            for (int index = 0; index < tokens.Length; index++)
            {
                if (int.TryParse(tokens[index], out int objectIdentifier))
                    expandedObjectIdentifiers.Add(objectIdentifier);
            }
#endif
        }
        #endregion // 상태 유지
    }
}
