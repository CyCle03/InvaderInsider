using UnityEngine;

namespace InvaderInsider.Core
{
    /// <summary>
    /// 스레드 안전한 Singleton 패턴의 기본 클래스
    /// 모든 매니저들이 상속받아 사용할 수 있습니다.
    /// </summary>
    public abstract class SingletonManager<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _isQuitting = false;
        private static bool _isInitialized = false;
        
        // 씬 전환 시 정적 변수 리셋을 위한 추가 플래그
        private static bool _isSceneChanging = false;

        public static T Instance
        {
            get
            {
                if (_isQuitting)
                {
                    Debug.LogWarning($"[{typeof(T).Name}] 애플리케이션 종료 중에 인스턴스에 접근하려고 했습니다.");
                    return null;
                }

                // 에디터에서 플레이 모드가 아닐 때는 인스턴스 생성하지 않음
                #if UNITY_EDITOR
                if (!Application.isPlaying) return null;
                #endif

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<T>();
                        if (_instance == null && !_isQuitting)
                        {
                            GameObject go = new GameObject($"{typeof(T).Name}");
                            _instance = go.AddComponent<T>();
                            DontDestroyOnLoad(go);
                        }
                    }
                    return _instance;
                }
            }
        }

        /// <summary>
        /// 인스턴스가 초기화되었는지 확인
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// 인스턴스 존재 여부 확인 (생성하지 않고)
        /// </summary>
        public static bool HasInstance => _instance != null && !_isQuitting;

        protected virtual void Awake()
        {
            // 기존 인스턴스가 파괴되었는지 확인
            if (_instance != null && _instance.gameObject == null)
            {
                _instance = null;
                _isInitialized = false;
            }
            
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
                _isInitialized = true;
                OnInitialize();
                
                DebugUtils.LogVerbose($"[{typeof(T).Name}]", 
                    $"새 싱글톤 인스턴스 생성 및 초기화 완료");
            }
            else if (_instance != this)
            {
                // 중복 로그 빈도 줄이기
                if (Time.time - _lastDuplicateWarningTime > 1f) // 1초 간격으로 제한
                {
                    DebugUtils.LogWarning($"[{typeof(T).Name}]", 
                        $"중복 인스턴스가 감지되어 제거합니다. 기존: {_instance.gameObject.name}, 새것: {gameObject.name}");
                    _lastDuplicateWarningTime = Time.time;
                }
                
                Destroy(gameObject);
            }
        }
        
        private static float _lastDuplicateWarningTime = 0f;

        protected virtual void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _isInitialized = false;
                OnCleanup();
                
                // 씬 전환이 아닌 실제 파괴인 경우에만 인스턴스 정리
                if (!_isSceneChanging && _isQuitting)
                {
                    _instance = null;
                }
                
                DebugUtils.LogVerbose($"[{typeof(T).Name}]", 
                    $"싱글톤 인스턴스 파괴됨 (씬 전환: {_isSceneChanging}, 종료: {_isQuitting})");
            }
        }

        /// <summary>
        /// 초기화 시 호출되는 가상 메서드
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// 정리 시 호출되는 가상 메서드
        /// </summary>
        protected virtual void OnCleanup() { }

        /// <summary>
        /// 강제로 인스턴스를 재설정 (테스트용)
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void ResetInstance()
        {
            lock (_lock)
            {
                if (_instance != null)
                {
                    DestroyImmediate(_instance.gameObject);
                }
                _instance = null;
                _isInitialized = false;
                _isQuitting = false;
                _isSceneChanging = false;
            }
        }
        
        /// <summary>
        /// 씬 전환 시 호출 - 기존 인스턴스 정리
        /// </summary>
        public static void PrepareForSceneChange()
        {
            lock (_lock)
            {
                _isSceneChanging = true;
                
                if (_instance != null)
                {
                    DebugUtils.LogInfo($"[{typeof(T).Name}]", 
                        "씬 전환을 위해 기존 인스턴스 정리 중...");
                    
                    // OnCleanup 호출
                    if (_instance is SingletonManager<T> manager)
                    {
                        manager.OnCleanup();
                    }
                    
                    // 인스턴스 파괴
                    if (Application.isPlaying)
                    {
                        Object.Destroy(_instance.gameObject);
                    }
                    else
                    {
                        Object.DestroyImmediate(_instance.gameObject);
                    }
                    
                    _instance = null;
                    _isInitialized = false;
                }
                
                _isSceneChanging = false;
            }
        }
        
        /// <summary>
        /// 모든 싱글톤 인스턴스 정리 (게임 재시작 시 사용)
        /// </summary>
        public static void CleanupForRestart()
        {
            PrepareForSceneChange();
        }
    }
} 