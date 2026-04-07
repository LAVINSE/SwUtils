using UnityEngine;

namespace SWPool
{
    public interface IPoolable
    {
        /// <summary>
        /// 자신을 관리하는 풀의 참조를 설정한다. Spawn 시 자동으로 호출된다
        /// </summary>
        /// <param name="pool">자신을 관리하는 풀</param>
        public void SetPool(IPool pool);

        /// <summary>
        /// 풀에서 꺼내진 후 호출, 상태 초기화
        /// </summary>
        public void OnSpawnFromPool();

        /// <summary>
        /// 풀로 반납되기 전 호출, 리소스 정리
        /// </summary>
        public void OnReturnToPool();
    }
}