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
        public event System.Action OnCardClicked;

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
                OnCardClicked?.Invoke();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Debug.Log($"[CardInteractionHandler] OnBeginDrag called for {gameObject.name}");
            if (cardDisplay?.GetCardData() == null) return;
            // GameManager를 통해 모델 미리보기 시작
            GameManager.Instance.StartPlacementPreview(cardDisplay.GetCardData());
        }

        public void OnDrag(PointerEventData eventData)
        {
            // GameManager가 미리보기 위치를 업데이트하므로 비워둡니다.
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // GameManager를 통해 배치 확정 또는 취소
            GameManager.Instance.ConfirmPlacement();
        }
    }
}

 