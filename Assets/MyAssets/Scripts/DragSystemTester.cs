using UnityEngine;
using System.Collections;
using InvaderInsider.Managers;

namespace InvaderInsider
{
    /// <summary>
    /// 새로운 드래그 시스템 테스터
    /// </summary>
    public class DragSystemTester : MonoBehaviour
    {
        private const string LOG_PREFIX = "[DragSystemTester] ";
        
        [Header("Test Settings")]
        [SerializeField] private bool runTestsOnStart = true;
        [SerializeField] private float testDelay = 2f;
        
        private void Start()
        {
            if (runTestsOnStart)
            {
                StartCoroutine(RunAllTests());
            }
        }
        
        private IEnumerator RunAllTests()
        {
            yield return new WaitForSeconds(testDelay);
            
            Debug.Log($"{LOG_PREFIX}=== 드래그 시스템 테스트 시작 ===");
            
            // 1. DragAndMergeSystem 테스트
            TestDragAndMergeSystem();
            
            yield return new WaitForSeconds(0.5f);
            
            // 2. 유닛 컴포넌트 테스트
            TestUnitComponents();
            
            yield return new WaitForSeconds(0.5f);
            
            // 3. GameManager 연동 테스트
            TestGameManagerIntegration();
            
            Debug.Log($"{LOG_PREFIX}=== 드래그 시스템 테스트 완료 ===");
        }
        
        private void TestDragAndMergeSystem()
        {
            Debug.Log($"{LOG_PREFIX}1. DragAndMergeSystem 테스트");
            
            if (DragAndMergeSystem.Instance != null)
            {
                Debug.Log($"{LOG_PREFIX}✅ DragAndMergeSystem 인스턴스 존재");
                Debug.Log($"{LOG_PREFIX}   - 현재 드래그 타입: {DragAndMergeSystem.Instance.CurrentDragType}");
                Debug.Log($"{LOG_PREFIX}   - 드래그 중: {DragAndMergeSystem.Instance.IsDragging}");
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX}❌ DragAndMergeSystem 인스턴스 없음");
            }
        }
        
        private void TestUnitComponents()
        {
            Debug.Log($"{LOG_PREFIX}2. 유닛 컴포넌트 테스트");
            
            BaseCharacter[] allCharacters = FindObjectsOfType<BaseCharacter>();
            int totalUnits = allCharacters.Length;
            int draggableUnits = 0;
            int mergeTargetUnits = 0;
            int properColliders = 0;
            
            foreach (BaseCharacter character in allCharacters)
            {
                if (character == null) continue;
                
                // SimpleDraggableUnit 확인
                if (character.GetComponent<SimpleDraggableUnit>() != null)
                    draggableUnits++;
                
                // SimpleMergeTarget 확인
                if (character.GetComponent<SimpleMergeTarget>() != null)
                    mergeTargetUnits++;
                
                // Collider 확인
                Collider col = character.GetComponent<Collider>();
                if (col != null && col.isTrigger)
                    properColliders++;
            }
            
            Debug.Log($"{LOG_PREFIX}   - 총 유닛 수: {totalUnits}");
            Debug.Log($"{LOG_PREFIX}   - 드래그 가능 유닛: {draggableUnits}");
            Debug.Log($"{LOG_PREFIX}   - 머지 타겟 유닛: {mergeTargetUnits}");
            Debug.Log($"{LOG_PREFIX}   - 올바른 콜라이더: {properColliders}");
            
            if (totalUnits > 0)
            {
                float draggablePercent = (float)draggableUnits / totalUnits * 100f;
                float mergePercent = (float)mergeTargetUnits / totalUnits * 100f;
                
                Debug.Log($"{LOG_PREFIX}   - 드래그 가능 비율: {draggablePercent:F1}%");
                Debug.Log($"{LOG_PREFIX}   - 머지 타겟 비율: {mergePercent:F1}%");
                
                if (draggablePercent >= 90f && mergePercent >= 90f)
                {
                    Debug.Log($"{LOG_PREFIX}✅ 유닛 컴포넌트 테스트 통과");
                }
                else
                {
                    Debug.LogWarning($"{LOG_PREFIX}⚠️ 일부 유닛에 컴포넌트가 누락됨");
                }
            }
        }
        
        private void TestGameManagerIntegration()
        {
            Debug.Log($"{LOG_PREFIX}3. GameManager 연동 테스트");
            
            if (GameManager.Instance != null)
            {
                Debug.Log($"{LOG_PREFIX}✅ GameManager 인스턴스 존재");
                
                // 프로퍼티 테스트
                bool cardDragStatus = GameManager.Instance.IsCardDragInProgress;
                Debug.Log($"{LOG_PREFIX}   - 카드 드래그 상태: {cardDragStatus}");
                
                // 드래그된 데이터 테스트
                var draggedCard = GameManager.Instance.DraggedCardData;
                var draggedUnit = GameManager.Instance.DraggedUnit;
                
                Debug.Log($"{LOG_PREFIX}   - 드래그된 카드: {(draggedCard != null ? draggedCard.cardName : "없음")}");
                Debug.Log($"{LOG_PREFIX}   - 드래그된 유닛: {(draggedUnit != null ? draggedUnit.name : "없음")}");
                
                Debug.Log($"{LOG_PREFIX}✅ GameManager 연동 테스트 통과");
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX}❌ GameManager 인스턴스 없음");
            }
        }
        
        [ContextMenu("Run Manual Test")]
        public void RunManualTest()
        {
            StartCoroutine(RunAllTests());
        }
        
        private void Update()
        {
            // F5 키로 수동 테스트 실행
            if (Input.GetKeyDown(KeyCode.F5))
            {
                RunManualTest();
            }
            
            // F6 키로 빠른 상태 확인
            if (Input.GetKeyDown(KeyCode.F6))
            {
                QuickStatusCheck();
            }
        }
        
        private void QuickStatusCheck()
        {
            Debug.Log($"{LOG_PREFIX}=== 빠른 상태 확인 ===");
            Debug.Log($"{LOG_PREFIX}DragAndMergeSystem: {(DragAndMergeSystem.Instance != null ? "활성" : "비활성")}");
            Debug.Log($"{LOG_PREFIX}GameManager: {(GameManager.Instance != null ? "활성" : "비활성")}");
            Debug.Log($"{LOG_PREFIX}총 유닛 수: {FindObjectsOfType<BaseCharacter>().Length}");
            Debug.Log($"{LOG_PREFIX}드래그 가능 유닛: {FindObjectsOfType<SimpleDraggableUnit>().Length}");
            Debug.Log($"{LOG_PREFIX}머지 타겟 유닛: {FindObjectsOfType<SimpleMergeTarget>().Length}");
        }
    }
}