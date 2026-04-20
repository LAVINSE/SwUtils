using SWTools;
using UnityEngine;
using UnityEngine.UI;

namespace SWUtils
{
    /// <summary>
    /// 화면 비율 구간별 CanvasScaler의 matchWidthOrHeight 값 설정.
    /// </summary>
    [System.Serializable]
    public class RatioSetting
    {
        /// <summary>이 설정이 적용될 최대 비율 (height/width).</summary>
        public float maxRatio;
        /// <summary>해당 구간에서 사용할 matchWidthOrHeight 값 (0 = Width 기준, 1 = Height 기준).</summary>
        [Range(0f, 1f)]
        public float matchWidthOrHeight;
    }

    /// <summary>
    /// 해상도(화면 비율)에 따라 CanvasScaler의 matchWidthOrHeight 값을 자동 조정한다.
    /// 예: 16:9 기기와 19.5:9 기기에서 서로 다른 매칭 값을 적용하여 UI가 자연스럽게 표시되도록 한다.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class SWCanvasResolution : SWMonoBehaviour
    {
        #region 필드
        /// <summary>대상 CanvasScaler. null이면 자동으로 GetComponent.</summary>
        [SerializeField] private CanvasScaler canvasScaler;

        [Space]
        [Header("=====> 화면 비율에 대한 설정 <=====")]
        /// <summary>화면 비율별 CanvasScaler 매칭 설정 목록.</summary>
        [SerializeField] private RatioSetting[] ratioSettings;

        /// <summary>어떤 구간에도 해당하지 않을 때 사용할 기본 매칭 값.</summary>
        [Range(0f, 1f)]
        [SerializeField] private float defaultMatchWidthOrHeight = 0f;

        /// <summary>Update에서 해상도 변경을 감지하여 재적용할지 여부 (기기 회전 대응).</summary>
        [SerializeField] private bool autoUpdateOnResize = false;

        /// <summary>마지막으로 적용된 화면 너비.</summary>
        private int lastScreenWidth;
        /// <summary>마지막으로 적용된 화면 높이.</summary>
        private int lastScreenHeight;
        #endregion // 필드

        #region 유니티 이벤트
        /// <summary>
        /// Awake 시 CanvasScaler 참조를 확보하고 초기 해상도를 적용한다.
        /// </summary>
        private void Awake()
        {
            if (canvasScaler == null)
                canvasScaler = GetComponent<CanvasScaler>();

            CanvasScalerResolution();
        }

        /// <summary>
        /// autoUpdateOnResize가 켜진 경우, 해상도 변경을 감지하여 매칭 값을 재적용한다.
        /// </summary>
        private void Update()
        {
            if (!autoUpdateOnResize) return;

            if (lastScreenWidth != Screen.width || lastScreenHeight != Screen.height)
            {
                CanvasScalerResolution();
            }
        }
        #endregion // 유니티 이벤트

        #region 프리셋
        /// <summary>
        /// 세로 모드 게임용 프리셋을 적용한다. (기준 해상도 1080x1920 기준)
        /// 태블릿(4:3)부터 노치폰(20:9)까지 자연스럽게 스케일되도록 구성.
        /// </summary>
        [SWButton("Preset/세로 게임 (Portrait)")]
        private void ApplyPortraitPreset()
        {
            ratioSettings = new RatioSetting[]
            {
                new RatioSetting { maxRatio = 1.5f,  matchWidthOrHeight = 0f   }, // 태블릿 (4:3, 3:2)
                new RatioSetting { maxRatio = 1.78f, matchWidthOrHeight = 0.3f }, // 16:9 표준
                new RatioSetting { maxRatio = 2.0f,  matchWidthOrHeight = 0.7f }, // 18:9
                new RatioSetting { maxRatio = 2.2f,  matchWidthOrHeight = 1f   }, // 19.5:9, 20:9 노치폰
            };
            defaultMatchWidthOrHeight = 1f;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            SWUtilsLog.Log("[CanvasResolution] Portrait preset applied");
        }

        /// <summary>
        /// 가로 모드 게임용 프리셋을 적용한다. (기준 해상도 1920x1080 기준)
        /// 태블릿(4:3)부터 와이드 모니터까지 자연스럽게 스케일되도록 구성.
        /// </summary>
        [SWButton("Preset/가로 게임 (Landscape)")]
        private void ApplyLandscapePreset()
        {
            // 가로 모드에서는 currentRatio = height/width가 0.4 ~ 0.75 사이로 작음
            ratioSettings = new RatioSetting[]
            {
                new RatioSetting { maxRatio = 0.46f, matchWidthOrHeight = 1f   }, // 20:9, 21:9 울트라와이드
                new RatioSetting { maxRatio = 0.50f, matchWidthOrHeight = 0.7f }, // 18:9
                new RatioSetting { maxRatio = 0.57f, matchWidthOrHeight = 0.3f }, // 16:9 표준
                new RatioSetting { maxRatio = 0.75f, matchWidthOrHeight = 0f   }, // 태블릿 (4:3)
            };
            defaultMatchWidthOrHeight = 0f;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            SWUtilsLog.Log("[CanvasResolution] Landscape preset applied");
        }

        /// <summary>
        /// 현재 설정을 로그로 출력한다. 디버깅용.
        /// </summary>
        [SWButton("현재 설정 로그 출력")]
        private void LogCurrentSettings()
        {
            float currentRatio = (float)Screen.height / Screen.width;
            SWUtilsLog.Log($"[CanvasResolution] Screen: {Screen.width}x{Screen.height}, " +
                           $"Ratio(h/w): {currentRatio:F3}, " +
                           $"Current match: {(canvasScaler != null ? canvasScaler.matchWidthOrHeight : -1f):F2}");

            if (ratioSettings != null)
            {
                for (int index = 0; index < ratioSettings.Length; index++)
                {
                    var setting = ratioSettings[index];
                    SWUtilsLog.Log($"  [{index}] maxRatio <= {setting.maxRatio:F2} → match {setting.matchWidthOrHeight:F2}");
                }
            }
            SWUtilsLog.Log($"default → match {defaultMatchWidthOrHeight:F2}");
        }
        #endregion // 프리셋

        #region 함수
        /// <summary>
        /// 현재 화면 비율을 계산하여 적절한 matchWidthOrHeight 값을 CanvasScaler에 적용한다.
        /// </summary>
        private void CanvasScalerResolution()
        {
            if (canvasScaler == null)
            {
                SWUtilsLog.LogWarning("[CanvasResolution] canvasScaler is null");
                return;
            }

            if (ratioSettings == null || ratioSettings.Length == 0)
            {
                canvasScaler.matchWidthOrHeight = defaultMatchWidthOrHeight;
                return;
            }

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            float currentRatio = (float)Screen.height / Screen.width;

            System.Array.Sort(ratioSettings, (valueA, valueB) => valueA.maxRatio.CompareTo(valueB.maxRatio));

            foreach (var setting in ratioSettings)
            {
                if (currentRatio <= setting.maxRatio)
                {
                    canvasScaler.matchWidthOrHeight = setting.matchWidthOrHeight;
                    return;
                }
            }

            canvasScaler.matchWidthOrHeight = defaultMatchWidthOrHeight;
        }

        /// <summary>
        /// 외부에서 수동으로 해상도 적용을 다시 수행한다.
        /// </summary>
        public void Refresh()
        {
            CanvasScalerResolution();
        }
        #endregion // 함수
    }
}