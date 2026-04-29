using System.Collections;
using System.Collections.Generic;
using SWTools;
using UnityEngine;
using UnityEngine.Pool;

namespace SWPool
{
    /// <summary>
    /// Unity ObjectPool 기반으로 프리팹별 오브젝트 풀을 관리하는 컴포넌트입니다.
    /// </summary>
    public class SWPool : SWMonoBehaviour, IPool
    {
        #region 필드
        [SWGroup("=====> 설정 <=====")]
        [SerializeField] private bool collectionCheck = true;
        [SerializeField] private int defaultCapacity = 10;
        [SerializeField] private int maxPoolSize = 1000;

        private readonly Dictionary<GameObject, ObjectPool<GameObject>> poolDict = new();
        private readonly Dictionary<GameObject, GameObject> instanceToPrefabDict = new();
        /// <summary>지연 반납 예약 중인 코루틴. 조기 반납 시 취소에 사용.</summary>
        private readonly Dictionary<GameObject, Coroutine> delayedReleaseDict = new();
        /// <summary>WaitForSeconds 캐시. GC 할당 방지.</summary>
        private readonly Dictionary<float, WaitForSeconds> waitCacheDict = new();
        #endregion // 필드

        #region 프로퍼티
        #endregion // 프로퍼티

        #region 초기화
        private void OnDestroy()
        {
            ClearAll();
        }
        #endregion // 초기화

        #region 풀 기능
        /// <inheritdoc/>
        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null)
            {
                SWUtils.SWUtilsLog.LogWarning("[SWPool] 프리웜 실패: 프리팹이 null입니다.");
                return;
            }

            if (count <= 0)
            {
                SWUtils.SWUtilsLog.LogWarning($"[SWPool] 프리웜 실패: 생성 수가 0 이하입니다. Count: {count}");
                return;
            }

            var pool = GetOrCreatePool(prefab);
            var tempInstances = new GameObject[count];

            for (int index = 0; index < count; index++)
                tempInstances[index] = pool.Get();

            for (int index = 0; index < count; index++)
                pool.Release(tempInstances[index]);
        }

        /// <inheritdoc/>
        public GameObject Spawn(GameObject prefab, Vector3 position = default,
            Quaternion rotation = default, Transform parent = null)
        {
            if (prefab == null)
            {
                SWUtils.SWUtilsLog.LogWarning("[SWPool] Spawn 실패: 프리팹이 null입니다.");
                return null;
            }

            var pool = GetOrCreatePool(prefab);
            var instance = pool.Get();

            var instanceTransform = instance.transform;
            instanceTransform.SetParent(parent != null ? parent : transform, false);
            instanceTransform.SetPositionAndRotation(position, rotation);

            NotifyPoolables(instance);

            return instance;
        }

        /// <inheritdoc/>
        public T Spawn<T>(GameObject prefab, Vector3 position = default,
            Quaternion rotation = default, Transform parent = null) where T : Component
        {
            var instance = Spawn(prefab, position, rotation, parent);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        /// <inheritdoc/>
        public void Release(GameObject instance)
        {
            if (instance == null)
            {
                SWUtils.SWUtilsLog.LogWarning("[SWPool] Release 실패: 반환할 인스턴스가 null입니다.");
                return;
            }

            // 예약된 지연 반납이 있으면 취소
            if (delayedReleaseDict.TryGetValue(instance, out var reservedCoroutine))
            {
                if (reservedCoroutine != null) StopCoroutine(reservedCoroutine);
                delayedReleaseDict.Remove(instance);
            }

            if (!instanceToPrefabDict.TryGetValue(instance, out var prefab))
            {
                SWUtils.SWUtilsLog.LogWarning($"[SWPool] 등록되지 않은 인스턴스라 파괴합니다: {instance.name}");
                Destroy(instance);
                return;
            }

            if (poolDict.TryGetValue(prefab, out var pool))
                pool.Release(instance);
            else
            {
                SWUtils.SWUtilsLog.LogWarning($"[SWPool] 연결된 풀이 없어 인스턴스를 파괴합니다: {instance.name}");
                Destroy(instance);
            }
        }

        /// <inheritdoc/>
        public void Release(GameObject instance, float delay)
        {
            if (instance == null) return;

            if (delay <= 0f)
            {
                Release(instance);
                return;
            }

            // 비활성 상태면 코루틴 시작 불가, 즉시 반납
            if (!gameObject.activeInHierarchy)
            {
                Release(instance);
                return;
            }

            // 이미 예약되어 있으면 기존 예약 취소
            if (delayedReleaseDict.TryGetValue(instance, out var existingCoroutine))
            {
                if (existingCoroutine != null) StopCoroutine(existingCoroutine);
            }

            var releaseCoroutine = StartCoroutine(DelayedReleaseRoutine(instance, delay));
            delayedReleaseDict[instance] = releaseCoroutine;
        }

        /// <inheritdoc/>
        public void Clear(GameObject prefab)
        {
            if (prefab == null)
            {
                SWUtils.SWUtilsLog.LogWarning("[SWPool] Clear 실패: 프리팹이 null입니다.");
                return;
            }

            if (!poolDict.TryGetValue(prefab, out var pool)) return;

            // 해당 프리팹의 예약된 지연 반납 취소
            var instancesToCancel = new List<GameObject>();
            foreach (var keyValue in delayedReleaseDict)
            {
                if (instanceToPrefabDict.TryGetValue(keyValue.Key, out var mappedPrefab) && mappedPrefab == prefab)
                    instancesToCancel.Add(keyValue.Key);
            }
            foreach (var instance in instancesToCancel)
            {
                if (delayedReleaseDict[instance] != null)
                    StopCoroutine(delayedReleaseDict[instance]);
                delayedReleaseDict.Remove(instance);
            }

            pool.Clear();
            poolDict.Remove(prefab);

            var instancesToRemove = new List<GameObject>();
            foreach (var keyValue in instanceToPrefabDict)
                if (keyValue.Value == prefab) instancesToRemove.Add(keyValue.Key);
            foreach (var instance in instancesToRemove)
                instanceToPrefabDict.Remove(instance);
        }

        /// <inheritdoc/>
        public void ClearAll()
        {
            StopAllCoroutines();
            delayedReleaseDict.Clear();

            foreach (var pool in poolDict.Values)
                pool.Clear();
            poolDict.Clear();
            instanceToPrefabDict.Clear();
        }

        /// <inheritdoc/>
        public int CountInPool(GameObject prefab)
        {
            return poolDict.TryGetValue(prefab, out var pool) ? pool.CountInactive : 0;
        }
        #endregion // 풀 기능

        #region 내부
        /// <summary>
        /// 지연 반납을 처리하는 코루틴.
        /// </summary>
        /// <param name="instance">반납할 오브젝트</param>
        /// <param name="delay">지연 시간(초)</param>
        /// <returns>IEnumerator</returns>
        private IEnumerator DelayedReleaseRoutine(GameObject instance, float delay)
        {
            yield return GetWait(delay);

            delayedReleaseDict.Remove(instance);
            Release(instance);
        }

        /// <summary>
        /// 캐시된 WaitForSeconds를 반환한다. 없으면 생성하여 캐싱한다.
        /// </summary>
        /// <param name="seconds">대기 시간(초)</param>
        /// <returns>캐시된 WaitForSeconds 인스턴스</returns>
        private WaitForSeconds GetWait(float seconds)
        {
            if (!waitCacheDict.TryGetValue(seconds, out var waitForSeconds))
            {
                waitForSeconds = new WaitForSeconds(seconds);
                waitCacheDict[seconds] = waitForSeconds;
            }
            return waitForSeconds;
        }

        /// <summary>
        /// 프리팹에 대한 ObjectPool을 가져오거나 생성한다.
        /// </summary>
        /// <param name="prefab">대상 프리팹</param>
        /// <returns>프리팹에 매핑된 ObjectPool</returns>
        private ObjectPool<GameObject> GetOrCreatePool(GameObject prefab)
        {
            if (poolDict.TryGetValue(prefab, out var existingPool))
                return existingPool;

            GameObject capturedPrefab = prefab;

            var pool = new ObjectPool<GameObject>(
                createFunc: () => CreatePooled(capturedPrefab),
                actionOnGet: OnGetFromPool,
                actionOnRelease: OnReleaseToPool,
                actionOnDestroy: OnDestroyPooled,
                collectionCheck: collectionCheck,
                defaultCapacity: defaultCapacity,
                maxSize: maxPoolSize
            );

            poolDict[prefab] = pool;
            SWUtils.SWUtilsLog.Log($"[SWPool] 풀 생성 완료: {prefab.name}");
            return pool;
        }

        /// <summary>
        /// 새 인스턴스를 생성하고 등록한다.
        /// </summary>
        /// <param name="prefab">생성할 원본 프리팹</param>
        /// <returns>생성된 인스턴스</returns>
        private GameObject CreatePooled(GameObject prefab)
        {
            var instance = Instantiate(prefab, transform);
            instance.name = prefab.name;
            instanceToPrefabDict[instance] = prefab;
            return instance;
        }

        /// <summary>
        /// 풀러블 컴포넌트들에게 풀 참조를 전달하고 Spawn 콜백을 호출한다.
        /// </summary>
        /// <param name="instance">대상 오브젝트</param>
        private void NotifyPoolables(GameObject instance)
        {
            var poolables = instance.GetComponentsInChildren<IPoolable>(true);
            for (int index = 0; index < poolables.Length; index++)
            {
                poolables[index].SetPool(this);
                poolables[index].OnSpawnFromPool();
            }
        }

        /// <summary>
        /// ObjectPool의 Get 호출 시 자동으로 실행된다.
        /// </summary>
        /// <param name="instance">꺼내진 오브젝트</param>
        private void OnGetFromPool(GameObject instance)
        {
            instance.SetActive(true);
        }

        /// <summary>
        /// ObjectPool의 Release 호출 시 자동으로 실행된다.
        /// </summary>
        /// <param name="instance">반납된 오브젝트</param>
        private void OnReleaseToPool(GameObject instance)
        {
            var poolables = instance.GetComponentsInChildren<IPoolable>(true);
            for (int index = 0; index < poolables.Length; index++)
                poolables[index].OnReturnToPool();

            instance.SetActive(false);
            instance.transform.SetParent(transform, false);
        }

        /// <summary>
        /// ObjectPool의 최대 크기 초과 시 자동으로 실행된다.
        /// </summary>
        /// <param name="instance">파괴할 오브젝트</param>
        private void OnDestroyPooled(GameObject instance)
        {
            if (instance != null)
            {
                instanceToPrefabDict.Remove(instance);
                Destroy(instance);
            }
        }
        #endregion // 내부
    }
}
