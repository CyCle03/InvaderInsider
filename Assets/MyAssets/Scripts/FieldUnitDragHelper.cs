using UnityEngine;
using InvaderInsider.Managers;

namespace InvaderInsider
{
    /// <summary>
    /// 필드에 있는 유닛들의 드래그 기능을 관리하는 헬퍼 클래스
    /// </summary>
    public class FieldUnitDragHelper : MonoBehaviour
    {
        private const string LOG_PREFIX = "[FieldUnitDragHelper] ";

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        private void Start()
        {
            // 게임 시작 시 모든 필드 유닛에 드래그 기능 활성화
            EnableDraggingForAllUnits();
        }

        /// <summary>
        /// 씬의 모든 BaseCharacter에 드래그 및 머지 컴포넌트를 추가합니다.
        /// </summary>
        [ContextMenu("Enable Dragging for All Units")]
        public void EnableDraggingForAllUnits()
        {
            BaseCharacter[] allCharacters = FindObjectsOfType<BaseCharacter>();
            int enabledCount = 0;

            foreach (BaseCharacter character in allCharacters)
            {
                if (character == null) continue;

                bool wasEnabled = EnableDraggingForUnit(character);
                if (wasEnabled) enabledCount++;
            }

            if (showDebugInfo)
            {
                Debug.Log($"{LOG_PREFIX}{enabledCount}개 유닛에 드래그 기능을 활성화했습니다.");
            }
        }

        /// <summary>
        /// 특정 유닛에 드래그 및 머지 컴포넌트를 추가합니다.
        /// </summary>
        /// <param name="character">대상 캐릭터</param>
        /// <returns>새로 활성화되었으면 true</returns>
        public bool EnableDraggingForUnit(BaseCharacter character)
        {
            if (character == null) return false;

            bool wasNewlyEnabled = false;

            // DraggableUnit 컴포넌트 추가
            DraggableUnit draggable = character.GetComponent<DraggableUnit>();
            if (draggable == null)
            {
                draggable = character.gameObject.AddComponent<DraggableUnit>();
                wasNewlyEnabled = true;
                
                if (showDebugInfo)
                {
                    Debug.Log($"{LOG_PREFIX}DraggableUnit 컴포넌트를 {character.name}에 추가했습니다.");
                }
            }
            draggable.enabled = true;

            // UnitMergeTarget 컴포넌트 추가
            UnitMergeTarget mergeTarget = character.GetComponent<UnitMergeTarget>();
            if (mergeTarget == null)
            {
                mergeTarget = character.gameObject.AddComponent<UnitMergeTarget>();
                
                if (showDebugInfo)
                {
                    Debug.Log($"{LOG_PREFIX}UnitMergeTarget 컴포넌트를 {character.name}에 추가했습니다.");
                }
            }

            return wasNewlyEnabled;
        }

        /// <summary>
        /// 새로 생성된 유닛에 자동으로 드래그 기능을 추가합니다.
        /// GameManager.SpawnObject에서 호출됩니다.
        /// </summary>
        /// <param name="spawnedObject">생성된 오브젝트</param>
        public static void EnableDraggingForNewUnit(GameObject spawnedObject)
        {
            if (spawnedObject == null) return;

            BaseCharacter character = spawnedObject.GetComponent<BaseCharacter>();
            if (character == null) return;

            // DraggableUnit 컴포넌트 추가
            DraggableUnit draggable = spawnedObject.GetComponent<DraggableUnit>();
            if (draggable == null)
            {
                draggable = spawnedObject.AddComponent<DraggableUnit>();
            }
            draggable.enabled = true;

            // UnitMergeTarget 컴포넌트 추가
            UnitMergeTarget mergeTarget = spawnedObject.GetComponent<UnitMergeTarget>();
            if (mergeTarget == null)
            {
                mergeTarget = spawnedObject.AddComponent<UnitMergeTarget>();
            }

            Debug.Log($"[FieldUnitDragHelper] 새로 생성된 유닛 {spawnedObject.name}에 드래그 기능을 활성화했습니다.");
        }

        /// <summary>
        /// 특정 유닛의 드래그 기능을 비활성화합니다.
        /// </summary>
        /// <param name="character">대상 캐릭터</param>
        public void DisableDraggingForUnit(BaseCharacter character)
        {
            if (character == null) return;

            DraggableUnit draggable = character.GetComponent<DraggableUnit>();
            if (draggable != null)
            {
                draggable.enabled = false;
            }

            if (showDebugInfo)
            {
                Debug.Log($"{LOG_PREFIX}{character.name}의 드래그 기능을 비활성화했습니다.");
            }
        }

        /// <summary>
        /// 모든 유닛의 드래그 기능을 비활성화합니다.
        /// </summary>
        [ContextMenu("Disable Dragging for All Units")]
        public void DisableDraggingForAllUnits()
        {
            BaseCharacter[] allCharacters = FindObjectsOfType<BaseCharacter>();
            int disabledCount = 0;

            foreach (BaseCharacter character in allCharacters)
            {
                if (character == null) continue;

                DraggableUnit draggable = character.GetComponent<DraggableUnit>();
                if (draggable != null && draggable.enabled)
                {
                    draggable.enabled = false;
                    disabledCount++;
                }
            }

            if (showDebugInfo)
            {
                Debug.Log($"{LOG_PREFIX}{disabledCount}개 유닛의 드래그 기능을 비활성화했습니다.");
            }
        }

        /// <summary>
        /// 드래그 가능한 유닛들의 상태를 디버그 출력합니다.
        /// </summary>
        [ContextMenu("Debug Draggable Units")]
        public void DebugDraggableUnits()
        {
            BaseCharacter[] allCharacters = FindObjectsOfType<BaseCharacter>();
            int draggableCount = 0;
            int mergeTargetCount = 0;

            foreach (BaseCharacter character in allCharacters)
            {
                if (character == null) continue;

                DraggableUnit draggable = character.GetComponent<DraggableUnit>();
                UnitMergeTarget mergeTarget = character.GetComponent<UnitMergeTarget>();

                if (draggable != null && draggable.enabled) draggableCount++;
                if (mergeTarget != null) mergeTargetCount++;

                Debug.Log($"{LOG_PREFIX}{character.name}: " +
                         $"Draggable={draggable != null && draggable.enabled}, " +
                         $"MergeTarget={mergeTarget != null}, " +
                         $"Initialized={character.IsInitialized}, " +
                         $"CardId={character.CardId}, " +
                         $"Level={character.Level}");
            }

            Debug.Log($"{LOG_PREFIX}총 {allCharacters.Length}개 유닛 중 " +
                     $"{draggableCount}개가 드래그 가능, " +
                     $"{mergeTargetCount}개가 머지 대상입니다.");
        }
    }
}