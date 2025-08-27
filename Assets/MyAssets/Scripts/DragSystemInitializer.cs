using UnityEngine;

namespace InvaderInsider
{
    /// <summary>
    /// 새로운 드래그 시스템을 자동으로 초기화하는 스크립트
    /// </summary>
    public class DragSystemInitializer : MonoBehaviour
    {
        private const string LOG_PREFIX = "[DragSystemInitializer] ";
        
        [Header("Auto Setup")]
        [SerializeField] private bool autoSetupOnStart = false; // GameManager에서 제어하므로 기본값 false
        [SerializeField] private float setupDelay = 1f;
        
        private void Start()
        {
            if (autoSetupOnStart)
            {
                Invoke(nameof(InitializeSystem), setupDelay);
            }
        }

        /// <summary>
        /// 외부에서 호출할 수 있는 초기화 메서드 (지연 시간 포함)
        /// </summary>
        public void InitializeSystemWithDelay(float delay = 1f)
        {
            Invoke(nameof(InitializeSystem), delay);
        }
        
        /// <summary>
        /// 시스템 초기화
        /// </summary>
        [ContextMenu("Initialize Drag System")]
        public void InitializeSystem()
        {
            Debug.Log($"{LOG_PREFIX}드래그 시스템 초기화 시작");
            
            // DragAndMergeSystem 인스턴스 확인/생성
            if (DragAndMergeSystem.Instance == null)
            {
                Debug.LogError($"{LOG_PREFIX}DragAndMergeSystem 인스턴스를 찾을 수 없습니다!");
                return;
            }
            
            // 마이그레이터 생성 및 실행
            GameObject migratorObj = new GameObject("DragSystemMigrator");
            DragSystemMigrator migrator = migratorObj.AddComponent<DragSystemMigrator>();
            
            // 모든 유닛 마이그레이션
            migrator.MigrateAllUnits();
            
            // 마이그레이터 정리
            Destroy(migratorObj);
            
            Debug.Log($"{LOG_PREFIX}드래그 시스템 초기화 완료");
        }
        
        /// <summary>
        /// 시스템 상태 확인
        /// </summary>
        [ContextMenu("Check System Status")]
        public void CheckSystemStatus()
        {
            Debug.Log($"{LOG_PREFIX}시스템 상태 확인");
            
            // DragAndMergeSystem 상태
            bool systemExists = DragAndMergeSystem.Instance != null;
            Debug.Log($"  - DragAndMergeSystem: {(systemExists ? "활성" : "비활성")}");
            
            if (systemExists)
            {
                Debug.Log($"  - 현재 드래그 타입: {DragAndMergeSystem.Instance.CurrentDragType}");
                Debug.Log($"  - 드래그 중: {DragAndMergeSystem.Instance.IsDragging}");
            }
            
            // 유닛 상태
            BaseCharacter[] allCharacters = FindObjectsOfType<BaseCharacter>();
            int newSystemCount = 0;
            int oldSystemCount = 0;
            
            foreach (BaseCharacter character in allCharacters)
            {
                if (character == null) continue;
                
                bool hasNewSystem = character.GetComponent<SimpleDraggableUnit>() != null || 
                                   character.GetComponent<SimpleMergeTarget>() != null;
                
                if (hasNewSystem) newSystemCount++;
                else oldSystemCount++;
            }
            
            Debug.Log($"  - 새로운 시스템 유닛: {newSystemCount}개");
            Debug.Log($"  - 기존 시스템 유닛: {oldSystemCount}개");
            Debug.Log($"  - 총 유닛: {allCharacters.Length}개");
        }
        
        private void Update()
        {
            // F1 키로 시스템 상태 확인
            if (Input.GetKeyDown(KeyCode.F1))
            {
                CheckSystemStatus();
            }
            
            // F3 키로 시스템 재초기화
            if (Input.GetKeyDown(KeyCode.F3))
            {
                InitializeSystem();
            }
        }
    }
}