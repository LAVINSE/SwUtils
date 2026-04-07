using SWTools;
using SWUtils;
using UnityEngine;

namespace SWPool
{
    public class SwPoolRegistry : SWMonoBehaviour
    {
        #region 데이터
        [System.Serializable]
        public class PoolEntry
        {
            public GameObject prefab; // 풀링할 프리팹
            public int prewarmCount = 1; // 미리 생성할 개수
        }
        #endregion // 데이터

        #region 필드
        [SerializeField] private SwPool targetPool;
        [SerializeField] private PoolEntry[] poolEntries;
        #endregion // 필드

        #region 프로퍼티
        #endregion // 프로퍼티

        #region 초기화
        private void Awake()
        {
            if (targetPool == null)
            {
                SwUtilsLog.LogError("[SwPoolRegistry] SwPool을 찾을 수 없습니다");
                return;
            }

            if (poolEntries == null)
            {
                return;
            }
            
            for(int i = 0; i < poolEntries.Length; ++i)
            {
                PoolEntry poolEntry = poolEntries[i];
                if(poolEntry?.prefab != null && poolEntry.prewarmCount > 0)
                {
                    targetPool.Prewarm(poolEntry.prefab, poolEntry.prewarmCount);
                }
            }
        }
        #endregion // 초기화
    }
}