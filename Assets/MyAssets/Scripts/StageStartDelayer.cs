using UnityEngine;
using System.Collections;
using InvaderInsider.Core;
using InvaderInsider.Managers;

namespace InvaderInsider
{
    /// <summary>
    /// 스테이지 시작을 지연시켜서 모든 시스템이 준비된 후에 적이 스폰되도록 하는 스크립트
    /// </summary>
    public class StageStartDelayer : MonoBehaviour
    {
        private const string LOG_PREFIX = "StageStartDelayer";
        
        [Header("Delay Settings")]
        [SerializeField] private float systemInitDelay = 3f; // 시스템 초기화 대기 시간
        [SerializeField] private float stageStartDelay = 2f; // 추가 스테이지 시작 지연
        [SerializeField] private bool enableDebugLogs = false;
        
        [Header("System Check")]
        [SerializeField] private bool waitForAllInOneFixer = true;
        [SerializeField] private bool waitForPlayerSystems = true;
        [SerializeField] private bool waitForProjectOptimizer = true;
        
        private StageManager stageManager;
        private bool isDelayActive = false;
        private bool systemsReady = false;
        
        private void Start()
        {
            // StageManager 찾기
            stageManager = StageManager.Instance;
            if (stageManager == null)
            {
                stageManager = FindObjectOfType<StageManager>();
            }
            
            if (stageManager != null)
            {
                StartCoroutine(DelayStageStart());
            }
            else
            {
                DebugUtils.LogError(LOG_PREFIX, "StageManager를 찾을 수 없습니다!");
            }
        }
        
        /// <summary>
        /// 스테이지 시작 지연 코루틴
        /// </summary>
        private IEnumerator DelayStageStart()
        {
            isDelayActive = true;
            DebugUtils.LogInfo(LOG_PREFIX, "스테이지 시작 지연 활성화");
            
            // StageManager의 적 스폰을 일시 중지
            PauseEnemySpawning();
            
            // 1단계: 기본 시스템 초기화 대기
            DebugUtils.LogVerbose(LOG_PREFIX, $"시스템 초기화 대기 중... ({systemInitDelay}초)");
            yield return new WaitForSeconds(systemInitDelay);
            
            // 2단계: 모든 시스템이 준비될 때까지 대기
            yield return StartCoroutine(WaitForSystemsReady());
            
            // 3단계: 추가 안전 지연
            DebugUtils.LogVerbose(LOG_PREFIX, $"추가 안전 지연... ({stageStartDelay}초)");
            yield return new WaitForSeconds(stageStartDelay);
            
            // 4단계: 스테이지 시작 허용
            systemsReady = true;
            isDelayActive = false;
            ResumeEnemySpawning();
            
            DebugUtils.LogInfo(LOG_PREFIX, "🚀 모든 시스템 준비 완료! 스테이지 시작 허용");
        }
        
        /// <summary>
        /// 모든 시스템이 준비될 때까지 대기
        /// </summary>
        private IEnumerator WaitForSystemsReady()
        {
            DebugUtils.LogVerbose(LOG_PREFIX, "시스템 준비 상태 확인 중...");
            
            int checkCount = 0;
            const int maxChecks = 20; // 최대 10초 대기 (0.5초 * 20)
            
            while (checkCount < maxChecks)
            {
                bool allSystemsReady = true;
                
                // AllInOneFixer 확인
                if (waitForAllInOneFixer)
                {
                    AllInOneFixer fixer = FindObjectOfType<AllInOneFixer>();
                    if (fixer == null)
                    {
                        allSystemsReady = false;
                        if (enableDebugLogs)
                        {
                            DebugUtils.LogVerbose(LOG_PREFIX, "AllInOneFixer 대기 중...");
                        }
                    }
                }
                
                // Player 시스템 확인
                if (waitForPlayerSystems && allSystemsReady)
                {
                    Player player = FindObjectOfType<Player>();
                    if (player == null)
                    {
                        allSystemsReady = false;
                        if (enableDebugLogs)
                        {
                            DebugUtils.LogVerbose(LOG_PREFIX, "Player 대기 중...");
                        }
                    }
                    else
                    {
                        // PlayerAttackFixer 확인
                        PlayerAttackFixer attackFixer = player.GetComponent<PlayerAttackFixer>();
                        OptimizedPlayerTargeting targeting = player.GetComponent<OptimizedPlayerTargeting>();
                        
                        if (attackFixer == null || targeting == null)
                        {
                            allSystemsReady = false;
                            if (enableDebugLogs)
                            {
                                DebugUtils.LogVerbose(LOG_PREFIX, "Player 시스템 컴포넌트 대기 중...");
                            }
                        }
                    }
                }
                
                // ProjectOptimizer 확인 (선택적)
                if (waitForProjectOptimizer && allSystemsReady)
                {
                    ProjectOptimizer optimizer = FindObjectOfType<ProjectOptimizer>();
                    if (optimizer == null)
                    {
                        // ProjectOptimizer는 필수가 아니므로 경고만 출력
                        if (enableDebugLogs && checkCount == 0)
                        {
                            DebugUtils.LogVerbose(LOG_PREFIX, "ProjectOptimizer 없음 (선택적)");
                        }
                    }
                }
                
                if (allSystemsReady)
                {
                    DebugUtils.LogInfo(LOG_PREFIX, "✅ 모든 시스템 준비 완료!");
                    break;
                }
                
                checkCount++;
                yield return new WaitForSeconds(0.5f);
            }
            
            if (checkCount >= maxChecks)
            {
                DebugUtils.LogError(LOG_PREFIX, "시스템 준비 대기 시간 초과! 강제로 스테이지 시작");
            }
        }
        
        /// <summary>
        /// 적 스폰 일시 중지
        /// </summary>
        private void PauseEnemySpawning()
        {
            if (stageManager == null) return;
            
            // StageManager의 적 스폰을 일시 중지하는 방법
            // 1. 스테이지 상태를 Pause로 변경
            try
            {
                var stageStateField = stageManager.GetType().GetField("currentStageState", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (stageStateField != null)
                {
                    // StageState.Pause 값으로 설정 (enum 값 추정)
                    stageStateField.SetValue(stageManager, 1); // Pause = 1로 추정
                    DebugUtils.LogVerbose(LOG_PREFIX, "StageManager 일시 중지됨");
                }
            }
            catch (System.Exception e)
            {
                DebugUtils.LogError(LOG_PREFIX, $"StageManager 일시 중지 실패: {e.Message}");
            }
            
            // 2. 대안: 기존 적들 비활성화
            EnemyObject[] existingEnemies = FindObjectsOfType<EnemyObject>();
            foreach (EnemyObject enemy in existingEnemies)
            {
                if (enemy != null)
                {
                    enemy.gameObject.SetActive(false);
                }
            }
            
            if (existingEnemies.Length > 0)
            {
                DebugUtils.LogVerbose(LOG_PREFIX, $"{existingEnemies.Length}개 기존 적 비활성화됨");
            }
        }
        
        /// <summary>
        /// 적 스폰 재개
        /// </summary>
        private void ResumeEnemySpawning()
        {
            if (stageManager == null) return;
            
            // StageManager의 적 스폰 재개
            try
            {
                var stageStateField = stageManager.GetType().GetField("currentStageState", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (stageStateField != null)
                {
                    // StageState.Run 값으로 설정 (enum 값 추정)
                    stageStateField.SetValue(stageManager, 2); // Run = 2로 추정
                    DebugUtils.LogInfo(LOG_PREFIX, "StageManager 재개됨");
                }
            }
            catch (System.Exception e)
            {
                DebugUtils.LogError(LOG_PREFIX, $"StageManager 재개 실패: {e.Message}");
            }
            
            // 스테이지 강제 시작 시도
            try
            {
                var startStageMethod = stageManager.GetType().GetMethod("StartStage", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (startStageMethod != null)
                {
                    startStageMethod.Invoke(stageManager, null);
                    DebugUtils.LogInfo(LOG_PREFIX, "스테이지 강제 시작됨");
                }
            }
            catch (System.Exception e)
            {
                DebugUtils.LogVerbose(LOG_PREFIX, $"스테이지 강제 시작 시도: {e.Message}");
            }
        }
        
        /// <summary>
        /// 시스템 준비 상태 확인
        /// </summary>
        public bool AreSystemsReady()
        {
            return systemsReady && !isDelayActive;
        }
        
        /// <summary>
        /// 강제로 스테이지 시작 허용
        /// </summary>
        [ContextMenu("Force Allow Stage Start")]
        public void ForceAllowStageStart()
        {
            DebugUtils.LogInfo(LOG_PREFIX, "강제로 스테이지 시작 허용");
            
            if (isDelayActive)
            {
                StopAllCoroutines();
                isDelayActive = false;
                systemsReady = true;
                ResumeEnemySpawning();
            }
        }
        
        /// <summary>
        /// 수동 시스템 체크 (F7 키)
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F7))
            {
                ManualSystemCheck();
            }
        }
        
        /// <summary>
        /// 수동 시스템 체크
        /// </summary>
        private void ManualSystemCheck()
        {
            DebugUtils.LogInfo(LOG_PREFIX, "=== 수동 시스템 체크 ===");
            DebugUtils.LogInfo(LOG_PREFIX, $"지연 활성화: {isDelayActive}");
            DebugUtils.LogInfo(LOG_PREFIX, $"시스템 준비: {systemsReady}");
            
            // 각 시스템 상태 확인
            AllInOneFixer fixer = FindObjectOfType<AllInOneFixer>();
            DebugUtils.LogInfo(LOG_PREFIX, $"AllInOneFixer: {(fixer != null ? "있음" : "없음")}");
            
            Player player = FindObjectOfType<Player>();
            DebugUtils.LogInfo(LOG_PREFIX, $"Player: {(player != null ? "있음" : "없음")}");
            
            if (player != null)
            {
                PlayerAttackFixer attackFixer = player.GetComponent<PlayerAttackFixer>();
                OptimizedPlayerTargeting targeting = player.GetComponent<OptimizedPlayerTargeting>();
                DebugUtils.LogInfo(LOG_PREFIX, $"PlayerAttackFixer: {(attackFixer != null ? "있음" : "없음")}");
                DebugUtils.LogInfo(LOG_PREFIX, $"OptimizedPlayerTargeting: {(targeting != null ? "있음" : "없음")}");
            }
            
            EnemyObject[] enemies = FindObjectsOfType<EnemyObject>();
            DebugUtils.LogInfo(LOG_PREFIX, $"현재 적 수: {enemies.Length}");
            
            if (!systemsReady)
            {
                DebugUtils.LogInfo(LOG_PREFIX, "시스템이 아직 준비되지 않았습니다. F7을 다시 눌러 강제 시작하시겠습니까?");
                ForceAllowStageStart();
            }
        }
    }
}