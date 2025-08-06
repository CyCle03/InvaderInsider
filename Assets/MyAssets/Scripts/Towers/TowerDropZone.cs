
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
            CardDBObject draggedCard = GameManager.Instance.DraggedCardData;

            if (draggedCard != null && tower != null)
            {
                // 업그레이드 조건 확인: ID와 레벨이 모두 같아야 함
                if (tower.CardId == draggedCard.cardId && tower.Level == draggedCard.level)
                {
                    // 업그레이드 실행
                    tower.LevelUp();

                    // 카드 소모
                    CardManager.Instance.RemoveCardFromHand(draggedCard.cardId);

                    // 드래그하던 UI 오브젝트 제거
                    if (eventData.pointerDrag != null)
                    {
                        Destroy(eventData.pointerDrag);
                    }
                }

                // 드래그 상태 초기화
                GameManager.Instance.DraggedCardData = null;
            }
        }
    }
}
