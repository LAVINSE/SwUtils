using UnityEngine;

namespace SWUtils
{
    /// <summary>
    /// 팝업이 외부에서 비활성화되거나 파괴될 때 관리자에게 숨김 완료를 통지합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SWPopupLifecycle : MonoBehaviour
    {
        private SWPopupManager owner;
        private SWPopupBase popup;

        /// <summary>
        /// 감시할 팝업과 숨김 완료를 처리할 관리자를 설정합니다.
        /// </summary>
        /// <param name="popupManager">숨김 완료를 통지받을 팝업 관리자입니다.</param>
        /// <param name="popupBase">감시할 팝업 인스턴스입니다.</param>
        public void Initialize(SWPopupManager popupManager, SWPopupBase popupBase)
        {
            owner = popupManager;
            popup = popupBase;
        }

        /// <summary>
        /// 팝업 GameObject가 비활성화될 때 관리자 상태를 정리합니다.
        /// </summary>
        private void OnDisable()
        {
            owner?.CompletePopupHidden(popup, false);
        }

        /// <summary>
        /// 팝업 GameObject가 파괴될 때 관리자 상태를 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            owner?.CompletePopupHidden(popup, true);
        }
    }
}
