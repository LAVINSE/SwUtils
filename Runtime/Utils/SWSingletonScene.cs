using UnityEngine;

namespace SWUtils
{
    /// <summary>
    /// 씬 단위로 존재하는 싱글톤 베이스 클래스.
    /// 씬이 전환되면 파괴되며, 새 씬에서 다시 생성된다.
    /// </summary>
    /// <typeparam name="T">싱글톤으로 관리할 컴포넌트 타입</typeparam>
    public class SWSingletonScene<T> : MonoBehaviour where T : Component
    {
        #region 변수
        /// <summary>싱글톤 인스턴스.</summary>
        private static T instance;
        #endregion // 변수

        #region 프로퍼티
        /// <summary>
        /// 싱글톤 인스턴스에 접근한다. 없으면 자동으로 생성한다.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (instance == null) CreateInstance();
                return instance;
            }
        }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 씬에서 인스턴스를 찾거나 새로 생성한다.
        /// </summary>
        private static void CreateInstance()
        {
            instance = FindAnyObjectByType<T>();
            if (instance == null)
            {
                var gameObject = new GameObject(typeof(T).Name);
                instance = gameObject.AddComponent<T>();
            }
        }

        /// <summary>
        /// Awake 시 인스턴스를 등록하거나 중복을 제거한다.
        /// </summary>
        public virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 파괴 시 인스턴스 참조를 해제한다.
        /// </summary>
        public virtual void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
        #endregion // 함수
    }
}