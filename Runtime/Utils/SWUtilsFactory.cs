using UnityEngine;

namespace SWUtils
{
    /// <summary>
    /// 게임 오브젝트 생성을 편리하게 처리하는 팩토리.
    /// 일반 생성, 프리팹 복제, 제네릭 컴포넌트 반환을 지원한다.
    /// </summary>
    public class SWUtilsFactory : MonoBehaviour
    {
        #region 함수
        /// <summary>
        /// 게임 객체를 생성한다.
        /// </summary>
        /// <param name="objectName">객체 이름</param>
        /// <param name="parentObject">부모 객체</param>
        /// <param name="position">위치</param>
        /// <param name="scale">크기</param>
        /// <param name="rotation">회전</param>
        /// <param name="isStayWorldPosition">부모 변경 시 월드 좌표 유지 여부</param>
        /// <returns>생성된 게임 오브젝트</returns>
        public static GameObject CreateGameObj(string objectName,
            GameObject parentObject, Vector3 position, Vector3 scale, Vector3 rotation,
            bool isStayWorldPosition = false)
        {
            var gameObject = new GameObject(objectName);
            gameObject.transform.SetParent(parentObject?.transform, isStayWorldPosition);

            gameObject.transform.localScale = scale;
            gameObject.transform.localPosition = position;
            gameObject.transform.localEulerAngles = rotation;

            return gameObject;
        }

        /// <summary>
        /// 프리팹을 복제하여 사본 객체를 생성한다.
        /// </summary>
        /// <param name="objectName">객체 이름</param>
        /// <param name="prefabObject">복제할 프리팹</param>
        /// <param name="parentObject">부모 객체</param>
        /// <param name="position">위치</param>
        /// <param name="scale">크기</param>
        /// <param name="rotation">회전</param>
        /// <param name="isStayWorldPosition">부모 변경 시 월드 좌표 유지 여부</param>
        /// <returns>생성된 게임 오브젝트</returns>
        public static GameObject CreateCloneGameObj(string objectName,
            GameObject prefabObject, GameObject parentObject, Vector3 position,
            Vector3 scale, Vector3 rotation, bool isStayWorldPosition = false)
        {
            if (prefabObject == null)
            {
                SWUtilsLog.LogError($"[SWUtilsFactory] 프리팹 복제 실패: 원본 프리팹이 null입니다. ObjectName: {objectName}");
                return null;
            }

            var gameObject = GameObject.Instantiate(prefabObject, Vector3.zero, Quaternion.identity);
            gameObject.name = objectName;
            gameObject.transform.SetParent(parentObject?.transform, isStayWorldPosition);

            gameObject.transform.localScale = scale;
            gameObject.transform.localPosition = position;
            gameObject.transform.localEulerAngles = rotation;

            return gameObject;
        }

        /// <summary>
        /// 게임 객체를 생성한다. (제네릭, 컴포넌트 반환)
        /// </summary>
        /// <typeparam name="T">반환할 컴포넌트 타입</typeparam>
        /// <param name="objectName">객체 이름</param>
        /// <param name="parentObject">부모 객체</param>
        /// <param name="position">위치</param>
        /// <param name="scale">크기</param>
        /// <param name="rotation">회전</param>
        /// <param name="isStayWorldPosition">부모 변경 시 월드 좌표 유지 여부</param>
        /// <returns>생성된 게임 오브젝트에 부착된 컴포넌트</returns>
        public static T CreateGameObj<T>(string objectName,
            GameObject parentObject, Vector3 position, Vector3 scale, Vector3 rotation,
            bool isStayWorldPosition = false) where T : Component
        {
            var gameObject = SWUtilsFactory.CreateGameObj(objectName,
                parentObject, position, scale, rotation, isStayWorldPosition);

            return gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
        }

        /// <summary>
        /// 프리팹을 복제하여 사본 객체를 생성한다. (제네릭, 컴포넌트 반환)
        /// </summary>
        /// <typeparam name="T">반환할 컴포넌트 타입</typeparam>
        /// <param name="objectName">객체 이름</param>
        /// <param name="prefabObject">복제할 프리팹</param>
        /// <param name="parentObject">부모 객체</param>
        /// <param name="position">위치</param>
        /// <param name="scale">크기</param>
        /// <param name="rotation">회전</param>
        /// <param name="isStayWorldPosition">부모 변경 시 월드 좌표 유지 여부</param>
        /// <returns>생성된 게임 오브젝트에 부착된 컴포넌트</returns>
        public static T CreateCloneGameObj<T>(string objectName,
            GameObject prefabObject, GameObject parentObject, Vector3 position,
            Vector3 scale, Vector3 rotation,
            bool isStayWorldPosition = false) where T : Component
        {
            var gameObject = SWUtilsFactory.CreateCloneGameObj(objectName,
                prefabObject, parentObject, position, scale, rotation, isStayWorldPosition);

            if (gameObject == null) return null;

            return gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
        }
        #endregion // 함수
    }
}
