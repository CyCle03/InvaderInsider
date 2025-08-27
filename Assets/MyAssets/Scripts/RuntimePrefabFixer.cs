using UnityEngine;
using System.Collections;

namespace InvaderInsider
{
    /// <summary>
    /// 런타임에서 프리팹 문제를 해결하는 도구
    /// </summary>
    public class RuntimePrefabFixer : MonoBehaviour
    {
        private const string LOG_PREFIX = "[RuntimePrefabFixer] ";
        
        [Header("Auto Fix")]
        [SerializeField] private bool autoFixOnStart = true;
        [SerializeField] private float fixDelay = 1f;
        
        private void Start()
        {
            if (autoFixOnStart)
            {
                StartCoroutine(FixAllIssues());
            }
        }
        
        private IEnumerator FixAllIssues()
        {
            yield return new WaitForSeconds(fixDelay);
            
            Debug.Log($"{LOG_PREFIX}=== 런타임 프리팹 수정 시작 ===");
            
            // 1. 모든 BaseCharacter 찾기
            BaseCharacter[] allCharacters = FindObjectsOfType<BaseCharacter>();
            Debug.Log($"{LOG_PREFIX}발견된 BaseCharacter: {allCharacters.Length}개");
            
            yield return new WaitForSeconds(0.1f);
            
            // 2. 각 캐릭터에 필요한 컴포넌트 강제 추가
            int fixedCount = 0;
            foreach (BaseCharacter character in allCharacters)
            {
                if (character == null) continue;
                
                bool wasFixed = ForceFixCharacter(character);
                if (wasFixed) fixedCount++;
                
                yield return null; // 한 프레임 대기
            }
            
            Debug.Log($"{LOG_PREFIX}=== 런타임 프리팹 수정 완료: {fixedCount}개 수정됨 ===");
            
            // 3. 시스템 테스트
            yield return new WaitForSeconds(0.5f);
            TestDragSystem();
        }
        
        /// <summary>
        /// 캐릭터 강제 수정
        /// </summary>
        private bool ForceFixCharacter(BaseCharacter character)
        {
            bool wasFixed = false;
            
            try
            {
                // SimpleDraggableUnit 확인/추가
                SimpleDraggableUnit draggable = character.GetComponent<SimpleDraggableUnit>();
                if (draggable == null)
                {
                    draggable = character.gameObject.AddComponent<SimpleDraggableUnit>();
                    wasFixed = true;
                    Debug.Log($"{LOG_PREFIX}SimpleDraggableUnit 추가: {character.name}");
                }
                
                // SimpleMergeTarget 확인/추가
                SimpleMergeTarget mergeTarget = character.GetComponent<SimpleMergeTarget>();
                if (mergeTarget == null)
                {
                    mergeTarget = character.gameObject.AddComponent<SimpleMergeTarget>();
                    wasFixed = true;
                    Debug.Log($"{LOG_PREFIX}SimpleMergeTarget 추가: {character.name}");
                }
                
                // Collider 확인/설정
                Collider col = character.GetComponent<Collider>();
                if (col == null)
                {
                    BoxCollider boxCol = character.gameObject.AddComponent<BoxCollider>();
                    boxCol.isTrigger = true;
                    wasFixed = true;
                    Debug.Log($"{LOG_PREFIX}BoxCollider 추가: {character.name}");
                }
                else if (!col.isTrigger)
                {
                    col.isTrigger = true;
                    wasFixed = true;
                    Debug.Log($"{LOG_PREFIX}Collider를 Trigger로 설정: {character.name}");
                }
                
                // 초기화 확인
                if (!character.IsInitialized)
                {
                    Debug.LogWarning($"{LOG_PREFIX}{character.name}이 초기화되지 않음");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"{LOG_PREFIX}{character.name} 수정 중 오류: {e.Message}");
            }
            
            return wasFixed;
        }
        
        /// <summary>
        /// 드래그 시스템 테스트
        /// </summary>
        private void TestDragSystem()
        {
            Debug.Log($"{LOG_PREFIX}=== 드래그 시스템 테스트 ===");
            
            // DragAndMergeSystem 확인
            bool dragSystemOK = DragAndMergeSystem.Instance != null;
            Debug.Log($"{LOG_PREFIX}DragAndMergeSystem: {(dragSystemOK ? "✅" : "❌")}");
            
            // 컴포넌트 개수 확인
            BaseCharacter[] allCharacters = FindObjectsOfType<BaseCharacter>();
            SimpleDraggableUnit[] draggables = FindObjectsOfType<SimpleDraggableUnit>();
            SimpleMergeTarget[] mergeTargets = FindObjectsOfType<SimpleMergeTarget>();
            
            Debug.Log($"{LOG_PREFIX}총 캐릭터: {allCharacters.Length}개");
            Debug.Log($"{LOG_PREFIX}드래그 가능: {draggables.Length}개");
            Debug.Log($"{LOG_PREFIX}머지 타겟: {mergeTargets.Length}개");
            
            // 완성도 계산
            if (allCharacters.Length > 0)
            {
                float completeness = (float)(draggables.Length + mergeTargets.Length) / (allCharacters.Length * 2) * 100f;
                Debug.Log($"{LOG_PREFIX}시스템 완성도: {completeness:F1}%");
                
                if (completeness >= 90f)
                {
                    Debug.Log($"{LOG_PREFIX}✅ 드래그 시스템 정상 작동 가능");
                }
                else
                {
                    Debug.LogWarning($"{LOG_PREFIX}⚠️ 일부 컴포넌트 누락됨");
                }
            }
        }
        
        /// <summary>
        /// 수동 수정
        /// </summary>
        [ContextMenu("Fix All Issues Now")]
        public void FixAllIssuesNow()
        {
            StartCoroutine(FixAllIssues());
        }
        
        private void Update()
        {
            // F7 키로 수동 수정
            if (Input.GetKeyDown(KeyCode.F7))
            {
                FixAllIssuesNow();
            }
        }
    }
}