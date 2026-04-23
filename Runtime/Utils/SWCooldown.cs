using UnityEngine;

namespace SWUtils
{
    /// <summary>
    /// 스킬, 버튼, 상호작용처럼 반복 사용에 제한 시간이 필요한 기능을 위한 쿨다운 유틸리티.
    /// 마지막 사용 시각과 현재 시각을 비교하여 남은 시간을 계산한다.
    /// </summary>
    [System.Serializable]
    public class SWCooldown
    {
        /// <summary>
        /// 쿨다운이 사용할 시간 기준.
        /// </summary>
        public enum TimeMode
        {
            /// <summary>Time.time 기준. Time.timeScale의 영향을 받는다.</summary>
            Scaled,
            /// <summary>Time.unscaledTime 기준. Time.timeScale의 영향을 받지 않는다.</summary>
            Unscaled
        }

        #region Fields
        [SerializeField] private float duration;
        [SerializeField] private float lastUseTime;
        [SerializeField] private bool hasUsed;
        [SerializeField] private TimeMode timeMode;
        #endregion // Fields

        #region Properties
        /// <summary>쿨다운 전체 시간(초).</summary>
        public float Duration => duration;
        /// <summary>한 번이라도 사용된 적이 있는지 여부.</summary>
        public bool HasUsed => hasUsed;
        /// <summary>현재 시간 기준.</summary>
        public TimeMode Mode => timeMode;
        /// <summary>지금 사용할 수 있는지 여부.</summary>
        public bool CanUse => !IsCoolingDown;
        /// <summary>현재 쿨다운 진행 중인지 여부.</summary>
        public bool IsCoolingDown => hasUsed && Elapsed < duration;
        /// <summary>마지막 사용 이후 경과 시간(초).</summary>
        public float Elapsed => hasUsed ? Mathf.Max(0f, CurrentTime - lastUseTime) : duration;
        /// <summary>쿨다운이 끝날 때까지 남은 시간(초).</summary>
        public float Remaining => Mathf.Max(0f, duration - Elapsed);
        /// <summary>쿨다운 진행률. 0은 방금 사용, 1은 사용 가능 상태를 의미한다.</summary>
        public float Progress => duration <= 0f ? 1f : Mathf.Clamp01(Elapsed / duration);
        #endregion // Properties

        #region Constructors
        /// <summary>
        /// 쿨다운을 생성한다.
        /// </summary>
        /// <param name="duration">쿨다운 전체 시간(초).</param>
        /// <param name="timeMode">사용할 시간 기준.</param>
        public SWCooldown(float duration = 1f, TimeMode timeMode = TimeMode.Scaled)
        {
            this.duration = Mathf.Max(0f, duration);
            this.timeMode = timeMode;
            lastUseTime = 0f;
            hasUsed = false;
        }
        #endregion // Constructors

        #region Controls
        /// <summary>
        /// 사용 가능하면 쿨다운을 시작한다.
        /// </summary>
        /// <returns>사용에 성공했으면 true.</returns>
        public bool TryUse()
        {
            if (!CanUse)
                return false;

            Use();
            return true;
        }

        /// <summary>
        /// 현재 시각을 마지막 사용 시각으로 기록하여 쿨다운을 시작한다.
        /// </summary>
        public void Use()
        {
            lastUseTime = CurrentTime;
            hasUsed = true;
        }

        /// <summary>
        /// 아직 사용하지 않은 상태로 초기화한다.
        /// </summary>
        public void Reset()
        {
            lastUseTime = 0f;
            hasUsed = false;
        }

        /// <summary>
        /// 쿨다운을 즉시 완료 상태로 만든다.
        /// </summary>
        public void Finish()
        {
            lastUseTime = CurrentTime - duration;
            hasUsed = true;
        }
        #endregion // Controls

        #region Settings
        /// <summary>
        /// 쿨다운 전체 시간을 변경한다.
        /// </summary>
        /// <param name="duration">새 쿨다운 시간(초).</param>
        /// <param name="keepProgress">현재 진행률을 유지할지 여부.</param>
        public void SetDuration(float duration, bool keepProgress = false)
        {
            float previousProgress = Progress;
            this.duration = Mathf.Max(0f, duration);

            if (keepProgress && hasUsed)
                lastUseTime = CurrentTime - this.duration * previousProgress;
        }

        /// <summary>
        /// 시간 기준을 변경한다. 기존 경과 시간은 유지된다.
        /// </summary>
        /// <param name="timeMode">새 시간 기준.</param>
        public void SetTimeMode(TimeMode timeMode)
        {
            if (this.timeMode == timeMode)
                return;

            float elapsed = Elapsed;
            this.timeMode = timeMode;

            if (hasUsed)
                lastUseTime = CurrentTime - elapsed;
        }
        #endregion // Settings

        #region Helpers
        /// <summary>
        /// 남은 시간을 MM:SS 형식으로 반환한다.
        /// </summary>
        /// <returns>MM:SS 형식 문자열.</returns>
        public string ToMinSec()
        {
            return SWUtilsTime.ToMinSec(Remaining);
        }

        /// <summary>
        /// 남은 시간을 HH:MM:SS 형식으로 반환한다.
        /// </summary>
        /// <returns>HH:MM:SS 형식 문자열.</returns>
        public string ToHourMinSec()
        {
            return SWUtilsTime.ToHourMinSec(Remaining);
        }

        /// <summary>
        /// 현재 시간 기준에 맞는 Unity 시간을 반환한다.
        /// </summary>
        private float CurrentTime
        {
            get
            {
                return timeMode == TimeMode.Unscaled ? Time.unscaledTime : Time.time;
            }
        }
        #endregion // Helpers
    }
}
