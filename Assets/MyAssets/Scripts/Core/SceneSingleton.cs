using UnityEngine;

namespace InvaderInsider.Core
{
    /// <summary>
    /// 특정 씬에만 존재하는 싱글톤 MonoBehaviour를 위한 베이스 클래스입니다.
    /// 씬이 언로드될 때 함께 파괴됩니다.
    /// </summary>
    /// <typeparam name="T">싱글톤으로 만들 컴포넌트 타입</typeparam>
    public abstract class SceneSingleton<T> : MonoBehaviour where T : Component
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _isQuitting = false;

        public static T Instance
        {
            get
            {
                if (_isQuitting)
                {
                    Debug.LogWarning($"[SceneSingleton] Instance of {typeof(T).Name} already destroyed on application quit. Returning null.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<T>();

                        if (_instance == null)
                        {
                            GameObject singletonObject = new GameObject();
                            _instance = singletonObject.AddComponent<T>();
                            singletonObject.name = typeof(T).Name + " (SceneSingleton)";
                            // DontDestroyOnLoad는 사용하지 않음 - 씬에 종속적
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
                _instance = this as T;
                // DontDestroyOnLoad는 사용하지 않음
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[SceneSingleton] Duplicate instance of {typeof(T).Name} found, destroying this one.");
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
