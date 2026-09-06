using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using SW.Attributes;

namespace SW.Util
{
    /// <summary>
    /// uGUI Button을 상속해 연타 방지, 롱프레스, 반복 발사, 클릭 사운드를 추가한 버튼입니다.
    /// </summary>
    /// <remarks>
    /// 기본 버튼의 클릭과 시각 전환을 유지하면서 연타 방지, 길게 누르기,
    /// 반복 실행 및 클릭 효과음 옵션을 선택적으로 적용합니다.
    /// </remarks>
    [AddComponentMenu("SWUtils/SW Button Extension")]
    public class SWButtonExtension : Button
    {
        #region 필드
        [SerializeField] private bool useCooldown;
        [Tooltip("클릭 후 재클릭을 무시할 시간(초)입니다.")]
        [SerializeField, SWCondition("useCooldown", true)] private float cooldownSeconds = 0.3f;
        [Tooltip("쿨다운 동안 interactable을 꺼서 비주얼로도 표시할지 여부입니다.")]
        [SerializeField, SWCondition("useCooldown", true)] private bool disableButtonDuringCooldown;

        [SerializeField] private bool useLongPress;
        [Tooltip("롱프레스로 인정할 홀드 시간(초)입니다.")]
        [SerializeField, SWCondition("useLongPress", true)] private float longPressSeconds = 0.5f;
        [Tooltip("롱프레스가 발동하면 클릭(onClick)을 무시할지 여부입니다.")]
        [SerializeField, SWCondition("useLongPress", true)] private bool suppressClickAfterLongPress = true;

        [SerializeField] private bool useRepeat;
        [Tooltip("반복이 시작되기까지의 대기 시간(초)입니다.")]
        [SerializeField, SWCondition("useRepeat", true)] private float repeatStartDelay = 0.4f;
        [Tooltip("반복 실행 간격(초)입니다.")]
        [SerializeField, SWCondition("useRepeat", true)] private float repeatInterval = 0.15f;
        [Tooltip("누르고 있을수록 간격이 이 값까지 줄어듭니다. 반복 간격과 같으면 가속하지 않습니다.")]
        [SerializeField, SWCondition("useRepeat", true)] private float repeatMinInterval = 0.05f;
        [Tooltip("간격이 최소까지 줄어드는 데 걸리는 시간(초)입니다.")]
        [SerializeField, SWCondition("useRepeat", true)] private float repeatAccelerateDuration = 2f;

        [Tooltip("클릭 사운드를 재생할지 여부입니다.")]
        [SerializeField] private bool useClickSfx;
        [Tooltip("클릭 시 재생할 SFX 키입니다.")]
        [SerializeField, SWCondition("useClickSfx", true)] private string clickSfxKey;

        [Tooltip("Time.timeScale의 영향을 받지 않을지 여부입니다. (일시정지 UI용)")]
        [SerializeField] private bool useUnscaledTime = true;

        [SerializeField, SWCondition("useLongPress", true)] private UnityEvent onLongPress;
        [SerializeField, SWCondition("useRepeat", true)] private UnityEvent onRepeat;

        private Coroutine holdRoutine;
        private Coroutine cooldownRoutine;
        private bool isCooldown;
        private bool isLongPressFired;
        private int repeatFiredCount;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>롱프레스 이벤트입니다. 코드에서 리스너를 추가할 때 사용합니다.</summary>
        public UnityEvent OnLongPressEvent => onLongPress;

        /// <summary>홀드 반복 이벤트입니다. 누르는 동안 반복 호출됩니다.</summary>
        public UnityEvent OnRepeatEvent => onRepeat;

        /// <summary>홀드 진행률 콜백입니다. 롱프레스 게이지 연출에 사용합니다. (0~1)</summary>
        public event System.Action<float> OnHoldProgress;

        /// <summary>현재 연타 방지 쿨다운 중인지 여부입니다.</summary>
        public bool IsCooldown => isCooldown;
        #endregion // 프로퍼티

        #region 초기화
        /// <inheritdoc/>
        protected override void OnDisable()
        {
            StopHoldRoutine();

            if (cooldownRoutine != null)
            {
                StopCoroutine(cooldownRoutine);
                cooldownRoutine = null;
            }

            EndCooldown();
            base.OnDisable();
        }

#if UNITY_EDITOR
        /// <inheritdoc/>
        protected override void OnValidate()
        {
            base.OnValidate();

            if (useLongPress && useRepeat)
                SWLog.LogWarning($"[SWButtonExtension] 롱프레스와 홀드 반복은 동시에 사용할 수 없습니다. 홀드 반복이 우선됩니다: {name}");

            if (repeatMinInterval > repeatInterval)
                repeatMinInterval = repeatInterval;
        }
#endif // UNITY_EDITOR
        #endregion // 초기화

        #region 포인터 재정의
        /// <summary>
        /// 포인터를 누르면 눌림 비주얼 처리 후 홀드 감시(롱프레스/반복)를 시작합니다.
        /// </summary>
        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (!IsInteractable()) return;

            isLongPressFired = false;
            repeatFiredCount = 0;

            StopHoldRoutine();
            if (useRepeat)
                holdRoutine = StartCoroutine(RepeatRoutine());
            else if (useLongPress)
                holdRoutine = StartCoroutine(LongPressRoutine());
        }

        /// <summary>
        /// 포인터를 떼면 홀드 감시를 종료합니다.
        /// </summary>
        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            StopHoldRoutine();
        }

        /// <summary>
        /// 포인터가 버튼 밖으로 나가면 홀드를 취소합니다.
        /// </summary>
        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            StopHoldRoutine();
            OnHoldProgress?.Invoke(0f);
        }

        /// <summary>
        /// 클릭을 가로채서 조건(쿨다운, 롱프레스 발동, 홀드 반복 중)을 통과할 때만
        /// Button.onClick을 호출합니다.
        /// </summary>
        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (!CanInvokeClick()) return;

            PlayClickSfx();
            // base.OnPointerClick이 Button.onClick을 호출합니다.
            base.OnPointerClick(eventData);

            if (useCooldown)
                StartCooldown();
        }

        /// <summary>
        /// 키보드/게임패드 Submit도 클릭과 동일한 규칙으로 처리합니다.
        /// </summary>
        public override void OnSubmit(BaseEventData eventData)
        {
            if (!CanInvokeClick()) return;

            PlayClickSfx();
            base.OnSubmit(eventData);

            if (useCooldown)
                StartCooldown();
        }
        #endregion // 포인터 재정의

        #region 내부
        /// <summary>
        /// 현재 onClick을 호출할 수 있는 상태인지 확인합니다.
        /// </summary>
        /// <returns>호출 가능하면 true입니다.</returns>
        private bool CanInvokeClick()
        {
            if (!IsActive() || !IsInteractable()) return false;
            if (isCooldown) return false;
            if (isLongPressFired && suppressClickAfterLongPress) return false;
            if (useRepeat && repeatFiredCount > 0) return false;

            return true;
        }

        /// <summary>
        /// 시간 옵션에 맞는 deltaTime을 반환합니다.
        /// </summary>
        private float GetDeltaTime()
        {
            return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        /// <summary>
        /// 홀드 감시 코루틴을 중지합니다.
        /// </summary>
        private void StopHoldRoutine()
        {
            if (holdRoutine == null) return;

            StopCoroutine(holdRoutine);
            holdRoutine = null;
        }

        /// <summary>
        /// 누른 시간의 진행률을 알리고, 설정한 시간이 지나면 롱프레스 이벤트를 호출합니다.
        /// </summary>
        private IEnumerator LongPressRoutine()
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, longPressSeconds);

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                OnHoldProgress?.Invoke(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            isLongPressFired = true;
            OnHoldProgress?.Invoke(1f);
            PlayClickSfx();
            onLongPress?.Invoke();
            holdRoutine = null;
        }

        /// <summary>
        /// 시작 대기 시간이 지나면 버튼을 누르는 동안 반복 이벤트를 호출합니다. 누른 시간에 따라 호출 간격을 줄입니다.
        /// </summary>
        private IEnumerator RepeatRoutine()
        {
            float elapsed = 0f;
            float startDelay = Mathf.Max(0f, repeatStartDelay);

            while (elapsed < startDelay)
            {
                elapsed += GetDeltaTime();
                yield return null;
            }

            float holdTime = 0f;
            float intervalTimer = 0f;

            FireRepeat();

            while (true)
            {
                float deltaTime = GetDeltaTime();
                holdTime += deltaTime;
                intervalTimer += deltaTime;

                float accelerateProgress = repeatAccelerateDuration > 0f
                    ? Mathf.Clamp01(holdTime / repeatAccelerateDuration)
                    : 1f;
                float currentInterval = Mathf.Lerp(repeatInterval, repeatMinInterval, accelerateProgress);

                if (intervalTimer >= currentInterval)
                {
                    intervalTimer = 0f;
                    FireRepeat();
                }

                yield return null;
            }
        }

        /// <summary>
        /// 반복 이벤트를 1회 실행합니다.
        /// </summary>
        private void FireRepeat()
        {
            if (!IsInteractable())
            {
                StopHoldRoutine();
                return;
            }

            repeatFiredCount++;
            onRepeat?.Invoke();
        }

        /// <summary>
        /// 연타 방지 쿨다운을 시작합니다.
        /// </summary>
        private void StartCooldown()
        {
            if (cooldownRoutine != null)
                StopCoroutine(cooldownRoutine);

            cooldownRoutine = StartCoroutine(CooldownRoutine());
        }

        /// <summary>
        /// 쿨다운 동안 연속 입력을 막고, 설정한 시간이 지나면 입력을 다시 허용합니다.
        /// </summary>
        private IEnumerator CooldownRoutine()
        {
            isCooldown = true;
            if (disableButtonDuringCooldown)
                interactable = false;

            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, cooldownSeconds);

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                yield return null;
            }

            EndCooldown();
            cooldownRoutine = null;
        }

        /// <summary>
        /// 쿨다운 상태를 해제합니다.
        /// </summary>
        private void EndCooldown()
        {
            if (!isCooldown) return;

            isCooldown = false;
            if (disableButtonDuringCooldown)
                interactable = true;
        }

        /// <summary>
        /// 클릭 사운드를 재생합니다. SWAudioManager가 없으면 무시합니다.
        /// </summary>
        private void PlayClickSfx()
        {
            if (!useClickSfx) return;
            if (string.IsNullOrEmpty(clickSfxKey)) return;
            if (!SWAudioManager.HasInstance) return;

            SWAudioManager.Instance.PlaySfx(clickSfxKey);
        }
        #endregion // 내부
    }
}
