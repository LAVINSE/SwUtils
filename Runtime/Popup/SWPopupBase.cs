using System;
using UnityEngine;

using SW.Attributes;

using SW.Base;

namespace SW.Popup
{
    /// <summary>
    /// 모든 팝업이 상속하는 기본 클래스입니다.
    /// </summary>
    /// <remarks>
    /// 기본 구현은 GameObject 활성 상태와 표시/숨김 연출을 제어합니다.
    /// 사운드, 입력 잠금 같은 부가 처리는 <see cref="OnShow"/>와 <see cref="OnHide"/>를 재정의해 확장합니다.
    /// <see cref="SWPopupEffectHandle"/>로 표시/숨김 연출의 재생 상태와 완료 콜백을 관리합니다.
    /// </remarks>
    public class SWPopupBase : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("=====> 표시 연출 <=====")]
        [SerializeField] private bool useShowEffect = true;
        [SerializeField, SWCondition("useShowEffect", true)] private SWPopupShowEffect showEffect;
        [SerializeField, SWCondition("useShowEffect", true)] private Transform showEffectTarget;

        [SWGroup("=====> 숨김 연출 <=====")]
        [SerializeField] private bool useHideEffect;
        [SerializeField, SWCondition("useHideEffect", true)] private SWPopupHideEffect hideEffect;
        [SerializeField, SWCondition("useHideEffect", true)] private Transform hideEffectTarget;

        private SWPopupEffectHandle showEffectHandle;
        private SWPopupEffectHandle hideEffectHandle;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>마지막으로 <see cref="Show"/>가 호출된 뒤 숨김 처리되지 않은 상태인지 여부입니다.</summary>
        public bool IsVisible { get; private set; }

        /// <summary>표시 연출을 재생할지 여부입니다.</summary>
        public bool UseShowEffect => useShowEffect;

        /// <summary>팝업 표시 시 재생할 연출입니다.</summary>
        public SWPopupShowEffect ShowEffect => showEffect;

        /// <summary>표시 연출 대상입니다. null이면 팝업 루트 Transform을 사용합니다.</summary>
        public Transform ShowEffectTarget => showEffectTarget;

        /// <summary>숨김 연출을 재생할지 여부입니다.</summary>
        public bool UseHideEffect => useHideEffect;

        /// <summary>팝업 숨김 시 재생할 연출입니다.</summary>
        public SWPopupHideEffect HideEffect => hideEffect;

        /// <summary>숨김 연출 대상입니다. null이면 팝업 루트 Transform을 사용합니다.</summary>
        public Transform HideEffectTarget => hideEffectTarget;
        #endregion // 프로퍼티

        #region 표시
        /// <summary>
        /// 팝업을 표시합니다.
        /// </summary>
        public virtual void Show()
        {
            IsVisible = true;
            StopHideEffect();
            gameObject.SetActive(true);
            OnShow();
            PlayShowEffect();
        }

        /// <summary>
        /// 팝업을 숨깁니다.
        /// </summary>
        public virtual void Hide()
        {
            Hide(null);
        }

        /// <summary>
        /// 팝업을 숨기고 숨김 연출 완료 후 콜백을 호출합니다.
        /// </summary>
        /// <param name="onHidden">숨김 완료 후 호출할 콜백입니다.</param>
        public virtual void Hide(Action onHidden)
        {
            IsVisible = false;
            StopShowEffect();
            OnHide();
            PlayHideEffect(() =>
            {
                gameObject.SetActive(false);
                onHidden?.Invoke();
            });
        }
        #endregion // 표시

        #region 표시 연출
        /// <summary>
        /// 표시 연출과 사용 여부를 설정합니다.
        /// </summary>
        /// <param name="effect">사용할 표시 연출입니다.</param>
        /// <param name="useEffect">표시 연출을 재생할지 여부입니다.</param>
        public void SetShowEffect(SWPopupShowEffect effect, bool useEffect = true)
        {
            showEffect = effect;
            useShowEffect = useEffect;
        }

        /// <summary>
        /// 표시 연출 대상을 설정합니다.
        /// </summary>
        /// <param name="target">표시 연출을 적용할 Transform입니다. null이면 팝업 루트를 사용합니다.</param>
        public void SetShowEffectTarget(Transform target)
        {
            showEffectTarget = target;
        }

        /// <summary>
        /// 현재 설정된 표시 연출을 재생합니다.
        /// </summary>
        protected virtual void PlayShowEffect()
        {
            StopShowEffect();

            if (!useShowEffect) return;
            if (showEffect == null) return;

            Transform target = showEffectTarget != null ? showEffectTarget : transform;
            showEffectHandle = showEffect.Play(this, target);
        }

        /// <summary>
        /// 재생 중인 표시 연출을 중지합니다.
        /// </summary>
        protected void StopShowEffect()
        {
            if (showEffectHandle == null) return;

            showEffectHandle.Kill();
            showEffectHandle = null;
        }
        #endregion // 표시 연출

        #region 숨김 연출
        /// <summary>
        /// 숨김 연출과 사용 여부를 설정합니다.
        /// </summary>
        /// <param name="effect">사용할 숨김 연출입니다.</param>
        /// <param name="useEffect">숨김 연출을 재생할지 여부입니다.</param>
        public void SetHideEffect(SWPopupHideEffect effect, bool useEffect = true)
        {
            hideEffect = effect;
            useHideEffect = useEffect;
        }

        /// <summary>
        /// 숨김 연출 대상을 설정합니다.
        /// </summary>
        /// <param name="target">숨김 연출을 적용할 Transform입니다. null이면 팝업 루트를 사용합니다.</param>
        public void SetHideEffectTarget(Transform target)
        {
            hideEffectTarget = target;
        }

        /// <summary>
        /// 현재 설정된 숨김 연출을 재생합니다.
        /// </summary>
        /// <param name="onComplete">숨김 연출 완료 후 호출할 콜백입니다.</param>
        protected virtual void PlayHideEffect(Action onComplete)
        {
            StopHideEffect();

            if (!useHideEffect || hideEffect == null)
            {
                onComplete?.Invoke();
                return;
            }

            Transform target = hideEffectTarget != null ? hideEffectTarget : transform;
            hideEffectHandle = hideEffect.Play(this, target);
            if (hideEffectHandle == null)
            {
                onComplete?.Invoke();
                return;
            }

            hideEffectHandle.OnComplete(() =>
            {
                hideEffectHandle = null;
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// 재생 중인 숨김 연출을 중지합니다.
        /// </summary>
        protected void StopHideEffect()
        {
            if (hideEffectHandle == null) return;

            hideEffectHandle.Kill();
            hideEffectHandle = null;
        }
        #endregion // 숨김 연출

        #region 재정의
        /// <summary>
        /// 팝업이 표시된 직후 호출됩니다.
        /// </summary>
        protected virtual void OnShow()
        {
        }

        /// <summary>
        /// 팝업이 숨겨지기 직전에 호출됩니다.
        /// </summary>
        protected virtual void OnHide()
        {
        }
        #endregion // 재정의

        #region 테스트
        [SWButton("팝업 닫기")]
        private void EditorTestHidePopup()
        {
            Hide(null);
        }
        #endregion // 테스트
    }
}
