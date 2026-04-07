using System.Collections;
using System.Collections.Generic;
using SWTools;
using UnityEngine;
using UnityEngine.Pool;

namespace SWPool
{
    public class SwPool : SWMonoBehaviour, IPool
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
            if (prefab == null || count <= 0) return;

            var pool = GetOrCreatePool(prefab);
            var temp = new GameObject[count];

            for (int i = 0; i < count; i++)
                temp[i] = pool.Get();

            for (int i = 0; i < count; i++)
                pool.Release(temp[i]);
        }

        /// <inheritdoc/>
        public GameObject Spawn(GameObject prefab, Vector3 pos = default,
            Quaternion rotation = default, Transform parent = null)
        {
            if (prefab == null) return null;

            var pool = GetOrCreatePool(prefab);
            var obj = pool.Get();

            var t = obj.transform;
            t.SetParent(parent != null ? parent : transform, false);
            t.SetPositionAndRotation(pos, rotation);

            NotifyPoolables(obj);

            return obj;
        }

        /// <inheritdoc/>
        public T Spawn<T>(GameObject prefab, Vector3 pos = default,
            Quaternion rotation = default, Transform parent = null) where T : Component
        {
            var obj = Spawn(prefab, pos, rotation, parent);
            return obj != null ? obj.GetComponent<T>() : null;
        }

        /// <inheritdoc/>
        public void Release(GameObject instance)
        {
            if (instance == null) return;

            // 예약된 지연 반납이 있으면 취소
            if (delayedReleaseDict.TryGetValue(instance, out var coroutine))
            {
                if (coroutine != null) StopCoroutine(coroutine);
                delayedReleaseDict.Remove(instance);
            }

            if (!instanceToPrefabDict.TryGetValue(instance, out var prefab))
            {
                Destroy(instance);
                return;
            }

            if (poolDict.TryGetValue(prefab, out var pool))
                pool.Release(instance);
            else
                Destroy(instance);
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
            if (delayedReleaseDict.TryGetValue(instance, out var existing))
            {
                if (existing != null) StopCoroutine(existing);
            }

            var coroutine = StartCoroutine(DelayedReleaseRoutine(instance, delay));
            delayedReleaseDict[instance] = coroutine;
        }

        /// <inheritdoc/>
        public void Clear(GameObject prefab)
        {
            if (prefab == null || !poolDict.TryGetValue(prefab, out var pool)) return;

            // 해당 프리팹의 예약된 지연 반납 취소
            var toCancel = new List<GameObject>();
            foreach (var kv in delayedReleaseDict)
            {
                if (instanceToPrefabDict.TryGetValue(kv.Key, out var p) && p == prefab)
                    toCancel.Add(kv.Key);
            }
            foreach (var obj in toCancel)
            {
                if (delayedReleaseDict[obj] != null)
                    StopCoroutine(delayedReleaseDict[obj]);
                delayedReleaseDict.Remove(obj);
            }

            pool.Clear();
            poolDict.Remove(prefab);

            var toRemove = new List<GameObject>();
            foreach (var kv in instanceToPrefabDict)
                if (kv.Value == prefab) toRemove.Add(kv.Key);
            foreach (var obj in toRemove)
                instanceToPrefabDict.Remove(obj);
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
            if (!waitCacheDict.TryGetValue(seconds, out var wait))
            {
                wait = new WaitForSeconds(seconds);
                waitCacheDict[seconds] = wait;
            }
            return wait;
        }

        /// <summary>
        /// 프리팹에 대한 ObjectPool을 가져오거나 생성한다.
        /// </summary>
        /// <param name="prefab">대상 프리팹</param>
        /// <returns>프리팹에 매핑된 ObjectPool</returns>
        private ObjectPool<GameObject> GetOrCreatePool(GameObject prefab)
        {
            if (poolDict.TryGetValue(prefab, out var existing))
                return existing;

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
            return pool;
        }

        /// <summary>
        /// 새 인스턴스를 생성하고 등록한다.
        /// </summary>
        /// <param name="prefab">생성할 원본 프리팹</param>
        /// <returns>생성된 인스턴스</returns>
        private GameObject CreatePooled(GameObject prefab)
        {
            var obj = Instantiate(prefab, transform);
            obj.name = prefab.name;
            instanceToPrefabDict[obj] = prefab;
            return obj;
        }

        /// <summary>
        /// 풀러블 컴포넌트들에게 풀 참조를 전달하고 Spawn 콜백을 호출한다.
        /// </summary>
        /// <param name="obj">대상 오브젝트</param>
        private void NotifyPoolables(GameObject obj)
        {
            var poolables = obj.GetComponentsInChildren<IPoolable>(true);
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].SetPool(this);
                poolables[i].OnSpawnFromPool();
            }
        }

        /// <summary>
        /// ObjectPool의 Get 호출 시 자동으로 실행된다.
        /// </summary>
        /// <param name="obj">꺼내진 오브젝트</param>
        private void OnGetFromPool(GameObject obj)
        {
            obj.SetActive(true);
        }

        /// <summary>
        /// ObjectPool의 Release 호출 시 자동으로 실행된다.
        /// </summary>
        /// <param name="obj">반납된 오브젝트</param>
        private void OnReleaseToPool(GameObject obj)
        {
            var poolables = obj.GetComponentsInChildren<IPoolable>(true);
            for (int i = 0; i < poolables.Length; i++)
                poolables[i].OnReturnToPool();

            obj.SetActive(false);
            obj.transform.SetParent(transform, false);
        }

        /// <summary>
        /// ObjectPool의 최대 크기 초과 시 자동으로 실행된다.
        /// </summary>
        /// <param name="obj">파괴할 오브젝트</param>
        private void OnDestroyPooled(GameObject obj)
        {
            if (obj != null)
            {
                instanceToPrefabDict.Remove(obj);
                Destroy(obj);
            }
        }
        #endregion // 내부
    }
}