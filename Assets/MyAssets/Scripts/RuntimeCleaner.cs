using UnityEngine;

namespace InvaderInsider
{
    /// <summary>
    /// 런타임에서 사용할 수 있는 정리 도구
    /// </summary>
    public class RuntimeCleaner : MonoBehaviour
    {
        private const string LOG_PREFIX = "[RuntimeCleaner] ";
        
        [Header("Auto Clean")]
        [SerializeField] private bool autoCleanOnStart = true;
        [SerializeField] private float cleanDelay = 1f;
        
        private void Start()
        {
            if (autoCleanOnStart)
            {
                Invoke(nameof(CleanAllSystems), cleanDelay);
            }
        }
        
        /// <summary>
        /// 모든 시스템 정리 및 재설정
        /// </summary>
        [ContextMenu("Clean All Systems")]
        public void CleanAllSystems()
        {
            Debug.Log($"{LOG_PREFIX}시스템 정리 시작");
            
            // 1. DragAndMergeSystem 확인/생성
            EnsureDragAndMergeSystem();
            
            // 2. 모든 유닛에 새로운 컴포넌트 추가
            SetupAllUnits();
            
            // 3. GameManager 연동 확인
            CheckGameManagerIntegration();
            
            Debug.Log($"{LOG_PREFIX}시스템 정리 완료");
        }
        
        private void EnsureDragAndMergeSystem()
        {
            if (DragAndMergeSystem.Instance == null)
            {
                GameObject systemObj = new GameObject("DragAndMergeSystem");
                systemObj.AddComponent<DragAndMergeSystem>();
                Debug.Log($"{LOG_PREFIX}DragAndMergeSystem 생성됨");
            }
            else
            {
                Debug.Log($"{LOG_PREFIX}DragAndMergeSystem 이미 존재함");
            }
        }
        
        private void SetupAllUnits()
        {
            BaseCharacter[] allCharacters = FindObjectsOfType<BaseCharacter>();
            int setupCount = 0;
            
            foreach (BaseCharacter character in allCharacters)
            {
                if (character == null) continue;
                
                bool wasSetup = false;
                
                // SimpleDraggableUnit 추가
                if (character.GetComponent<SimpleDraggableUnit>() == null)
                {
                    character.gameObject.AddComponent<SimpleDraggableUnit>();
                    wasSetup = true;
                }
                
                // SimpleMergeTarget 추가
                if (character.GetComponent<SimpleMergeTarget>() == null)
                {
                    character.gameObject.AddComponent<SimpleMergeTarget>();
                    wasSetup = true;
                }
                
                // Collider 확인
                Collider col = character.GetComponent<Collider>();
                if (col == null)
                {
                    BoxCollider boxCol = character.gameObject.AddComponent<BoxCollider>();
                    boxCol.isTrigger = true;
                    wasSetup = true;
                }
                else if (!col.isTrigger)
                {
                    col.isTrigger = true;
                    wasSetup = true;
                }
                
                if (wasSetup)
                {
                    setupCount++;
                }
            }
            
            Debug.Log($"{LOG_PREFIX}유닛 설정 완료: {setupCount}/{allCharacters.Length}개 유닛 처리됨");
        }
        
        private void CheckGameManagerIntegration()
        {
            if (InvaderInsider.Managers.GameManager.Instance != null)
            {
                Debug.Log($"{LOG_PREFIX}GameManager 연동 확인됨");
            }
            else
            {
                Debug.LogWarning($"{LOG_PREFIX}GameManager를 찾을 수 없음");
            }
        }
        
        /// <summary>
        /// 시스템 상태 확인
        /// </summary>
        [ContextMenu("Check System Status")]
        public void CheckSystemStatus()
        {
            Debug.Log($"{LOG_PREFIX}=== 시스템 상태 확인 ===");
            
            // DragAndMergeSystem 상태
            bool dragSystemExists = DragAndMergeSystem.Instance != null;
            Debug.Log($"{LOG_PREFIX}DragAndMergeSystem: {(dragSystemExists ? "활성" : "비활성")}");
            
            // GameManager 상태
            bool gameManagerExists = InvaderInsider.Managers.GameManager.Instance != null;
            Debug.Log($"{LOG_PREFIX}GameManager: {(gameManagerExists ? "활성" : "비활성")}");
            
            // 유닛 상태
            BaseCharacter[] allCharacters = FindObjectsOfType<BaseCharacter>();
            SimpleDraggableUnit[] draggables = FindObjectsOfType<SimpleDraggableUnit>();
            SimpleMergeTarget[] mergeTargets = FindObjectsOfType<SimpleMergeTarget>();
            
            Debug.Log($"{LOG_PREFIX}총 유닛: {allCharacters.Length}개");
            Debug.Log($"{LOG_PREFIX}드래그 가능: {draggables.Length}개");
            Debug.Log($"{LOG_PREFIX}머지 타겟: {mergeTargets.Length}개");
            
            // 완성도 계산
            if (allCharacters.Length > 0)
            {
                float completeness = (float)(draggables.Length + mergeTargets.Length) / (allCharacters.Length * 2) * 100f;
                Debug.Log($"{LOG_PREFIX}시스템 완성도: {completeness:F1}%");
                
                if (completeness >= 90f)
                {
                    Debug.Log($"{LOG_PREFIX}✅ 시스템이 정상적으로 설정됨");
                }
                else
                {
                    Debug.LogWarning($"{LOG_PREFIX}⚠️ 일부 유닛에 컴포넌트가 누락됨");
                }
            }
        }
        
        private void Update()
        {
            // Ctrl + F1: 시스템 정리
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.F1))
            {
                CleanAllSystems();
            }
            
            // Ctrl + F2: 상태 확인
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.F2))
            {
                CheckSystemStatus();
            }
        }
    }
}