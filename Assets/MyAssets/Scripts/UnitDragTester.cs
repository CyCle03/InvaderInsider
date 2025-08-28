using UnityEngine;

namespace InvaderInsider
{
    /// <summary>
    /// 유닛 드래그 기능 전용 테스터
    /// </summary>
    public class UnitDragTester : MonoBehaviour
    {
        private const string LOG_PREFIX = "[UnitDragTester] ";
        
        [Header("Test Settings")]
        [SerializeField] private bool enableDetailedLogs = true;
        
        private void Start()
        {
            if (enableDetailedLogs)
            {
                Debug.Log($"{LOG_PREFIX}유닛 드래그 테스터 시작 (상세 로그 활성화)");
            }
            else
            {
                Debug.Log($"{LOG_PREFIX}유닛 드래그 테스터 시작");
            }
        }
        
        private void Update()
        {
            // F7 키로 유닛 드래그 상태 확인
            if (Input.GetKeyDown(KeyCode.F7))
            {
                CheckUnitDragStatus();
            }
            
            // F8 키로 모든 유닛 정보 출력
            if (Input.GetKeyDown(KeyCode.F8))
            {
                ShowAllUnitsInfo();
            }
            
            // F9 키로 드래그 시스템 강제 리셋
            if (Input.GetKeyDown(KeyCode.F9))
            {
                ForceResetDragSystem();
            }
        }
        
        private void CheckUnitDragStatus()
        {
            Debug.Log($"{LOG_PREFIX}=== 유닛 드래그 상태 확인 ===");
            
            if (DragAndMergeSystem.Instance != null)
            {
                Debug.Log($"{LOG_PREFIX}현재 드래그 타입: {DragAndMergeSystem.Instance.CurrentDragType}");
                Debug.Log($"{LOG_PREFIX}드래그 중: {DragAndMergeSystem.Instance.IsDragging}");
                Debug.Log($"{LOG_PREFIX}유닛 드래그 중: {DragAndMergeSystem.Instance.IsUnitDragging}");
                
                if (DragAndMergeSystem.Instance.DraggedUnit != null)
                {
                    var unit = DragAndMergeSystem.Instance.DraggedUnit;
                    Debug.Log($"{LOG_PREFIX}드래그된 유닛: {unit.name} (ID: {unit.CardId}, Level: {unit.Level})");
                    Debug.Log($"{LOG_PREFIX}현재 위치: {unit.transform.position}");
                }
                
                Debug.Log($"{LOG_PREFIX}마지막 드롭 성공: {DragAndMergeSystem.Instance.WasDropSuccessful}");
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX}DragAndMergeSystem이 없습니다!");
            }
        }
        
        private void ShowAllUnitsInfo()
        {
            Debug.Log($"{LOG_PREFIX}=== 모든 유닛 정보 ===");
            
            BaseCharacter[] allUnits = FindObjectsOfType<BaseCharacter>();
            Debug.Log($"{LOG_PREFIX}총 {allUnits.Length}개 유닛 발견");
            
            for (int i = 0; i < allUnits.Length; i++)
            {
                var unit = allUnits[i];
                if (unit == null) continue;
                
                bool hasDraggable = unit.GetComponent<SimpleDraggableUnit>() != null;
                bool hasMergeTarget = unit.GetComponent<SimpleMergeTarget>() != null;
                bool hasCollider = unit.GetComponent<Collider>() != null;
                bool colliderIsTrigger = hasCollider && unit.GetComponent<Collider>().isTrigger;
                
                Debug.Log($"{LOG_PREFIX}[{i}] {unit.name}:");
                Debug.Log($"{LOG_PREFIX}    ID: {unit.CardId}, Level: {unit.Level}");
                Debug.Log($"{LOG_PREFIX}    위치: {unit.transform.position}");
                Debug.Log($"{LOG_PREFIX}    초기화됨: {unit.IsInitialized}");
                Debug.Log($"{LOG_PREFIX}    드래그 가능: {hasDraggable}");
                Debug.Log($"{LOG_PREFIX}    머지 타겟: {hasMergeTarget}");
                Debug.Log($"{LOG_PREFIX}    콜라이더: {hasCollider} (트리거: {colliderIsTrigger})");
            }
        }
        
        private void ForceResetDragSystem()
        {
            Debug.Log($"{LOG_PREFIX}드래그 시스템 강제 리셋");
            
            if (DragAndMergeSystem.Instance != null)
            {
                DragAndMergeSystem.Instance.CancelAllDrags();
                Debug.Log($"{LOG_PREFIX}✅ 드래그 시스템 리셋 완료");
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX}❌ DragAndMergeSystem이 없습니다!");
            }
        }
        
        /// <summary>
        /// 특정 유닛들의 머지 가능성 테스트
        /// </summary>
        [ContextMenu("Test Unit Merge Compatibility")]
        public void TestUnitMergeCompatibility()
        {
            BaseCharacter[] allUnits = FindObjectsOfType<BaseCharacter>();
            
            Debug.Log($"{LOG_PREFIX}=== 유닛 머지 호환성 테스트 ===");
            
            for (int i = 0; i < allUnits.Length; i++)
            {
                for (int j = i + 1; j < allUnits.Length; j++)
                {
                    var unit1 = allUnits[i];
                    var unit2 = allUnits[j];
                    
                    if (unit1 == null || unit2 == null) continue;
                    
                    bool canMerge = (unit1.CardId == unit2.CardId && unit1.Level == unit2.Level);
                    
                    if (canMerge)
                    {
                        Debug.Log($"{LOG_PREFIX}✅ 머지 가능: {unit1.name} <-> {unit2.name} (ID: {unit1.CardId}, Level: {unit1.Level})");
                    }
                }
            }
        }
    }
}