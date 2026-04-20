using SWTools;
using SWUtils;
using UnityEngine;

namespace SWPool
{
    public class SWPoolRegistry : SWMonoBehaviour
    {
        #region 데이터
        [System.Serializable]
        public class PoolEntry
        {
            /// <summary>풀링할 프리팹</summary>
            public GameObject prefab;
            /// <summary>미리 생성할 개수</summary>
            public int prewarmCount = 1;
        }
        #endregion // 데이터

        #region 필드
        [SerializeField] private SWPool targetPool;
        [SerializeField] private PoolEntry[] poolEntries;
        #endregion // 필드

        #region 프로퍼티
        #endregion // 프로퍼티

        #region 초기화
        private void Awake()
        {
            if (targetPool == null)
            {
                SWUtilsLog.LogError("[SwPoolRegistry] SWPool을 찾을 수 없습니다");
                return;
            }

            if (poolEntries == null)
            {
                return;
            }

            for (int index = 0; index < poolEntries.Length; ++index)
            {
                PoolEntry poolEntry = poolEntries[index];
                if (poolEntry?.prefab != null && poolEntry.prewarmCount > 0)
                {
                    targetPool.Prewarm(poolEntry.prefab, poolEntry.prewarmCount);
                }
            }
        }
        #endregion // 초기화
    }
}