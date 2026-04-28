using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SWUtils
{
    /// <summary>
    /// 팝업 생성, 표시, 숨김, 등록, 캐시를 관리합니다.
    /// </summary>
    /// <remarks>
    /// 프리팹 직접 표시, 이미 생성된 인스턴스 표시, 카탈로그 키 기반 표시를 모두 지원합니다.
    /// 닫힘 완료를 Task로 기다릴 수 있어 확인창, 보상창처럼 순서가 중요한 UI 흐름에 사용할 수 있습니다.
    /// </remarks>
    public class PopupManager : SWSingleton<PopupManager>
    {
        #region 데이터
        /// <summary>
        /// 활성 팝업의 등록 키, 캐시 여부, 소유권, 닫힘 대기 상태를 저장합니다.
        /// </summary>
        private class PopupRecord
        {
            public string Key;
            public bool UseCache;
            public bool OwnedByManager;
            public TaskCompletionSource<bool> HideCompletionSource;
        }

        /// <summary>
        /// 팝업이 외부에서 비활성화되거나 파괴될 때 관리자에게 숨김 완료를 통지하는 내부 컴포넌트입니다.
        /// </summary>
        [DisallowMultipleComponent]
        private sealed class PopupLifecycleWatcher : MonoBehaviour
        {
            private PopupManager owner;
            private PopupBase popup;

            /// <summary>
            /// 감시할 팝업과 숨김 완료를 처리할 관리자를 설정합니다.
            /// </summary>
            /// <param name="popupManager">숨김 완료를 통지받을 팝업 관리자입니다.</param>
            /// <param name="popupBase">감시할 팝업 인스턴스입니다.</param>
            public void Initialize(PopupManager popupManager, PopupBase popupBase)
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
        #endregion // 데이터

        #region 필드
        [Header("=====> 전역 팝업 루트 <=====")]
        [SerializeField] private Canvas popupCanvas;
        [SerializeField] private Transform defaultParent;

        [Header("=====> 카탈로그 <=====")]
        [SerializeField] private PopupCatalog catalog;

        private readonly Dictionary<string, PopupBase> registeredPrefabs = new();
        private readonly Dictionary<string, PopupCatalog.Entry> registeredEntries = new();
        private readonly Dictionary<string, PopupBase> cachedPopups = new();
        private readonly Dictionary<PopupBase, PopupRecord> activeRecords = new();
        private readonly List<PopupBase> activePopups = new();
        private readonly HashSet<string> unregisteredKeys = new();
        #endregion // 필드

        #region 이벤트
        /// <summary>팝업이 표시된 직후 호출됩니다.</summary>
        public event Action<PopupBase> PopupShown;

        /// <summary>팝업이 숨김 완료 처리된 직후 호출됩니다.</summary>
        public event Action<PopupBase> PopupHidden;
        #endregion // 이벤트

        #region 프로퍼티
        /// <summary>현재 표시 중인 팝업 개수입니다.</summary>
        public int ActivePopupCount
        {
            get
            {
                RemoveNullPopups();
                return activePopups.Count;
            }
        }

        /// <summary>현재 표시 중인 팝업 목록입니다. 뒤쪽 항목일수록 화면 위에 표시됩니다.</summary>
        public IReadOnlyList<PopupBase> ActivePopups
        {
            get
            {
                RemoveNullPopups();
                return activePopups;
            }
        }

        /// <summary>팝업을 표시할 전역 Canvas입니다.</summary>
        public Canvas PopupCanvas => popupCanvas;

        /// <summary>팝업 인스턴스가 배치되는 기본 부모 Transform입니다.</summary>
        public Transform DefaultParent => EnsureDefaultParent();
        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 인스펙터에 연결된 카탈로그의 팝업 정보를 런타임 등록 테이블에 반영합니다.
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            EnsureDefaultParent();
            RegisterCatalog();
        }
        #endregion // 초기화

        #region 표시
        /// <summary>
        /// 팝업 프리팹을 생성하고 표시합니다.
        /// </summary>
        public T Show<T>(T prefab, Transform parent = null) where T : PopupBase
        {
            return Show(prefab, null, parent);
        }

        /// <summary>
        /// 팝업 프리팹을 생성하고 초기화한 뒤 표시합니다.
        /// </summary>
        public T Show<T>(T prefab, Action<T> setup, Transform parent = null) where T : PopupBase
        {
            if (prefab == null) return null;

            Transform targetParent = ResolveParent(parent);
            if (targetParent == null) return null;

            T popup = Instantiate(prefab, targetParent);
            return ShowPopup(popup, targetParent, null, false, true, setup);
        }

        /// <summary>
        /// 이미 존재하는 팝업 인스턴스를 표시합니다.
        /// </summary>
        public T ShowExisting<T>(T popup, Transform parent = null) where T : PopupBase
        {
            return ShowExisting(popup, null, parent);
        }

        /// <summary>
        /// 이미 존재하는 팝업 인스턴스를 초기화한 뒤 표시합니다.
        /// </summary>
        public T ShowExisting<T>(T popup, Action<T> setup, Transform parent = null) where T : PopupBase
        {
            if (popup == null) return null;

            Transform targetParent = ResolveParent(parent);
            if (targetParent == null) return null;

            return ShowPopup(popup, targetParent, null, false, false, setup);
        }

        /// <summary>
        /// 키로 등록된 팝업을 표시합니다.
        /// </summary>
        public PopupBase Show(string key, Transform parent = null)
        {
            if (!TryGetEntry(key, out PopupCatalog.Entry entry)) return null;

            Transform targetParent = ResolveParent(parent);
            if (targetParent == null) return null;

            PopupBase popup = GetOrCreatePopup(key, entry, targetParent);
            return ShowPopup(popup, targetParent, key, entry.useCache, true);
        }

        /// <summary>
        /// 키로 등록된 팝업을 가져와 지정한 타입으로 초기화한 뒤 표시합니다.
        /// </summary>
        public T Show<T>(string key, Action<T> setup = null, Transform parent = null) where T : PopupBase
        {
            if (!TryGetEntry(key, out PopupCatalog.Entry entry)) return null;

            Transform targetParent = ResolveParent(parent);
            if (targetParent == null) return null;

            PopupBase popup = GetOrCreatePopup(key, entry, targetParent);
            if (popup is not T typedPopup)
            {
                SWUtilsLog.LogWarning($"[PopupManager] Popup type mismatch. Key: {key}, Expected: {typeof(T).Name}, Actual: {popup.GetType().Name}");
                return null;
            }

            return ShowPopup(typedPopup, targetParent, key, entry.useCache, true, setup);
        }

        /// <summary>
        /// 팝업 프리팹을 표시하고 숨겨질 때까지 기다립니다.
        /// </summary>
        public async Task ShowAsync(PopupBase prefab, Transform parent = null)
        {
            PopupBase popup = Show(prefab, parent);
            if (popup == null) return;

            await GetHiddenTask(popup);
        }

        /// <summary>
        /// 팝업 프리팹을 초기화하고 표시한 뒤 숨겨질 때까지 기다립니다.
        /// </summary>
        public async Task ShowAsync<T>(T prefab, Action<T> setup, Transform parent = null) where T : PopupBase
        {
            T popup = Show(prefab, setup, parent);
            if (popup == null) return;

            await GetHiddenTask(popup);
        }

        /// <summary>
        /// 키로 등록된 팝업을 표시하고 숨겨질 때까지 기다립니다.
        /// </summary>
        public async Task ShowAsync(string key, Transform parent = null)
        {
            PopupBase popup = Show(key, parent);
            if (popup == null) return;

            await GetHiddenTask(popup);
        }

        /// <summary>
        /// 키로 등록된 팝업을 초기화하고 표시한 뒤 숨겨질 때까지 기다립니다.
        /// </summary>
        public async Task ShowAsync<T>(string key, Action<T> setup, Transform parent = null) where T : PopupBase
        {
            T popup = Show(key, setup, parent);
            if (popup == null) return;

            await GetHiddenTask(popup);
        }
        #endregion // 표시

        #region 숨김
        /// <summary>
        /// 지정한 팝업을 숨깁니다.
        /// </summary>
        public bool Hide(PopupBase popup)
        {
            if (popup == null) return false;
            if (!activeRecords.ContainsKey(popup)) return false;

            popup.Hide();
            CompletePopupHidden(popup, false);
            return true;
        }

        /// <summary>
        /// 지정한 키로 표시된 가장 위쪽 팝업을 숨깁니다.
        /// </summary>
        public bool Hide(string key)
        {
            return TryGetActive(key, out PopupBase popup) && Hide(popup);
        }

        /// <summary>
        /// 가장 위에 있는 팝업을 숨깁니다.
        /// </summary>
        public bool HideTop()
        {
            RemoveNullPopups();
            if (activePopups.Count == 0) return false;

            PopupBase popup = activePopups[activePopups.Count - 1];
            return Hide(popup);
        }

        /// <summary>
        /// 모든 팝업을 숨깁니다.
        /// </summary>
        public void HideAll()
        {
            PopupBase[] popups = activePopups.ToArray();
            for (int i = popups.Length - 1; i >= 0; i--)
            {
                Hide(popups[i]);
            }
        }
        #endregion // 숨김

        #region 등록
        /// <summary>
        /// 팝업 프리팹을 키로 등록합니다.
        /// </summary>
        public void Register(string key, PopupBase prefab)
        {
            if (string.IsNullOrEmpty(key) || prefab == null) return;

            ClearCache(key);
            unregisteredKeys.Remove(key);
            registeredPrefabs[key] = prefab;
            registeredEntries.Remove(key);
        }

        /// <summary>
        /// 키로 등록된 팝업 프리팹을 제거합니다.
        /// </summary>
        public bool Unregister(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

            ClearCache(key);
            bool removedEntry = registeredEntries.Remove(key);
            bool removedPrefab = registeredPrefabs.Remove(key);
            bool catalogHasKey = catalog != null && catalog.TryGetPrefab(key, out _);

            if (removedEntry || removedPrefab || catalogHasKey)
                unregisteredKeys.Add(key);

            return removedEntry || removedPrefab || catalogHasKey;
        }

        /// <summary>
        /// 키로 등록된 팝업 프리팹을 가져옵니다.
        /// </summary>
        public bool TryGetPrefab(string key, out PopupBase prefab)
        {
            prefab = null;
            if (string.IsNullOrEmpty(key)) return false;
            if (unregisteredKeys.Contains(key)) return false;

            if (registeredPrefabs.TryGetValue(key, out prefab) && prefab != null)
                return true;

            return catalog != null && catalog.TryGetPrefab(key, out prefab);
        }
        #endregion // 등록

        #region 전역 루트
        /// <summary>
        /// 외부에서 준비한 전역 팝업 루트를 설정합니다.
        /// </summary>
        /// <param name="parent">팝업 인스턴스를 배치할 Transform입니다.</param>
        public void SetDefaultParent(Transform parent)
        {
            if (parent == null) return;

            defaultParent = parent;
            popupCanvas = parent.GetComponentInParent<Canvas>();
        }

        /// <summary>
        /// 외부에서 준비한 전역 팝업 Canvas와 루트를 설정합니다.
        /// </summary>
        /// <param name="canvas">팝업용 Canvas입니다.</param>
        /// <param name="parent">팝업 인스턴스를 배치할 Transform입니다. null이면 Canvas Transform을 사용합니다.</param>
        public void SetPopupCanvas(Canvas canvas, Transform parent = null)
        {
            if (canvas == null) return;

            popupCanvas = canvas;
            defaultParent = parent != null ? parent : canvas.transform;
        }
        #endregion // 전역 루트

        #region 조회 및 캐시
        /// <summary>
        /// 키로 표시된 가장 위쪽 활성 팝업을 가져옵니다.
        /// </summary>
        public bool TryGetActive(string key, out PopupBase popup)
        {
            popup = null;
            if (string.IsNullOrEmpty(key)) return false;

            RemoveNullPopups();
            for (int i = activePopups.Count - 1; i >= 0; i--)
            {
                PopupBase current = activePopups[i];
                if (current == null) continue;
                if (!activeRecords.TryGetValue(current, out PopupRecord record)) continue;
                if (record.Key != key) continue;

                popup = current;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 캐시된 팝업을 제거합니다.
        /// </summary>
        public void ClearCache(string key = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                foreach (PopupBase cachedPopup in cachedPopups.Values)
                    DestroyCachedPopup(cachedPopup);

                cachedPopups.Clear();
                return;
            }

            if (!cachedPopups.TryGetValue(key, out PopupBase popup)) return;

            cachedPopups.Remove(key);
            DestroyCachedPopup(popup);
        }
        #endregion // 조회 및 캐시

        #region 내부
        /// <summary>
        /// 팝업을 활성 목록에 등록하고 부모, 정렬, lifecycle 감시자를 설정한 뒤 표시합니다.
        /// </summary>
        /// <typeparam name="T">표시할 팝업 타입입니다.</typeparam>
        /// <param name="popup">표시할 팝업 인스턴스입니다.</param>
        /// <param name="parent">팝업을 배치할 부모 Transform입니다.</param>
        /// <param name="key">키 기반 팝업이면 등록 키, 직접 표시 팝업이면 null입니다.</param>
        /// <param name="useCache">숨김 후 인스턴스를 캐시에 남길지 여부입니다.</param>
        /// <param name="ownedByManager">관리자가 생성한 인스턴스라서 숨김 후 파괴할 수 있는지 여부입니다.</param>
        /// <returns>표시된 팝업 인스턴스입니다.</returns>
        private T ShowPopup<T>(T popup, Transform parent, string key, bool useCache, bool ownedByManager) where T : PopupBase
        {
            return ShowPopup(popup, parent, key, useCache, ownedByManager, null);
        }

        /// <summary>
        /// 팝업을 활성 목록에 등록하고 초기화, 부모, 정렬, lifecycle 감시자를 설정한 뒤 표시합니다.
        /// </summary>
        /// <typeparam name="T">표시할 팝업 타입입니다.</typeparam>
        /// <param name="popup">표시할 팝업 인스턴스입니다.</param>
        /// <param name="parent">팝업을 배치할 부모 Transform입니다.</param>
        /// <param name="key">키 기반 팝업이면 등록 키, 직접 표시 팝업이면 null입니다.</param>
        /// <param name="useCache">숨김 후 인스턴스를 캐시에 남길지 여부입니다.</param>
        /// <param name="ownedByManager">관리자가 생성한 인스턴스라서 숨김 후 파괴할 수 있는지 여부입니다.</param>
        /// <param name="setup">팝업이 표시되기 전에 실행할 초기화 콜백입니다.</param>
        /// <returns>표시된 팝업 인스턴스입니다.</returns>
        private T ShowPopup<T>(T popup, Transform parent, string key, bool useCache, bool ownedByManager, Action<T> setup) where T : PopupBase
        {
            if (popup == null) return null;

            if (popup.transform.parent != parent)
                popup.transform.SetParent(parent, false);

            activePopups.Remove(popup);
            activePopups.Add(popup);

            if (!activeRecords.ContainsKey(popup))
            {
                activeRecords[popup] = new PopupRecord
                {
                    Key = key,
                    UseCache = useCache,
                    OwnedByManager = ownedByManager,
                    HideCompletionSource = new TaskCompletionSource<bool>()
                };
            }

            EnsureLifecycleWatcher(popup);
            popup.transform.SetAsLastSibling();
            setup?.Invoke(popup);
            popup.Show();
            PopupShown?.Invoke(popup);
            return popup;
        }

        /// <summary>
        /// 키 등록 정보에 맞춰 캐시된 팝업을 재사용하거나 새 팝업을 생성합니다.
        /// </summary>
        /// <param name="key">조회할 팝업 키입니다.</param>
        /// <param name="entry">카탈로그 또는 런타임 등록에서 가져온 팝업 정보입니다.</param>
        /// <param name="parent">새로 생성할 때 사용할 부모 Transform입니다.</param>
        /// <returns>재사용 또는 생성된 팝업 인스턴스입니다.</returns>
        private PopupBase GetOrCreatePopup(string key, PopupCatalog.Entry entry, Transform parent)
        {
            if (entry.useCache && cachedPopups.TryGetValue(key, out PopupBase cachedPopup) && cachedPopup != null)
                return cachedPopup;

            PopupBase popup = Instantiate(entry.prefab, parent);

            if (entry.useCache)
                cachedPopups[key] = popup;

            return popup;
        }

        /// <summary>
        /// 팝업이 숨겨질 때 완료되는 Task를 가져옵니다.
        /// </summary>
        /// <param name="popup">대기할 팝업 인스턴스입니다.</param>
        /// <returns>활성 팝업이면 숨김 완료 Task, 아니면 완료된 Task입니다.</returns>
        private Task GetHiddenTask(PopupBase popup)
        {
            return popup != null && activeRecords.TryGetValue(popup, out PopupRecord record)
                ? record.HideCompletionSource.Task
                : Task.CompletedTask;
        }

        /// <summary>
        /// 팝업 GameObject에 lifecycle 감시자를 추가하거나 기존 감시자를 갱신합니다.
        /// </summary>
        /// <param name="popup">감시자를 연결할 팝업 인스턴스입니다.</param>
        private void EnsureLifecycleWatcher(PopupBase popup)
        {
            PopupLifecycleWatcher watcher = popup.GetComponent<PopupLifecycleWatcher>();
            if (watcher == null)
                watcher = popup.gameObject.AddComponent<PopupLifecycleWatcher>();

            watcher.Initialize(this, popup);
        }

        /// <summary>
        /// 호출자가 지정한 부모, 기본 부모, 전역 Canvas 순서로 팝업 부모를 결정합니다.
        /// </summary>
        /// <param name="parent">호출자가 지정한 부모 Transform입니다.</param>
        /// <returns>팝업을 배치할 부모 Transform입니다.</returns>
        private Transform ResolveParent(Transform parent)
        {
            if (parent != null) return parent;
            return EnsureDefaultParent();
        }

        /// <summary>
        /// 전역 팝업 부모 Transform을 확보합니다.
        /// </summary>
        private Transform EnsureDefaultParent()
        {
            if (defaultParent != null) return defaultParent;

            if (popupCanvas == null)
                popupCanvas = GetComponentInChildren<Canvas>(true);

            if (popupCanvas == null)
            {
                SWUtilsLog.LogWarning("[PopupManager] Popup Canvas is null. Assign a global popup Canvas or default parent.");
                return null;
            }

            defaultParent = popupCanvas.transform;
            return defaultParent;
        }

        /// <summary>
        /// 인스펙터에 연결된 카탈로그의 유효한 항목을 런타임 등록 테이블에 등록합니다.
        /// </summary>
        private void RegisterCatalog()
        {
            if (catalog == null) return;

            foreach (PopupCatalog.Entry entry in catalog.Entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.key) || entry.prefab == null) continue;

                registeredEntries[entry.key] = entry;
                registeredPrefabs[entry.key] = entry.prefab;
            }
        }

        /// <summary>
        /// 키에 해당하는 팝업 등록 정보를 런타임 등록, 카탈로그, 직접 등록 프리팹 순서로 찾습니다.
        /// </summary>
        /// <param name="key">조회할 팝업 키입니다.</param>
        /// <param name="entry">조회된 팝업 등록 정보입니다.</param>
        /// <returns>유효한 등록 정보가 있으면 true입니다.</returns>
        private bool TryGetEntry(string key, out PopupCatalog.Entry entry)
        {
            entry = null;
            if (string.IsNullOrEmpty(key)) return false;
            if (unregisteredKeys.Contains(key)) return false;

            if (registeredEntries.TryGetValue(key, out entry) && entry != null && entry.prefab != null)
                return true;

            if (registeredPrefabs.TryGetValue(key, out PopupBase prefab) && prefab != null)
            {
                entry = new PopupCatalog.Entry
                {
                    key = key,
                    prefab = prefab,
                    useCache = false
                };
                return true;
            }

            if (catalog != null && catalog.TryGetEntry(key, out entry))
                return true;

            return false;
        }

        /// <summary>
        /// 이미 파괴된 팝업 참조를 활성 목록에서 제거합니다.
        /// </summary>
        private void RemoveNullPopups()
        {
            for (int i = activePopups.Count - 1; i >= 0; i--)
            {
                if (activePopups[i] == null)
                    activePopups.RemoveAt(i);
            }
        }

        /// <summary>
        /// 활성 상태가 아닌 캐시 팝업 인스턴스를 파괴합니다.
        /// </summary>
        /// <param name="popup">파괴할 캐시 팝업 인스턴스입니다.</param>
        private void DestroyCachedPopup(PopupBase popup)
        {
            if (popup == null) return;
            if (activeRecords.ContainsKey(popup)) return;

            Destroy(popup.gameObject);
        }

        /// <summary>
        /// 팝업 숨김, 비활성화, 파괴 이벤트를 하나의 완료 경로로 정리합니다.
        /// </summary>
        /// <param name="popup">숨김 처리가 완료된 팝업 인스턴스입니다.</param>
        /// <param name="destroyed">팝업 GameObject가 이미 파괴되는 중이면 true입니다.</param>
        internal void CompletePopupHidden(PopupBase popup, bool destroyed)
        {
            if (popup == null) return;
            if (!activeRecords.TryGetValue(popup, out PopupRecord record)) return;

            activeRecords.Remove(popup);
            activePopups.Remove(popup);
            record.HideCompletionSource.TrySetResult(true);
            PopupHidden?.Invoke(popup);

            if (record.UseCache)
            {
                if (!destroyed)
                    popup.gameObject.SetActive(false);
                return;
            }

            if (record.OwnedByManager && !destroyed)
                Destroy(popup.gameObject);
        }
        #endregion // 내부
    }
}
