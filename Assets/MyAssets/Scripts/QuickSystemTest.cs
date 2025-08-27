using UnityEngine;
using System.Collections;

namespace InvaderInsider
{
    /// <summary>
    /// 드래그 시스템 빠른 테스트 및 수정
    /// </summary>
    public class QuickSystemTest : MonoBehaviour
    {
        private const string LOG_PREFIX = "[QuickSystemTest] ";
        
        [Header("Test Settings")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool autoFix = true;
        
        private void Start()
        {
            if (runOnStart)
            {
                StartCoroutine(RunQuickTest());
            }
        }
        
        private IEnumerator RunQuickTest()
        {
            Debug.Log($"{LOG_PREFIX}=== 빠른 시스템 테스트 시작 ===");
            
            yield return new WaitForSeconds(1f);
            
            // 1. 기본 시스템 확인
            TestBasicSystems();
            
            yield return new WaitForSeconds(0.5f);
            
            // 2. 자동 수정 실행 (필요한 경우)
            if (autoFix)
            {
                RunAutoFix();
            }
            
            yield return new WaitForSeconds(1f);
            
            // 3. 최종 상태 확인
            TestFinalState();
            
            Debug.Log($"{LOG_PREFIX}=== 빠른 시스템 테스트 완료 ===");
        }
        
        private void TestBasicSystems()
        {
            Debug.Log($"{LOG_PREFIX}1. 기본 시스템 확인");
            
            // DragAndMergeSystem 확인
            bool dragSystemOK = DragAndMergeSystem.Instance != null;
            Debug.Log($"{LOG_PREFIX}   DragAndMergeSystem: {(dragSystemOK ? "✅ 정상" : "❌ 없음")}");
            
            // GameManager 확인
            bool gameManagerOK = InvaderInsider.Managers.GameManager.Instance != null;
            Debug.Log($"{LOG_PREFIX}   GameManager: {(gameManagerOK ? "✅ 정상" : "❌ 없음")}");
            
            // 유닛 수 확인
            BaseCharacter[] allUnits = FindObjectsOfType<BaseCharacter>();
            Debug.Log($"{LOG_PREFIX}   총 유닛 수: {allUnits.Length}개");
            
            // 드래그 컴포넌트 확인
            SimpleDraggableUnit[] draggables = FindObjectsOfType<SimpleDraggableUnit>();
            SimpleMergeTarget[] mergeTargets = FindObjectsOfType<SimpleMergeTarget>();
            
            Debug.Log($"{LOG_PREFIX}   드래그 가능 유닛: {draggables.Length}개");
            Debug.Log($"{LOG_PREFIX}   머지 타겟 유닛: {mergeTargets.Length}개");
            
            // 문제 감지
            if (!dragSystemOK || !gameManagerOK || (allUnits.Length > 0 && draggables.Length == 0))
            {
                Debug.LogWarning($"{LOG_PREFIX}⚠️ 문제가 감지되었습니다. 자동 수정이 필요합니다.");
            }
        }
        
        private void RunAutoFix()
        {
            Debug.Log($"{LOG_PREFIX}2. 자동 수정 실행");
            
            // AllInOneFixer 찾기 또는 생성
            AllInOneFixer fixer = FindObjectOfType<AllInOneFixer>();
            if (fixer == null)
            {
                GameObject fixerObj = new GameObject("AllInOneFixer");
                fixer = fixerObj.AddComponent<AllInOneFixer>();
                Debug.Log($"{LOG_PREFIX}   AllInOneFixer 생성됨");
            }
            
            // 수정 실행
            fixer.FixEverythingNow();
            Debug.Log($"{LOG_PREFIX}   자동 수정 실행됨");
        }
        
        private void TestFinalState()
        {
            Debug.Log($"{LOG_PREFIX}3. 최종 상태 확인");
            
            // 시스템 재확인
            bool dragSystemOK = DragAndMergeSystem.Instance != null;
            bool gameManagerOK = InvaderInsider.Managers.GameManager.Instance != null;
            
            // 유닛 컴포넌트 재확인
            BaseCharacter[] allUnits = FindObjectsOfType<BaseCharacter>();
            SimpleDraggableUnit[] draggables = FindObjectsOfType<SimpleDraggableUnit>();
            SimpleMergeTarget[] mergeTargets = FindObjectsOfType<SimpleMergeTarget>();
            
            Debug.Log($"{LOG_PREFIX}   === 최종 결과 ===");
            Debug.Log($"{LOG_PREFIX}   DragAndMergeSystem: {(dragSystemOK ? "✅" : "❌")}");
            Debug.Log($"{LOG_PREFIX}   GameManager: {(gameManagerOK ? "✅" : "❌")}");
            Debug.Log($"{LOG_PREFIX}   총 유닛: {allUnits.Length}개");
            Debug.Log($"{LOG_PREFIX}   드래그 가능: {draggables.Length}개");
            Debug.Log($"{LOG_PREFIX}   머지 타겟: {mergeTargets.Length}개");
            
            // 완성도 계산
            float completeness = 0f;
            if (allUnits.Length > 0)
            {
                float draggableRatio = (float)draggables.Length / allUnits.Length;
                float mergeRatio = (float)mergeTargets.Length / allUnits.Length;
                completeness = (draggableRatio + mergeRatio) / 2f * 100f;
            }
            
            Debug.Log($"{LOG_PREFIX}   시스템 완성도: {completeness:F1}%");
            
            if (completeness >= 90f && dragSystemOK && gameManagerOK)
            {
                Debug.Log($"{LOG_PREFIX}🎉 모든 시스템이 정상 작동합니다!");
                ShowUsageInstructions();
            }
            else
            {
                Debug.LogWarning($"{LOG_PREFIX}⚠️ 일부 시스템에 문제가 있습니다.");
            }
        }
        
        private void ShowUsageInstructions()
        {
            Debug.Log($"{LOG_PREFIX}");
            Debug.Log($"{LOG_PREFIX}🎮 === 사용법 ===");
            Debug.Log($"{LOG_PREFIX}");
            Debug.Log($"{LOG_PREFIX}✨ 카드 드래그:");
            Debug.Log($"{LOG_PREFIX}   - 카드를 클릭하고 드래그하여 타일에 배치");
            Debug.Log($"{LOG_PREFIX}   - 같은 ID/레벨 유닛에 드롭하면 레벨업");
            Debug.Log($"{LOG_PREFIX}");
            Debug.Log($"{LOG_PREFIX}✨ 유닛 드래그:");
            Debug.Log($"{LOG_PREFIX}   - 필드의 유닛을 클릭하고 드래그");
            Debug.Log($"{LOG_PREFIX}   - 같은 ID/레벨 유닛에 드롭하면 머지");
            Debug.Log($"{LOG_PREFIX}");
            Debug.Log($"{LOG_PREFIX}🔧 단축키:");
            Debug.Log($"{LOG_PREFIX}   ESC - 모든 드래그 취소");
            Debug.Log($"{LOG_PREFIX}   F5 - 시스템 테스트");
            Debug.Log($"{LOG_PREFIX}   F6 - 빠른 상태 확인");
            Debug.Log($"{LOG_PREFIX}   Ctrl+Shift+F - 긴급 수정");
            Debug.Log($"{LOG_PREFIX}");
        }
        
        private void Update()
        {
            // Ctrl + T: 빠른 테스트 실행
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.T))
            {
                StartCoroutine(RunQuickTest());
            }
        }
        
        [ContextMenu("Run Quick Test")]
        public void RunQuickTestManual()
        {
            StartCoroutine(RunQuickTest());
        }
    }
}