
using UnityEngine;
using UnityEngine.EventSystems;
using InvaderInsider.Data;
using InvaderInsider.Managers;
using InvaderInsider.Cards;

namespace InvaderInsider.Towers
{
    [RequireComponent(typeof(Tower))]
    public class TowerDropZone : MonoBehaviour, IDropHandler
    {
        private Tower tower;

        private void Awake()
        {
            tower = GetComponent<Tower>();
        }

        public void OnDrop(PointerEventData eventData)
        {
            Debug.Log($"[TowerDropZone] OnDrop called on {gameObject.name}");
            CardDBObject draggedCard = GameManager.Instance.DraggedCardData;

            if (draggedCard != null && tower != null)
            {
                Debug.Log($"[TowerDropZone] Dragged Card: {draggedCard.cardName} (ID: {draggedCard.cardId}, Level: {draggedCard.level})");
                Debug.Log($"[TowerDropZone] Target Tower: {tower.gameObject.name} (ID: {tower.CardId}, Level: {tower.Level})");

                // 업그레이드 조건 확인: ID와 레벨이 모두 같아야 함
                if (tower.CardId == draggedCard.cardId && tower.Level == draggedCard.level)
                {
                    Debug.Log($"[TowerDropZone] Upgrade conditions met! Upgrading {tower.gameObject.name}.");
                    // 업그레이드 실행
                    tower.LevelUp();

                    // 카드 소모
                    CardManager.Instance.RemoveCardFromHand(draggedCard.cardId);

                    // 드래그하던 UI 오브젝트 제거
                    if (eventData.pointerDrag != null)
                    {
                        Destroy(eventData.pointerDrag);
                    }
                    GameManager.Instance.WasCardDroppedOnTower = true; // 성공적으로 타워에 드롭되었음을 알림
                }
                else
                {
                    Debug.Log($"[TowerDropZone] Upgrade conditions NOT met. ID Match: {tower.CardId == draggedCard.cardId}, Level Match: {tower.Level == draggedCard.level}");
                }

                // 드래그 상태 초기화
                GameManager.Instance.DraggedCardData = null;
            }
            else
            {
                Debug.Log($"[TowerDropZone] Dragged card or target tower is null. DraggedCard: {draggedCard != null}, TargetTower: {tower != null}");
            }
        }
    }
}
