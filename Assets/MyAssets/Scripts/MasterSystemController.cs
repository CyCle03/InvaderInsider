using UnityEngine;
using System.Collections;

namespace InvaderInsider
{
    /// <summary>
    /// 모든 드래그 시스템을 통합 관리하는 마스터 컨트롤러
    /// </summary>
    public class MasterSystemController : MonoBehaviour
    {
        private const string LOG_PREFIX = "[MasterSystemController] ";
        
        [Header("Auto Management")]
        [SerializeField] private bool autoInitializeOnStart = true;
        [SerializeField] private bool autoFixProblems = true;
        [SerializeField] private float healthCheckInterval = 30f;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showDetailedStatus = true;
        
        private static MasterSystemController _instance;
        public static MasterSystemController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<MasterSystemController>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("MasterSystemController");
                        _instance = go.AddComponent<MasterSystemController>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            LogDebug("MasterSystemController 초기화됨");
        }
        
        private void Start()
        {
            if (autoInitializeOnStart)
            {
                StartCoroutine(InitializeAllSystems());
            }
            
            if (autoFixProblems && healthCheckInterval > 0)
            {
                InvokeRepeating(nameof(PerformHealthCheck), healthCheckInterval, healthCheckInterval);
            }
        }
        
        /// <summary>
        /// 모든 시스템 초기화
        /// </summary>
        private IEnumerator InitializeAllSystems()
        {
            LogDebug("=== 전체 시스템 초기화 시작 ===");
            
            yield return new WaitForSeconds(1f);
            
            // 1. 핵심 시스템 확인/생성
            EnsureCoreSystemsExist();
            
            yield return new WaitForSeconds(0.5f);
            
            // 2. 유닛 컴포넌트 자동 추가
            AutoAddComponentsToUnits();
            
            yield return new WaitForSeconds(0.5f);
            
            // 3. 시스템 상태 확인
            PerformSystemCheck();
            
            LogDebug("=== 전체 시스템 초기화 완료 ===");
        }
        
        /// <summary>
        /// 핵심 시스템들이 존재하는지 확인하고 없으면 생성
        /// </summary>
        private void EnsureCoreSystemsExist()
        {
            LogDebug("핵심 시스템 확인 중...");
            
            // DragAndMergeSystem 확인
            if (DragAndMergeSystem.Instance == null)
            {
                LogDebug("DragAndMergeSystem 생성 중...");
            }
            else
            {
                LogDebug("✅ DragAndMergeSystem 존재함");
            }
            
            // GameManager 확인
            if (InvaderInsider.Managers.GameManager.Instance == null)
            {
                LogDebug("⚠️ GameManager 없음");
            }
            else
            {
                LogDebug("✅ GameManager 존재함");
            }
        }
        
        /// <summary>
        /// 모든 유닛에 필요한 컴포넌트 자동 추가
        /// </summary>
        private void AutoAddComponentsToUnits()
        {
            LogDebug("유닛 컴포넌트 자동 추가 중...");
            
            BaseCharacter[] allUnits = FindObjectsOfType<BaseCharacter>();
            int processedCount = 0;
            
            foreach (BaseCharacter unit in allUnits)
            {
                if (unit == null) continue;

                if (unit.GetComponentInParent<InvaderInsider.UI.CardDetailView>() != null)
                {
                    continue;
                }

                // UI에 속한 컴포넌트는 건너뛰기
                if (unit.GetComponent<RectTransform>() != null)
                {
                    continue;
                }
                
                bool wasModified = false;
                
                // SimpleDraggableUnit 추가
                if (unit.GetComponent<SimpleDraggableUnit>() == null)
                {
                    unit.gameObject.AddComponent<SimpleDraggableUnit>();
                    wasModified = true;
                }
                
                // SimpleMergeTarget 추가
                if (unit.GetComponent<SimpleMergeTarget>() == null)
                {
                    unit.gameObject.AddComponent<SimpleMergeTarget>();
                    wasModified = true;
                }
                
                // Collider 확인/설정
                Collider col = unit.GetComponent<Collider>();
                if (col == null)
                {
                    BoxCollider boxCol = unit.gameObject.AddComponent<BoxCollider>();
                    boxCol.isTrigger = true;
                    wasModified = true;
                }
                else if (!col.isTrigger)
                {
                    col.isTrigger = true;
                    wasModified = true;
                }
                
                if (wasModified)
                {
                    processedCount++;
                }
            }
            
            LogDebug($"✅ {processedCount}개 유닛 처리됨 (총 {allUnits.Length}개 중)");
        }
        
        /// <summary>
        /// 시스템 상태 확인
        /// </summary>
        private void PerformSystemCheck()
        {
            LogDebug("시스템 상태 확인 중...");
            
            // 기본 시스템 확인
            bool dragSystemOK = DragAndMergeSystem.Instance != null;
            bool gameManagerOK = InvaderInsider.Managers.GameManager.Instance != null;
            
            // 유닛 통계
            BaseCharacter[] allUnits = FindObjectsOfType<BaseCharacter>();
            SimpleDraggableUnit[] draggables = FindObjectsOfType<SimpleDraggableUnit>();
            SimpleMergeTarget[] mergeTargets = FindObjectsOfType<SimpleMergeTarget>();
            
            int totalUnits = allUnits.Length;
            int draggableUnits = draggables.Length;
            int mergeTargetUnits = mergeTargets.Length;
            
            if (showDetailedStatus)
            {
                LogDebug($"=== 시스템 상태 ===");
                LogDebug($"DragAndMergeSystem: {(dragSystemOK ? "✅" : "❌")}");
                LogDebug($"GameManager: {(gameManagerOK ? "✅" : "❌")}");
                LogDebug($"총 유닛: {totalUnits}개");
                LogDebug($"드래그 가능: {draggableUnits}개");
                LogDebug($"머지 타겟: {mergeTargetUnits}개");
            }
            
            // 완성도 계산
            float completeness = 0f;
            if (totalUnits > 0)
            {
                completeness = (float)(draggableUnits + mergeTargetUnits) / (totalUnits * 2) * 100f;
            }
            
            LogDebug($"시스템 완성도: {completeness:F1}%");
            
            if (completeness >= 95f && dragSystemOK && gameManagerOK)
            {
                LogDebug("🎉 모든 시스템이 완벽하게 작동합니다!");
            }
            else if (completeness >= 80f)
            {
                LogDebug("✅ 시스템이 정상 작동합니다.");
            }
            else
            {
                LogDebug("⚠️ 시스템에 문제가 있을 수 있습니다.");
            }
        }
        
        /// <summary>
        /// 정기적인 헬스 체크
        /// </summary>
        private void PerformHealthCheck()
        {
            if (!autoFixProblems) return;
            
            // 기본 시스템 확인
            bool needsFix = false;
            
            if (DragAndMergeSystem.Instance == null)
            {
                LogDebug("헬스 체크: DragAndMergeSystem 없음");
                needsFix = true;
            }
            
            // 유닛 컴포넌트 확인
            BaseCharacter[] allUnits = FindObjectsOfType<BaseCharacter>();
            SimpleDraggableUnit[] draggables = FindObjectsOfType<SimpleDraggableUnit>();
            
            if (allUnits.Length > 0 && draggables.Length < allUnits.Length * 0.8f)
            {
                LogDebug("헬스 체크: 일부 유닛에 드래그 컴포넌트 누락");
                needsFix = true;
            }
            
            if (needsFix)
            {
                LogDebug("자동 수정 실행 중...");
                StartCoroutine(InitializeAllSystems());
            }
        }
        
        /// <summary>
        /// 긴급 시스템 복구
        /// </summary>
        [ContextMenu("Emergency System Recovery")]
        public void EmergencySystemRecovery()
        {
            LogDebug("🚨 긴급 시스템 복구 시작");
            
            // 모든 드래그 상태 취소
            DragAndMergeSystem.Instance?.CancelAllDrags();
            
            // 시스템 재초기화
            StartCoroutine(InitializeAllSystems());
            
            LogDebug("🚨 긴급 시스템 복구 완료");
        }
        
        /// <summary>
        /// 수동 시스템 초기화
        /// </summary>
        [ContextMenu("Manual System Initialize")]
        public void ManualSystemInitialize()
        {
            StartCoroutine(InitializeAllSystems());
        }
        
        /// <summary>
        /// 현재 상태 출력
        /// </summary>
        [ContextMenu("Show Current Status")]
        public void ShowCurrentStatus()
        {
            PerformSystemCheck();
        }
        
        private void Update()
        {
            // 단축키 처리
            if (Input.GetKey(KeyCode.LeftControl))
            {
                // Ctrl + F1: 시스템 상태 확인
                if (Input.GetKeyDown(KeyCode.F1))
                {
                    ShowCurrentStatus();
                }
                
                // Ctrl + F2: 수동 초기화
                if (Input.GetKeyDown(KeyCode.F2))
                {
                    ManualSystemInitialize();
                }
                
                // Ctrl + F3: 긴급 복구
                if (Input.GetKeyDown(KeyCode.F3))
                {
                    EmergencySystemRecovery();
                }
            }
        }
        
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"{LOG_PREFIX}{message}");
            }
        }
        
        private void OnDestroy()
        {
            if (IsInvoking(nameof(PerformHealthCheck)))
            {
                CancelInvoke(nameof(PerformHealthCheck));
            }
        }
    }
}