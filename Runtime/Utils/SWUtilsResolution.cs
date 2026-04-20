using System;
using UnityEngine;

namespace SWUtils
{
    /// <summary>
    /// 해상도 관련 정적 유틸리티.
    /// 에디터에서는 Game View 카메라의 픽셀 크기를, 빌드에서는 Screen 크기를 사용한다.
    /// 해상도/방향 변경 이벤트도 전역으로 제공한다.
    /// </summary>
    public static class SWUtilsResolution
    {
        #region 필드
        /// <summary>프로젝트 기준 해상도 너비(디자인 기준).</summary>
        public static float BaseWidth { get; set; } = 1080f;
        /// <summary>프로젝트 기준 해상도 높이(디자인 기준).</summary>
        public static float BaseHeight { get; set; } = 1920f;

        /// <summary>마지막으로 감지된 화면 너비.</summary>
        private static int lastScreenWidth;
        /// <summary>마지막으로 감지된 화면 높이.</summary>
        private static int lastScreenHeight;
        /// <summary>마지막으로 감지된 화면 방향.</summary>
        private static ScreenOrientation lastOrientation;
        /// <summary>이벤트 감지 초기화 여부.</summary>
        private static bool isWatcherInitialized;
        #endregion // 필드

        #region 이벤트
        /// <summary>
        /// 해상도가 변경되었을 때 발생하는 이벤트. (newWidth, newHeight)
        /// 사용하려면 SWUtilsResolution.StartWatcher()를 한 번 호출해야 한다.
        /// </summary>
        public static event Action<int, int> OnResolutionChanged;

        /// <summary>
        /// 화면 방향이 변경되었을 때 발생하는 이벤트.
        /// 사용하려면 SWUtilsResolution.StartWatcher()를 한 번 호출해야 한다.
        /// </summary>
        public static event Action<ScreenOrientation> OnOrientationChanged;
        #endregion // 이벤트

        #region 프로퍼티
        /// <summary>
        /// 현재 화면 너비(픽셀).
        /// 에디터에서는 메인 카메라의 pixelWidth를 반환한다.
        /// </summary>
        public static float ScreenWidth
        {
            get
            {
#if UNITY_EDITOR
                var mainCamera = Camera.main;
                return mainCamera != null ? mainCamera.pixelWidth : Screen.width;
#else
                return Screen.width;
#endif // UNITY_EDITOR
            }
        }

        /// <summary>
        /// 현재 화면 높이(픽셀).
        /// 에디터에서는 메인 카메라의 pixelHeight를 반환한다.
        /// </summary>
        public static float ScreenHeight
        {
            get
            {
#if UNITY_EDITOR
                var mainCamera = Camera.main;
                return mainCamera != null ? mainCamera.pixelHeight : Screen.height;
#else
                return Screen.height;
#endif // UNITY_EDITOR
            }
        }

        /// <summary>현재 화면 크기를 Vector3로 반환한다. (z = 0)</summary>
        public static Vector3 ScreenSize => new Vector3(ScreenWidth, ScreenHeight, 0f);

        /// <summary>현재 화면 크기를 Vector2로 반환한다.</summary>
        public static Vector2 ScreenSize2D => new Vector2(ScreenWidth, ScreenHeight);

        /// <summary>화면 중앙 좌표.</summary>
        public static Vector2 ScreenCenter => new Vector2(ScreenWidth * 0.5f, ScreenHeight * 0.5f);

        /// <summary>현재 화면의 종횡비 (width / height).</summary>
        public static float ScreenAspect => ScreenHeight > 0f ? ScreenWidth / ScreenHeight : 1f;

        /// <summary>기준 해상도의 종횡비 (BaseWidth / BaseHeight).</summary>
        public static float BaseAspect => BaseHeight > 0f ? BaseWidth / BaseHeight : 1f;

        /// <summary>현재 화면이 세로 모드인지 여부.</summary>
        public static bool IsPortrait => ScreenHeight > ScreenWidth;

        /// <summary>현재 화면이 가로 모드인지 여부.</summary>
        public static bool IsLandscape => ScreenWidth >= ScreenHeight;

        /// <summary>
        /// 현재 DPI 카테고리.
        /// Android 기준: ldpi(&lt;140) / mdpi(~160) / hdpi(~240) / xhdpi(~320) / xxhdpi(~480) / xxxhdpi(&gt;480)
        /// </summary>
        public static DPICategory DpiCategory
        {
            get
            {
                float dpi = Screen.dpi;
                if (dpi <= 0f) return DPICategory.Unknown;
                if (dpi < 140f) return DPICategory.LDPI;
                if (dpi < 200f) return DPICategory.MDPI;
                if (dpi < 280f) return DPICategory.HDPI;
                if (dpi < 400f) return DPICategory.XHDPI;
                if (dpi < 560f) return DPICategory.XXHDPI;
                return DPICategory.XXXHDPI;
            }
        }
        #endregion // 프로퍼티

        #region 열거형
        /// <summary>DPI 카테고리.</summary>
        public enum DPICategory
        {
            /// <summary>DPI 정보 없음.</summary>
            Unknown,
            /// <summary>저밀도 (~120 dpi).</summary>
            LDPI,
            /// <summary>중밀도 (~160 dpi).</summary>
            MDPI,
            /// <summary>고밀도 (~240 dpi).</summary>
            HDPI,
            /// <summary>초고밀도 (~320 dpi).</summary>
            XHDPI,
            /// <summary>2배 초고밀도 (~480 dpi).</summary>
            XXHDPI,
            /// <summary>3배 초고밀도 (640+ dpi).</summary>
            XXXHDPI,
        }
        #endregion // 열거형

        #region 해상도 감지
        /// <summary>
        /// 해상도/방향 변경 감지를 시작한다.
        /// 런타임에 한 번 호출하면 매 프레임 체크하여 이벤트를 발생시킨다.
        /// 내부적으로 GameObject를 하나 생성하여 MonoBehaviour로 Update를 돌린다.
        /// </summary>
        public static void StartWatcher()
        {
            if (isWatcherInitialized) return;
            isWatcherInitialized = true;

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastOrientation = Screen.orientation;

            var watcherObject = new GameObject("[SWUtilsResolutionWatcher]");
            UnityEngine.Object.DontDestroyOnLoad(watcherObject);
            watcherObject.hideFlags = HideFlags.HideAndDontSave;
            watcherObject.AddComponent<ResolutionWatcher>();
        }

        /// <summary>
        /// 외부에서 강제로 해상도 변경을 체크하고 이벤트를 발생시킨다.
        /// </summary>
        internal static void CheckResolutionChange()
        {
            if (lastScreenWidth != Screen.width || lastScreenHeight != Screen.height)
            {
                lastScreenWidth = Screen.width;
                lastScreenHeight = Screen.height;
                OnResolutionChanged?.Invoke(Screen.width, Screen.height);
            }

            if (lastOrientation != Screen.orientation)
            {
                lastOrientation = Screen.orientation;
                OnOrientationChanged?.Invoke(Screen.orientation);
            }
        }

        /// <summary>
        /// 해상도 변경을 매 프레임 감지하는 내부 MonoBehaviour.
        /// </summary>
        private class ResolutionWatcher : MonoBehaviour
        {
            /// <summary>매 프레임 해상도 변경을 체크한다.</summary>
            private void Update()
            {
                CheckResolutionChange();
            }
        }
        #endregion // 해상도 감지

        #region 해상도 스케일
        /// <summary>
        /// 기준 해상도 비율(scale.x / scale.y)로 계산한 너비를 반환한다.
        /// 화면 높이에 비율을 곱하여 필요한 너비를 산출한다.
        /// </summary>
        /// <param name="scale">기준 해상도 벡터 (x=너비, y=높이)</param>
        /// <returns>현재 화면 높이 기준의 비례 너비</returns>
        public static float GetResolutionWidth(Vector3 scale)
        {
            if (scale.y <= 0f) return 0f;
            float aspect = scale.x / scale.y;
            return ScreenHeight * aspect;
        }

        /// <summary>
        /// 기준 해상도에 맞추기 위한 스케일 비율을 반환한다.
        /// 현재 화면이 기준보다 좁으면 축소 비율(0~1), 넓으면 1을 반환한다.
        /// </summary>
        /// <param name="scale">기준 해상도 벡터</param>
        /// <returns>적용할 스케일 비율</returns>
        public static float GetResolutionScale(Vector3 scale)
        {
            float resolutionWidth = GetResolutionWidth(scale);
            if (resolutionWidth <= 0f) return 1f;
            return resolutionWidth.ExIsLessEquals(ScreenWidth) ? 1f : ScreenWidth / resolutionWidth;
        }

        /// <summary>
        /// 프로젝트 기준 해상도(BaseWidth, BaseHeight)에 맞추기 위한 스케일 비율을 반환한다.
        /// </summary>
        /// <returns>적용할 스케일 비율</returns>
        public static float GetBaseResolutionScale()
        {
            return GetResolutionScale(new Vector3(BaseWidth, BaseHeight, 0f));
        }

        /// <summary>
        /// 특정 비율(width / height) 기준으로 현재 화면이 더 좁은지 여부를 반환한다.
        /// </summary>
        /// <param name="targetAspect">기준 종횡비 (width / height)</param>
        /// <returns>현재 화면이 기준보다 좁으면 true</returns>
        public static bool IsNarrowerThan(float targetAspect)
        {
            return ScreenAspect < targetAspect;
        }
        #endregion // 해상도 스케일

        #region 레터박스 / 필러박스
        /// <summary>
        /// 기준 비율 대비 현재 화면에서 발생할 레터박스(상하 여백) 크기를 반환한다.
        /// 화면이 기준보다 넓을 때(가로로 더 김) 0이 나오고, 좁을 때 상하 여백이 발생.
        /// </summary>
        /// <param name="targetAspect">기준 종횡비 (width / height)</param>
        /// <returns>한쪽 여백 크기(픽셀). 0이면 여백 없음</returns>
        public static float GetLetterboxHeight(float targetAspect)
        {
            if (targetAspect <= 0f) return 0f;
            if (ScreenAspect >= targetAspect) return 0f;

            float targetHeight = ScreenWidth / targetAspect;
            return Mathf.Max(0f, (ScreenHeight - targetHeight) * 0.5f);
        }

        /// <summary>
        /// 기준 비율 대비 현재 화면에서 발생할 필러박스(좌우 여백) 크기를 반환한다.
        /// 화면이 기준보다 좁을 때(세로로 더 김) 0이 나오고, 넓을 때 좌우 여백이 발생.
        /// </summary>
        /// <param name="targetAspect">기준 종횡비 (width / height)</param>
        /// <returns>한쪽 여백 크기(픽셀). 0이면 여백 없음</returns>
        public static float GetPillarboxWidth(float targetAspect)
        {
            if (targetAspect <= 0f) return 0f;
            if (ScreenAspect <= targetAspect) return 0f;

            float targetWidth = ScreenHeight * targetAspect;
            return Mathf.Max(0f, (ScreenWidth - targetWidth) * 0.5f);
        }
        #endregion // 레터박스 / 필러박스

        #region 좌표 변환
        /// <summary>
        /// 스크린 좌표 (0,0)~(ScreenWidth, ScreenHeight)를 0~1 정규화된 UV 좌표로 변환한다.
        /// </summary>
        /// <param name="screenPosition">스크린 좌표</param>
        /// <returns>0~1 범위의 정규화된 좌표</returns>
        public static Vector2 ToNormalized(Vector2 screenPosition)
        {
            return new Vector2(
                ScreenWidth > 0f ? screenPosition.x / ScreenWidth : 0f,
                ScreenHeight > 0f ? screenPosition.y / ScreenHeight : 0f
            );
        }

        /// <summary>
        /// 0~1 정규화된 UV 좌표를 스크린 좌표로 변환한다.
        /// </summary>
        /// <param name="normalizedPosition">0~1 범위의 정규화된 좌표</param>
        /// <returns>스크린 좌표</returns>
        public static Vector2 FromNormalized(Vector2 normalizedPosition)
        {
            return new Vector2(
                normalizedPosition.x * ScreenWidth,
                normalizedPosition.y * ScreenHeight
            );
        }

        /// <summary>
        /// 현재 화면의 월드 공간 경계 좌표를 반환한다. (지정 카메라의 Z 거리 기준)
        /// </summary>
        /// <param name="camera">기준 카메라</param>
        /// <param name="depth">카메라로부터의 Z 거리</param>
        /// <returns>월드 경계 (xMin, yMin, xMax, yMax)</returns>
        public static Rect GetWorldScreenBounds(Camera camera, float depth = 10f)
        {
            if (camera == null) return Rect.zero;

            Vector3 bottomLeft = camera.ScreenToWorldPoint(new Vector3(0f, 0f, depth));
            Vector3 topRight = camera.ScreenToWorldPoint(new Vector3(ScreenWidth, ScreenHeight, depth));

            return new Rect(
                bottomLeft.x,
                bottomLeft.y,
                topRight.x - bottomLeft.x,
                topRight.y - bottomLeft.y
            );
        }
        #endregion // 좌표 변환

        #region DPI
        /// <summary>
        /// DPI를 고려하여 물리적 크기(인치)를 픽셀 크기로 변환한다.
        /// Screen.dpi가 0이면 96 DPI로 가정한다.
        /// </summary>
        /// <param name="inches">변환할 물리적 크기(인치)</param>
        /// <returns>픽셀 크기</returns>
        public static float InchesToPixels(float inches)
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : 96f;
            return inches * dpi;
        }

        /// <summary>
        /// DPI를 고려하여 픽셀 크기를 물리적 크기(인치)로 변환한다.
        /// Screen.dpi가 0이면 96 DPI로 가정한다.
        /// </summary>
        /// <param name="pixels">변환할 픽셀 크기</param>
        /// <returns>물리적 크기(인치)</returns>
        public static float PixelsToInches(float pixels)
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : 96f;
            return pixels / dpi;
        }

        /// <summary>
        /// 밀리미터를 픽셀로 변환한다. (1인치 = 25.4mm)
        /// </summary>
        /// <param name="millimeters">변환할 밀리미터</param>
        /// <returns>픽셀 크기</returns>
        public static float MillimetersToPixels(float millimeters)
        {
            return InchesToPixels(millimeters / 25.4f);
        }

        /// <summary>
        /// 픽셀을 밀리미터로 변환한다.
        /// </summary>
        /// <param name="pixels">변환할 픽셀</param>
        /// <returns>밀리미터</returns>
        public static float PixelsToMillimeters(float pixels)
        {
            return PixelsToInches(pixels) * 25.4f;
        }

        /// <summary>
        /// 접근성 가이드라인에 따른 최소 터치 타겟 크기를 픽셀로 반환한다.
        /// iOS HIG: 44pt(~9mm), Material Design: 48dp(~9mm) 기준.
        /// </summary>
        /// <returns>최소 터치 크기(픽셀)</returns>
        public static float GetMinTouchSizePixels()
        {
            return MillimetersToPixels(9f);
        }
        #endregion // DPI

        #region 카메라 FOV
        /// <summary>
        /// 수평 FOV를 수직 FOV로 변환하여 카메라에 적용한다.
        /// Unity 카메라는 기본적으로 수직 FOV를 사용하므로, 수평 FOV 기준으로 일관된 시야를 유지하고 싶을 때 사용한다.
        /// </summary>
        /// <param name="camera">적용할 카메라</param>
        /// <param name="horizontalFov">설정할 수평 시야각 (도 단위)</param>
        public static void SetHorizontalFov(this Camera camera, float horizontalFov)
        {
            if (camera == null) return;

            float verticalFov = 2f * Mathf.Atan(
                Mathf.Tan(horizontalFov * Mathf.Deg2Rad / 2f) / camera.aspect
            ) * Mathf.Rad2Deg;

            camera.fieldOfView = verticalFov;
        }

        /// <summary>
        /// 카메라의 현재 수직 FOV로부터 수평 FOV를 계산하여 반환한다.
        /// </summary>
        /// <param name="camera">대상 카메라</param>
        /// <returns>수평 시야각 (도 단위)</returns>
        public static float GetHorizontalFov(this Camera camera)
        {
            if (camera == null) return 0f;

            return 2f * Mathf.Atan(
                Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad / 2f) * camera.aspect
            ) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Orthographic 카메라의 크기를 기준 해상도 기준으로 자동 조정한다.
        /// 세로가 짧은 기기에서 orthographicSize를 키워 더 넓은 영역이 보이게 한다.
        /// </summary>
        /// <param name="camera">대상 카메라 (Orthographic이어야 함)</param>
        /// <param name="baseOrthographicSize">기준 해상도에서의 orthographicSize</param>
        public static void FitOrthographicSize(this Camera camera, float baseOrthographicSize)
        {
            if (camera == null || !camera.orthographic) return;

            float scale = GetBaseResolutionScale();
            camera.orthographicSize = scale <= 0f ? baseOrthographicSize : baseOrthographicSize / scale;
        }
        #endregion // 카메라 FOV
    }
}