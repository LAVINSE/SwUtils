using UnityEngine;
using SWTools;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SWUtils
{
    /// <summary>
    /// 노치/다이나믹 아일랜드/펀치홀 등을 피해 UI를 SafeArea 영역에 자동 배치한다.
    /// RectTransform의 anchorMin/Max를 SafeArea에 맞게 설정한다.
    /// 에디터에서 가상 SafeArea를 시뮬레이션하는 기능도 제공한다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class SWSafeArea : SWMonoBehaviour
    {
        #region 변수
        /// <summary>SafeArea를 적용할 RectTransform. null이면 자동으로 GetComponent.</summary>
        [SerializeField] private RectTransform rectTransform;

        [Header("=====> 방향별 적용 설정 <=====")]
        /// <summary>좌측 SafeArea 적용 여부.</summary>
        [SerializeField] private bool applyLeft = true;
        /// <summary>우측 SafeArea 적용 여부.</summary>
        [SerializeField] private bool applyRight = true;
        /// <summary>상단 SafeArea 적용 여부.</summary>
        [SerializeField] private bool applyTop = true;
        /// <summary>하단 SafeArea 적용 여부.</summary>
        [SerializeField] private bool applyBottom = true;

        [Header("=====> 추가 여백(픽셀) <=====")]
        /// <summary>좌측 추가 패딩(픽셀). SafeArea 위에 추가로 들어감.</summary>
        [SerializeField] private float paddingLeft = 0f;
        /// <summary>우측 추가 패딩(픽셀).</summary>
        [SerializeField] private float paddingRight = 0f;
        /// <summary>상단 추가 패딩(픽셀).</summary>
        [SerializeField] private float paddingTop = 0f;
        /// <summary>하단 추가 패딩(픽셀).</summary>
        [SerializeField] private float paddingBottom = 0f;

        [Header("=====> 동작 옵션 <=====")]
        /// <summary>Update에서 해상도/회전 변경을 감지하여 재적용할지 여부.</summary>
        [SerializeField] private bool autoUpdateOnResize = true;
        /// <summary>에디터에서 Scene 뷰에 SafeArea 영역을 기즈모로 표시할지 여부.</summary>
        [SerializeField] private bool showGizmo = true;

#if UNITY_EDITOR
        [Header("=====> 에디터 시뮬레이션 <=====")]
        /// <summary>에디터에서 가상 SafeArea를 사용할지 여부.</summary>
        [SerializeField] private bool useSimulatedSafeArea = false;
        /// <summary>시뮬레이션 프리셋.</summary>
        [SerializeField] private SimulationPreset simulationPreset = SimulationPreset.iPhoneNotch;
        /// <summary>커스텀 시뮬레이션 SafeArea (normalized 0~1).</summary>
        [SerializeField] private Rect customSimulatedSafeArea = new Rect(0f, 0.04f, 1f, 0.92f);
#endif

        /// <summary>마지막으로 적용된 SafeArea 캐시.</summary>
        private Rect lastSafeArea;
        /// <summary>마지막으로 적용된 화면 너비.</summary>
        private int lastScreenWidth;
        /// <summary>마지막으로 적용된 화면 높이.</summary>
        private int lastScreenHeight;
        /// <summary>마지막으로 적용된 화면 방향.</summary>
        private ScreenOrientation lastOrientation;
        #endregion // 변수

        #region 열거형
        /// <summary>에디터 시뮬레이션용 기기 프리셋.</summary>
        public enum SimulationPreset
        {
            /// <summary>시뮬레이션 없음 (전체 화면).</summary>
            None,
            /// <summary>아이폰 노치 (상단 약 44pt, 하단 홈인디케이터 34pt).</summary>
            iPhoneNotch,
            /// <summary>아이폰 다이나믹 아일랜드.</summary>
            iPhoneDynamicIsland,
            /// <summary>안드로이드 펀치홀 (상단만).</summary>
            AndroidPunchHole,
            /// <summary>안드로이드 상단 노치.</summary>
            AndroidNotch,
            /// <summary>커스텀 Rect.</summary>
            Custom,
        }
        #endregion // 열거형

        #region 유니티 이벤트
        /// <summary>
        /// Awake 시 RectTransform 참조를 확보하고 SafeArea를 적용한다.
        /// </summary>
        private void Awake()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            ApplySafeArea();
        }

        /// <summary>
        /// 해상도/회전 변경 감지 시 SafeArea를 재적용한다.
        /// </summary>
        private void Update()
        {
            if (!autoUpdateOnResize) return;

            var currentSafeArea = GetEffectiveSafeArea();

            if (lastSafeArea != currentSafeArea
                || lastScreenWidth != Screen.width
                || lastScreenHeight != Screen.height
                || lastOrientation != Screen.orientation)
            {
                ApplySafeArea();
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// 인스펙터 값이 변경되면 즉시 반영한다.
        /// </summary>
        private void OnValidate()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// Scene 뷰에 SafeArea 영역을 시각화한다.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!showGizmo || rectTransform == null) return;

            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            for (int index = 0; index < 4; index++)
            {
                Gizmos.DrawLine(corners[index], corners[(index + 1) % 4]);
            }
        }
#endif
        #endregion // 유니티 이벤트

        #region 함수
        /// <summary>
        /// 현재 적용할 SafeArea를 반환한다.
        /// 에디터 시뮬레이션이 켜져 있으면 가상 값을 반환하고, 아니면 Screen.safeArea를 반환한다.
        /// </summary>
        /// <returns>적용할 SafeArea Rect</returns>
        private Rect GetEffectiveSafeArea()
        {
#if UNITY_EDITOR
            if (useSimulatedSafeArea)
            {
                return GetSimulatedSafeArea();
            }
#endif
            return Screen.safeArea;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 선택된 시뮬레이션 프리셋에 해당하는 SafeArea Rect를 반환한다.
        /// </summary>
        /// <returns>시뮬레이션 SafeArea (픽셀 단위)</returns>
        private Rect GetSimulatedSafeArea()
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            // 픽셀 단위로 변환
            switch (simulationPreset)
            {
                case SimulationPreset.None:
                    return new Rect(0f, 0f, screenWidth, screenHeight);

                case SimulationPreset.iPhoneNotch:
                    // 상단 약 5%, 하단 홈인디케이터 약 3%
                    return new Rect(0f, screenHeight * 0.03f, screenWidth, screenHeight * 0.92f);

                case SimulationPreset.iPhoneDynamicIsland:
                    // 상단 약 6%, 하단 약 3%
                    return new Rect(0f, screenHeight * 0.03f, screenWidth, screenHeight * 0.91f);

                case SimulationPreset.AndroidPunchHole:
                    // 상단 약 3%만
                    return new Rect(0f, 0f, screenWidth, screenHeight * 0.97f);

                case SimulationPreset.AndroidNotch:
                    // 상단 약 4%
                    return new Rect(0f, 0f, screenWidth, screenHeight * 0.96f);

                case SimulationPreset.Custom:
                    return new Rect(
                        customSimulatedSafeArea.x * screenWidth,
                        customSimulatedSafeArea.y * screenHeight,
                        customSimulatedSafeArea.width * screenWidth,
                        customSimulatedSafeArea.height * screenHeight
                    );

                default:
                    return new Rect(0f, 0f, screenWidth, screenHeight);
            }
        }
#endif

        /// <summary>
        /// SafeArea에 맞춰 UI를 자동 배치한다.
        /// </summary>
        private void ApplySafeArea()
        {
            if (rectTransform == null) return;
            if (Screen.width <= 0 || Screen.height <= 0) return;

            var safeArea = GetEffectiveSafeArea();

            var anchorMin = safeArea.position;
            var anchorMax = anchorMin + safeArea.size;

            // 추가 패딩 적용 (픽셀 단위를 normalized 전에 적용)
            anchorMin.x += paddingLeft;
            anchorMin.y += paddingBottom;
            anchorMax.x -= paddingRight;
            anchorMax.y -= paddingTop;

            // 0~1 normalized 변환
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            // 방향별 적용 여부 반영 (해당 방향 비활성화 시 화면 경계 사용)
            if (!applyLeft) anchorMin.x = 0f;
            if (!applyBottom) anchorMin.y = 0f;
            if (!applyRight) anchorMax.x = 1f;
            if (!applyTop) anchorMax.y = 1f;

            // 0~1 범위로 클램프
            anchorMin.x = Mathf.Clamp01(anchorMin.x);
            anchorMin.y = Mathf.Clamp01(anchorMin.y);
            anchorMax.x = Mathf.Clamp01(anchorMax.x);
            anchorMax.y = Mathf.Clamp01(anchorMax.y);

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            lastSafeArea = safeArea;
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastOrientation = Screen.orientation;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                EditorUtility.SetDirty(rectTransform);
            }
#endif
        }

        /// <summary>
        /// 외부에서 수동으로 SafeArea 적용을 다시 수행한다.
        /// </summary>
        public void Refresh()
        {
            ApplySafeArea();
        }
        #endregion // 함수

        #region 에디터 버튼
        /// <summary>
        /// 기본 구역(전체 화면)에 UI를 배치한다.
        /// </summary>
        [SWButton("전체 화면으로 리셋")]
        private void ResetToFullscreen()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

#if UNITY_EDITOR
            Undo.RecordObject(rectTransform, "Reset To Fullscreen");
#endif
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                EditorUtility.SetDirty(rectTransform);
            }
#endif
        }

        /// <summary>
        /// 수동으로 SafeArea를 재적용한다.
        /// </summary>
        [SWButton("SafeArea 재적용")]
        private void ManualRefresh()
        {
            ApplySafeArea();
        }

        /// <summary>
        /// 현재 SafeArea 정보를 로그로 출력한다.
        /// </summary>
        [SWButton("SafeArea 정보 로그")]
        private void LogSafeAreaInfo()
        {
            var effectiveSafeArea = GetEffectiveSafeArea();
            SWUtilsLog.Log($"[SWSafeArea] 화면: {Screen.width}x{Screen.height}\n" +
                           $"  실제 SafeArea: {Screen.safeArea}\n" +
                           $"  적용 SafeArea: {effectiveSafeArea}\n" +
                           $"  화면 방향: {Screen.orientation}\n" +
                           $"  DPI: {Screen.dpi}");
        }
        #endregion // 에디터 버튼
    }
}
