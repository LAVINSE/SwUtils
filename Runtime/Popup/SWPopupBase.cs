using UnityEngine;

namespace SWUtils
{
    /// <summary>
    /// 모든 팝업이 상속하는 기본 클래스입니다.
    /// </summary>
    /// <remarks>
    /// 기본 구현은 GameObject 활성 상태만 제어합니다.
    /// 애니메이션, 사운드, 입력 잠금 같은 연출은 <see cref="OnShow"/>와 <see cref="OnHide"/>를 재정의해 확장합니다.
    /// </remarks>
    public class SWPopupBase : MonoBehaviour
    {
        #region 프로퍼티
        /// <summary>마지막으로 <see cref="Show"/>가 호출된 뒤 숨김 처리되지 않은 상태인지 여부입니다.</summary>
        public bool IsVisible { get; private set; }
        #endregion // 프로퍼티

        #region 표시
        /// <summary>
        /// 팝업을 표시합니다.
        /// </summary>
        public virtual void Show()
        {
            IsVisible = true;
            gameObject.SetActive(true);
            OnShow();
        }

        /// <summary>
        /// 팝업을 숨깁니다.
        /// </summary>
        public virtual void Hide()
        {
            IsVisible = false;
            OnHide();
            gameObject.SetActive(false);
        }
        #endregion // 표시

        #region 확장 지점
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
        #endregion // 확장 지점
    }
}

