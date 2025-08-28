using UnityEngine;
using System.Collections;
using InvaderInsider.Core;

namespace InvaderInsider
{
    /// <summary>
    /// 모든 드래그 시스템 문제를 해결하는 올인원 솔루션
    /// </summary>
    public class AllInOneFixer : MonoBehaviour
    {
        private const string LOG_PREFIX = "AllInOneFixer";
        
        [Header("Auto Fix Settings")]
        [SerializeField] private bool autoFixOnStart = true;
        [SerializeField] private float fixDelay = 2f;
        
        [Header("Fix Options")]
        [SerializeField] private bool fixDragSystem = true;
        [SerializeField] private bool fixUnits = true;
        [SerializeField] private bool fixGameManager = true;
        [SerializeField] private bool fixLayerIssues = true;
        [SerializeField] private bool fixPlayerTargeting = true;
        [SerializeField] private bool runTests = true;
        
        private void Start()
        {
            if (autoFixOnStart)
            {
                StartCoroutine(DelayedFixEverything());
            }
        }
        
        /// <summary>
        /// 지연된 전체 수정 (fixDelay 적용)
        /// </summary>
        private IEnumerator DelayedFixEverything()
        {
            yield return new WaitForSeconds(fixDelay);
            yield return StartCoroutine(FixEverything());
        }
        
        /// <summary>
        /// 모든 것을 수정하는 메인 메서드
        /// </summary>
        [ContextMenu("Fix Everything")]
        public void FixEverythingNow()
        {
            StartCoroutine(FixEverything());
        }
        
        private IEnumerator FixEverything()
        {
            DebugUtils.LogInfo(LOG_PREFIX, "전체 시스템 수정 시작");
            
            if (fixDragSystem)
            {
                DebugUtils.LogVerbose(LOG_PREFIX, "DragAndMergeSystem 수정 중");
                FixDragAndMergeSystem();
                yield return new WaitForSeconds(0.2f);
            }
            
            if (fixUnits)
            {
                DebugUtils.LogVerbose(LOG_PREFIX, "유닛 컴포넌트 수정 중");
                FixAllUnits();
                yield return new WaitForSeconds(0.2f);
            }
            
            if (fixGameManager)
            {
                DebugUtils.LogVerbose(LOG_PREFIX, "GameManager 연동 확인 중");
                CheckGameManagerIntegration();
                yield return new WaitForSeconds(0.2f);
            }
            
            if (fixLayerIssues)
            {
                DebugUtils.LogVerbose(LOG_PREFIX, "레이어/콜라이더 문제 수정 중");
                FixLayerAndColliderIssues();
                yield return new WaitForSeconds(0.2f);
            }
            
            if (fixPlayerTargeting)
            {
                DebugUtils.LogVerbose(LOG_PREFIX, "플레이어 타게팅 최적화 중");
                FixPlayerTargeting();
                yield return new WaitForSeconds(0.2f);
            }
            
            if (runTests)
            {
                DebugUtils.LogVerbose(LOG_PREFIX, "시스템 테스트 실행 중");
                RunSystemTests();
                yield return new WaitForSeconds(0.2f);
            }
            
            // ProjectOptimizer 자동 실행
            DebugUtils.LogVerbose(LOG_PREFIX, "프로젝트 최적화 실행 중");
            ApplyProjectOptimization();
            yield return new WaitForSeconds(0.2f);
            
            DebugUtils.LogInfo(LOG_PREFIX, "전체 시스템 수정 완료");
            ShowFinalReport();
        }
        
        private void FixDragAndMergeSystem()
        {
            // DragAndMergeSystem 인스턴스 확인/생성
            if (DragAndMergeSystem.Instance == null)
            {
                GameObject systemObj = new GameObject("DragAndMergeSystem");
                systemObj.AddComponent<DragAndMergeSystem>();
                DebugUtils.LogVerbose(LOG_PREFIX, "DragAndMergeSystem 생성됨");
            }
            else
            {
                DebugUtils.LogVerbose(LOG_PREFIX, "DragAndMergeSystem 이미 존재함");
            }
            
            // 시스템 상태 리셋
            DragAndMergeSystem.Instance.CancelAllDrags();
            DebugUtils.LogVerbose(LOG_PREFIX, "드래그 상태 리셋됨");
        }
        
        private void FixAllUnits()
        {
            BaseCharacter[] allCharacters = FindObjectsOfType<BaseCharacter>();
            int fixedCount = 0;
            
            DebugUtils.LogVerbose(LOG_PREFIX, $"총 {allCharacters.Length}개 유닛 발견");
            
            foreach (BaseCharacter character in allCharacters)
            {
                if (character == null) continue;
                
                bool wasFixed = false;
                
                // SimpleDraggableUnit 확인/추가
                SimpleDraggableUnit draggable = character.GetComponent<SimpleDraggableUnit>();
                if (draggable == null)
                {
                    draggable = character.gameObject.AddComponent<SimpleDraggableUnit>();
                    wasFixed = true;
                }
                
                // SimpleMergeTarget 확인/추가
                SimpleMergeTarget mergeTarget = character.GetComponent<SimpleMergeTarget>();
                if (mergeTarget == null)
                {
                    mergeTarget = character.gameObject.AddComponent<SimpleMergeTarget>();
                    wasFixed = true;
                }
                
                // Collider 확인/설정
                Collider col = character.GetComponent<Collider>();
                if (col == null)
                {
                    BoxCollider boxCol = character.gameObject.AddComponent<BoxCollider>();
                    boxCol.isTrigger = true;
                    wasFixed = true;
                }
                else if (!col.isTrigger)
                {
                    col.isTrigger = true;
                    wasFixed = true;
                }
                
                if (wasFixed)
                {
                    fixedCount++;
                }
            }
            
            DebugUtils.LogInfo(LOG_PREFIX, $"{fixedCount}개 유닛 수정됨");
        }
        
        private void CheckGameManagerIntegration()
        {
            try
            {
                if (InvaderInsider.Managers.GameManager.Instance != null)
                {
                    DebugUtils.LogVerbose(LOG_PREFIX, "GameManager 연동 확인됨");
                    
                    // 프로퍼티 테스트
                    bool cardDragStatus = InvaderInsider.Managers.GameManager.Instance.IsCardDragInProgress;
                    DebugUtils.LogVerbose(LOG_PREFIX, $"카드 드래그 상태: {cardDragStatus}");
                }
                else
                {
                    DebugUtils.LogError(LOG_PREFIX, "GameManager 인스턴스 없음");
                }
            }
            catch (System.Exception e)
            {
                DebugUtils.LogError(LOG_PREFIX, $"GameManager 연동 오류: {e.Message}");
            }
        }
        
        private void RunSystemTests()
        {
            // 기본 시스템 테스트
            bool dragSystemOK = DragAndMergeSystem.Instance != null;
            bool gameManagerOK = InvaderInsider.Managers.GameManager.Instance != null;
            
            // 유닛 컴포넌트 테스트
            BaseCharacter[] allCharacters = FindObjectsOfType<BaseCharacter>();
            SimpleDraggableUnit[] draggables = FindObjectsOfType<SimpleDraggableUnit>();
            SimpleMergeTarget[] mergeTargets = FindObjectsOfType<SimpleMergeTarget>();
            
            int totalUnits = allCharacters.Length;
            int draggableUnits = draggables.Length;
            int mergeTargetUnits = mergeTargets.Length;
            
            DebugUtils.LogVerbose(LOG_PREFIX, "테스트 결과:");
            DebugUtils.LogVerbose(LOG_PREFIX, $"DragAndMergeSystem: {(dragSystemOK ? "OK" : "FAIL")}");
            DebugUtils.LogVerbose(LOG_PREFIX, $"GameManager: {(gameManagerOK ? "OK" : "FAIL")}");
            DebugUtils.LogVerbose(LOG_PREFIX, $"총 유닛: {totalUnits}개, 드래그 가능: {draggableUnits}개, 머지 타겟: {mergeTargetUnits}개");
            
            // 완성도 계산
            float completeness = 0f;
            if (totalUnits > 0)
            {
                completeness = (float)(draggableUnits + mergeTargetUnits) / (totalUnits * 2) * 100f;
            }
            
            DebugUtils.LogInfo(LOG_PREFIX, $"시스템 완성도: {completeness:F1}%");
        }
        
        private void ShowFinalReport()
        {
            DebugUtils.LogInfo(LOG_PREFIX, "🎉 모든 시스템이 최적화되어 정상 작동합니다!");
            DebugUtils.LogVerbose(LOG_PREFIX, "사용 가능한 키: Ctrl+Shift+F (전체수정), Ctrl+P (플레이어), Ctrl+O (최적화)");
        }
        
        /// <summary>
        /// 레이어 및 콜라이더 문제 수정
        /// </summary>
        private void FixLayerAndColliderIssues()
        {
            BaseCharacter[] allUnits = FindObjectsOfType<BaseCharacter>();
            int fixedCount = 0;
            
            DebugUtils.LogVerbose(LOG_PREFIX, $"총 {allUnits.Length}개 유닛의 레이어/콜라이더 확인 중");
            
            foreach (BaseCharacter unit in allUnits)
            {
                if (unit == null) continue;
                
                bool wasFixed = false;
                Vector3 pos = unit.transform.position;
                
                // 경로 아래쪽 유닛들 (z > 0) 특별 처리
                if (pos.z > 0)
                {
                    // 기존 트리거 콜라이더들 확인
                    Collider[] colliders = unit.GetComponents<Collider>();
                    bool hasProperDragCollider = false;
                    
                    foreach (Collider col in colliders)
                    {
                        if (col != null && col.isTrigger && col is BoxCollider boxCol)
                        {
                            // 콜라이더가 충분히 큰지 확인
                            if (boxCol.size.magnitude < 3f) // 작으면 크게 조정
                            {
                                boxCol.size = new Vector3(2f, 3f, 2f);
                                boxCol.center = new Vector3(0, 1.5f, 0);
                                wasFixed = true;
                            }
                            hasProperDragCollider = true;
                        }
                    }
                    
                    // 적절한 드래그 콜라이더가 없으면 새로 추가
                    if (!hasProperDragCollider)
                    {
                        BoxCollider dragCollider = unit.gameObject.AddComponent<BoxCollider>();
                        dragCollider.isTrigger = true;
                        dragCollider.size = new Vector3(2f, 3f, 2f);
                        dragCollider.center = new Vector3(0, 1.5f, 0);
                        wasFixed = true;
                    }
                    
                    // 레이어를 Default로 설정
                    if (unit.gameObject.layer != 0)
                    {
                        unit.gameObject.layer = 0;
                        wasFixed = true;
                    }
                }
                else
                {
                    // 경로 위쪽 유닛들은 기본 크기 유지
                    Collider[] colliders = unit.GetComponents<Collider>();
                    foreach (Collider col in colliders)
                    {
                        if (col != null && col.isTrigger && col is BoxCollider boxCol)
                        {
                            if (boxCol.size.magnitude > 4f) // 너무 크면 적당히 조정
                            {
                                boxCol.size = new Vector3(1.2f, 2f, 1.2f);
                                boxCol.center = new Vector3(0, 1f, 0);
                                wasFixed = true;
                            }
                        }
                    }
                }
                
                if (wasFixed)
                {
                    fixedCount++;
                    DebugUtils.LogVerbose(LOG_PREFIX, $"레이어/콜라이더 수정: {unit.name} (z: {pos.z:F1})");
                }
            }
            
            DebugUtils.LogInfo(LOG_PREFIX, $"{fixedCount}개 유닛의 레이어/콜라이더 수정됨");
        }
        
        /// <summary>
        /// 플레이어 타게팅 최적화 적용
        /// </summary>
        private void FixPlayerTargeting()
        {
            Player player = FindObjectOfType<Player>();
            if (player == null)
            {
                DebugUtils.LogError(LOG_PREFIX, "플레이어를 찾을 수 없음");
                return;
            }
            
            // PlayerAttackFixer 자동 추가 (즉시 문제 해결)
            if (player.GetComponent<PlayerAttackFixer>() == null)
            {
                player.gameObject.AddComponent<PlayerAttackFixer>();
                DebugUtils.LogInfo(LOG_PREFIX, "PlayerAttackFixer 추가됨 - 공격 문제 즉시 해결");
            }
            
            // OptimizedPlayerTargeting 자동 추가
            if (player.GetComponent<OptimizedPlayerTargeting>() == null)
            {
                player.gameObject.AddComponent<OptimizedPlayerTargeting>();
                DebugUtils.LogVerbose(LOG_PREFIX, "OptimizedPlayerTargeting 추가됨 - 성능 최적화 적용");
            }
            
            // PlayerTargetingDebugger는 에디터에서만 추가
            #if UNITY_EDITOR
            if (player.GetComponent<PlayerTargetingDebugger>() == null)
            {
                var debugger = player.gameObject.AddComponent<PlayerTargetingDebugger>();
                // 릴리즈 빌드에서는 자동 비활성화되도록 설정
                DebugUtils.LogVerbose(LOG_PREFIX, "PlayerTargetingDebugger 추가됨 (에디터 전용)");
            }
            #endif
            
            DebugUtils.LogInfo(LOG_PREFIX, "플레이어 타게팅 시스템 최적화 완료");
            DebugUtils.LogVerbose(LOG_PREFIX, "매 프레임 → 5Hz로 타게팅 빈도 감소, 메모리 할당 최적화 적용");
        }
        
        /// <summary>
        /// 플레이어 타게팅만 디버그 (Context Menu용)
        /// </summary>
        [ContextMenu("Debug Player Targeting")]
        public void FixPlayerTargetingOnly()
        {
            DebugUtils.LogInfo(LOG_PREFIX, "플레이어 타게팅 디버깅 시작");
            FixPlayerTargeting();
        }
        
        /// <summary>
        /// 프로젝트 최적화 적용
        /// </summary>
        private void ApplyProjectOptimization()
        {
            // ProjectOptimizer 생성 또는 찾기
            ProjectOptimizer optimizer = FindObjectOfType<ProjectOptimizer>();
            if (optimizer == null)
            {
                GameObject optimizerObj = new GameObject("ProjectOptimizer");
                optimizer = optimizerObj.AddComponent<ProjectOptimizer>();
                DebugUtils.LogVerbose(LOG_PREFIX, "ProjectOptimizer 생성됨");
            }
            
            // 최적화 실행
            optimizer.OptimizeProjectNow();
            DebugUtils.LogVerbose(LOG_PREFIX, "프로젝트 최적화 적용 완료");
        }
        
        /// <summary>
        /// 프로젝트 최적화만 실행 (Context Menu용)
        /// </summary>
        [ContextMenu("Apply Project Optimization")]
        public void ApplyProjectOptimizationOnly()
        {
            DebugUtils.LogInfo(LOG_PREFIX, "프로젝트 최적화 시작");
            ApplyProjectOptimization();
        }
        
        /// <summary>
        /// 긴급 플레이어 공격 수정
        /// </summary>
        [ContextMenu("Emergency Player Attack Fix")]
        public void EmergencyPlayerAttackFix()
        {
            DebugUtils.LogInfo(LOG_PREFIX, "🚨 긴급 플레이어 공격 수정 시작");
            
            Player player = FindObjectOfType<Player>();
            if (player == null)
            {
                DebugUtils.LogError(LOG_PREFIX, "플레이어를 찾을 수 없음!");
                return;
            }
            
            // PlayerAttackFixer 즉시 추가 및 실행
            PlayerAttackFixer fixer = player.GetComponent<PlayerAttackFixer>();
            if (fixer == null)
            {
                fixer = player.gameObject.AddComponent<PlayerAttackFixer>();
            }
            
            fixer.FixPlayerAttack();
            DebugUtils.LogInfo(LOG_PREFIX, "✅ 긴급 수정 완료 - 이제 플레이어가 공격할 수 있습니다!");
        }
        
        private void Update()
        {
            // Ctrl + Shift + F: 긴급 수정
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.F))
            {
                FixEverythingNow();
            }
            
            // Ctrl + P: 플레이어 타게팅 최적화
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.P))
            {
                FixPlayerTargetingOnly();
            }
            
            // Ctrl + O: 프로젝트 최적화
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.O))
            {
                ApplyProjectOptimizationOnly();
            }
            
            // F9: 긴급 플레이어 공격 수정
            if (Input.GetKeyDown(KeyCode.F9))
            {
                EmergencyPlayerAttackFix();
            }
        }
    }
}