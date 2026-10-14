using UnityEngine;
using UnityEngine.Events;

namespace TDOM.Unity
{
    public abstract class Singleton<T> : MonoBehaviour
        where T : Singleton<T>
    {
        private static T _instance;
        private static readonly object _lock = new object();

        public bool IsInitialized { get; private set; }

        public UnityEvent OnFinishedInitializing { get; private set; }

        [SerializeField]
        private bool dontDestroyOnLoad = true;

        public bool DontDestroyOnLoadEnabled => dontDestroyOnLoad;

        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindAnyObjectByType<T>(FindObjectsInactive.Include);
                        if (_instance != null && _instance.dontDestroyOnLoad)
                        {
                            if (_instance.transform.parent != null)
                                _instance.transform.SetParent(null);
                            DontDestroyOnLoad(_instance.gameObject);
                        }
                    }
                    return _instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null || _instance == this)
            {
                _instance = this as T;
                if (dontDestroyOnLoad)
                {
                    if (transform.parent != null)
                        transform.SetParent(null);
                    DontDestroyOnLoad(gameObject);
                }
            }
            else
            {
                Debug.LogWarning($"[Singleton] Instancia duplicada de {typeof(T).Name} destruida en {gameObject.name}");
                Destroy(gameObject);
                return;
            }

            OnFinishedInitializing?.Invoke();
            IsInitialized = true;
        }
    }
}
