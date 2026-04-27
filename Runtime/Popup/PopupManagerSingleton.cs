using System.Threading.Tasks;
using UnityEngine;

namespace SWUtils
{
    /// <summary>
    /// PopupManager를 DI로 등록하지 않는 프로젝트에서 사용하는 싱글톤 래퍼.
    /// </summary>
    public class PopupManagerSingleton : SWSingleton<PopupManagerSingleton>, IPopupService
    {
        #region 필드
        [Header("=====> 참조 <=====")]
        [SerializeField] private PopupManager popupManager;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>실제 팝업 서비스를 처리하는 구현체.</summary>
        public PopupManager PopupManager => popupManager;

        /// <summary>현재 표시 중인 팝업 개수.</summary>
        public int ActivePopupCount
        {
            get
            {
                EnsurePopupManager();
                return popupManager.ActivePopupCount;
            }
        }
        #endregion // 프로퍼티

        #region 초기화
        public override void Awake()
        {
            base.Awake();

            if (Instance != this) return;

            EnsurePopupManager();
            SWUtilsLog.Log("[PopupManagerSingleton] Initialized.");
        }
        #endregion // 초기화

        #region 팝업 서비스
        /// <summary>
        /// 팝업 프리팹을 생성하고 표시한다.
        /// </summary>
        public T Show<T>(T prefab, Transform parent = null) where T : PopupBase
        {
            EnsurePopupManager();
            return popupManager.Show(prefab, parent);
        }

        /// <summary>
        /// 이미 존재하는 팝업 인스턴스를 표시한다.
        /// </summary>
        public T ShowExisting<T>(T popup, Transform parent = null) where T : PopupBase
        {
            EnsurePopupManager();
            return popupManager.ShowExisting(popup, parent);
        }

        /// <summary>
        /// 등록된 키로 팝업을 생성하고 표시한다.
        /// </summary>
        public PopupBase Show(string key, Transform parent = null)
        {
            EnsurePopupManager();
            return popupManager.Show(key, parent);
        }

        /// <summary>
        /// 팝업 프리팹을 표시하고 숨겨질 때까지 기다린다.
        /// </summary>
        public Task ShowAsync(PopupBase prefab, Transform parent = null)
        {
            EnsurePopupManager();
            return popupManager.ShowAsync(prefab, parent);
        }

        /// <summary>
        /// 등록된 키로 팝업을 표시하고 숨겨질 때까지 기다린다.
        /// </summary>
        public Task ShowAsync(string key, Transform parent = null)
        {
            EnsurePopupManager();
            return popupManager.ShowAsync(key, parent);
        }

        /// <summary>
        /// 가장 위에 있는 활성 팝업을 숨긴다.
        /// </summary>
        public bool HideTop()
        {
            EnsurePopupManager();
            return popupManager.HideTop();
        }

        /// <summary>
        /// 모든 활성 팝업을 숨긴다.
        /// </summary>
        public void HideAll()
        {
            EnsurePopupManager();
            popupManager.HideAll();
        }

        /// <summary>
        /// 런타임에 팝업 프리팹을 키로 등록한다.
        /// </summary>
        public void Register(string key, PopupBase prefab)
        {
            EnsurePopupManager();
            popupManager.Register(key, prefab);
        }

        /// <summary>
        /// 키로 등록된 팝업 프리팹을 해제한다.
        /// </summary>
        public bool Unregister(string key)
        {
            EnsurePopupManager();
            return popupManager.Unregister(key);
        }

        /// <summary>
        /// 등록된 팝업 프리팹을 가져온다.
        /// </summary>
        public bool TryGetPrefab(string key, out PopupBase prefab)
        {
            EnsurePopupManager();
            return popupManager.TryGetPrefab(key, out prefab);
        }
        #endregion // 팝업 서비스

        #region 내부
        /// <summary>
        /// 실제 PopupManager 컴포넌트를 찾거나 생성한다.
        /// </summary>
        private void EnsurePopupManager()
        {
            if (popupManager != null) return;

            popupManager = GetComponent<PopupManager>();
            if (popupManager == null)
            {
                popupManager = gameObject.AddComponent<PopupManager>();
                SWUtilsLog.Log("[PopupManagerSingleton] PopupManager auto-created.");
            }
        }
        #endregion // 내부
    }
}
