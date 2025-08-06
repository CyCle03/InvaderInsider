using UnityEngine;
using UnityEngine.EventSystems;
using InvaderInsider.Data;
using InvaderInsider.Managers;

namespace InvaderInsider.Cards
{
    [RequireComponent(typeof(CardDisplay))]
    [RequireComponent(typeof(CanvasGroup))]
    public class CardInteractionHandler : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private CardDisplay cardDisplay;
        private CanvasGroup canvasGroup;
        private Transform originalParent;
        private Vector3 originalPosition;

        public event System.Action OnCardClicked;

        private void Awake()
        {
            cardDisplay = GetComponent<CardDisplay>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Initialize(Transform parent)
        {
            originalParent = parent;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!eventData.dragging)
            {
                Debug.Log($"Card Clicked: {cardDisplay?.GetCardData()?.cardName}");
                OnCardClicked?.Invoke();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Debug.Log($"[CardInteractionHandler] OnBeginDrag called for {cardDisplay?.GetCardData()?.cardName}");
            if (cardDisplay?.GetCardData() == null) return;

            originalPosition = transform.position;
            originalParent = transform.parent;

            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false;

            GameManager.Instance.DraggedCardData = cardDisplay.GetCardData();
            GameManager.Instance.StartPlacementPreview(cardDisplay.GetCardData()); // 미리보기 시작

            transform.SetParent(GetComponentInParent<Canvas>().transform, true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (GameManager.Instance.DraggedCardData == null) return;
            transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (gameObject != null)
            {
                transform.position = originalPosition;
                transform.SetParent(originalParent, true);
                canvasGroup.alpha = 1.0f;
                canvasGroup.blocksRaycasts = true;
            }

            GameManager.Instance.ConfirmPlacement(); // 배치 확정 또는 취소
            GameManager.Instance.DraggedCardData = null;
        }
    }
}

 