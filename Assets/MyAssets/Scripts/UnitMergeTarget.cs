using UnityEngine;
using UnityEngine.EventSystems;
using InvaderInsider.Data;
using InvaderInsider.Managers;
using InvaderInsider.Cards;

namespace InvaderInsider
{
    public class UnitMergeTarget : MonoBehaviour, IDropHandler
    {
        private BaseCharacter targetCharacter;

        private void Awake()
        {
            targetCharacter = GetComponent<BaseCharacter>();
            if (targetCharacter == null)
            {
                Debug.LogError($"[UnitMergeTarget] {gameObject.name} requires a BaseCharacter component.");
                enabled = false; // BaseCharacter가 없으면 이 스크립트 비활성화
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            CardDBObject draggedCard = GameManager.Instance.DraggedCardData;
            BaseCharacter draggedUnit = GameManager.Instance.DraggedUnit;

            Debug.Log($"[UnitMergeTarget] OnDrop - DraggedCardData: {(draggedCard != null ? draggedCard.cardName : "null")}, DraggedUnit: {(draggedUnit != null ? draggedUnit.gameObject.name : "null")}");

            if (targetCharacter == null) return; // 타겟 캐릭터가 없으면 처리하지 않음

            // 1. 카드 아이콘을 드래그한 경우
            if (draggedCard != null)
            {
                Debug.Log($"[UnitMergeTarget] OnDrop called on {gameObject.name} with a Card.");
                Debug.Log($"[UnitMergeTarget] Dragged Card: {draggedCard.cardName} (ID: {draggedCard.cardId}, Level: {draggedCard.level})");
                
                if (!targetCharacter.IsInitialized)
                {
                    Debug.LogWarning($"[UnitMergeTarget] Target character {targetCharacter.gameObject.name} is not initialized. Cannot upgrade with card.");
                    GameManager.Instance.WasCardDroppedOnTower = false; // 업그레이드 실패
                    return;
                }

                Debug.Log($"[UnitMergeTarget] Target Character: {targetCharacter.gameObject.name} (ID: {targetCharacter.CardId}, Level: {targetCharacter.Level})");

                // 업그레이드 조건 확인: ID와 레벨이 모두 같아야 함
                if (targetCharacter.CardId == draggedCard.cardId && targetCharacter.Level == draggedCard.level)
                {
                    Debug.Log($"[UnitMergeTarget] Upgrade conditions met! Upgrading {targetCharacter.gameObject.name} with card.");
                    targetCharacter.LevelUp();

                    // 카드 소모
                    CardManager.Instance.RemoveCardFromHand(draggedCard.cardId);

                    GameManager.Instance.WasCardDroppedOnTower = true; // 성공적으로 유닛에 드롭되었음을 알림
                }
                else
                {
                    Debug.Log($"[UnitMergeTarget] Upgrade conditions NOT met for card. ID Match: {targetCharacter.CardId == draggedCard.cardId}, Level Match: {targetCharacter.Level == draggedCard.level}");
                    GameManager.Instance.WasCardDroppedOnTower = false; // 업그레이드 실패
                }
            }
            // 2. 유닛을 드래그한 경우
            else if (draggedUnit != null && draggedUnit != targetCharacter) // 자기 자신에게 드롭하는 것 방지
            {
                Debug.Log($"[UnitMergeTarget] OnDrop called on {gameObject.name} with a Unit.");
                Debug.Log($"[UnitMergeTarget] Dragged Unit: {draggedUnit.gameObject.name} (ID: {draggedUnit.CardId}, Level: {draggedUnit.Level})");
                Debug.Log($"[UnitMergeTarget] Target Unit: {targetCharacter.gameObject.name} (ID: {targetCharacter.CardId}, Level: {targetCharacter.Level})");

                if (!draggedUnit.IsInitialized || !targetCharacter.IsInitialized)
                {
                    Debug.LogWarning($"[UnitMergeTarget] Dragged or target unit is not initialized. Cannot merge.");
                    GameManager.Instance.DroppedOnUnitTarget = null; // 드롭 실패
                    return;
                }

                // 합치기 조건 확인: ID와 레벨이 모두 같아야 함
                if (draggedUnit.CardId == targetCharacter.CardId && draggedUnit.Level == targetCharacter.Level)
                {
                    Debug.Log($"[UnitMergeTarget] Merge conditions met! Merging {draggedUnit.gameObject.name} into {targetCharacter.gameObject.name}.");
                    targetCharacter.LevelUp();
                    Destroy(draggedUnit.gameObject); // 드래그된 유닛 파괴
                    GameManager.Instance.DroppedOnUnitTarget = targetCharacter; // 성공적으로 드롭되었음을 알림
                }
                else
                {
                    Debug.Log($"[UnitMergeTarget] Merge conditions NOT met for units. ID Match: {draggedUnit.CardId == targetCharacter.CardId}, Level Match: {draggedUnit.Level == targetCharacter.Level}");
                    GameManager.Instance.DroppedOnUnitTarget = null; // 드롭 실패
                }
            }
            else
            {
                Debug.Log($"[UnitMergeTarget] No valid dragged item (card or unit) or target character is null.");
                GameManager.Instance.WasCardDroppedOnTower = false; // 카드 드롭 실패
                GameManager.Instance.DroppedOnUnitTarget = null; // 유닛 드롭 실패
            }
        }
    }
}