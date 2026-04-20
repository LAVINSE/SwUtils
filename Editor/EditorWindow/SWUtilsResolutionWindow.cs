#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SWUtils.Editor
{
    /// <summary>
    /// SWUtilsResolution 기능을 시각적으로 확인하고 테스트할 수 있는 에디터 윈도우.
    /// 실시간 해상도 모니터링, 기준 해상도 설정, 좌표 변환 테스트, 카메라 FOV 테스트 기능을 제공한다.
    /// 메뉴: Tools > SWUtils > Resolution Window
    /// </summary>
    public class SWUtilsResolutionWindow : EditorWindow
    {
        #region 필드
        /// <summary>탭 종류.</summary>
        private enum Tab
        {
            /// <summary>현재 해상도 정보 모니터링.</summary>
            Monitor,
            /// <summary>기준 해상도(BaseResolution) 설정.</summary>
            BaseResolution,
            /// <summary>스크린 ↔ 정규화 좌표 변환 테스트.</summary>
            CoordinateTest,
            /// <summary>카메라 FOV/Orthographic 테스트.</summary>
            CameraTest,
            /// <summary>DPI / 물리 크기 변환 테스트.</summary>
            DPITest,
        }

        /// <summary>현재 선택된 탭.</summary>
        private Tab currentTab = Tab.Monitor;
        /// <summary>스크롤 위치.</summary>
        private Vector2 scrollPosition;

        // 좌표 변환 테스트용
        /// <summary>좌표 변환 테스트 입력 스크린 좌표.</summary>
        private Vector2 testScreenPosition;
        /// <summary>좌표 변환 테스트 입력 정규화 좌표.</summary>
        private Vector2 testNormalizedPosition;

        // 카메라 테스트용
        /// <summary>카메라 FOV 테스트 대상 카메라.</summary>
        private Camera testCamera;
        /// <summary>테스트할 수평 FOV 값.</summary>
        private float testHorizontalFov = 60f;
        /// <summary>테스트할 기준 orthographicSize 값.</summary>
        private float testBaseOrthoSize = 5f;

        // DPI 테스트용
        /// <summary>DPI 테스트 입력 값.</summary>
        private float testDpiInputValue = 10f;
        #endregion // 필드

        #region 윈도우 열기
        /// <summary>
        /// 메뉴에서 윈도우를 연다.
        /// </summary>
        [MenuItem("SWTools/Resolution Window")]
        public static void OpenWindow()
        {
            var window = GetWindow<SWUtilsResolutionWindow>("Resolution");
            window.minSize = new Vector2(360f, 400f);
            window.Show();
        }
        #endregion // 윈도우 열기

        #region GUI
        /// <summary>
        /// 매 프레임 갱신하여 실시간 모니터링을 지원한다.
        /// </summary>
        private void OnInspectorUpdate()
        {
            if (currentTab == Tab.Monitor) Repaint();
        }

        /// <summary>
        /// 윈도우 GUI를 그린다.
        /// </summary>
        private void OnGUI()
        {
            DrawHeader();
            DrawTabs();

            EditorGUILayout.Space(4f);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (currentTab)
            {
                case Tab.Monitor: DrawMonitorTab(); break;
                case Tab.BaseResolution: DrawBaseResolutionTab(); break;
                case Tab.CoordinateTest: DrawCoordinateTab(); break;
                case Tab.CameraTest: DrawCameraTab(); break;
                case Tab.DPITest: DrawDpiTab(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 헤더를 그린다.
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("SWUtils Resolution Tool", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("해상도 관련 유틸리티를 시각적으로 테스트합니다.", EditorStyles.miniLabel);
            EditorGUILayout.Space(4f);
        }

        /// <summary>
        /// 탭 버튼을 그린다.
        /// </summary>
        private void DrawTabs()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                currentTab = (Tab)GUILayout.Toolbar((int)currentTab,
                    new[] { "모니터", "기준해상도", "좌표변환", "카메라", "DPI" });
            }
        }
        #endregion // GUI

        #region 모니터 탭
        /// <summary>
        /// 실시간 해상도 정보를 표시한다.
        /// </summary>
        private void DrawMonitorTab()
        {
            EditorGUILayout.LabelField("실시간 화면 정보", EditorStyles.boldLabel);
            EditorGUILayout.Space(2f);

            DrawInfo("Screen.width x height", $"{Screen.width} x {Screen.height}");
            DrawInfo("SWUtilsResolution.ScreenWidth", SWUtilsResolution.ScreenWidth.ToString("F1"));
            DrawInfo("SWUtilsResolution.ScreenHeight", SWUtilsResolution.ScreenHeight.ToString("F1"));
            DrawInfo("ScreenAspect (w/h)", SWUtilsResolution.ScreenAspect.ToString("F3"));
            DrawInfo("Ratio (h/w)", ((float)Screen.height / Screen.width).ToString("F3"));
            DrawInfo("IsPortrait", SWUtilsResolution.IsPortrait.ToString());
            DrawInfo("IsLandscape", SWUtilsResolution.IsLandscape.ToString());
            DrawInfo("Orientation", Screen.orientation.ToString());

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("DPI 정보", EditorStyles.boldLabel);
            EditorGUILayout.Space(2f);
            DrawInfo("Screen.dpi", Screen.dpi > 0f ? Screen.dpi.ToString("F1") : "Unknown");
            DrawInfo("DPI Category", SWUtilsResolution.DpiCategory.ToString());
            DrawInfo("MinTouchSize (px)", SWUtilsResolution.GetMinTouchSizePixels().ToString("F1"));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("SafeArea", EditorStyles.boldLabel);
            EditorGUILayout.Space(2f);
            DrawInfo("Screen.safeArea", Screen.safeArea.ToString());

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox("Game View 또는 Device Simulator 해상도를 변경하면 값이 실시간으로 갱신됩니다.", MessageType.Info);
        }

        /// <summary>
        /// 한 줄짜리 라벨-값 정보를 그린다.
        /// </summary>
        /// <param name="label">라벨</param>
        /// <param name="value">값</param>
        private void DrawInfo(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(220f));
                EditorGUILayout.SelectableLabel(value, EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
        }
        #endregion // 모니터 탭

        #region 기준 해상도 탭
        /// <summary>
        /// 기준 해상도 설정을 그린다.
        /// </summary>
        private void DrawBaseResolutionTab()
        {
            EditorGUILayout.LabelField("기준 해상도 (BaseResolution)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "프로젝트의 디자인 기준 해상도를 설정합니다. " +
                "SWUtilsResolution.GetBaseResolutionScale() 등에서 사용됩니다.\n" +
                "※ 런타임에만 유효하며, 플레이 모드에서도 값이 유지되진 않습니다.",
                MessageType.Info);
            EditorGUILayout.Space(4f);

            float newBaseWidth = EditorGUILayout.FloatField("Base Width", SWUtilsResolution.BaseWidth);
            float newBaseHeight = EditorGUILayout.FloatField("Base Height", SWUtilsResolution.BaseHeight);

            if (!Mathf.Approximately(newBaseWidth, SWUtilsResolution.BaseWidth))
                SWUtilsResolution.BaseWidth = newBaseWidth;
            if (!Mathf.Approximately(newBaseHeight, SWUtilsResolution.BaseHeight))
                SWUtilsResolution.BaseHeight = newBaseHeight;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("프리셋", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("1080x1920 (세로)"))
                {
                    SWUtilsResolution.BaseWidth = 1080f;
                    SWUtilsResolution.BaseHeight = 1920f;
                }
                if (GUILayout.Button("1920x1080 (가로)"))
                {
                    SWUtilsResolution.BaseWidth = 1920f;
                    SWUtilsResolution.BaseHeight = 1080f;
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("750x1334 (iPhone)"))
                {
                    SWUtilsResolution.BaseWidth = 750f;
                    SWUtilsResolution.BaseHeight = 1334f;
                }
                if (GUILayout.Button("2048x1536 (iPad)"))
                {
                    SWUtilsResolution.BaseWidth = 2048f;
                    SWUtilsResolution.BaseHeight = 1536f;
                }
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("계산 결과", EditorStyles.boldLabel);
            EditorGUILayout.Space(2f);
            DrawInfo("BaseAspect (w/h)", SWUtilsResolution.BaseAspect.ToString("F3"));
            DrawInfo("ScreenAspect (w/h)", SWUtilsResolution.ScreenAspect.ToString("F3"));
            DrawInfo("GetBaseResolutionScale", SWUtilsResolution.GetBaseResolutionScale().ToString("F3"));
            DrawInfo("LetterboxHeight (px)", SWUtilsResolution.GetLetterboxHeight(SWUtilsResolution.BaseAspect).ToString("F1"));
            DrawInfo("PillarboxWidth (px)", SWUtilsResolution.GetPillarboxWidth(SWUtilsResolution.BaseAspect).ToString("F1"));
        }
        #endregion // 기준 해상도 탭

        #region 좌표 변환 탭
        /// <summary>
        /// 좌표 변환 테스트를 그린다.
        /// </summary>
        private void DrawCoordinateTab()
        {
            EditorGUILayout.LabelField("스크린 → 정규화", EditorStyles.boldLabel);
            testScreenPosition = EditorGUILayout.Vector2Field("Screen Position (px)", testScreenPosition);
            Vector2 normalized = SWUtilsResolution.ToNormalized(testScreenPosition);
            DrawInfo("Normalized (0~1)", $"({normalized.x:F3}, {normalized.y:F3})");

            EditorGUILayout.Space(8f);

            EditorGUILayout.LabelField("정규화 → 스크린", EditorStyles.boldLabel);
            testNormalizedPosition = EditorGUILayout.Vector2Field("Normalized (0~1)", testNormalizedPosition);
            Vector2 screenResult = SWUtilsResolution.FromNormalized(testNormalizedPosition);
            DrawInfo("Screen Position (px)", $"({screenResult.x:F1}, {screenResult.y:F1})");

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("중앙 좌표 설정"))
                {
                    testScreenPosition = SWUtilsResolution.ScreenCenter;
                    testNormalizedPosition = new Vector2(0.5f, 0.5f);
                }
                if (GUILayout.Button("우상단 좌표 설정"))
                {
                    testScreenPosition = SWUtilsResolution.ScreenSize2D;
                    testNormalizedPosition = new Vector2(1f, 1f);
                }
            }
        }
        #endregion // 좌표 변환 탭

        #region 카메라 탭
        /// <summary>
        /// 카메라 FOV / Orthographic 테스트를 그린다.
        /// </summary>
        private void DrawCameraTab()
        {
            EditorGUILayout.LabelField("카메라 선택", EditorStyles.boldLabel);
            testCamera = (Camera)EditorGUILayout.ObjectField("Camera", testCamera, typeof(Camera), true);
            if (testCamera == null && Camera.main != null && GUILayout.Button("Main Camera 사용"))
            {
                testCamera = Camera.main;
            }

            EditorGUILayout.Space(8f);

            if (testCamera == null)
            {
                EditorGUILayout.HelpBox("카메라를 선택하세요.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("현재 카메라 정보", EditorStyles.boldLabel);
            DrawInfo("Is Orthographic", testCamera.orthographic.ToString());
            DrawInfo("Vertical FOV", testCamera.fieldOfView.ToString("F2"));
            DrawInfo("Aspect", testCamera.aspect.ToString("F3"));
            DrawInfo("Horizontal FOV (계산)", testCamera.GetHorizontalFov().ToString("F2"));
            DrawInfo("Orthographic Size", testCamera.orthographicSize.ToString("F2"));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("수평 FOV 적용 테스트", EditorStyles.boldLabel);
            testHorizontalFov = EditorGUILayout.Slider("Horizontal FOV", testHorizontalFov, 10f, 170f);
            if (GUILayout.Button("SetHorizontalFov 적용"))
            {
                Undo.RecordObject(testCamera, "Set Horizontal FOV");
                testCamera.SetHorizontalFov(testHorizontalFov);
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Orthographic 크기 자동 조정", EditorStyles.boldLabel);
            testBaseOrthoSize = EditorGUILayout.FloatField("Base OrthographicSize", testBaseOrthoSize);
            using (new EditorGUI.DisabledScope(!testCamera.orthographic))
            {
                if (GUILayout.Button("FitOrthographicSize 적용"))
                {
                    Undo.RecordObject(testCamera, "Fit Orthographic Size");
                    testCamera.FitOrthographicSize(testBaseOrthoSize);
                }
            }
            if (!testCamera.orthographic)
            {
                EditorGUILayout.HelpBox("FitOrthographicSize는 Orthographic 카메라에만 적용됩니다.", MessageType.Info);
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("월드 화면 경계", EditorStyles.boldLabel);
            Rect worldBounds = SWUtilsResolution.GetWorldScreenBounds(testCamera, 10f);
            DrawInfo("World Bounds @z=10", worldBounds.ToString());
        }
        #endregion // 카메라 탭

        #region DPI 탭
        /// <summary>
        /// DPI / 물리 크기 변환 테스트를 그린다.
        /// </summary>
        private void DrawDpiTab()
        {
            EditorGUILayout.LabelField("DPI 정보", EditorStyles.boldLabel);
            DrawInfo("Screen.dpi", Screen.dpi > 0f ? Screen.dpi.ToString("F1") : "Unknown (96 가정)");
            DrawInfo("DPI Category", SWUtilsResolution.DpiCategory.ToString());

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("변환 테스트", EditorStyles.boldLabel);
            testDpiInputValue = EditorGUILayout.FloatField("입력 값", testDpiInputValue);

            EditorGUILayout.Space(4f);
            DrawInfo($"{testDpiInputValue} inches → pixels",
                SWUtilsResolution.InchesToPixels(testDpiInputValue).ToString("F1"));
            DrawInfo($"{testDpiInputValue} pixels → inches",
                SWUtilsResolution.PixelsToInches(testDpiInputValue).ToString("F3"));
            DrawInfo($"{testDpiInputValue} mm → pixels",
                SWUtilsResolution.MillimetersToPixels(testDpiInputValue).ToString("F1"));
            DrawInfo($"{testDpiInputValue} pixels → mm",
                SWUtilsResolution.PixelsToMillimeters(testDpiInputValue).ToString("F3"));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("접근성 가이드라인", EditorStyles.boldLabel);
            DrawInfo("최소 터치 타겟 (9mm 기준)", $"{SWUtilsResolution.GetMinTouchSizePixels():F1} px");
            EditorGUILayout.HelpBox(
                "iOS HIG: 44pt (~9mm)\n" +
                "Material Design: 48dp (~9mm)\n" +
                "버튼의 최소 크기는 위 값 이상이어야 합니다.",
                MessageType.Info);
        }
        #endregion // DPI 탭
    }
}
#endif