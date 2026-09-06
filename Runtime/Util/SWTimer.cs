using UnityEngine;

namespace SW.Util
{
    /// <summary>
    /// 시작, 정지, 일시정지, 재개를 지원하는 런타임 타이머.
    /// Update에서 Tick()을 호출하거나 Tick(deltaTime)에 경과 시간을 전달해 타이머를 갱신합니다.
    /// </summary>
    [System.Serializable]
    public class SWTimer
    {
        /// <summary>
        /// 타이머가 사용할 시간 기준.
        /// </summary>
        public enum TimeMode
        {
            /// <summary>Time.deltaTime 기준. Time.timeScale의 영향을 받습니다.</summary>
            Scaled,
            /// <summary>Time.unscaledDeltaTime 기준. Time.timeScale의 영향을 받지 않습니다.</summary>
            Unscaled,
            /// <summary>자동 Tick을 사용하지 않고 Tick(deltaTime)으로 직접 진행합니다.</summary>
            Manual
        }

        #region 필드
        [SerializeField] private float duration;
        [SerializeField] private float elapsed;
        [SerializeField] private bool isRunning;
        [SerializeField] private bool isPaused;
        [SerializeField] private bool loop;
        [SerializeField] private TimeMode timeMode;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>타이머 전체 시간(초).</summary>
        public float Duration => duration;
        /// <summary>현재까지 진행된 시간(초).</summary>
        public float Elapsed => elapsed;
        /// <summary>남은 시간(초). 0보다 작아지지 않습니다.</summary>
        public float Remaining => Mathf.Max(0f, duration - elapsed);
        /// <summary>진행률. 0은 시작, 1은 완료를 의미합니다.</summary>
        public float Progress => duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
        /// <summary>타이머가 실행 중인지 여부.</summary>
        public bool IsRunning => isRunning;
        /// <summary>타이머가 일시정지 상태인지 여부.</summary>
        public bool IsPaused => isPaused;
        /// <summary>타이머가 완료되었는지 여부.</summary>
        public bool IsDone => duration <= 0f || elapsed >= duration;
        /// <summary>완료 후 반복할지 여부.</summary>
        public bool Loop => loop;
        /// <summary>현재 시간 기준.</summary>
        public TimeMode Mode => timeMode;
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 타이머를 생성합니다.
        /// </summary>
        /// <param name="duration">타이머 전체 시간(초).</param>
        /// <param name="playOnCreate">생성 즉시 실행할지 여부.</param>
        /// <param name="loop">완료 후 반복할지 여부.</param>
        /// <param name="timeMode">사용할 시간 기준.</param>
        public SWTimer(float duration = 1f, bool playOnCreate = false,
            bool loop = false, TimeMode timeMode = TimeMode.Scaled)
        {
            this.duration = Mathf.Max(0f, duration);
            this.loop = loop;
            this.timeMode = timeMode;
            elapsed = 0f;
            isRunning = playOnCreate;
            isPaused = false;
        }
        #endregion // 생성자

        #region 제어
        /// <summary>
        /// 타이머를 처음부터 시작합니다.
        /// </summary>
        public void Start()
        {
            elapsed = 0f;
            isRunning = true;
            isPaused = false;
        }

        /// <summary>
        /// 타이머 진행을 중지합니다. 진행 시간은 유지됩니다.
        /// </summary>
        public void Stop()
        {
            isRunning = false;
            isPaused = false;
        }

        /// <summary>
        /// 실행 중인 타이머를 일시 정지합니다.
        /// </summary>
        public void Pause()
        {
            if (!isRunning) return;
            isPaused = true;
        }

        /// <summary>
        /// 일시정지된 타이머를 다시 진행합니다.
        /// </summary>
        public void Resume()
        {
            if (!isRunning) return;
            isPaused = false;
        }

        /// <summary>
        /// 진행 시간과 일시정지 상태를 초기화합니다. 실행 여부는 유지합니다.
        /// </summary>
        public void Reset()
        {
            elapsed = 0f;
            isPaused = false;
        }

        /// <summary>
        /// 진행 시간을 0으로 되돌리고 타이머를 실행합니다.
        /// </summary>
        public void Restart()
        {
            Reset();
            isRunning = true;
        }

        /// <summary>
        /// 타이머를 즉시 완료합니다.
        /// </summary>
        public void Complete()
        {
            elapsed = duration;
            isRunning = false;
            isPaused = false;
        }
        #endregion // 제어

        #region 설정
        /// <summary>
        /// 타이머 전체 시간을 변경합니다.
        /// </summary>
        /// <param name="duration">새 전체 시간(초).</param>
        /// <param name="keepProgress">현재 진행률을 유지할지 여부.</param>
        public void SetDuration(float duration, bool keepProgress = false)
        {
            float previousProgress = Progress;
            this.duration = Mathf.Max(0f, duration);
            elapsed = keepProgress ? this.duration * previousProgress : Mathf.Min(elapsed, this.duration);
        }

        /// <summary>
        /// 반복 여부를 설정합니다.
        /// </summary>
        /// <param name="loop">완료 후 반복하려면 true.</param>
        public void SetLoop(bool loop)
        {
            this.loop = loop;
        }

        /// <summary>
        /// 시간 기준을 설정합니다.
        /// </summary>
        /// <param name="timeMode">새 시간 기준.</param>
        public void SetTimeMode(TimeMode timeMode)
        {
            this.timeMode = timeMode;
        }

        /// <summary>
        /// 진행 시간을 직접 설정합니다.
        /// </summary>
        /// <param name="elapsed">설정할 진행 시간(초).</param>
        public void SetElapsed(float elapsed)
        {
            this.elapsed = Mathf.Clamp(elapsed, 0f, duration);
        }
        #endregion // 설정

        #region Tick
        /// <summary>
        /// 설정된 시간 기준에 따라 타이머를 한 프레임 진행합니다.
        /// </summary>
        /// <returns>이번 호출로 타이머가 완료되었으면 true.</returns>
        public bool Tick()
        {
            if (timeMode == TimeMode.Manual)
                return false;

            float deltaTime = timeMode == TimeMode.Unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            return Tick(deltaTime);
        }

        /// <summary>
        /// 지정한 deltaTime만큼 타이머를 진행합니다.
        /// </summary>
        /// <param name="deltaTime">진행시킬 시간(초).</param>
        /// <returns>이번 호출로 타이머가 완료되었으면 true.</returns>
        public bool Tick(float deltaTime)
        {
            if (!isRunning || isPaused || IsDone && !loop)
                return false;

            elapsed += Mathf.Max(0f, deltaTime);

            if (elapsed < duration)
                return false;

            if (loop && duration > 0f)
            {
                elapsed %= duration;
                return true;
            }

            elapsed = duration;
            isRunning = false;
            isPaused = false;
            return true;
        }
        #endregion // Tick

        #region 시간 포맷
        /// <summary>
        /// 남은 시간을 MM:SS 형식으로 반환합니다.
        /// </summary>
        /// <returns>MM:SS 형식 문자열.</returns>
        public string ToMinSec()
        {
            return SWTime.ToMinSec(Remaining);
        }

        /// <summary>
        /// 남은 시간을 HH:MM:SS 형식으로 반환합니다.
        /// </summary>
        /// <returns>HH:MM:SS 형식 문자열.</returns>
        public string ToHourMinSec()
        {
            return SWTime.ToHourMinSec(Remaining);
        }
        #endregion // 시간 포맷
    }
}
