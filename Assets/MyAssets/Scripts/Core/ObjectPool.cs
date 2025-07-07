using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System;
using InvaderInsider.Managers;

namespace InvaderInsider.Core
{
    /// <summary>
    /// 제네릭 오브젝트 풀링 시스템
    /// 투사체, 이펙트 등의 빈번한 생성/제거로 인한 GC 압박을 줄입니다.
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform parent;
        private readonly Queue<T> availableObjects = new Queue<T>();
        private readonly HashSet<T> activeObjects = new HashSet<T>();
        private readonly int maxPoolSize;
        private readonly bool expandPool;

        public int AvailableCount => availableObjects.Count;
        public int ActiveCount => activeObjects.Count;
        public int TotalCount => AvailableCount + ActiveCount;

        public ObjectPool(T prefab, int initialSize = 10, int maxSize = 100, bool expandPool = true, Transform parent = null)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.maxPoolSize = maxSize;
            this.expandPool = expandPool;

            // 초기 풀 생성
            PrewarmPool(initialSize);
        }

        /// <summary>
        /// 풀을 초기 크기로 미리 생성
        /// </summary>
        private void PrewarmPool(int size)
        {
            for (int i = 0; i < size; i++)
            {
                CreateNewObject();
            }
        }

        /// <summary>
        /// 새로운 오브젝트 생성 및 풀에 추가
        /// </summary>
        private T CreateNewObject()
        {
            T newObj = UnityEngine.Object.Instantiate(prefab, parent);
            newObj.gameObject.SetActive(false);
            availableObjects.Enqueue(newObj);
            return newObj;
        }

        /// <summary>
        /// 풀에서 오브젝트 가져오기
        /// </summary>
        public T GetObject()
        {
            T obj;

            // 사용 가능한 오브젝트가 있는 경우
            if (availableObjects.Count > 0)
            {
                obj = availableObjects.Dequeue();
            }
            // 풀이 비어있고 확장 가능한 경우
            else if (expandPool && TotalCount < maxPoolSize)
            {
                obj = CreateNewObject();
                availableObjects.Dequeue(); // 방금 생성한 객체를 다시 꺼냄
            }
            // 풀이 가득 찬 경우 null 반환
            else
            {
                LogManager.Warning(GameConstants.LOG_PREFIX_GAME, 
                    $"ObjectPool<{typeof(T).Name}>: 풀이 가득 참 (최대: {maxPoolSize})");
                return null;
            }

            activeObjects.Add(obj);
            obj.gameObject.SetActive(true);
            return obj;
        }

        /// <summary>
        /// 오브젝트를 풀로 반환
        /// </summary>
        public void ReturnObject(T obj)
        {
            if (obj == null) return;

            if (activeObjects.Remove(obj))
            {
                obj.gameObject.SetActive(false);
                
                // 부모 위치로 이동 (정리)
                if (parent != null)
                {
                    obj.transform.SetParent(parent);
                }
                
                availableObjects.Enqueue(obj);
            }
            else
            {
                LogManager.Warning(GameConstants.LOG_PREFIX_GAME, 
                    $"ObjectPool<{typeof(T).Name}>: 풀에 속하지 않는 오브젝트를 반환하려고 했습니다.");
            }
        }

        /// <summary>
        /// 모든 활성 오브젝트를 풀로 반환
        /// </summary>
        public void ReturnAllActiveObjects()
        {
            var activeList = new List<T>(activeObjects);
            foreach (var obj in activeList)
            {
                ReturnObject(obj);
            }
        }

        /// <summary>
        /// 풀 정리 - 사용하지 않는 오브젝트 제거
        /// </summary>
        public void Clear()
        {
            // 활성 오브젝트들 먼저 반환
            ReturnAllActiveObjects();

            // 사용 가능한 오브젝트들 제거
            while (availableObjects.Count > 0)
            {
                var obj = availableObjects.Dequeue();
                if (obj != null)
                {
                    UnityEngine.Object.DestroyImmediate(obj.gameObject);
                }
            }

            activeObjects.Clear();
        }

        /// <summary>
        /// 풀 상태 정보 출력 (디버깅용)
        /// </summary>
        public void LogPoolStatus()
        {
            LogManager.Info(GameConstants.LOG_PREFIX_GAME, 
                $"ObjectPool<{typeof(T).Name}> - 사용 가능: {AvailableCount}, 활성: {ActiveCount}, 총합: {TotalCount}/{maxPoolSize}");
        }
    }

    /// <summary>
    /// 오브젝트 풀링 시스템용 컴포넌트 (개선된 버전)
    /// 이 컴포넌트가 있는 오브젝트는 자동으로 풀에 반환됩니다
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Pool Settings")]
        [SerializeField] private float autoReturnTime = GameConstants.OBJECT_AUTO_RETURN_TIME;
        [SerializeField] private bool autoReturnOnDisable = true;
        [SerializeField] private float autoReturnDelay = 0f;
        
        [Header("Debug Info")]
        [SerializeField] private bool showDebugInfo = false;
        
        #endregion
        
        #region Private Fields
        
        private ObjectPoolManager poolManager;
        
        private System.Type componentType;
        private bool isInPool = true;
        private float spawnTime;
        
        #endregion
        
        #region Properties
        
        /// <summary>현재 풀에 있는 상태인지 확인</summary>
        public bool IsInPool => isInPool;
        
        /// <summary>스폰된 시간</summary>
        public float SpawnTime => spawnTime;
        
        /// <summary>활성화된 시간</summary>
        public float ActiveTime => isInPool ? 0f : Time.time - spawnTime;
        
        #endregion
        
        #region Unity Events
        
        private void Awake()
        {
            DetectComponentType();
        }
        
        private void OnDisable()
        {
            // 자동 반환이 활성화된 경우에만
            if (autoReturnOnDisable && !isInPool)
            {
                if (autoReturnDelay > 0f)
                {
                    Invoke(nameof(ReturnToPool), autoReturnDelay);
                }
                else
                {
                    ReturnToPool();
                }
            }
            
            // 기존 코루틴 정리 (UniTask는 별도의 Stop이 필요 없음)
        }
        
        #endregion
        
        #region Pool Management
        
        /// <summary>
        /// 풀 매니저 초기화
        /// </summary>
        public void Initialize(ObjectPoolManager manager)
        {
            poolManager = manager;
            
            if (showDebugInfo)
            {
                LogManager.Verbose(GameConstants.LOG_PREFIX_POOL, 
                    $"PooledObject 초기화: {gameObject.name}");
            }
        }

        /// <summary>
        /// 오브젝트가 풀에서 스폰될 때 호출
        /// </summary>
        public void OnObjectSpawned()
        {
            isInPool = false;
            spawnTime = Time.time;
            
            // 활성화 상태 확인
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }
            
            // 자동 반환 타이머 시작
            if (autoReturnTime > 0)
            {
                AutoReturnRoutine().Forget();
            }
            
            if (showDebugInfo)
            {
                LogManager.Verbose(GameConstants.LOG_PREFIX_POOL, 
                    $"오브젝트 스폰: {gameObject.name} (타입: {componentType?.Name})");
            }
        }

        /// <summary>
        /// 오브젝트를 풀로 반환
        /// </summary>
        public void ReturnToPool()
        {
            if (isInPool)
            {
                if (showDebugInfo)
                {
                    LogManager.Warning(GameConstants.LOG_PREFIX_POOL, 
                        $"이미 풀에 있는 오브젝트를 반환하려고 시도: {gameObject.name}");
                }
                return;
            }
            
            // 코루틴 및 Invoke 정리 (UniTask는 별도의 Stop이 필요 없음)
            CancelInvoke(nameof(ReturnToPool));
            
            isInPool = true;
            
            if (showDebugInfo)
            {
                LogManager.Verbose(GameConstants.LOG_PREFIX_POOL, 
                    $"오브젝트 반환: {gameObject.name} (활성 시간: {ActiveTime:F2}초)");
            }

            // ObjectPoolManager를 통한 반환
            if (poolManager != null)
            {
                poolManager.ReturnObject(this);
            }
            else if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.ReturnObject(this);
            }
            else
            {
                // 폴백: 직접 비활성화
                gameObject.SetActive(false);
                
                if (showDebugInfo)
                {
                    LogManager.Warning(GameConstants.LOG_PREFIX_POOL, 
                        "ObjectPoolManager가 없어 직접 비활성화합니다.");
                }
            }
        }
        
        /// <summary>
        /// 즉시 파괴 (풀링 시스템 우회)
        /// </summary>
        public void DestroyImmediately()
        {
            isInPool = true; // 반환 시도 방지
            CancelInvoke();
            
            // UniTask는 별도의 Stop이 필요 없음
            
            if (showDebugInfo)
            {
                LogManager.Verbose(GameConstants.LOG_PREFIX_POOL, 
                    $"오브젝트 즉시 파괴: {gameObject.name}");
            }
            
            Destroy(gameObject);
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// 자동 반환 코루틴
        /// </summary>
        private async UniTask AutoReturnRoutine()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(autoReturnTime));
            
            if (showDebugInfo)
            {
                LogManager.Verbose(GameConstants.LOG_PREFIX_POOL, 
                    $"자동 반환 타이머 만료: {gameObject.name}");
            }
            
            ReturnToPool();
        }
        
        /// <summary>
        /// 컴포넌트 타입 자동 감지
        /// </summary>
        private void DetectComponentType()
        {
            // Projectile 컴포넌트가 있는지 확인
            if (GetComponent<Projectile>() != null)
            {
                componentType = typeof(Projectile);
                return;
            }
            
            // EnemyObject 컴포넌트가 있는지 확인
            if (GetComponent<EnemyObject>() != null)
            {
                componentType = typeof(EnemyObject);
                return;
            }
            
            // 다른 주요 컴포넌트들 확인
            var components = GetComponents<Component>();
            foreach (var component in components)
            {
                var type = component.GetType();
                if (type != typeof(Transform) && 
                    type != typeof(PooledObject) && 
                    type != typeof(GameObject))
                {
                    componentType = type;
                    break;
                }
            }
            
            if (showDebugInfo && componentType != null)
            {
                LogManager.Verbose(GameConstants.LOG_PREFIX_POOL, 
                    $"감지된 컴포넌트 타입: {componentType.Name}");
            }
        }
        
        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        public void LogDebugInfo()
        {
            string status = isInPool ? "풀에 있음" : "활성 상태";
            float activeTime = isInPool ? 0f : Time.time - spawnTime;
            
            LogManager.Info(GameConstants.LOG_PREFIX_POOL, 
                $"[{gameObject.name}] 상태: {status}, 활성 시간: {activeTime:F2}초, 타입: {componentType?.Name}");
        }
        
        #endregion
        
        #region Editor Support
        
        #if UNITY_EDITOR
        [Header("Editor Tools")]
        [SerializeField] private bool testReturnToPool = false;
        
        private void OnValidate()
        {
            if (testReturnToPool)
            {
                testReturnToPool = false;
                if (Application.isPlaying)
                {
                    ReturnToPool();
                }
            }
        }
        #endif
        
        #endregion
    }
} 