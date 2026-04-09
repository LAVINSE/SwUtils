using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SWTools
{
    /// <summary>
    /// 플레이 중 타임스케일 조절, FPS 모니터링, 씬 등록/로드, 유틸리티 등
    /// 테스트에 필요한 기능을 한곳에서 제공하는 에디터 윈도우입니다.
    /// </summary>
    public class SWTestToolsWindow : EditorWindow
    {
        #region 필드
        private const string TIME_SCALE_KEY = "SWTools.TestTools.TimeScale";
        private const string VSYNC_KEY = "SWTools.TestTools.VSync";
        private const string TARGET_FPS_KEY = "SWTools.TestTools.TargetFPS";
        private const string REGISTERED_SCENES_KEY = "SWTools.TestTools.RegisteredScenes";
        private const string BOOKMARKED_OBJECTS_KEY = "SWTools.TestTools.BookmarkedObjects";
        private const string SLOWMO_SCALE_KEY = "SWTools.TestTools.SlowMoScale";

        private static readonly float[] presetTimeScales = { 0f, 0.1f, 0.25f, 0.5f, 1f, 2f, 4f, 8f };
        private static readonly string[] presetTimeScaleLabels = { "0x", "0.1x", "0.25x", "0.5x", "1x", "2x", "4x", "8x" };

        private float timeScale = 1f;
        private int targetFrameRate = -1;
        private int vSyncCount = 0;

        // Time 탭 확장
        private float slowMoScale = 0.1f;
        private bool isSlowMoActive = false;
        private float originalFixedDeltaTime;
        private float customFixedDeltaTime = 0.02f;

        private Vector2 scrollPosition;

        // FPS 측정
        private float fpsAccumulator;
        private int fpsFrameCount;
        private float currentFps;
        private float fpsUpdateTimer;
        private double lastEditorTime;

        // FPS 통계 (플레이 시작 후 누적)
        private float fpsMin = float.MaxValue;
        private float fpsMax = 0f;
        private float fpsAvgAccumulator;
        private int fpsAvgCount;
        private float fpsAverage;

        // FPS 그래프용 히스토리
        private const int FPS_HISTORY_SIZE = 120;
        private readonly float[] fpsHistory = new float[FPS_HISTORY_SIZE];
        private int fpsHistoryIndex = 0;

        // 탭
        private int selectedTab = 0;
        private static readonly string[] tabNames = { "Time", "Performance", "Scene", "Utility" };

        // Scene 탭: 등록된 씬 목록
        private List<string> registeredScenePaths = new();
        private SceneAsset sceneToAdd;
        private Vector2 sceneListScroll;

        // Scene 탭: BuildSettings 씬 표시 토글
        private bool showBuildSettingsScenes = false;
        private Vector2 buildScenesScroll;

        // Scene 탭: GameObject 북마크 (씬경로|하이어라키경로 형식으로 저장)
        private List<string> bookmarkedObjects = new();
        private Vector2 bookmarkScroll;

        // 캐시된 문자열 (OnGUI에서의 할당 최소화용)
        private string cachedRegisteredScenesHeader;
        private int lastRegisteredSceneCount = -1;

        // 씬별 캐시 (이름 / GUIContent)
        private readonly List<SceneDisplayCache> sceneDisplayCaches = new();

        // Screen 정보 캐시
        private string cachedScreenInfo;
        private string cachedDpiInfo;
        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;
        private float lastScreenDpi = -1f;

        // 메모리 표시 캐시 (주기적 갱신)
        private long cachedTotalMemoryMB;
        private double lastMemoryUpdateTime;
        private const double MEMORY_UPDATE_INTERVAL = 1.0;

        // UnityStats 렌더링 정보 (리플렉션)
        private static System.Type unityStatsType;
        private static PropertyInfo drawCallsProp;
        private static PropertyInfo batchesProp;
        private static PropertyInfo trianglesProp;
        private static PropertyInfo verticesProp;
        private static PropertyInfo setPassCallsProp;
        private static bool unityStatsInitialized = false;

        // Quality Level 캐시
        private string[] qualityLevelNames;

        private struct SceneDisplayCache
        {
            public string path;
            public string displayName;
            public GUIContent content;
            public bool exists;
        }
        #endregion // 필드

        [MenuItem("SWTools/Test Tools Window %#t")]
        public static void ShowWindow()
        {
            SWTestToolsWindow window = GetWindow<SWTestToolsWindow>();
            window.titleContent = new GUIContent("SW Test Tools", EditorGUIUtility.FindTexture("d_SettingsIcon"));
            window.minSize = new Vector2(320, 400);
            window.Show();
        }

        private void OnEnable()
        {
            // 저장된 설정 불러오기
            timeScale = EditorPrefs.GetFloat(TIME_SCALE_KEY, 1f);
            vSyncCount = EditorPrefs.GetInt(VSYNC_KEY, 0);
            targetFrameRate = EditorPrefs.GetInt(TARGET_FPS_KEY, -1);
            slowMoScale = EditorPrefs.GetFloat(SLOWMO_SCALE_KEY, 0.1f);

            originalFixedDeltaTime = Time.fixedDeltaTime;
            customFixedDeltaTime = Time.fixedDeltaTime;

            LoadRegisteredScenes();
            RebuildSceneDisplayCaches();
            LoadBookmarkedObjects();

            qualityLevelNames = QualitySettings.names;

            InitializeUnityStats();

            lastEditorTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorPrefs.SetFloat(TIME_SCALE_KEY, timeScale);
            EditorPrefs.SetInt(VSYNC_KEY, vSyncCount);
            EditorPrefs.SetInt(TARGET_FPS_KEY, targetFrameRate);
            EditorPrefs.SetFloat(SLOWMO_SCALE_KEY, slowMoScale);

            SaveRegisteredScenes();
            SaveBookmarkedObjects();

            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        /// <summary>
        /// 플레이 상태 변경 시 FPS 통계를 리셋합니다.
        /// </summary>
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                ResetFpsStats();
            }
        }

        private void ResetFpsStats()
        {
            fpsMin = float.MaxValue;
            fpsMax = 0f;
            fpsAvgAccumulator = 0f;
            fpsAvgCount = 0;
            fpsAverage = 0f;
            System.Array.Clear(fpsHistory, 0, fpsHistory.Length);
            fpsHistoryIndex = 0;
        }

        /// <summary>
        /// UnityStats 내부 API를 리플렉션으로 초기화합니다.
        /// </summary>
        private void InitializeUnityStats()
        {
            if (unityStatsInitialized) return;

            unityStatsType = System.Type.GetType("UnityEditor.UnityStats, UnityEditor");
            if (unityStatsType != null)
            {
                BindingFlags flags = BindingFlags.Static | BindingFlags.Public;
                drawCallsProp = unityStatsType.GetProperty("drawCalls", flags);
                batchesProp = unityStatsType.GetProperty("batches", flags);
                trianglesProp = unityStatsType.GetProperty("triangles", flags);
                verticesProp = unityStatsType.GetProperty("vertices", flags);
                setPassCallsProp = unityStatsType.GetProperty("setPassCalls", flags);
            }
            unityStatsInitialized = true;
        }

        /// <summary>
        /// FPS 측정 및 주기적 리페인트
        /// </summary>
        private void OnEditorUpdate()
        {
            double now = EditorApplication.timeSinceStartup;
            float delta = (float)(now - lastEditorTime);
            lastEditorTime = now;

            if (Application.isPlaying && delta > 0f)
            {
                fpsAccumulator += 1f / delta;
                fpsFrameCount++;
                fpsUpdateTimer += delta;

                if (fpsUpdateTimer >= 0.5f)
                {
                    currentFps = fpsAccumulator / fpsFrameCount;
                    fpsAccumulator = 0f;
                    fpsFrameCount = 0;
                    fpsUpdateTimer = 0f;

                    // 통계 업데이트
                    if (currentFps < fpsMin) fpsMin = currentFps;
                    if (currentFps > fpsMax) fpsMax = currentFps;
                    fpsAvgAccumulator += currentFps;
                    fpsAvgCount++;
                    fpsAverage = fpsAvgAccumulator / fpsAvgCount;

                    // 히스토리 업데이트 (원형 버퍼)
                    fpsHistory[fpsHistoryIndex] = currentFps;
                    fpsHistoryIndex = (fpsHistoryIndex + 1) % FPS_HISTORY_SIZE;

                    Repaint();
                }
            }
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space(5);
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(25));
            EditorGUILayout.Space(10);

            switch (selectedTab)
            {
                case 0: DrawTimeTab(); break;
                case 1: DrawPerformanceTab(); break;
                case 2: DrawSceneTab(); break;
                case 3: DrawUtilityTab(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        #region Time 탭
        /// <summary>
        /// 타임스케일 조작 탭
        /// </summary>
        private void DrawTimeTab()
        {
            DrawHeader("Time Scale");

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 중에만 Time.timeScale에 반영됩니다.", MessageType.Info);
            }

            EditorGUI.BeginChangeCheck();
            timeScale = EditorGUILayout.Slider("Time Scale", timeScale, 0f, 10f);
            if (EditorGUI.EndChangeCheck() && Application.isPlaying)
            {
                Time.timeScale = timeScale;
            }

            // 프리셋 버튼
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < presetTimeScales.Length; i++)
            {
                if (GUILayout.Button(presetTimeScaleLabels[i], GUILayout.Height(25)))
                {
                    float preset = presetTimeScales[i];
                    timeScale = preset;
                    if (Application.isPlaying) Time.timeScale = preset;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Pause", GUILayout.Height(30)))
            {
                timeScale = 0f;
                if (Application.isPlaying) Time.timeScale = 0f;
            }
            if (GUILayout.Button("Resume (1x)", GUILayout.Height(30)))
            {
                timeScale = 1f;
                if (Application.isPlaying) Time.timeScale = 1f;
            }
            EditorGUILayout.EndHorizontal();

            // Step Frame
            EditorGUILayout.Space(10);
            DrawHeader("Step Frame");
            EditorGUILayout.HelpBox("일시정지 상태에서 1프레임씩 진행합니다.", MessageType.None);

            GUI.enabled = Application.isPlaying;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("⏸ Pause & Step", GUILayout.Height(28)))
            {
                EditorApplication.isPaused = true;
                EditorApplication.Step();
            }
            if (GUILayout.Button("▶ Step (1 Frame)", GUILayout.Height(28)))
            {
                if (!EditorApplication.isPaused) EditorApplication.isPaused = true;
                EditorApplication.Step();
            }
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

            // Slow Motion 토글
            EditorGUILayout.Space(10);
            DrawHeader("Slow Motion Toggle");
            slowMoScale = EditorGUILayout.Slider("Slow Mo Scale", slowMoScale, 0.01f, 1f);

            GUI.backgroundColor = isSlowMoActive ? Color.cyan : Color.white;
            string slowMoLabel = isSlowMoActive
                ? $"■ Slow Motion ON ({slowMoScale}x) - 클릭해서 끄기"
                : $"▶ Slow Motion OFF - 클릭해서 켜기 ({slowMoScale}x)";
            if (GUILayout.Button(slowMoLabel, GUILayout.Height(32)))
            {
                ToggleSlowMotion();
            }
            GUI.backgroundColor = Color.white;

            // Fixed DeltaTime
            EditorGUILayout.Space(10);
            DrawHeader("Fixed DeltaTime (물리)");
            EditorGUILayout.HelpBox($"기본값: {originalFixedDeltaTime:F4}s ({1f / originalFixedDeltaTime:F0} Hz)", MessageType.None);

            EditorGUI.BeginChangeCheck();
            customFixedDeltaTime = EditorGUILayout.Slider("Fixed DeltaTime", customFixedDeltaTime, 0.001f, 0.1f);
            if (EditorGUI.EndChangeCheck() && Application.isPlaying)
            {
                Time.fixedDeltaTime = customFixedDeltaTime;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("30Hz", GUILayout.Height(22))) SetFixedDeltaTime(1f / 30f);
            if (GUILayout.Button("50Hz", GUILayout.Height(22))) SetFixedDeltaTime(1f / 50f);
            if (GUILayout.Button("60Hz", GUILayout.Height(22))) SetFixedDeltaTime(1f / 60f);
            if (GUILayout.Button("120Hz", GUILayout.Height(22))) SetFixedDeltaTime(1f / 120f);
            if (GUILayout.Button("Reset", GUILayout.Height(22))) SetFixedDeltaTime(originalFixedDeltaTime);
            EditorGUILayout.EndHorizontal();

            // 현재 상태 표시
            EditorGUILayout.Space(10);
            DrawHeader("Current State");
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.FloatField("Time.timeScale", Application.isPlaying ? Time.timeScale : timeScale);
                EditorGUILayout.FloatField("Time.fixedDeltaTime", Time.fixedDeltaTime);
                EditorGUILayout.FloatField("Time.time", Time.time);
                EditorGUILayout.FloatField("Time.realtimeSinceStartup", Time.realtimeSinceStartup);
            }
        }

        private void ToggleSlowMotion()
        {
            isSlowMoActive = !isSlowMoActive;
            if (isSlowMoActive)
            {
                timeScale = slowMoScale;
                if (Application.isPlaying) Time.timeScale = slowMoScale;
            }
            else
            {
                timeScale = 1f;
                if (Application.isPlaying) Time.timeScale = 1f;
            }
        }

        private void SetFixedDeltaTime(float value)
        {
            customFixedDeltaTime = value;
            if (Application.isPlaying) Time.fixedDeltaTime = value;
        }
        #endregion // Time 탭

        #region Performance 탭
        /// <summary>
        /// 성능 관련 설정 및 모니터링
        /// </summary>
        private void DrawPerformanceTab()
        {
            DrawHeader("Frame Rate");

            EditorGUI.BeginChangeCheck();
            targetFrameRate = EditorGUILayout.IntField("Target FPS (-1 = 무제한)", targetFrameRate);
            if (EditorGUI.EndChangeCheck() && Application.isPlaying)
            {
                Application.targetFrameRate = targetFrameRate;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("30")) SetTargetFPS(30);
            if (GUILayout.Button("60")) SetTargetFPS(60);
            if (GUILayout.Button("120")) SetTargetFPS(120);
            if (GUILayout.Button("∞")) SetTargetFPS(-1);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUI.BeginChangeCheck();
            vSyncCount = EditorGUILayout.IntSlider("VSync Count", vSyncCount, 0, 4);
            if (EditorGUI.EndChangeCheck() && Application.isPlaying)
            {
                QualitySettings.vSyncCount = vSyncCount;
            }

            // Quality Level
            EditorGUILayout.Space(10);
            DrawHeader("Quality Level");
            int currentQuality = QualitySettings.GetQualityLevel();
            EditorGUI.BeginChangeCheck();
            int newQuality = EditorGUILayout.Popup("Level", currentQuality, qualityLevelNames);
            if (EditorGUI.EndChangeCheck())
            {
                QualitySettings.SetQualityLevel(newQuality, true);
            }

            // FPS 그래프
            EditorGUILayout.Space(10);
            DrawHeader("FPS Graph");
            DrawFpsGraph();

            // FPS 통계
            EditorGUILayout.Space(5);
            DrawHeader("FPS Statistics");
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.FloatField("Current", currentFps);
                EditorGUILayout.FloatField("Average", fpsAverage);
                EditorGUILayout.FloatField("Min", fpsMin == float.MaxValue ? 0f : fpsMin);
                EditorGUILayout.FloatField("Max", fpsMax);
            }
            if (GUILayout.Button("통계 리셋", GUILayout.Height(22)))
            {
                ResetFpsStats();
            }

            // 렌더링 통계
            EditorGUILayout.Space(10);
            DrawHeader("Rendering Stats");
            DrawRenderingStats();

            // 메모리
            EditorGUILayout.Space(10);
            DrawHeader("Memory");

            double now = EditorApplication.timeSinceStartup;
            if (now - lastMemoryUpdateTime >= MEMORY_UPDATE_INTERVAL)
            {
                cachedTotalMemoryMB = System.GC.GetTotalMemory(false) / (1024 * 1024);
                lastMemoryUpdateTime = now;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.LongField("총 할당 메모리 (MB)", cachedTotalMemoryMB);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("메모리 갱신 (정확)"))
            {
                cachedTotalMemoryMB = System.GC.GetTotalMemory(true) / (1024 * 1024);
                lastMemoryUpdateTime = EditorApplication.timeSinceStartup;
            }
            if (GUILayout.Button("GC.Collect 강제 실행"))
            {
                System.GC.Collect();
                cachedTotalMemoryMB = System.GC.GetTotalMemory(false) / (1024 * 1024);
                Debug.Log("[SWTestTools] GC.Collect 실행됨");
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// FPS 히스토리를 간단한 막대 그래프로 그립니다.
        /// </summary>
        private void DrawFpsGraph()
        {
            const float labelWidth = 32f;
            Rect totalRect = GUILayoutUtility.GetRect(0, 80, GUILayout.ExpandWidth(true));
            Rect graphRect = new Rect(totalRect.x + labelWidth, totalRect.y,
                totalRect.width - labelWidth, totalRect.height);

            EditorGUI.DrawRect(graphRect, new Color(0.15f, 0.15f, 0.15f, 1f));

            if (fpsMax <= 0f) return;

            float graphMaxFps = Mathf.Max(fpsMax * 1.1f, 60f);

            // 히스토리 막대 (기준선보다 먼저 그려서 선이 위에 보이도록)
            float barWidth = graphRect.width / FPS_HISTORY_SIZE;
            for (int i = 0; i < FPS_HISTORY_SIZE; i++)
            {
                // 원형 버퍼를 시간순으로 읽기
                int idx = (fpsHistoryIndex + i) % FPS_HISTORY_SIZE;
                float value = fpsHistory[idx];
                if (value <= 0f) continue;

                float normalized = Mathf.Clamp01(value / graphMaxFps);
                float barHeight = graphRect.height * normalized;

                Color barColor;
                if (value >= 55f) barColor = new Color(0.4f, 0.9f, 0.4f, 1f);
                else if (value >= 28f) barColor = new Color(0.9f, 0.8f, 0.3f, 1f);
                else barColor = new Color(0.9f, 0.4f, 0.4f, 1f);

                Rect bar = new Rect(
                    graphRect.x + i * barWidth,
                    graphRect.y + graphRect.height - barHeight,
                    barWidth,
                    barHeight);
                EditorGUI.DrawRect(bar, barColor);
            }

            // 기준선 + 라벨용 스타일 (우측 정렬, 작은 글씨)
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
            labelStyle.alignment = TextAnchor.MiddleRight;
            labelStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f, 1f);

            DrawFpsGuideLine(graphRect, totalRect.x, labelWidth, 60f, graphMaxFps,
                new Color(0.3f, 0.7f, 0.3f, 0.7f), labelStyle);
            DrawFpsGuideLine(graphRect, totalRect.x, labelWidth, 30f, graphMaxFps,
                new Color(0.7f, 0.6f, 0.2f, 0.7f), labelStyle);
        }

        /// <summary>
        /// FPS 그래프에 기준선 하나와 해당 숫자 라벨을 그립니다.
        /// </summary>
        private void DrawFpsGuideLine(Rect graphRect, float labelX, float labelWidth,
            float fps, float graphMaxFps, Color lineColor, GUIStyle labelStyle)
        {
            if (fps > graphMaxFps) return;

            float y = graphRect.y + graphRect.height * (1f - fps / graphMaxFps);
            EditorGUI.DrawRect(new Rect(graphRect.x, y, graphRect.width, 1), lineColor);

            Rect labelRect = new Rect(labelX, y - 7f, labelWidth - 2f, 14f);
            GUI.Label(labelRect, ((int)fps).ToString(), labelStyle);
        }

        /// <summary>
        /// UnityStats로부터 렌더링 통계를 표시합니다.
        /// </summary>
        private void DrawRenderingStats()
        {
            if (unityStatsType == null)
            {
                EditorGUILayout.HelpBox("UnityStats API에 접근할 수 없습니다.", MessageType.Warning);
                return;
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 중에만 렌더링 통계가 표시됩니다.", MessageType.Info);
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                if (drawCallsProp != null)
                    EditorGUILayout.IntField("Draw Calls", (int)drawCallsProp.GetValue(null));
                if (batchesProp != null)
                    EditorGUILayout.IntField("Batches", (int)batchesProp.GetValue(null));
                if (setPassCallsProp != null)
                    EditorGUILayout.IntField("SetPass Calls", (int)setPassCallsProp.GetValue(null));
                if (trianglesProp != null)
                    EditorGUILayout.IntField("Triangles", (int)trianglesProp.GetValue(null));
                if (verticesProp != null)
                    EditorGUILayout.IntField("Vertices", (int)verticesProp.GetValue(null));
            }
        }

        private void SetTargetFPS(int fps)
        {
            targetFrameRate = fps;
            if (Application.isPlaying) Application.targetFrameRate = fps;
        }
        #endregion // Performance 탭

        #region Scene 탭
        /// <summary>
        /// 사용자가 자주 쓰는 씬들을 등록/저장/로드할 수 있는 탭
        /// </summary>
        private void DrawSceneTab()
        {
            DrawHeader("Current Scene");

            Scene activeScene = SceneManager.GetActiveScene();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Name", activeScene.name);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("저장", GUILayout.Height(22)))
            {
                EditorSceneManager.SaveOpenScenes();
            }
            if (GUILayout.Button("현재 씬 등록", GUILayout.Height(22)))
            {
                RegisterScene(activeScene.path);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            DrawHeader("Register Scene");

            // ObjectField를 통한 추가
            EditorGUILayout.BeginHorizontal();
            sceneToAdd = (SceneAsset)EditorGUILayout.ObjectField(sceneToAdd, typeof(SceneAsset), false);
            GUI.enabled = sceneToAdd != null;
            if (GUILayout.Button("추가", GUILayout.Width(60), GUILayout.Height(18)))
            {
                string path = AssetDatabase.GetAssetPath(sceneToAdd);
                RegisterScene(path);
                sceneToAdd = null;
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // 드래그앤드롭 영역
            Rect dropRect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "여기에 Scene Asset을 드래그하세요", EditorStyles.helpBox);
            HandleSceneDragAndDrop(dropRect);

            EditorGUILayout.Space(10);

            // 캐시된 헤더 문자열 사용
            if (registeredScenePaths.Count != lastRegisteredSceneCount)
            {
                lastRegisteredSceneCount = registeredScenePaths.Count;
                cachedRegisteredScenesHeader = $"Registered Scenes ({lastRegisteredSceneCount})";
            }
            DrawHeader(cachedRegisteredScenesHeader);

            if (registeredScenePaths.Count == 0)
            {
                EditorGUILayout.HelpBox("등록된 씬이 없습니다. 위에서 씬을 추가하세요.", MessageType.Info);
            }
            else
            {
                sceneListScroll = EditorGUILayout.BeginScrollView(sceneListScroll, GUILayout.MaxHeight(250));

                string activeScenePath = activeScene.path;

                for (int i = 0; i < registeredScenePaths.Count; i++)
                {
                    if (i >= sceneDisplayCaches.Count) break;
                    SceneDisplayCache cache = sceneDisplayCaches[i];

                    string path = cache.path;
                    bool isActive = path == activeScenePath;

                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                    GUILayout.Label(isActive ? "▶" : "  ", GUILayout.Width(15));

                    if (!cache.exists) GUI.color = Color.red;
                    GUILayout.Label(cache.content, GUILayout.ExpandWidth(true));
                    GUI.color = Color.white;

                    GUI.enabled = i > 0;
                    if (GUILayout.Button("▲", GUILayout.Width(22), GUILayout.Height(18)))
                    {
                        MoveScene(i, i - 1);
                        GUIUtility.ExitGUI();
                    }
                    GUI.enabled = i < registeredScenePaths.Count - 1;
                    if (GUILayout.Button("▼", GUILayout.Width(22), GUILayout.Height(18)))
                    {
                        MoveScene(i, i + 1);
                        GUIUtility.ExitGUI();
                    }
                    GUI.enabled = true;

                    GUI.enabled = cache.exists && !isActive;
                    if (GUILayout.Button("Load", GUILayout.Width(45), GUILayout.Height(18)))
                    {
                        LoadScene(path);
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("+", GUILayout.Width(22), GUILayout.Height(18)))
                    {
                        LoadSceneAdditive(path);
                        GUIUtility.ExitGUI();
                    }
                    GUI.enabled = true;

                    if (GUILayout.Button("◎", GUILayout.Width(22), GUILayout.Height(18)))
                    {
                        PingScene(path);
                    }

                    if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(18)))
                    {
                        registeredScenePaths.RemoveAt(i);
                        SaveRegisteredScenes();
                        RebuildSceneDisplayCaches();
                        GUIUtility.ExitGUI();
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("모두 삭제", GUILayout.Height(22)))
                {
                    if (EditorUtility.DisplayDialog("확인",
                        "등록된 모든 씬을 목록에서 제거하시겠습니까?\n(씬 파일 자체는 삭제되지 않습니다)", "삭제", "취소"))
                    {
                        registeredScenePaths.Clear();
                        SaveRegisteredScenes();
                        RebuildSceneDisplayCaches();
                    }
                }
                if (GUILayout.Button("존재하지 않는 항목 정리", GUILayout.Height(22)))
                {
                    registeredScenePaths.RemoveAll(p => !File.Exists(p));
                    SaveRegisteredScenes();
                    RebuildSceneDisplayCaches();
                }
                if (GUILayout.Button("새로고침", GUILayout.Height(22)))
                {
                    RebuildSceneDisplayCaches();
                }
                EditorGUILayout.EndHorizontal();
            }

            // Build Settings Scenes
            EditorGUILayout.Space(10);
            showBuildSettingsScenes = EditorGUILayout.Foldout(showBuildSettingsScenes,
                $"Build Settings Scenes ({EditorBuildSettings.scenes.Length})", true);
            if (showBuildSettingsScenes)
            {
                DrawBuildSettingsScenes(activeScene.path);
            }

            // GameObject Bookmarks
            EditorGUILayout.Space(10);
            DrawHeader($"GameObject Bookmarks ({bookmarkedObjects.Count})");
            DrawBookmarkedObjects();
        }

        /// <summary>
        /// BuildSettings에 등록된 씬들을 표시합니다.
        /// </summary>
        private void DrawBuildSettingsScenes(string activeScenePath)
        {
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            if (buildScenes.Length == 0)
            {
                EditorGUILayout.HelpBox("Build Settings에 등록된 씬이 없습니다.", MessageType.Info);
                return;
            }

            buildScenesScroll = EditorGUILayout.BeginScrollView(buildScenesScroll, GUILayout.MaxHeight(180));

            for (int i = 0; i < buildScenes.Length; i++)
            {
                EditorBuildSettingsScene bScene = buildScenes[i];
                bool exists = File.Exists(bScene.path);
                bool isActive = bScene.path == activeScenePath;

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                GUILayout.Label($"[{i}]", GUILayout.Width(28));
                GUILayout.Label(isActive ? "▶" : "  ", GUILayout.Width(15));

                if (!exists) GUI.color = Color.red;
                else if (!bScene.enabled) GUI.color = Color.gray;

                string name = Path.GetFileNameWithoutExtension(bScene.path);
                if (!bScene.enabled) name += " (비활성)";
                GUILayout.Label(name, GUILayout.ExpandWidth(true));
                GUI.color = Color.white;

                GUI.enabled = exists && !isActive;
                if (GUILayout.Button("Load", GUILayout.Width(45), GUILayout.Height(18)))
                {
                    LoadScene(bScene.path);
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("+", GUILayout.Width(22), GUILayout.Height(18)))
                {
                    LoadSceneAdditive(bScene.path);
                    GUIUtility.ExitGUI();
                }
                GUI.enabled = true;

                if (GUILayout.Button("★", GUILayout.Width(22), GUILayout.Height(18)))
                {
                    RegisterScene(bScene.path);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 북마크된 GameObject 목록을 그립니다.
        /// </summary>
        private void DrawBookmarkedObjects()
        {
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = Selection.activeGameObject != null;
            if (GUILayout.Button("선택한 GameObject 북마크", GUILayout.Height(22)))
            {
                BookmarkSelectedObject();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (bookmarkedObjects.Count == 0)
            {
                EditorGUILayout.HelpBox("북마크된 GameObject가 없습니다. 씬에서 오브젝트를 선택한 후 위 버튼을 누르세요.", MessageType.Info);
                return;
            }

            bookmarkScroll = EditorGUILayout.BeginScrollView(bookmarkScroll, GUILayout.MaxHeight(180));

            for (int i = 0; i < bookmarkedObjects.Count; i++)
            {
                string entry = bookmarkedObjects[i];
                ParseBookmark(entry, out string scenePath, out string hierarchyPath);

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                string objName = GetLastHierarchyName(hierarchyPath);
                GUILayout.Label(new GUIContent($"{objName}", $"Scene: {sceneName}\nPath: {hierarchyPath}"),
                    GUILayout.ExpandWidth(true));

                if (GUILayout.Button("Select", GUILayout.Width(55), GUILayout.Height(18)))
                {
                    SelectBookmark(scenePath, hierarchyPath);
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("Focus", GUILayout.Width(50), GUILayout.Height(18)))
                {
                    SelectBookmark(scenePath, hierarchyPath);
                    if (SceneView.lastActiveSceneView != null)
                    {
                        SceneView.lastActiveSceneView.FrameSelected();
                    }
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(18)))
                {
                    bookmarkedObjects.RemoveAt(i);
                    SaveBookmarkedObjects();
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            if (bookmarkedObjects.Count > 0)
            {
                if (GUILayout.Button("북마크 모두 삭제", GUILayout.Height(20)))
                {
                    if (EditorUtility.DisplayDialog("확인", "모든 GameObject 북마크를 삭제하시겠습니까?", "삭제", "취소"))
                    {
                        bookmarkedObjects.Clear();
                        SaveBookmarkedObjects();
                    }
                }
            }
        }

        private void BookmarkSelectedObject()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null) return;

            string scenePath = selected.scene.path;
            string hierarchyPath = GetHierarchyPath(selected.transform);
            string entry = $"{scenePath}|{hierarchyPath}";

            if (bookmarkedObjects.Contains(entry))
            {
                Debug.Log($"[SWTestTools] 이미 북마크된 GameObject입니다: {hierarchyPath}");
                return;
            }

            bookmarkedObjects.Add(entry);
            SaveBookmarkedObjects();
        }

        private string GetHierarchyPath(Transform t)
        {
            if (t.parent == null) return t.name;
            return GetHierarchyPath(t.parent) + "/" + t.name;
        }

        private void ParseBookmark(string entry, out string scenePath, out string hierarchyPath)
        {
            int sep = entry.IndexOf('|');
            if (sep < 0)
            {
                scenePath = "";
                hierarchyPath = entry;
                return;
            }
            scenePath = entry.Substring(0, sep);
            hierarchyPath = entry.Substring(sep + 1);
        }

        private string GetLastHierarchyName(string hierarchyPath)
        {
            int slash = hierarchyPath.LastIndexOf('/');
            return slash < 0 ? hierarchyPath : hierarchyPath.Substring(slash + 1);
        }

        private void SelectBookmark(string scenePath, string hierarchyPath)
        {
            Scene currentActive = SceneManager.GetActiveScene();
            if (currentActive.path != scenePath && !string.IsNullOrEmpty(scenePath))
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                if (File.Exists(scenePath))
                {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                }
                else
                {
                    Debug.LogWarning($"[SWTestTools] 씬 파일이 없습니다: {scenePath}");
                    return;
                }
            }

            GameObject found = FindByHierarchyPath(hierarchyPath);
            if (found != null)
            {
                Selection.activeGameObject = found;
                EditorGUIUtility.PingObject(found);
            }
            else
            {
                Debug.LogWarning($"[SWTestTools] GameObject를 찾을 수 없습니다: {hierarchyPath}");
            }
        }

        private GameObject FindByHierarchyPath(string hierarchyPath)
        {
            if (string.IsNullOrEmpty(hierarchyPath)) return null;

            string[] parts = hierarchyPath.Split('/');
            if (parts.Length == 0) return null;

            // 루트 찾기
            Scene active = SceneManager.GetActiveScene();
            GameObject[] roots = active.GetRootGameObjects();
            GameObject current = null;
            foreach (GameObject root in roots)
            {
                if (root.name == parts[0])
                {
                    current = root;
                    break;
                }
            }

            if (current == null) return null;

            for (int i = 1; i < parts.Length; i++)
            {
                Transform child = current.transform.Find(parts[i]);
                if (child == null) return null;
                current = child.gameObject;
            }

            return current;
        }

        private void SaveBookmarkedObjects()
        {
            string joined = string.Join(";;", bookmarkedObjects);
            EditorPrefs.SetString(GetProjectKey(BOOKMARKED_OBJECTS_KEY), joined);
        }

        private void LoadBookmarkedObjects()
        {
            string joined = EditorPrefs.GetString(GetProjectKey(BOOKMARKED_OBJECTS_KEY), "");
            bookmarkedObjects.Clear();
            if (!string.IsNullOrEmpty(joined))
            {
                string[] entries = joined.Split(new[] { ";;" }, System.StringSplitOptions.RemoveEmptyEntries);
                bookmarkedObjects.AddRange(entries);
            }
        }

        /// <summary>
        /// 드래그앤드롭으로 SceneAsset을 등록 목록에 추가합니다.
        /// </summary>
        private void HandleSceneDragAndDrop(Rect dropRect)
        {
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
                        if (obj is SceneAsset)
                        {
                            string path = AssetDatabase.GetAssetPath(obj);
                            RegisterScene(path);
                        }
                    }
                    evt.Use();
                }
            }
        }

        /// <summary>
        /// 씬 경로를 등록 목록에 추가합니다. 중복은 무시됩니다.
        /// </summary>
        private void RegisterScene(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (registeredScenePaths.Contains(path))
            {
                Debug.Log($"[SWTestTools] 이미 등록된 씬입니다: {path}");
                return;
            }

            registeredScenePaths.Add(path);
            SaveRegisteredScenes();
            RebuildSceneDisplayCaches();
        }

        /// <summary>
        /// 등록된 씬의 순서를 변경합니다.
        /// </summary>
        private void MoveScene(int from, int to)
        {
            string item = registeredScenePaths[from];
            registeredScenePaths.RemoveAt(from);
            registeredScenePaths.Insert(to, item);
            SaveRegisteredScenes();
            RebuildSceneDisplayCaches();
        }

        /// <summary>
        /// 씬 표시용 캐시를 재구성합니다.
        /// </summary>
        private void RebuildSceneDisplayCaches()
        {
            sceneDisplayCaches.Clear();
            foreach (string path in registeredScenePaths)
            {
                bool exists = File.Exists(path);
                string displayName = Path.GetFileNameWithoutExtension(path);
                if (!exists) displayName += " (없음)";

                sceneDisplayCaches.Add(new SceneDisplayCache
                {
                    path = path,
                    displayName = displayName,
                    content = new GUIContent(displayName, path),
                    exists = exists,
                });
            }
        }

        /// <summary>
        /// 지정된 씬을 단일 모드로 엽니다. 변경사항은 저장 여부를 확인합니다.
        /// </summary>
        private void LoadScene(string path)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            }
        }

        /// <summary>
        /// 지정된 씬을 Additive 모드로 추가 로드합니다.
        /// </summary>
        private void LoadSceneAdditive(string path)
        {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        /// <summary>
        /// 프로젝트 창에서 해당 씬 에셋을 하이라이트합니다.
        /// </summary>
        private void PingScene(string path)
        {
            SceneAsset asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (asset != null)
            {
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
            }
        }

        /// <summary>
        /// EditorPrefs에 등록된 씬 목록을 저장합니다. 구분자로 '|' 사용.
        /// </summary>
        private void SaveRegisteredScenes()
        {
            string joined = string.Join("|", registeredScenePaths);
            EditorPrefs.SetString(GetProjectKey(REGISTERED_SCENES_KEY), joined);
        }

        /// <summary>
        /// EditorPrefs에서 등록된 씬 목록을 불러옵니다.
        /// </summary>
        private void LoadRegisteredScenes()
        {
            string joined = EditorPrefs.GetString(GetProjectKey(REGISTERED_SCENES_KEY), "");
            registeredScenePaths.Clear();

            if (!string.IsNullOrEmpty(joined))
            {
                string[] paths = joined.Split('|');
                foreach (string path in paths)
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        registeredScenePaths.Add(path);
                    }
                }
            }
        }

        /// <summary>
        /// 프로젝트별로 고유한 EditorPrefs 키를 생성합니다.
        /// </summary>
        private string GetProjectKey(string key)
        {
            return $"{key}.{Application.dataPath.GetHashCode()}";
        }
        #endregion // Scene 탭

        #region Utility 탭
        /// <summary>
        /// 기타 테스트 유틸리티
        /// </summary>
        private void DrawUtilityTab()
        {
            DrawHeader("PlayerPrefs");
            if (GUILayout.Button("PlayerPrefs.DeleteAll", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("PlayerPrefs 삭제",
                    "모든 PlayerPrefs를 삭제하시겠습니까?", "삭제", "취소"))
                {
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();
                    Debug.Log("[SWTestTools] PlayerPrefs 전체 삭제됨");
                }
            }

            EditorGUILayout.Space(10);
            DrawHeader("Editor");
            if (GUILayout.Button("Console Clear", GUILayout.Height(25)))
            {
                ClearConsole();
            }

            if (GUILayout.Button("GameObject 선택 해제", GUILayout.Height(25)))
            {
                Selection.activeObject = null;
            }

            EditorGUILayout.Space(10);
            DrawHeader("Screen");

            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                lastScreenWidth = Screen.width;
                lastScreenHeight = Screen.height;
                cachedScreenInfo = $"Screen: {lastScreenWidth} x {lastScreenHeight}";
            }
            if (!Mathf.Approximately(Screen.dpi, lastScreenDpi))
            {
                lastScreenDpi = Screen.dpi;
                cachedDpiInfo = $"DPI: {lastScreenDpi}";
            }

            EditorGUILayout.LabelField(cachedScreenInfo);
            EditorGUILayout.LabelField(cachedDpiInfo);
        }

        /// <summary>
        /// 리플렉션을 사용해 Console을 클리어합니다.
        /// </summary>
        private void ClearConsole()
        {
            var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
            var clearMethod = logEntries?.GetMethod("Clear",
                BindingFlags.Static | BindingFlags.Public);
            clearMethod?.Invoke(null, null);
        }
        #endregion // Utility 탭

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