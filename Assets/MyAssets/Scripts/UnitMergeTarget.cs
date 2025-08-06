
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

            if (draggedCard != null && targetCharacter != null)
            {
                Debug.Log($"[UnitMergeTarget] OnDrop called on {gameObject.name}");
                Debug.Log($"[UnitMergeTarget] Dragged Card: {draggedCard.cardName} (ID: {draggedCard.cardId}, Level: {draggedCard.level})");
                
                // targetCharacter.sourceCardData == null 체크 로직 제거 (public 프로퍼티 사용)

                Debug.Log($"[UnitMergeTarget] Target Character: {targetCharacter.gameObject.name} (ID: {targetCharacter.CardId}, Level: {targetCharacter.Level})");

                // 업그레이드 조건 확인: ID와 레벨이 모두 같아야 함
                if (targetCharacter.CardId == draggedCard.cardId && targetCharacter.Level == draggedCard.level)
                {
                    Debug.Log($"[UnitMergeTarget] Upgrade conditions met! Upgrading {targetCharacter.gameObject.name}.");
                    targetCharacter.LevelUp();

                    // 카드 소모
                    CardManager.Instance.RemoveCardFromHand(draggedCard.cardId);

                    // 드래그하던 UI 오브젝트 제거 (CardInteractionHandler에서 처리되므로 여기서는 주석 처리)
                    // if (eventData.pointerDrag != null)
                    // {
                    //     Destroy(eventData.pointerDrag);
                    // }
                    GameManager.Instance.WasCardDroppedOnTower = true; // 성공적으로 유닛에 드롭되었음을 알림
                }
                else
                {
                    Debug.Log($"[UnitMergeTarget] Upgrade conditions NOT met. ID Match: {targetCharacter.CardId == draggedCard.cardId}, Level Match: {targetCharacter.Level == draggedCard.level}");
                    GameManager.Instance.WasCardDroppedOnTower = false; // 업그레이드 실패
                }

                // 드래그 상태 초기화는 CardInteractionHandler에서 최종적으로 처리
                // GameManager.Instance.DraggedCardData = null;
            }
            else
            {
                Debug.Log($"[UnitMergeTarget] Dragged card or target character is null. DraggedCard: {draggedCard != null}, TargetCharacter: {targetCharacter != null}");
                GameManager.Instance.WasCardDroppedOnTower = false; // 드롭 실패
            }
        }
    }
}
