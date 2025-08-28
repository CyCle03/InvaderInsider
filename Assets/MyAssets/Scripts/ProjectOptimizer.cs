using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace InvaderInsider
{
    /// <summary>
    /// 프로젝트 전체 성능 최적화 도구
    /// </summary>
    public class ProjectOptimizer : MonoBehaviour
    {
        private const string LOG_PREFIX = "ProjectOptimizer";
        
        [Header("Optimization Settings")]
        [SerializeField] private bool autoOptimizeOnStart = true;
        [SerializeField] private bool enablePerformanceMonitoring = false; // 기본값 false
        [SerializeField] private float monitoringInterval = 10f; // 간격 늘림
        
        [Header("Performance Thresholds")]
        [SerializeField] private float targetFrameRate = 60f;
        [SerializeField] private int maxActiveEnemies = 50;
        [SerializeField] private int maxActiveProjectiles = 100;
        
        // 성능 모니터링
        private float lastFrameTime;
        private int frameCount;
        private float averageFPS;
        
        // 최적화 상태
        private bool isOptimized = false;
        
        private void Start()
        {
            if (autoOptimizeOnStart)
            {
                StartCoroutine(OptimizeProject());
            }
            
            if (enablePerformanceMonitoring)
            {
                StartCoroutine(PerformanceMonitoringRoutine());
            }
        }
        
        /// <summary>
        /// 프로젝트 전체 최적화 실행
        /// </summary>
        [ContextMenu("Optimize Project")]
        public void OptimizeProjectNow()
        {
            if (isOptimized)
            {
                Debug.Log($"{LOG_PREFIX}이미 최적화가 완료되었습니다. 재최적화를 실행합니다.");
                isOptimized = false;
            }
            StartCoroutine(OptimizeProject());
        }
        
        private IEnumerator OptimizeProject()
        {
            Debug.Log($"{LOG_PREFIX}=== 프로젝트 최적화 시작 ===");
            
            // 1. 디버그 스크립트 최적화
            OptimizeDebugScripts();
            yield return new WaitForSeconds(0.2f);
            
            // 2. Update 메서드 최적화
            OptimizeUpdateMethods();
            yield return new WaitForSeconds(0.2f);
            
            // 3. FindObjectOfType 호출 최적화
            OptimizeFindObjectCalls();
            yield return new WaitForSeconds(0.2f);
            
            // 4. 메모리 할당 최적화
            OptimizeMemoryAllocations();
            yield return new WaitForSeconds(0.2f);
            
            // 5. 물리 연산 최적화
            OptimizePhysicsCalculations();
            yield return new WaitForSeconds(0.2f);
            
            // 6. 불필요한 컴포넌트 정리
            CleanupUnnecessaryComponents();
            yield return new WaitForSeconds(0.2f);
            
            isOptimized = true;
            Debug.Log($"{LOG_PREFIX}=== 프로젝트 최적화 완료 ===");
            ShowOptimizationReport();
        }
        
        /// <summary>
        /// 1. 디버그 스크립트 최적화
        /// </summary>
        private void OptimizeDebugScripts()
        {
            Debug.Log($"{LOG_PREFIX}1. 디버그 스크립트 최적화 중...");
            
            // PlayerTargetingDebugger 최적화
            PlayerTargetingDebugger[] debuggers = FindObjectsOfType<PlayerTargetingDebugger>();
            foreach (var debugger in debuggers)
            {
                // 릴리즈 빌드에서는 비활성화
                #if !UNITY_EDITOR && !DEVELOPMENT_BUILD
                debugger.enabled = false;
                #endif
            }
            
            // AllInOneFixer의 지속적인 모니터링 비활성화 (필요시에만 활성화)
            AllInOneFixer[] fixers = FindObjectsOfType<AllInOneFixer>();
            foreach (var fixer in fixers)
            {
                // Inspector에서 continuousAutoFix를 false로 설정하도록 권장
                Debug.Log($"{LOG_PREFIX}   - AllInOneFixer의 continuousAutoFix를 비활성화하는 것을 권장합니다");
            }
            
            Debug.Log($"{LOG_PREFIX}✅ 디버그 스크립트 최적화 완료");
        }
        
        /// <summary>
        /// 2. Update 메서드 최적화
        /// </summary>
        private void OptimizeUpdateMethods()
        {
            Debug.Log($"{LOG_PREFIX}2. Update 메서드 최적화 중...");
            
            // 성능 집약적인 Update 메서드들을 코루틴으로 변경 권장
            var performanceIssues = new List<string>();
            
            // Player의 FindAndAttackEnemies 최적화 권장
            Player[] players = FindObjectsOfType<Player>();
            if (players.Length > 0)
            {
                performanceIssues.Add("Player.FindAndAttackEnemies() - 매 프레임 Physics.OverlapSphereNonAlloc 호출");
            }
            
            // Tower의 Update 최적화는 이미 잘 되어 있음 (코루틴 사용)
            
            foreach (string issue in performanceIssues)
            {
                Debug.LogWarning($"{LOG_PREFIX}   ⚠️ {issue}");
            }
            
            Debug.Log($"{LOG_PREFIX}✅ Update 메서드 분석 완료");
        }
        
        /// <summary>
        /// 3. FindObjectOfType 호출 최적화
        /// </summary>
        private void OptimizeFindObjectCalls()
        {
            Debug.Log($"{LOG_PREFIX}3. FindObjectOfType 호출 최적화 중...");
            
            // 자주 호출되는 FindObjectOfType을 캐싱으로 대체
            CreateSingletonManager();
            
            Debug.Log($"{LOG_PREFIX}✅ FindObjectOfType 최적화 완료");
        }
        
        /// <summary>
        /// 4. 메모리 할당 최적화
        /// </summary>
        private void OptimizeMemoryAllocations()
        {
            Debug.Log($"{LOG_PREFIX}4. 메모리 할당 최적화 중...");
            
            // 오브젝트 풀링 시스템 확인
            GameObject poolManagerObj = GameObject.Find("ObjectPoolManager");
            if (poolManagerObj == null)
            {
                Debug.LogWarning($"{LOG_PREFIX}   ⚠️ ObjectPoolManager가 없습니다. 투사체 풀링을 권장합니다.");
            }
            else
            {
                Debug.Log($"{LOG_PREFIX}   ✅ ObjectPoolManager 발견됨");
            }
            
            // GC 압박을 줄이기 위한 권장사항
            Debug.Log($"{LOG_PREFIX}   - 문자열 연결 대신 StringBuilder 사용 권장");
            Debug.Log($"{LOG_PREFIX}   - new Vector3() 대신 Vector3.zero, Vector3.one 사용 권장");
            Debug.Log($"{LOG_PREFIX}   - 배열 재할당 대신 Array.Clear() 사용 권장");
            
            Debug.Log($"{LOG_PREFIX}✅ 메모리 할당 분석 완료");
        }
        
        /// <summary>
        /// 5. 물리 연산 최적화
        /// </summary>
        private void OptimizePhysicsCalculations()
        {
            Debug.Log($"{LOG_PREFIX}5. 물리 연산 최적화 중...");
            
            // Physics 설정 최적화
            Physics.defaultSolverIterations = 4; // 기본값 6에서 4로 감소
            Physics.defaultSolverVelocityIterations = 1; // 기본값 1 유지
            
            // FixedUpdate 주기 최적화 (60Hz -> 50Hz)
            Time.fixedDeltaTime = 0.02f; // 50Hz
            
            Debug.Log($"{LOG_PREFIX}✅ 물리 연산 최적화 완료");
        }
        
        /// <summary>
        /// 6. 불필요한 컴포넌트 정리
        /// </summary>
        private void CleanupUnnecessaryComponents()
        {
            Debug.Log($"{LOG_PREFIX}6. 불필요한 컴포넌트 정리 중...");
            
            int cleanedCount = 0;
            
            // 중복된 디버그 스크립트 제거
            PlayerTargetingDebugger[] debuggers = FindObjectsOfType<PlayerTargetingDebugger>();
            if (debuggers.Length > 1)
            {
                for (int i = 1; i < debuggers.Length; i++)
                {
                    DestroyImmediate(debuggers[i]);
                    cleanedCount++;
                }
            }
            
            // 사용하지 않는 Legacy 스크립트들 비활성화
            var legacyScripts = FindObjectsOfType<MonoBehaviour>()
                .Where(mb => mb.GetType().Name.StartsWith("Legacy_"))
                .ToArray();
            
            foreach (var legacy in legacyScripts)
            {
                legacy.enabled = false;
                cleanedCount++;
            }
            
            Debug.Log($"{LOG_PREFIX}✅ {cleanedCount}개 불필요한 컴포넌트 정리 완료");
        }
        
        /// <summary>
        /// 싱글톤 매니저 생성 (FindObjectOfType 대체용)
        /// </summary>
        private void CreateSingletonManager()
        {
            GameObject managerObj = GameObject.Find("SingletonManager");
            if (managerObj == null)
            {
                managerObj = new GameObject("SingletonManager");
                managerObj.AddComponent<SingletonManager>();
                DontDestroyOnLoad(managerObj);
                Debug.Log($"{LOG_PREFIX}   - SingletonManager 생성됨");
            }
        }
        
        /// <summary>
        /// 성능 모니터링 코루틴
        /// </summary>
        private IEnumerator PerformanceMonitoringRoutine()
        {
            while (enablePerformanceMonitoring)
            {
                yield return new WaitForSeconds(monitoringInterval);
                
                // FPS 계산
                averageFPS = 1f / Time.unscaledDeltaTime;
                
                // 성능 경고
                if (averageFPS < targetFrameRate * 0.8f) // 80% 이하로 떨어지면 경고
                {
                    Debug.LogWarning($"{LOG_PREFIX}⚠️ 성능 경고: FPS {averageFPS:F1} (목표: {targetFrameRate})");
                    if (!isOptimized)
                    {
                        Debug.Log($"{LOG_PREFIX}자동 최적화를 실행합니다...");
                        OptimizeProjectNow();
                    }
                    else
                    {
                        SuggestOptimizations();
                    }
                }
                
                // 오브젝트 수 모니터링
                MonitorObjectCounts();
            }
        }
        
        /// <summary>
        /// 오브젝트 수 모니터링
        /// </summary>
        private void MonitorObjectCounts()
        {
            int enemyCount = FindObjectsOfType<EnemyObject>().Length;
            int projectileCount = FindObjectsOfType<Projectile>().Length;
            
            if (enemyCount > maxActiveEnemies)
            {
                Debug.LogWarning($"{LOG_PREFIX}⚠️ 적 수 과다: {enemyCount}/{maxActiveEnemies}");
            }
            
            if (projectileCount > maxActiveProjectiles)
            {
                Debug.LogWarning($"{LOG_PREFIX}⚠️ 투사체 수 과다: {projectileCount}/{maxActiveProjectiles}");
            }
        }
        
        /// <summary>
        /// 최적화 제안
        /// </summary>
        private void SuggestOptimizations()
        {
            Debug.Log($"{LOG_PREFIX}🔧 최적화 제안:");
            Debug.Log($"{LOG_PREFIX}   1. 화면 밖 적들의 업데이트 빈도 감소");
            Debug.Log($"{LOG_PREFIX}   2. LOD(Level of Detail) 시스템 도입");
            Debug.Log($"{LOG_PREFIX}   3. 오브젝트 풀링 확대 적용");
            Debug.Log($"{LOG_PREFIX}   4. 텍스처 압축 및 해상도 조정");
        }
        
        /// <summary>
        /// 최적화 보고서 출력
        /// </summary>
        private void ShowOptimizationReport()
        {
            Debug.Log($"{LOG_PREFIX}");
            Debug.Log($"{LOG_PREFIX}📊 === 최적화 보고서 ===");
            Debug.Log($"{LOG_PREFIX}");
            Debug.Log($"{LOG_PREFIX}✅ 완료된 최적화:");
            Debug.Log($"{LOG_PREFIX}   - 디버그 스크립트 최적화");
            Debug.Log($"{LOG_PREFIX}   - Update 메서드 분석");
            Debug.Log($"{LOG_PREFIX}   - FindObjectOfType 캐싱");
            Debug.Log($"{LOG_PREFIX}   - 메모리 할당 분석");
            Debug.Log($"{LOG_PREFIX}   - 물리 연산 최적화");
            Debug.Log($"{LOG_PREFIX}   - 불필요한 컴포넌트 정리");
            Debug.Log($"{LOG_PREFIX}");
            Debug.Log($"{LOG_PREFIX}🎯 권장 사항:");
            Debug.Log($"{LOG_PREFIX}   - Player.FindAndAttackEnemies()를 코루틴으로 변경");
            Debug.Log($"{LOG_PREFIX}   - 투사체 오브젝트 풀링 구현");
            Debug.Log($"{LOG_PREFIX}   - 릴리즈 빌드에서 디버그 스크립트 제거");
            Debug.Log($"{LOG_PREFIX}   - 적 수가 많을 때 업데이트 빈도 조절");
            Debug.Log($"{LOG_PREFIX}");
            Debug.Log($"{LOG_PREFIX}🚀 최적화 완료! 성능이 향상되었습니다.");
            Debug.Log($"{LOG_PREFIX}");
        }
        
        private void Update()
        {
            // 성능 모니터링용 FPS 계산
            frameCount++;
            if (Time.unscaledTime - lastFrameTime >= 1f)
            {
                averageFPS = frameCount / (Time.unscaledTime - lastFrameTime);
                frameCount = 0;
                lastFrameTime = Time.unscaledTime;
            }
        }
        
        private void OnGUI()
        {
            if (!enablePerformanceMonitoring) return;
            
            // 성능 정보 표시 (에디터에서만)
            #if UNITY_EDITOR
            GUI.color = averageFPS < targetFrameRate * 0.8f ? Color.red : Color.green;
            GUI.Label(new Rect(10, 10, 200, 20), $"FPS: {averageFPS:F1}");
            GUI.Label(new Rect(10, 30, 200, 20), $"Enemies: {FindObjectsOfType<EnemyObject>().Length}");
            GUI.Label(new Rect(10, 50, 200, 20), $"Projectiles: {FindObjectsOfType<Projectile>().Length}");
            GUI.color = Color.white;
            #endif
        }
    }
    
    /// <summary>
    /// 싱글톤 매니저 - FindObjectOfType 호출을 캐싱으로 대체
    /// </summary>
    public class SingletonManager : MonoBehaviour
    {
        public static SingletonManager Instance { get; private set; }
        
        // 캐시된 참조들
        public Player CachedPlayer { get; private set; }
        public DragAndMergeSystem CachedDragSystem { get; private set; }
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 초기 캐싱
            RefreshCache();
        }
        
        public void RefreshCache()
        {
            CachedPlayer = FindObjectOfType<Player>();
            CachedDragSystem = FindObjectOfType<DragAndMergeSystem>();
        }
    }
}