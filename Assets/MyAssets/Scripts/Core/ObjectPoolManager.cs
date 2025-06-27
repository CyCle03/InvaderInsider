using UnityEngine;
using System.Collections.Generic;
using System;

namespace InvaderInsider.Core
{
    /// <summary>
    /// 중앙화된 오브젝트 풀 매니저
    /// 여러 타입의 오브젝트 풀을 관리하고 성능을 모니터링합니다.
    /// </summary>
    public class ObjectPoolManager : SingletonManager<ObjectPoolManager>
    {
        [Header("Pool Configuration")]
        [SerializeField] private PoolConfig[] poolConfigs;
        [SerializeField] private Transform poolParent; // 풀 오브젝트들의 부모

        [Header("Performance Monitoring")]
        [SerializeField] private bool enablePerformanceLogging = false;
        [SerializeField] private float logInterval = 10f; // 로그 출력 간격 (초)

        // 타입별 풀 저장소
        private readonly Dictionary<Type, object> pools = new Dictionary<Type, object>();
        private readonly Dictionary<string, object> namedPools = new Dictionary<string, object>();

        private float nextLogTime = 0f;

        [Serializable]
        public class PoolConfig
        {
            [Header("Pool Setup")]
            public string poolName;
            public GameObject prefab;
            
            [Header("Pool Settings")]
            public int initialSize = 10;
            public int maxSize = 100;
            public bool expandPool = true;
            
            [Header("Auto Management")]
            public bool autoCleanup = true;
            public float cleanupInterval = 30f; // 정리 간격 (초)
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            // 풀 부모 오브젝트 설정
            if (poolParent == null)
            {
                GameObject poolParentObj = new GameObject("Object Pools");
                poolParentObj.transform.SetParent(transform);
                poolParent = poolParentObj.transform;
            }

            // 설정된 풀들 초기화
            InitializePools();

            int poolCount = poolConfigs?.Length ?? 0;
            DebugUtils.LogInitialization("ObjectPoolManager", true, $"{poolCount}개의 풀 초기화 완료");
        }

        private void InitializePools()
        {
            // poolConfigs가 null이거나 비어있는 경우 기본 풀 생성
            if (poolConfigs == null || poolConfigs.Length == 0)
            {
                DebugUtils.LogWarning(GameConstants.LOG_PREFIX_GAME, 
                    "풀 설정이 없습니다. 런타임에 동적 풀 생성을 사용합니다.");
                return;
            }

            foreach (var config in poolConfigs)
            {
                if (config == null)
                {
                    DebugUtils.LogWarning(GameConstants.LOG_PREFIX_GAME, 
                        "null 풀 설정을 건너뜁니다.");
                    continue;
                }

                if (config.prefab == null)
                {
                    DebugUtils.LogError(GameConstants.LOG_PREFIX_GAME, 
                        $"풀 설정 '{config.poolName}'의 prefab이 null입니다.");
                    continue;
                }

                CreatePool(config);
            }
        }

        private void CreatePool(PoolConfig config)
        {
            // 풀별 부모 오브젝트 생성
            GameObject poolContainer = new GameObject($"Pool_{config.poolName}");
            poolContainer.transform.SetParent(poolParent);

            // 프리팹의 컴포넌트 타입 확인
            var component = config.prefab.GetComponent<Component>();
            if (component == null)
            {
                DebugUtils.LogError(GameConstants.LOG_PREFIX_GAME, 
                    $"프리팹 '{config.prefab.name}'에 Component가 없습니다.");
                return;
            }

            // 제네릭 풀 생성을 위한 리플렉션 사용
            Type componentType = component.GetType();
            Type poolType = typeof(ObjectPool<>).MakeGenericType(componentType);
            
            object pool = Activator.CreateInstance(poolType, 
                component, config.initialSize, config.maxSize, config.expandPool, poolContainer.transform);

            // 풀 등록
            pools[componentType] = pool;
            namedPools[config.poolName] = pool;

            DebugUtils.LogFormat(GameConstants.LOG_PREFIX_GAME, 
                "풀 생성 완료: {0} (타입: {1}, 초기 크기: {2})", 
                config.poolName, componentType.Name, config.initialSize);
        }

        /// <summary>
        /// 동적 풀 생성 시도 (강화된 버전)
        /// </summary>
        private bool TryCreateDynamicPool<T>() where T : Component
        {
            Type targetType = typeof(T);
            
            // 1. 씬에서 활성 오브젝트 중 해당 타입 찾기
            var existingObject = FindObjectOfType<T>();
            if (existingObject != null)
            {
                DebugUtils.LogInfo(GameConstants.LOG_PREFIX_GAME, 
                    $"씬에서 {targetType.Name} 찾음: {existingObject.name}");
                return CreateDynamicPool(existingObject);
            }

            // 2. Resources 폴더에서 프리팹 찾기 (Projectile의 경우)
            if (targetType == typeof(Projectile))
            {
                var projectilePrefabs = Resources.LoadAll<GameObject>("Prefabs");
                foreach (var prefab in projectilePrefabs)
                {
                    var component = prefab.GetComponent<T>();
                    if (component != null)
                    {
                        DebugUtils.LogInfo(GameConstants.LOG_PREFIX_GAME, 
                            $"Resources에서 {targetType.Name} 프리팹 찾음: {prefab.name}");
                        return CreateDynamicPool(component);
                    }
                }
            }

            // 3. 모든 Tower에서 projectilePrefab 참조 확인
            if (targetType == typeof(Projectile))
            {
                var towers = FindObjectsOfType<Tower>();
                foreach (var tower in towers)
                {
                    // Tower의 projectilePrefab 필드에 접근
                    var projectilePrefab = GetProjectilePrefabFromTower(tower);
                    if (projectilePrefab != null)
                    {
                        var component = projectilePrefab.GetComponent<T>();
                        if (component != null)
                        {
                            DebugUtils.LogInfo(GameConstants.LOG_PREFIX_GAME, 
                                $"Tower에서 {targetType.Name} 프리팹 찾음: {projectilePrefab.name}");
                            return CreateDynamicPool(component);
                        }
                    }
                }
            }

            DebugUtils.LogWarning(GameConstants.LOG_PREFIX_GAME, 
                $"타입 '{targetType.Name}'의 동적 풀 생성 실패: 어디서도 해당 타입을 찾을 수 없습니다.");
            return false;
        }

        /// <summary>
        /// Tower에서 projectilePrefab 가져오기 (public 프로퍼티 사용)
        /// </summary>
        private GameObject GetProjectilePrefabFromTower(Tower tower)
        {
            if (tower == null) return null;

            try
            {
                // public 프로퍼티를 통해 접근
                return tower.ProjectilePrefab;
            }
            catch (System.Exception ex)
            {
                DebugUtils.LogWarning(GameConstants.LOG_PREFIX_GAME, 
                    $"Tower에서 ProjectilePrefab 접근 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 컴포넌트를 기반으로 동적 풀 생성
        /// </summary>
        private bool CreateDynamicPool<T>(T template) where T : Component
        {
            if (template == null) return false;

            Type componentType = typeof(T);
            
            // 이미 풀이 존재하는지 확인
            if (pools.ContainsKey(componentType)) return true;

            // 풀별 부모 오브젝트 생성
            GameObject poolContainer = new GameObject($"Pool_Dynamic_{componentType.Name}");
            poolContainer.transform.SetParent(poolParent);

            // 기본 설정으로 풀 생성
            int defaultSize = GameConstants.DEFAULT_POOL_SIZE;
            int maxSize = GameConstants.MAX_POOL_SIZE;
            
            Type poolType = typeof(ObjectPool<>).MakeGenericType(componentType);
            object pool = Activator.CreateInstance(poolType, 
                template, defaultSize, maxSize, true, poolContainer.transform);

            // 풀 등록
            pools[componentType] = pool;
            namedPools[$"Dynamic_{componentType.Name}"] = pool;

            DebugUtils.LogFormat(GameConstants.LOG_PREFIX_GAME, 
                "동적 풀 생성 완료: {0} (초기 크기: {1})", 
                componentType.Name, defaultSize);

            return true;
        }

        /// <summary>
        /// 타입으로 오브젝트 가져오기
        /// </summary>
        public T GetObject<T>() where T : Component
        {
            Type type = typeof(T);
            if (pools.TryGetValue(type, out object poolObj))
            {
                var pool = poolObj as ObjectPool<T>;
                return pool?.GetObject();
            }

            // 풀이 없는 경우 동적으로 생성 시도
            if (TryCreateDynamicPool<T>())
            {
                if (pools.TryGetValue(type, out poolObj))
                {
                    var pool = poolObj as ObjectPool<T>;
                    return pool?.GetObject();
                }
            }

            DebugUtils.LogWarning(GameConstants.LOG_PREFIX_GAME, 
                $"타입 '{type.Name}'에 대한 풀을 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// 이름으로 오브젝트 가져오기
        /// </summary>
        public T GetObject<T>(string poolName) where T : Component
        {
            if (namedPools.TryGetValue(poolName, out object poolObj))
            {
                var pool = poolObj as ObjectPool<T>;
                return pool?.GetObject();
            }

            DebugUtils.LogWarning(GameConstants.LOG_PREFIX_GAME, 
                $"이름 '{poolName}'에 대한 풀을 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// 오브젝트를 풀로 반환
        /// </summary>
        public void ReturnObject<T>(T obj) where T : Component
        {
            if (obj == null) return;

            Type type = typeof(T);
            if (pools.TryGetValue(type, out object poolObj))
            {
                var pool = poolObj as ObjectPool<T>;
                pool?.ReturnObject(obj);
            }
            else
            {
                DebugUtils.LogWarning(GameConstants.LOG_PREFIX_GAME, 
                    $"타입 '{type.Name}'에 대한 풀을 찾을 수 없어 오브젝트를 직접 제거합니다.");
                DestroyImmediate(obj.gameObject);
            }
        }

        /// <summary>
        /// PooledObject 컴포넌트를 통한 반환
        /// </summary>
        public void ReturnObject(PooledObject pooledObj)
        {
            if (pooledObj == null) return;

            // PooledObject가 있는 오브젝트의 다른 컴포넌트들 확인
            var components = pooledObj.GetComponents<Component>();
            
            foreach (var component in components)
            {
                if (component == pooledObj) continue; // PooledObject 자체는 제외
                
                Type componentType = component.GetType();
                if (pools.TryGetValue(componentType, out object poolObj))
                {
                    // 리플렉션으로 ReturnObject 메서드 호출
                    var method = poolObj.GetType().GetMethod("ReturnObject");
                    method?.Invoke(poolObj, new object[] { component });
                    return;
                }
            }

            DebugUtils.LogWarning(GameConstants.LOG_PREFIX_GAME, 
                "PooledObject를 적절한 풀로 반환할 수 없어 직접 제거합니다.");
            DestroyImmediate(pooledObj.gameObject);
        }

        /// <summary>
        /// 모든 풀의 활성 오브젝트 반환
        /// </summary>
        public void ReturnAllActiveObjects()
        {
            foreach (var poolObj in pools.Values)
            {
                var method = poolObj.GetType().GetMethod("ReturnAllActiveObjects");
                method?.Invoke(poolObj, null);
            }
        }

        /// <summary>
        /// 모든 풀 정리
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var poolObj in pools.Values)
            {
                var method = poolObj.GetType().GetMethod("Clear");
                method?.Invoke(poolObj, null);
            }
        }

        /// <summary>
        /// 풀 상태 로깅
        /// </summary>
        public void LogAllPoolStatus()
        {
            DebugUtils.Log(GameConstants.LOG_PREFIX_GAME, "=== 오브젝트 풀 상태 ===");
            
            foreach (var kvp in namedPools)
            {
                var method = kvp.Value.GetType().GetMethod("LogPoolStatus");
                method?.Invoke(kvp.Value, null);
            }
        }

        private void Update()
        {
            // 성능 로깅
            if (enablePerformanceLogging && Time.time >= nextLogTime)
            {
                LogAllPoolStatus();
                nextLogTime = Time.time + logInterval;
            }
        }

        protected override void OnCleanup()
        {
            base.OnCleanup();
            ClearAllPools();
        }

        #if UNITY_EDITOR
        [Header("Editor Tools")]
        [SerializeField] private bool showDebugInfo = true;

        private void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("Object Pool Manager", GUI.skin.box);
            
            foreach (var kvp in namedPools)
            {
                var poolObj = kvp.Value;
                var availableCount = (int)poolObj.GetType().GetProperty("AvailableCount")?.GetValue(poolObj, null);
                var activeCount = (int)poolObj.GetType().GetProperty("ActiveCount")?.GetValue(poolObj, null);
                
                GUILayout.Label($"{kvp.Key}: Active={activeCount}, Available={availableCount}");
            }
            
            GUILayout.EndArea();
        }
        #endif
    }
} 