using UnityEngine;
using UnityEngine.Events;

namespace TDOM.Unity
{
    public abstract class Singleton<T> : MonoBehaviour
        where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();

        public bool IsInitialized { get; private set; }

        public UnityEvent OnFinishedInitializing { get; private set; }

        [SerializeField]
        private bool dontDestroyOnLoad = true;

        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindAnyObjectByType<T>();
                        if (_instance == null)
                        {
                            GameObject singletonObject = new GameObject(typeof(T).Name);
                            _instance = singletonObject.AddComponent<T>();
                        }
                    }
                    return _instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                if (dontDestroyOnLoad)
                    DontDestroyOnLoad(gameObject);
                _instance = this as T;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }

            OnFinishedInitializing?.Invoke();
            IsInitialized = true;
        }
    }
}

