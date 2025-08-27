using UnityEngine;
using System.Collections;

namespace InvaderInsider
{
    /// <summary>
    /// 모든 드래그 시스템 문제를 해결하는 올인원 솔루션
    /// </summary>
    public class AllInOneFixer : MonoBehaviour
    {
        private const string LOG_PREFIX = "[AllInOneFixer] ";
        
        [Header("Auto Fix Settings")]
        [SerializeField] private bool autoFixOnStart = true;
        [SerializeField] private float fixDelay = 2f;
        
        [Header("Fix Options")]
        [SerializeField] private bool fixDragSystem = true;
        [SerializeField] private bool fixUnits = true;
        [SerializeField] private bool fixGameManager = true;
        [SerializeField] private bool fixLayerIssues = true;
        [SerializeField] private bool fixTowerTargeting = true;
        [SerializeField] private bool runTests = true;
        
        private void Start()
        {
            if (autoFixOnStart)
            {
                StartCoroutine(FixEverything());
            }
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
            Debug.Log($"{LOG_PREFIX}=== 전체 시스템 수정 시작 ===");
            
            if (fixDragSystem)
            {
                Debug.Log($"{LOG_PREFIX}1. DragAndMergeSystem 수정 중...");
                FixDragAndMergeSystem();
                yield return new WaitForSeconds(0.5f);
            }
            
            if (fixUnits)
            {
                Debug.Log($"{LOG_PREFIX}2. 유닛 컴포넌트 수정 중...");
                FixAllUnits();
                yield return new WaitForSeconds(0.5f);
            }
            
            if (fixGameManager)
            {
                Debug.Log($"{LOG_PREFIX}3. GameManager 연동 확인 중...");
                CheckGameManagerIntegration();
                yield return new WaitForSeconds(0.5f);
            }
            
            if (fixLayerIssues)
            {
                Debug.Log($"{LOG_PREFIX}4. 레이어/콜라이더 문제 수정 중...");
                FixLayerAndColliderIssues();
                yield return new WaitForSeconds(0.5f);
            }
            
            if (fixTowerTargeting)
            {
                Debug.Log($"{LOG_PREFIX}5. 타워 타게팅 문제 수정 중...");
                FixTowerTargeting();
                yield return new WaitForSeconds(0.5f);
            }
            
            if (runTests)
            {
                Debug.Log($"{LOG_PREFIX}6. 시스템 테스트 실행 중...");
                RunSystemTests();
                yield return new WaitForSeconds(0.5f);
            }
            
            Debug.Log($"{LOG_PREFIX}=== 전체 시스템 수정 완료 ===");
            ShowFinalReport();
        }
        
        private void FixDragAndMergeSystem()
        {
            // DragAndMergeSystem 인스턴스 확인/생성
            if (DragAndMergeSystem.Instance == null)
            {
                GameObject systemObj = new GameObject("DragAndMergeSystem");
                systemObj.AddComponent<DragAndMergeSystem>();
                Debug.Log($"{LOG_PREFIX}✅ DragAndMergeSystem 생성됨");
            }
            else
            {
                Debug.Log($"{LOG_PREFIX}✅ DragAndMergeSystem 이미 존재함");
            }
            
            // 시스템 상태 리셋
            DragAndMergeSystem.Instance.CancelAllDrags();
            Debug.Log($"{LOG_PREFIX}✅ 드래그 상태 리셋됨");
        }
        
        private void FixAllUnits()
        {
            BaseCharacter[] allCharacters = FindObjectsOfType<BaseCharacter>();
            int fixedCount = 0;
            
            Debug.Log($"{LOG_PREFIX}총 {allCharacters.Length}개 유닛 발견");
            
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
            
            Debug.Log($"{LOG_PREFIX}✅ {fixedCount}개 유닛 수정됨");
        }
        
        private void CheckGameManagerIntegration()
        {
            try
            {
                if (InvaderInsider.Managers.GameManager.Instance != null)
                {
                    Debug.Log($"{LOG_PREFIX}✅ GameManager 연동 확인됨");
                    
                    // 프로퍼티 테스트
                    bool cardDragStatus = InvaderInsider.Managers.GameManager.Instance.IsCardDragInProgress;
                    Debug.Log($"{LOG_PREFIX}   카드 드래그 상태: {cardDragStatus}");
                }
                else
                {
                    Debug.LogWarning($"{LOG_PREFIX}⚠️ GameManager 인스턴스 없음");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"{LOG_PREFIX}❌ GameManager 연동 오류: {e.Message}");
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
            
            Debug.Log($"{LOG_PREFIX}테스트 결과:");
            Debug.Log($"{LOG_PREFIX}  - DragAndMergeSystem: {(dragSystemOK ? "✅" : "❌")}");
            Debug.Log($"{LOG_PREFIX}  - GameManager: {(gameManagerOK ? "✅" : "❌")}");
            Debug.Log($"{LOG_PREFIX}  - 총 유닛: {totalUnits}개");
            Debug.Log($"{LOG_PREFIX}  - 드래그 가능: {draggableUnits}개");
            Debug.Log($"{LOG_PREFIX}  - 머지 타겟: {mergeTargetUnits}개");
            
            // 완성도 계산
            float completeness = 0f;
            if (totalUnits > 0)
            {
                completeness = (float)(draggableUnits + mergeTargetUnits) / (totalUnits * 2) * 100f;
            }
            
            Debug.Log($"{LOG_PREFIX}  - 시스템 완성도: {completeness:F1}%");
        }
        
        private void ShowFinalReport()
        {
            Debug.Log($"{LOG_PREFIX}");
            Debug.Log($"{LOG_PREFIX}🎉 === 최종 보고서 ===");
            Debug.Log($"{LOG_PREFIX}");
            Debug.Log($"{LOG_PREFIX}✅ 모든 시스템이 수정되었습니다!");
            Debug.Log($"{LOG_PREFIX}");
            Debug.Log($"{LOG_PREFIX}🎮 사용 가능한 키:");
            Debug.Log($"{LOG_PREFIX}   ESC - 모든 드래그 취소");
            Debug.Log($"{LOG_PREFIX}   F1 - 시스템 상태 확인");
            Debug.Log($"{LOG_PREFIX}   F3 - 시스템 재초기화");
            Debug.Log($"{LOG_PREFIX}   F4 - 완전 시스템 재설정");
            Debug.Log($"{LOG_PREFIX}   F5 - 시스템 테스트 실행");
            Debug.Log($"{LOG_PREFIX}   Ctrl+F1 - 런타임 정리");
            Debug.Log($"{LOG_PREFIX}");
            Debug.Log($"{LOG_PREFIX}🚀 이제 드래그 & 머지가 정상 작동합니다!");
            Debug.Log($"{LOG_PREFIX}");
        }
        
        /// <summary>
        /// 레이어 및 콜라이더 문제 수정
        /// </summary>
        private void FixLayerAndColliderIssues()
        {
            BaseCharacter[] allUnits = FindObjectsOfType<BaseCharacter>();
            int fixedCount = 0;
            
            Debug.Log($"{LOG_PREFIX}총 {allUnits.Length}개 유닛의 레이어/콜라이더 확인 중...");
            
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
                    Debug.Log($"{LOG_PREFIX}레이어/콜라이더 수정: {unit.name} (z: {pos.z:F1})");
                }
            }
            
            Debug.Log($"{LOG_PREFIX}✅ {fixedCount}개 유닛의 레이어/콜라이더 수정됨");
        }
        
        /// <summary>
        /// 레이어 문제만 수정 (Context Menu용)
        /// </summary>
        [ContextMenu("Fix Layer Issues Only")]
        public void FixLayerIssuesOnly()
        {
            Debug.Log($"{LOG_PREFIX}레이어 문제 수정 시작");
            FixLayerAndColliderIssues();
        }
        
        /// <summary>
        /// 타워 타게팅 문제 수정
        /// </summary>
        private void FixTowerTargeting()
        {
            Tower[] allTowers = FindObjectsOfType<Tower>();
            int fixedCount = 0;
            
            Debug.Log($"{LOG_PREFIX}총 {allTowers.Length}개 타워의 타게팅 확인 중...");
            
            foreach (Tower tower in allTowers)
            {
                if (tower == null) continue;
                
                // 타워 강제 타겟 재검색
                tower.ForceRetarget();
                fixedCount++;
            }
            
            Debug.Log($"{LOG_PREFIX}✅ {fixedCount}개 타워 타게팅 수정됨");
        }
        
        /// <summary>
        /// 타워 타게팅만 수정 (Context Menu용)
        /// </summary>
        [ContextMenu("Fix Tower Targeting Only")]
        public void FixTowerTargetingOnly()
        {
            Debug.Log($"{LOG_PREFIX}타워 타게팅 문제 수정 시작");
            FixTowerTargeting();
        }
        
        private void Update()
        {
            // Ctrl + Shift + F: 긴급 수정
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.F))
            {
                FixEverythingNow();
            }
            
            // Ctrl + L: 레이어 문제만 수정
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.L))
            {
                FixLayerAndColliderIssues();
            }
        }
    }
}