using UnityEngine;
using UnityEngine.EventSystems;
using InvaderInsider.Cards;
using InvaderInsider.Managers;

namespace InvaderInsider.Cards
{
    [RequireComponent(typeof(CardDisplay))]
    public class CardInteractionHandler : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private CardDisplay cardDisplay;

        private void Awake()
        {
            cardDisplay = GetComponent<CardDisplay>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 드래그 중이 아닐 때만 클릭으로 간주합니다.
            if (!eventData.dragging)
            {
                Debug.Log($"Card Clicked: {cardDisplay?.GetCardData()?.cardName}");
                // TODO: 여기에 카드를 클릭했을 때 상세 정보 패널을 여는 로직을 추가할 수 있습니다.
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // 카드 아이콘 위에서 시작된 드래그는 항상 카드 배치로 취급합니다.
            if (cardDisplay?.GetCardData() == null) return;
            GameManager.Instance.StartPlacementPreview(cardDisplay.GetCardData());
        }

        public void OnDrag(PointerEventData eventData)
        {
            // 카드 배치 중에는 GameManager가 프리뷰 위치를 업데이트하므로 이 메소드는 비워둡니다.
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // 드래그가 끝나면 배치를 확정하거나 취소합니다.
            GameManager.Instance.ConfirmPlacement();
        }
    }
}

 