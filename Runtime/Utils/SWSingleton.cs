using UnityEngine;

namespace SWUtils
{
    /// <summary>
    /// 씬 전환 시에도 유지되는 싱글톤 베이스 클래스.
    /// DontDestroyOnLoad가 적용되어 전역 관리자 용도로 사용한다.
    /// </summary>
    /// <typeparam name="T">싱글톤으로 관리할 컴포넌트 타입</typeparam>
    public class SWSingleton<T> : MonoBehaviour where T : Component
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
                if (instance == null)
                {
                    instance = (T)FindAnyObjectByType(typeof(T));

                    if (instance == null)
                    {
                        SetupInstance();
                    }
                }

                return instance;
            }
        }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 씬에서 인스턴스를 찾거나 새로 생성한다.
        /// </summary>
        private static void SetupInstance()
        {
            instance = (T)FindAnyObjectByType(typeof(T));

            if (instance == null)
            {
                GameObject gameObject = new GameObject();
                gameObject.name = typeof(T).Name;
                instance = gameObject.AddComponent<T>();
                DontDestroyOnLoad(gameObject);
            }
        }

        /// <summary>
        /// Awake 시 중복 인스턴스를 제거한다.
        /// </summary>
        public virtual void Awake()
        {
            RemoveDuplicates();
        }

        /// <summary>
        /// 중복 인스턴스를 검사하여 제거한다.
        /// 이미 인스턴스가 존재하면 자신을 파괴한다.
        /// </summary>
        private void RemoveDuplicates()
        {
            if (instance == null)
            {
                instance = this as T;
                DontDestroyOnLoad(this.gameObject);
            }
            else if (instance == this)
            {
                DontDestroyOnLoad(this.gameObject);
            }
            else
            {
                Destroy(this.gameObject);
            }
        }
        #endregion // 함수
    }
}
