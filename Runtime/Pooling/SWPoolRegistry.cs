using SWTools;
using SWUtils;
using UnityEngine;

namespace SWPool
{
    /// <summary>
    /// 시작 시 지정한 프리팹들을 대상 풀에 미리 등록하고 생성하는 컴포넌트입니다.
    /// </summary>
    public class SWPoolRegistry : SWMonoBehaviour
    {
        #region 데이터
        /// <summary>
        /// 미리 생성할 프리팹과 개수를 저장하는 풀 등록 정보입니다.
        /// </summary>
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
