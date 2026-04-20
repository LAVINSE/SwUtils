using UnityEngine;

namespace SWPool
{
    public interface IPool
    {
        /// <summary>
        /// 지정 프리팹을 미리 생성해둔다.
        /// </summary>
        /// <param name="prefab">원본 프리팹</param>
        /// <param name="count">미리 생성할 개수</param>
        public void Prewarm(GameObject prefab, int count);

        /// <summary>
        /// 풀에서 오브젝트를 꺼낸다.
        /// </summary>
        /// <param name="prefab">원본 프리팹</param>
        /// <param name="position">월드 위치</param>
        /// <param name="rotation">월드 회전</param>
        /// <param name="parent">부모 트랜스폼</param>
        /// <returns>활성화된 오브젝트</returns>
        public GameObject Spawn(GameObject prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null);

        /// <summary>
        /// 풀에서 오브젝트를 꺼낸다 (제네릭).
        /// </summary>
        /// <typeparam name="T">반환받을 컴포넌트 타입</typeparam>
        /// <param name="prefab">원본 프리팹</param>
        /// <param name="position">월드 위치</param>
        /// <param name="rotation">월드 회전</param>
        /// <param name="parent">부모 트랜스폼</param>
        /// <returns>활성화된 오브젝트의 T 컴포넌트</returns>
        public T Spawn<T>(GameObject prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null) where T : Component;

        /// <summary>
        /// 오브젝트를 풀로 반환한다.
        /// </summary>
        /// <param name="instance">반납할 오브젝트</param>
        public void Release(GameObject instance);

        /// <summary>
        /// 일정 시간 후 오브젝트를 풀로 반환한다.
        /// </summary>
        /// <param name="instance">반납할 오브젝트</param>
        /// <param name="delay">지연 시간(초)</param>
        public void Release(GameObject instance, float delay);

        /// <summary>
        /// 특정 프리팹의 풀을 비운다.
        /// </summary>
        /// <param name="prefab">비울 대상 프리팹</param>
        public void Clear(GameObject prefab);

        /// <summary>
        /// 모든 풀을 비운다.
        /// </summary>
        public void ClearAll();

        /// <summary>
        /// 특정 프리팹의 현재 대기 중 오브젝트 수를 반환한다.
        /// </summary>
        /// <param name="prefab">대상 프리팹</param>
        /// <returns>대기 중 오브젝트 수</returns>
        public int CountInPool(GameObject prefab);
    }
}