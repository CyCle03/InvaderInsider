using UnityEngine;

namespace InvaderInsider
{
    /// <summary>
    /// 즉시 실행되는 드래그 설정 스크립트
    /// </summary>
    public class QuickFixDragSetup : MonoBehaviour
    {
        [ContextMenu("Fix All Units Now")]
        public void FixAllUnitsNow()
        {
            BaseCharacter[] allCharacters = FindObjectsOfType<BaseCharacter>();
            int fixedCount = 0;

            foreach (BaseCharacter character in allCharacters)
            {
                if (character == null) continue;

                // DraggableUnit 컴포넌트 추가
                DraggableUnit draggable = character.GetComponent<DraggableUnit>();
                if (draggable == null)
                {
                    draggable = character.gameObject.AddComponent<DraggableUnit>();
                    Debug.Log($"[QuickFix] DraggableUnit 추가: {character.name}");
                }
                draggable.enabled = true;

                // UnitMergeTarget 컴포넌트 추가
                UnitMergeTarget mergeTarget = character.GetComponent<UnitMergeTarget>();
                if (mergeTarget == null)
                {
                    mergeTarget = character.gameObject.AddComponent<UnitMergeTarget>();
                    Debug.Log($"[QuickFix] UnitMergeTarget 추가: {character.name}");
                }

                // Collider 확인 및 추가
                Collider col = character.GetComponent<Collider>();
                if (col == null)
                {
                    BoxCollider boxCol = character.gameObject.AddComponent<BoxCollider>();
                    boxCol.isTrigger = true;
                    Debug.Log($"[QuickFix] BoxCollider 추가: {character.name}");
                }
                else
                {
                    col.isTrigger = true;
                    Debug.Log($"[QuickFix] Collider isTrigger 설정: {character.name}");
                }

                fixedCount++;
            }

            Debug.Log($"[QuickFix] 총 {fixedCount}개 유닛 수정 완료!");
        }

        private void Start()
        {
            // 게임 시작 시 자동 실행
            Invoke(nameof(FixAllUnitsNow), 1f); // 1초 후 실행
        }
    }
}