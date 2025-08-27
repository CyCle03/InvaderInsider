using UnityEngine;

namespace InvaderInsider
{
    /// <summary>
    /// 기존 드래그 시스템을 새로운 통합 시스템으로 마이그레이션하는 유틸리티
    /// </summary>
    public class DragSystemMigrator : MonoBehaviour
    {
        private const string LOG_PREFIX = "[DragSystemMigrator] ";
        
        [Header("Migration Settings")]
        [SerializeField] private bool removeOldComponents = true;
        [SerializeField] private bool addNewComponents = true;
        [SerializeField] private bool showDetailedLogs = true;
        
        /// <summary>
        /// 모든 유닛을 새로운 시스템으로 마이그레이션
        /// </summary>
        [ContextMenu("Migrate All Units to New System")]
        public void MigrateAllUnits()
        {
            BaseCharacter[] allCharacters = FindObjectsOfType<BaseCharacter>();
            int migratedCount = 0;
            
            Debug.Log($"{LOG_PREFIX}총 {allCharacters.Length}개의 BaseCharacter 발견");
            
            foreach (BaseCharacter character in allCharacters)
            {
                if (character == null) continue;
                
                bool migrated = MigrateUnit(character);
                if (migrated) migratedCount++;
            }
            
            Debug.Log($"{LOG_PREFIX}마이그레이션 완료: {migratedCount}/{allCharacters.Length}개 유닛 처리됨");
        }
        
        /// <summary>
        /// 특정 유닛을 새로운 시스템으로 마이그레이션
        /// </summary>
        public bool MigrateUnit(BaseCharacter character)
        {
            if (character == null) return false;
            
            bool changed = false;
            
            if (showDetailedLogs)
            {
                Debug.Log($"{LOG_PREFIX}마이그레이션 시작: {character.name}");
            }
            
            // 기존 컴포넌트 제거 (더 이상 존재하지 않음)
            if (removeOldComponents)
            {
                // 기존 컴포넌트들은 이미 제거됨
                if (showDetailedLogs)
                {
                    Debug.Log($"{LOG_PREFIX}기존 컴포넌트 확인 완료: {character.name}");
                }
            }
            
            // 새로운 컴포넌트 추가
            if (addNewComponents)
            {
                SimpleDraggableUnit newDraggable = character.GetComponent<SimpleDraggableUnit>();
                if (newDraggable == null)
                {
                    newDraggable = character.gameObject.AddComponent<SimpleDraggableUnit>();
                    changed = true;
                    if (showDetailedLogs)
                    {
                        Debug.Log($"{LOG_PREFIX}새로운 SimpleDraggableUnit 추가: {character.name}");
                    }
                }
                
                SimpleMergeTarget newMergeTarget = character.GetComponent<SimpleMergeTarget>();
                if (newMergeTarget == null)
                {
                    newMergeTarget = character.gameObject.AddComponent<SimpleMergeTarget>();
                    changed = true;
                    if (showDetailedLogs)
                    {
                        Debug.Log($"{LOG_PREFIX}새로운 SimpleMergeTarget 추가: {character.name}");
                    }
                }
            }
            
            // Collider 확인
            Collider col = character.GetComponent<Collider>();
            if (col == null)
            {
                BoxCollider boxCol = character.gameObject.AddComponent<BoxCollider>();
                boxCol.isTrigger = true;
                changed = true;
                if (showDetailedLogs)
                {
                    Debug.Log($"{LOG_PREFIX}BoxCollider 추가: {character.name}");
                }
            }
            else if (!col.isTrigger)
            {
                col.isTrigger = true;
                changed = true;
                if (showDetailedLogs)
                {
                    Debug.Log($"{LOG_PREFIX}Collider를 Trigger로 설정: {character.name}");
                }
            }
            
            return changed;
        }
        
        /// <summary>
        /// 새로운 시스템 상태 확인
        /// </summary>
        [ContextMenu("Check New System Status")]
        public void CheckNewSystemStatus()
        {
            BaseCharacter[] allCharacters = FindObjectsOfType<BaseCharacter>();
            int newSystemCount = 0;
            int oldSystemCount = 0;
            int mixedSystemCount = 0;
            
            foreach (BaseCharacter character in allCharacters)
            {
                if (character == null) continue;
                
                bool hasNewDraggable = character.GetComponent<SimpleDraggableUnit>() != null;
                bool hasNewMergeTarget = character.GetComponent<SimpleMergeTarget>() != null;
                
                bool isNewSystem = hasNewDraggable || hasNewMergeTarget;
                
                if (isNewSystem) newSystemCount++;
                else oldSystemCount++;
                
                if (showDetailedLogs)
                {
                    Debug.Log($"{LOG_PREFIX}{character.name}: " +
                             $"New(D:{hasNewDraggable}, M:{hasNewMergeTarget})");
                }
            }
            
            Debug.Log($"{LOG_PREFIX}시스템 상태 요약:");
            Debug.Log($"  - 새로운 시스템: {newSystemCount}개");
            Debug.Log($"  - 기존 시스템: {oldSystemCount}개");
            Debug.Log($"  - 혼재 상태: {mixedSystemCount}개");
            Debug.Log($"  - 총 유닛: {allCharacters.Length}개");
        }
        
        /// <summary>
        /// 기존 시스템 컴포넌트만 제거 (더 이상 필요 없음)
        /// </summary>
        [ContextMenu("Remove Old System Components Only")]
        public void RemoveOldSystemComponents()
        {
            Debug.Log($"{LOG_PREFIX}기존 시스템 컴포넌트는 이미 제거되었습니다.");
        }
        
        /// <summary>
        /// 새로운 시스템 컴포넌트만 추가
        /// </summary>
        [ContextMenu("Add New System Components Only")]
        public void AddNewSystemComponents()
        {
            BaseCharacter[] allCharacters = FindObjectsOfType<BaseCharacter>();
            int addedCount = 0;
            
            foreach (BaseCharacter character in allCharacters)
            {
                if (character == null) continue;
                
                SimpleDraggableUnit newDraggable = character.GetComponent<SimpleDraggableUnit>();
                if (newDraggable == null)
                {
                    character.gameObject.AddComponent<SimpleDraggableUnit>();
                    addedCount++;
                }
                
                SimpleMergeTarget newMergeTarget = character.GetComponent<SimpleMergeTarget>();
                if (newMergeTarget == null)
                {
                    character.gameObject.AddComponent<SimpleMergeTarget>();
                    addedCount++;
                }
            }
            
            Debug.Log($"{LOG_PREFIX}새로운 시스템 컴포넌트 {addedCount}개 추가 완료");
        }
        
        private void Start()
        {
            // 게임 시작 시 자동 마이그레이션 (선택사항)
            // Invoke(nameof(MigrateAllUnits), 1f);
        }
    }
}