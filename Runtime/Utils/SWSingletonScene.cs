using UnityEngine;

namespace SWUtils
{
    public class SWSingletonScene<T> : MonoBehaviour where T : Component
    {
        private static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null) CreateInstance();
                return instance;
            }
        }

        private static void CreateInstance()
        {
            instance = FindAnyObjectByType<T>();
            if (instance == null)
            {
                var obj = new GameObject(typeof(T).Name);
                instance = obj.AddComponent<T>();
            }
        }

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

        public virtual void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}