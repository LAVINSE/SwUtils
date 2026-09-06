using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

using SW.Attributes;

using SW.Util;

namespace SW.Pooling
{
    /// <summary>
    /// Unity ObjectPool 기반으로 프리팹별 오브젝트 풀을 전역 관리하는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 프리팹별 풀 생성, 그룹 선택, 지연 반환, 예열 및 유휴 풀 정리를 지원합니다.
    /// </remarks>
    public class SWPool : SWSingleton<SWPool>, IPool
    {
        #region 필드
        [SWGroup("=====> 설정 <=====")]
        [SerializeField] private bool collectionCheck = true;
        [SerializeField] private int defaultCapacity = 10;
        [SerializeField] private int maxPoolSize = 1000;

        [SWGroup("=====> 자동 정리 <=====")]
        [SerializeField] private bool enableAutoClear = false;
        [SerializeField, SWCondition("enableAutoClear", true)] private float autoClearIdleSeconds = 60f;
        [SerializeField, SWCondition("enableAutoClear", true)] private float autoClearCheckInterval = 10f;

        private readonly Dictionary<GameObject, ObjectPool<GameObject>> poolDictionary = new();
        private readonly Dictionary<GameObject, GameObject> instanceToPrefabDictionary = new();
        private readonly Dictionary<string, GameObject> nameToPrefabDictionary = new();
        private readonly Dictionary<string, List<GameObject>> groupToPrefabListDictionary = new();
        private readonly Dictionary<string, int> groupSequenceIndexDictionary = new();
        private readonly Dictionary<GameObject, PoolStatistics> poolStatisticsDictionary = new();
        /// <summary>지연 반환 예약 중인 코루틴을 저장하여 조기 반환 때 취소합니다.</summary>
        private readonly Dictionary<GameObject, Coroutine> delayedReleaseDictionary = new();
        /// <summary>WaitForSeconds 캐시입니다. 반복 생성에 따른 메모리 할당을 줄입니다.</summary>
        private readonly Dictionary<float, WaitForSeconds> waitCacheDictionary = new();
        /// <summary>프리팹 풀별 마지막 사용 시각(Time.unscaledTime)입니다. 자동 정리 판정에 사용합니다.</summary>
        private readonly Dictionary<GameObject, float> lastUseTimeDictionary = new();
        /// <summary>자동 정리 대상 수집용 임시 버퍼입니다. 매 체크마다 재사용합니다.</summary>
        private readonly List<GameObject> autoClearBuffer = new();

        /// <summary>WaitForSeconds 캐시 최대 개수입니다. 초과하면 캐시하지 않고 새로 생성합니다.</summary>
        private const int MaxWaitCacheCount = 64;
        /// <summary>WaitForSeconds 캐시 키 반올림 단위(초)입니다.</summary>
        private const float WaitCacheStep = 0.01f;

        private Coroutine autoClearRoutine;
        #endregion // 필드

        #region 데이터
        /// <summary>
        /// 프리팹 풀별 누적 사용량을 저장하는 내부 통계입니다.
        /// </summary>
        private sealed class PoolStatistics
        {
            /// <summary>생성된 인스턴스 수입니다.</summary>
            public int createdCount;
            /// <summary>풀에서 꺼낸 총 횟수입니다.</summary>
            public int spawnCount;
            /// <summary>풀로 반환한 총 횟수입니다.</summary>
            public int releaseCount;
            /// <summary>풀 크기 제한 또는 정리로 파괴된 총 횟수입니다.</summary>
            public int destroyedCount;
        }
        #endregion // 데이터

        #region 초기화
        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            if (enableAutoClear)
                autoClearRoutine = StartCoroutine(AutoClearRoutine());
        }

        /// <inheritdoc/>
        public override void OnDestroy()
        {
            ClearAll();
            base.OnDestroy();
        }
        #endregion // 초기화

        #region 풀 기능
        /// <summary>
        /// 이름으로 프리팹을 찾을 수 있도록 등록합니다.
        /// </summary>
        /// <param name="poolName">등록할 풀 이름입니다.</param>
        /// <param name="prefab">등록할 프리팹입니다.</param>
        public void RegisterPrefab(string poolName, GameObject prefab)
        {
            if (prefab == null)
            {
                SWLog.LogWarning("[SWPool] 이름 등록 실패: 프리팹이 null입니다.");
                return;
            }

            string normalizedPoolName = NormalizeKey(poolName);
            if (string.IsNullOrEmpty(normalizedPoolName))
            {
                SWLog.LogWarning("[SWPool] 이름 등록 실패: 풀 이름이 비어 있습니다.");
                return;
            }

            if (nameToPrefabDictionary.TryGetValue(normalizedPoolName, out GameObject registeredPrefab)
                && registeredPrefab != prefab)
            {
                SWLog.LogWarning($"[SWPool] 같은 이름의 프리팹 등록을 교체합니다. Name: {normalizedPoolName}");
            }

            nameToPrefabDictionary[normalizedPoolName] = prefab;
        }

        /// <summary>
        /// 그룹으로 프리팹을 선택할 수 있도록 등록합니다.
        /// </summary>
        /// <param name="groupName">등록할 그룹 이름입니다.</param>
        /// <param name="prefab">등록할 프리팹입니다.</param>
        public void RegisterGroup(string groupName, GameObject prefab)
        {
            if (prefab == null)
            {
                SWLog.LogWarning("[SWPool] 그룹 등록 실패: 프리팹이 null입니다.");
                return;
            }

            string normalizedGroupName = NormalizeKey(groupName);
            if (string.IsNullOrEmpty(normalizedGroupName))
                return;

            if (!groupToPrefabListDictionary.TryGetValue(normalizedGroupName, out List<GameObject> prefabList))
            {
                prefabList = new List<GameObject>();
                groupToPrefabListDictionary[normalizedGroupName] = prefabList;
            }

            if (!prefabList.Contains(prefab))
                prefabList.Add(prefab);
        }

        /// <summary>
        /// 이름으로 등록된 프리팹을 해제합니다.
        /// </summary>
        /// <param name="poolName">해제할 풀 이름입니다.</param>
        /// <param name="prefab">해제할 프리팹입니다.</param>
        public void UnregisterPrefab(string poolName, GameObject prefab)
        {
            string normalizedPoolName = NormalizeKey(poolName);
            if (string.IsNullOrEmpty(normalizedPoolName) || prefab == null)
                return;

            if (nameToPrefabDictionary.TryGetValue(normalizedPoolName, out GameObject registeredPrefab)
                && registeredPrefab == prefab)
            {
                nameToPrefabDictionary.Remove(normalizedPoolName);
            }
        }

        /// <summary>
        /// 그룹으로 등록된 프리팹을 해제합니다.
        /// </summary>
        /// <param name="groupName">해제할 그룹 이름입니다.</param>
        /// <param name="prefab">해제할 프리팹입니다.</param>
        public void UnregisterGroup(string groupName, GameObject prefab)
        {
            string normalizedGroupName = NormalizeKey(groupName);
            if (string.IsNullOrEmpty(normalizedGroupName) || prefab == null)
                return;

            if (!groupToPrefabListDictionary.TryGetValue(normalizedGroupName, out List<GameObject> prefabList))
                return;

            prefabList.Remove(prefab);
            if (prefabList.Count > 0) return;

            groupToPrefabListDictionary.Remove(normalizedGroupName);
            groupSequenceIndexDictionary.Remove(normalizedGroupName);
        }

        /// <inheritdoc/>
        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null)
            {
                SWLog.LogWarning("[SWPool] 프리웜 실패: 프리팹이 null입니다.");
                return;
            }

            if (count <= 0)
            {
                SWLog.LogWarning($"[SWPool] 프리웜 실패: 생성 수가 0 이하입니다. Count: {count}");
                return;
            }

            ObjectPool<GameObject> pool = GetOrCreatePool(prefab);
            GameObject[] temporaryInstances = new GameObject[count];

            for (int index = 0; index < count; index++)
                temporaryInstances[index] = pool.Get();

            for (int index = 0; index < count; index++)
                pool.Release(temporaryInstances[index]);

            MarkUsed(prefab);
        }

        /// <inheritdoc/>
        public void Prewarm(string poolName, int count)
        {
            if (!TryGetPrefab(poolName, out GameObject prefab))
                return;

            Prewarm(prefab, count);
        }

        /// <inheritdoc/>
        public GameObject Spawn(GameObject prefab, Vector3 position = default,
            Quaternion rotation = default, Transform parent = null)
        {
            if (prefab == null)
            {
                SWLog.LogWarning("[SWPool] Spawn 실패: 프리팹이 null입니다.");
                return null;
            }

            ObjectPool<GameObject> pool = GetOrCreatePool(prefab);
            GameObject instance = pool.Get();
            GetOrCreateStatistics(prefab).spawnCount++;
            MarkUsed(prefab);

            Transform instanceTransform = instance.transform;
            instanceTransform.SetParent(parent, false);
            instanceTransform.SetPositionAndRotation(position, rotation);

            NotifyPoolables(instance);
            return instance;
        }

        /// <inheritdoc/>
        public GameObject Spawn(string poolName, Vector3 position = default,
            Quaternion rotation = default, Transform parent = null)
        {
            return TryGetPrefab(poolName, out GameObject prefab)
                ? Spawn(prefab, position, rotation, parent)
                : null;
        }

        /// <inheritdoc/>
        public T Spawn<T>(GameObject prefab, Vector3 position = default,
            Quaternion rotation = default, Transform parent = null) where T : Component
        {
            GameObject instance = Spawn(prefab, position, rotation, parent);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        /// <inheritdoc/>
        public T Spawn<T>(string poolName, Vector3 position = default,
            Quaternion rotation = default, Transform parent = null) where T : Component
        {
            GameObject instance = Spawn(poolName, position, rotation, parent);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        /// <inheritdoc/>
        public GameObject SpawnFromGroup(string groupName, SWPoolGroupSelectionMode selectionMode = SWPoolGroupSelectionMode.Random,
            Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            return TryGetPrefabFromGroup(groupName, selectionMode, out GameObject prefab)
                ? Spawn(prefab, position, rotation, parent)
                : null;
        }

        /// <inheritdoc/>
        public GameObject SpawnFromGroup(string groupName, string poolName,
            Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            return TryGetPrefabFromGroup(groupName, poolName, out GameObject prefab)
                ? Spawn(prefab, position, rotation, parent)
                : null;
        }

        /// <inheritdoc/>
        public T SpawnFromGroup<T>(string groupName, SWPoolGroupSelectionMode selectionMode = SWPoolGroupSelectionMode.Random,
            Vector3 position = default, Quaternion rotation = default, Transform parent = null) where T : Component
        {
            GameObject instance = SpawnFromGroup(groupName, selectionMode, position, rotation, parent);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        /// <inheritdoc/>
        public T SpawnFromGroup<T>(string groupName, string poolName,
            Vector3 position = default, Quaternion rotation = default, Transform parent = null) where T : Component
        {
            GameObject instance = SpawnFromGroup(groupName, poolName, position, rotation, parent);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        /// <inheritdoc/>
        public void Release(GameObject instance)
        {
            if (instance == null)
            {
                SWLog.LogWarning("[SWPool] Release 실패: 반환할 인스턴스가 null입니다.");
                return;
            }

            CancelDelayedRelease(instance);

            if (!instanceToPrefabDictionary.TryGetValue(instance, out GameObject prefab))
            {
                SWLog.LogWarning($"[SWPool] 등록되지 않은 인스턴스라 파괴합니다: {instance.name}");
                Destroy(instance);
                return;
            }

            if (poolDictionary.TryGetValue(prefab, out ObjectPool<GameObject> pool))
            {
                pool.Release(instance);
                GetOrCreateStatistics(prefab).releaseCount++;
                MarkUsed(prefab);
                return;
            }

            SWLog.LogWarning($"[SWPool] 연결된 풀이 없어 인스턴스를 파괴합니다: {instance.name}");
            instanceToPrefabDictionary.Remove(instance);
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

            if (!gameObject.activeInHierarchy)
            {
                Release(instance);
                return;
            }

            CancelDelayedRelease(instance);

            Coroutine releaseCoroutine = StartCoroutine(DelayedReleaseRoutine(instance, delay));
            delayedReleaseDictionary[instance] = releaseCoroutine;
        }

        /// <inheritdoc/>
        public void Clear(GameObject prefab)
        {
            if (prefab == null)
            {
                SWLog.LogWarning("[SWPool] Clear 실패: 프리팹이 null입니다.");
                return;
            }

            if (!poolDictionary.TryGetValue(prefab, out ObjectPool<GameObject> pool)) return;

            CancelDelayedReleases(prefab);

            pool.Clear();
            poolDictionary.Remove(prefab);
            RemovePrefabInstances(prefab);
            poolStatisticsDictionary.Remove(prefab);
            lastUseTimeDictionary.Remove(prefab);
        }

        /// <inheritdoc/>
        public void ClearAll()
        {
            StopAllCoroutines();
            autoClearRoutine = null;
            delayedReleaseDictionary.Clear();

            foreach (ObjectPool<GameObject> pool in poolDictionary.Values)
                pool.Clear();

            poolDictionary.Clear();
            instanceToPrefabDictionary.Clear();
            poolStatisticsDictionary.Clear();
            lastUseTimeDictionary.Clear();
            waitCacheDictionary.Clear();
        }

        /// <inheritdoc/>
        public int CountInPool(GameObject prefab)
        {
            return poolDictionary.TryGetValue(prefab, out ObjectPool<GameObject> pool) ? pool.CountInactive : 0;
        }

        /// <summary>
        /// 이름으로 특정 프리팹의 현재 대기 중 오브젝트 수를 반환합니다.
        /// </summary>
        /// <param name="poolName">등록된 풀 이름입니다.</param>
        /// <returns>대기 중 오브젝트 수입니다.</returns>
        public int CountInPool(string poolName)
        {
            return TryGetPrefab(poolName, out GameObject prefab) ? CountInPool(prefab) : 0;
        }

        /// <summary>
        /// 활성 인스턴스가 없는 유휴 풀의 대기 인스턴스를 즉시 정리합니다.
        /// 풀 등록 정보(이름, 그룹)는 유지되므로 다음 Spawn에서 다시 생성됩니다.
        /// </summary>
        /// <param name="idleSeconds">이 시간(초) 이상 사용되지 않은 풀만 정리합니다. 0이면 유휴 시간과 무관하게 정리합니다.</param>
        /// <returns>정리된 풀 개수입니다.</returns>
        public int TrimIdlePools(float idleSeconds = 0f)
        {
            autoClearBuffer.Clear();
            float now = Time.unscaledTime;

            foreach (KeyValuePair<GameObject, ObjectPool<GameObject>> pair in poolDictionary)
            {
                GameObject prefab = pair.Key;
                ObjectPool<GameObject> pool = pair.Value;

                if (prefab == null) continue;
                if (pool.CountActive > 0) continue;
                if (pool.CountInactive <= 0) continue;
                if (CountDelayedReleases(prefab) > 0) continue;

                if (idleSeconds > 0f
                    && lastUseTimeDictionary.TryGetValue(prefab, out float lastUseTime)
                    && now - lastUseTime < idleSeconds)
                {
                    continue;
                }

                autoClearBuffer.Add(prefab);
            }

            for (int index = 0; index < autoClearBuffer.Count; index++)
            {
                GameObject prefab = autoClearBuffer[index];
                if (!poolDictionary.TryGetValue(prefab, out ObjectPool<GameObject> pool)) continue;

                // Clear는 대기 인스턴스에 대해 OnDestroyPooled를 호출하므로
                // instanceToPrefabDictionary 정리와 destroyedCount 집계가 자동으로 이루어집니다.
                pool.Clear();
                lastUseTimeDictionary.Remove(prefab);
                SWLog.Log($"[SWPool] 유휴 풀 정리: {prefab.name}");
            }

            int trimmedCount = autoClearBuffer.Count;
            autoClearBuffer.Clear();
            return trimmedCount;
        }

        /// <summary>
        /// 현재 풀 상태를 모니터 창에서 표시할 수 있는 스냅샷으로 반환합니다.
        /// </summary>
        /// <returns>프리팹 풀별 현재 상태 목록입니다.</returns>
        public IReadOnlyList<SWPoolSnapshot> GetPoolSnapshots()
        {
            List<SWPoolSnapshot> snapshots = new();
            HashSet<GameObject> prefabs = new();

            foreach (GameObject prefab in poolDictionary.Keys)
            {
                if (prefab != null)
                    prefabs.Add(prefab);
            }

            foreach (GameObject prefab in nameToPrefabDictionary.Values)
            {
                if (prefab != null)
                    prefabs.Add(prefab);
            }

            foreach (List<GameObject> prefabList in groupToPrefabListDictionary.Values)
            {
                for (int index = 0; index < prefabList.Count; index++)
                {
                    if (prefabList[index] != null)
                        prefabs.Add(prefabList[index]);
                }
            }

            foreach (GameObject prefab in prefabs)
            {
                PoolStatistics statistics = GetOrCreateStatistics(prefab);
                bool hasPool = poolDictionary.TryGetValue(prefab, out ObjectPool<GameObject> pool);

                snapshots.Add(new SWPoolSnapshot(
                    prefab,
                    GetPoolNames(prefab),
                    GetGroupNames(prefab),
                    statistics.createdCount,
                    hasPool ? pool.CountActive : 0,
                    hasPool ? pool.CountInactive : 0,
                    statistics.spawnCount,
                    statistics.releaseCount,
                    statistics.destroyedCount,
                    CountDelayedReleases(prefab)
                ));
            }

            snapshots.Sort((left, right) =>
            {
                string leftName = left.Prefab != null ? left.Prefab.name : string.Empty;
                string rightName = right.Prefab != null ? right.Prefab.name : string.Empty;
                return string.Compare(leftName, rightName, System.StringComparison.Ordinal);
            });

            return snapshots;
        }
        #endregion // 풀 기능

        #region 내부
        /// <summary>
        /// 검색 키를 비교 가능한 형태로 정리합니다.
        /// </summary>
        /// <param name="key">정리할 키입니다.</param>
        /// <returns>앞뒤 공백이 제거된 키입니다.</returns>
        private string NormalizeKey(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
        }

        /// <summary>
        /// 프리팹 풀의 마지막 사용 시각을 갱신합니다.
        /// </summary>
        /// <param name="prefab">사용된 프리팹입니다.</param>
        private void MarkUsed(GameObject prefab)
        {
            lastUseTimeDictionary[prefab] = Time.unscaledTime;
        }

        /// <summary>
        /// 등록된 이름으로 프리팹을 찾습니다.
        /// </summary>
        /// <param name="poolName">등록된 풀 이름입니다.</param>
        /// <param name="prefab">찾은 프리팹입니다.</param>
        /// <returns>프리팹을 찾았으면 true입니다.</returns>
        private bool TryGetPrefab(string poolName, out GameObject prefab)
        {
            string normalizedPoolName = NormalizeKey(poolName);
            if (string.IsNullOrEmpty(normalizedPoolName))
            {
                prefab = null;
                SWLog.LogWarning("[SWPool] 이름 검색 실패: 풀 이름이 비어 있습니다.");
                return false;
            }

            if (nameToPrefabDictionary.TryGetValue(normalizedPoolName, out prefab))
                return true;

            SWLog.LogWarning($"[SWPool] 이름 검색 실패: 등록되지 않은 풀 이름입니다. Name: {normalizedPoolName}");
            return false;
        }

        /// <summary>
        /// 그룹에서 선택 방식에 맞는 프리팹을 찾습니다.
        /// </summary>
        /// <param name="groupName">등록된 그룹 이름입니다.</param>
        /// <param name="selectionMode">프리팹 선택 방식입니다.</param>
        /// <param name="prefab">선택된 프리팹입니다.</param>
        /// <returns>프리팹을 찾았으면 true입니다.</returns>
        private bool TryGetPrefabFromGroup(string groupName, SWPoolGroupSelectionMode selectionMode, out GameObject prefab)
        {
            string normalizedGroupName = NormalizeKey(groupName);
            if (string.IsNullOrEmpty(normalizedGroupName))
            {
                prefab = null;
                SWLog.LogWarning("[SWPool] 그룹 검색 실패: 그룹 이름이 비어 있습니다.");
                return false;
            }

            if (!groupToPrefabListDictionary.TryGetValue(normalizedGroupName, out List<GameObject> prefabList)
                || prefabList.Count <= 0)
            {
                prefab = null;
                SWLog.LogWarning($"[SWPool] 그룹 검색 실패: 등록되지 않은 그룹입니다. Group: {normalizedGroupName}");
                return false;
            }

            prefab = SelectPrefabFromGroup(normalizedGroupName, prefabList, selectionMode);
            return prefab != null;
        }

        /// <summary>
        /// 그룹 안에서 이름이 일치하는 프리팹을 찾습니다.
        /// </summary>
        /// <param name="groupName">등록된 그룹 이름입니다.</param>
        /// <param name="poolName">등록된 풀 이름입니다.</param>
        /// <param name="prefab">찾은 프리팹입니다.</param>
        /// <returns>프리팹을 찾았으면 true입니다.</returns>
        private bool TryGetPrefabFromGroup(string groupName, string poolName, out GameObject prefab)
        {
            prefab = null;
            if (!TryGetPrefab(poolName, out GameObject namedPrefab))
                return false;

            string normalizedGroupName = NormalizeKey(groupName);
            if (string.IsNullOrEmpty(normalizedGroupName))
            {
                SWLog.LogWarning("[SWPool] 그룹 이름 검색 실패: 그룹 이름이 비어 있습니다.");
                return false;
            }

            if (!groupToPrefabListDictionary.TryGetValue(normalizedGroupName, out List<GameObject> prefabList)
                || prefabList.Count <= 0)
            {
                SWLog.LogWarning($"[SWPool] 그룹 이름 검색 실패: 등록되지 않은 그룹입니다. Group: {normalizedGroupName}");
                return false;
            }

            if (!prefabList.Contains(namedPrefab))
            {
                SWLog.LogWarning($"[SWPool] 그룹 이름 검색 실패: 해당 그룹에 등록되지 않은 풀 이름입니다. Group: {normalizedGroupName}, Name: {poolName}");
                return false;
            }

            prefab = namedPrefab;
            return true;
        }

        /// <summary>
        /// 그룹 목록에서 선택 방식에 맞는 프리팹을 반환합니다.
        /// </summary>
        /// <param name="groupName">등록된 그룹 이름입니다.</param>
        /// <param name="prefabList">선택 대상 프리팹 목록입니다.</param>
        /// <param name="selectionMode">프리팹 선택 방식입니다.</param>
        /// <returns>선택된 프리팹입니다.</returns>
        private GameObject SelectPrefabFromGroup(string groupName, List<GameObject> prefabList, SWPoolGroupSelectionMode selectionMode)
        {
            switch (selectionMode)
            {
                case SWPoolGroupSelectionMode.Sequence:
                    int sequenceIndex = GetGroupSequenceIndex(groupName, prefabList.Count);
                    return prefabList[sequenceIndex];
                case SWPoolGroupSelectionMode.Random:
                default:
                    return prefabList[Random.Range(0, prefabList.Count)];
            }
        }

        /// <summary>
        /// 그룹의 순차 선택 인덱스를 반환하고 다음 인덱스로 갱신합니다.
        /// </summary>
        /// <param name="groupName">등록된 그룹 이름입니다.</param>
        /// <param name="count">그룹에 등록된 프리팹 수입니다.</param>
        /// <returns>이번에 사용할 인덱스입니다.</returns>
        private int GetGroupSequenceIndex(string groupName, int count)
        {
            groupSequenceIndexDictionary.TryGetValue(groupName, out int currentIndex);

            int useIndex = count > 0 ? currentIndex % count : 0;
            groupSequenceIndexDictionary[groupName] = count > 0 ? (useIndex + 1) % count : 0;
            return useIndex;
        }

        /// <summary>
        /// 인스턴스의 지연 반환 예약을 취소합니다.
        /// </summary>
        /// <param name="instance">취소할 인스턴스입니다.</param>
        private void CancelDelayedRelease(GameObject instance)
        {
            if (!delayedReleaseDictionary.TryGetValue(instance, out Coroutine releaseCoroutine))
                return;

            if (releaseCoroutine != null)
                StopCoroutine(releaseCoroutine);

            delayedReleaseDictionary.Remove(instance);
        }

        /// <summary>
        /// 지정 프리팹의 모든 지연 반환 예약을 취소합니다.
        /// </summary>
        /// <param name="prefab">취소할 프리팹입니다.</param>
        private void CancelDelayedReleases(GameObject prefab)
        {
            autoClearBuffer.Clear();

            foreach (GameObject instance in delayedReleaseDictionary.Keys)
            {
                if (instanceToPrefabDictionary.TryGetValue(instance, out GameObject instancePrefab)
                    && instancePrefab == prefab)
                {
                    autoClearBuffer.Add(instance);
                }
            }

            for (int index = 0; index < autoClearBuffer.Count; index++)
                CancelDelayedRelease(autoClearBuffer[index]);

            autoClearBuffer.Clear();
        }

        /// <summary>
        /// 지정 프리팹의 지연 반환 예약 수를 반환합니다.
        /// </summary>
        /// <param name="prefab">확인할 프리팹입니다.</param>
        /// <returns>지연 반환 예약 수입니다.</returns>
        private int CountDelayedReleases(GameObject prefab)
        {
            int count = 0;
            foreach (GameObject instance in delayedReleaseDictionary.Keys)
            {
                if (instanceToPrefabDictionary.TryGetValue(instance, out GameObject instancePrefab)
                    && instancePrefab == prefab)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 지정 프리팹의 모든 인스턴스 매핑을 제거합니다.
        /// </summary>
        /// <param name="prefab">제거할 프리팹입니다.</param>
        private void RemovePrefabInstances(GameObject prefab)
        {
            autoClearBuffer.Clear();

            foreach (KeyValuePair<GameObject, GameObject> pair in instanceToPrefabDictionary)
            {
                if (pair.Value == prefab)
                    autoClearBuffer.Add(pair.Key);
            }

            for (int index = 0; index < autoClearBuffer.Count; index++)
                instanceToPrefabDictionary.Remove(autoClearBuffer[index]);

            autoClearBuffer.Clear();
        }

        /// <summary>
        /// 지정한 시간만큼 대기한 뒤 오브젝트를 풀에 반환합니다.
        /// </summary>
        /// <param name="instance">반환할 오브젝트입니다.</param>
        /// <param name="delay">지연 시간(초)입니다.</param>
        private IEnumerator DelayedReleaseRoutine(GameObject instance, float delay)
        {
            yield return GetWait(delay);

            delayedReleaseDictionary.Remove(instance);
            Release(instance);
        }

        /// <summary>
        /// 캐시된 WaitForSeconds를 반환합니다.
        /// 키는 0.01초 단위로 반올림되고, 캐시가 상한을 넘으면 캐시 없이 새로 생성합니다.
        /// </summary>
        /// <param name="seconds">대기 시간(초)입니다.</param>
        /// <returns>WaitForSeconds 인스턴스입니다.</returns>
        private WaitForSeconds GetWait(float seconds)
        {
            float roundedSeconds = Mathf.Round(seconds / WaitCacheStep) * WaitCacheStep;

            if (waitCacheDictionary.TryGetValue(roundedSeconds, out WaitForSeconds waitForSeconds))
                return waitForSeconds;

            waitForSeconds = new WaitForSeconds(roundedSeconds);

            if (waitCacheDictionary.Count < MaxWaitCacheCount)
                waitCacheDictionary[roundedSeconds] = waitForSeconds;

            return waitForSeconds;
        }

        /// <summary>
        /// 일정 간격으로 사용하지 않는 풀을 정리합니다.
        /// </summary>
        private IEnumerator AutoClearRoutine()
        {
            float interval = Mathf.Max(1f, autoClearCheckInterval);
            WaitForSecondsRealtime wait = new(interval);

            while (true)
            {
                yield return wait;
                TrimIdlePools(Mathf.Max(0f, autoClearIdleSeconds));
            }
        }

        /// <summary>
        /// 프리팹에 대한 ObjectPool을 가져오거나 생성합니다.
        /// </summary>
        /// <param name="prefab">풀링할 프리팹입니다.</param>
        /// <returns>프리팹에 매핑된 ObjectPool입니다.</returns>
        private ObjectPool<GameObject> GetOrCreatePool(GameObject prefab)
        {
            if (poolDictionary.TryGetValue(prefab, out ObjectPool<GameObject> existingPool))
                return existingPool;

            GameObject capturedPrefab = prefab;

            ObjectPool<GameObject> pool = new(
                createFunc: () => CreatePooled(capturedPrefab),
                actionOnGet: OnGetFromPool,
                actionOnRelease: OnReleaseToPool,
                actionOnDestroy: OnDestroyPooled,
                collectionCheck: collectionCheck,
                defaultCapacity: defaultCapacity,
                maxSize: maxPoolSize
            );

            poolDictionary[prefab] = pool;
            GetOrCreateStatistics(prefab);
            SWLog.Log($"[SWPool] 풀 생성 완료: {prefab.name}");
            return pool;
        }

        /// <summary>
        /// 새 인스턴스를 생성하고 등록합니다.
        /// </summary>
        /// <param name="prefab">생성할 원본 프리팹입니다.</param>
        /// <returns>생성된 인스턴스입니다.</returns>
        private GameObject CreatePooled(GameObject prefab)
        {
            GameObject instance = Instantiate(prefab, transform);
            instance.name = prefab.name;
            instanceToPrefabDictionary[instance] = prefab;
            GetOrCreateStatistics(prefab).createdCount++;
            return instance;
        }

        /// <summary>
        /// 풀링 대상 컴포넌트에 풀 참조를 전달하고 Spawn 콜백을 호출합니다.
        /// </summary>
        /// <param name="instance">풀에서 꺼낸 오브젝트입니다.</param>
        private void NotifyPoolables(GameObject instance)
        {
            IPoolable[] poolables = instance.GetComponentsInChildren<IPoolable>(true);
            for (int index = 0; index < poolables.Length; index++)
            {
                poolables[index].SetPool(this);
                poolables[index].OnSpawnFromPool();
            }
        }

        /// <summary>
        /// ObjectPool의 Get 호출 때 자동으로 실행됩니다.
        /// </summary>
        /// <param name="instance">꺼내진 오브젝트입니다.</param>
        private void OnGetFromPool(GameObject instance)
        {
            instance.SetActive(true);
        }

        /// <summary>
        /// ObjectPool의 Release 호출 때 자동으로 실행됩니다.
        /// </summary>
        /// <param name="instance">반환되는 오브젝트입니다.</param>
        private void OnReleaseToPool(GameObject instance)
        {
            IPoolable[] poolables = instance.GetComponentsInChildren<IPoolable>(true);
            for (int index = 0; index < poolables.Length; index++)
                poolables[index].OnReturnToPool();

            instance.SetActive(false);
            instance.transform.SetParent(transform, false);
        }

        /// <summary>
        /// ObjectPool의 최대 크기 초과 또는 정리 때 자동으로 실행됩니다.
        /// 인스턴스 매핑을 함께 제거해 딕셔너리 누수를 방지합니다.
        /// </summary>
        /// <param name="instance">파괴할 오브젝트입니다.</param>
        private void OnDestroyPooled(GameObject instance)
        {
            if (instance == null) return;

            if (instanceToPrefabDictionary.TryGetValue(instance, out GameObject prefab))
                GetOrCreateStatistics(prefab).destroyedCount++;

            instanceToPrefabDictionary.Remove(instance);
            delayedReleaseDictionary.Remove(instance);
            Destroy(instance);
        }

        /// <summary>
        /// 지정 프리팹의 통계 정보를 반환하거나 새로 생성합니다.
        /// </summary>
        /// <param name="prefab">통계를 찾을 프리팹입니다.</param>
        /// <returns>프리팹 풀 통계입니다.</returns>
        private PoolStatistics GetOrCreateStatistics(GameObject prefab)
        {
            if (!poolStatisticsDictionary.TryGetValue(prefab, out PoolStatistics statistics))
            {
                statistics = new PoolStatistics();
                poolStatisticsDictionary[prefab] = statistics;
            }

            return statistics;
        }

        /// <summary>
        /// 지정 프리팹에 연결된 풀 이름 목록을 반환합니다.
        /// </summary>
        /// <param name="prefab">확인할 프리팹입니다.</param>
        /// <returns>연결된 풀 이름 목록입니다.</returns>
        private IReadOnlyList<string> GetPoolNames(GameObject prefab)
        {
            List<string> names = new();
            foreach (KeyValuePair<string, GameObject> pair in nameToPrefabDictionary)
            {
                if (pair.Value == prefab)
                    names.Add(pair.Key);
            }

            return names;
        }

        /// <summary>
        /// 지정 프리팹이 포함된 그룹 이름 목록을 반환합니다.
        /// </summary>
        /// <param name="prefab">확인할 프리팹입니다.</param>
        /// <returns>포함된 그룹 이름 목록입니다.</returns>
        private IReadOnlyList<string> GetGroupNames(GameObject prefab)
        {
            List<string> names = new();
            foreach (KeyValuePair<string, List<GameObject>> pair in groupToPrefabListDictionary)
            {
                if (pair.Value.Contains(prefab))
                    names.Add(pair.Key);
            }

            return names;
        }
        #endregion // 내부
    }
}
