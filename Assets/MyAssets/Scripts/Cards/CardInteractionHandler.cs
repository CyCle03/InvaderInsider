using UnityEngine;
using UnityEngine.EventSystems;
using InvaderInsider.Data;
using InvaderInsider.Managers;

namespace InvaderInsider.Cards
{
    [RequireComponent(typeof(CardDisplay))]
    [RequireComponent(typeof(CanvasGroup))]
    public class CardInteractionHandler : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
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
            if (cardDisplay?.GetCardData() == null) return;

            originalPosition = transform.position;
            originalParent = transform.parent;

            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false;

            // 새로운 통합 시스템 사용
            DragAndMergeSystem.Instance.StartCardDrag(cardDisplay.GetCardData());
            Debug.Log($"[CardInteractionHandler] OnBeginDrag - 통합 시스템으로 카드 드래그 시작: {cardDisplay.GetCardData()?.cardName}");

            transform.SetParent(GetComponentInParent<Canvas>().transform, true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!DragAndMergeSystem.Instance.IsCardDragging) return;
            transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // Reset the visual state of the dragged card first.
            // This makes it snap back to the hand if no valid drop occurs.
            if (gameObject != null)
            {
                transform.position = originalPosition;
                transform.SetParent(originalParent, true);
                canvasGroup.alpha = 1.0f;
                canvasGroup.blocksRaycasts = true;
            }

            // Check if the drop occurred on a UI element.
            // The EventSystem calls OnDrop on the target before OnEndDrag on the source.
            // If we dropped on a UI element, we assume it was handled (or intentionally ignored) by the UI system.
            // We should not proceed with trying to place the card in the 3D world.
            if (eventData.pointerEnter != null && eventData.pointerEnter.layer == LayerMask.NameToLayer("UI"))
            {
                DragAndMergeSystem.Instance.EndCardDrag(); // Clean up the 3D preview
                Debug.Log($"[CardInteractionHandler] OnEndDrag: Drop was on UI layer. World placement aborted.");
                // If the drop was on a valid card target, OnDrop would have been called,
                // triggering a merge and a UI refresh that will destroy this card object.
                // If the drop was on another UI element, nothing happens, and the card has snapped back.
                return;
            }

            // If we reach here, the drop was not on a UI element.
            // Proceed with attempting to place the card in the 3D world.
            bool success = DragAndMergeSystem.Instance.TryPlaceCard(eventData.position);
            
            if (success)
            {
                // If placement in the world was successful, the card is consumed.
                // The CardManager will be notified and the UI will update.
                // We destroy the icon that was being dragged.
                if (eventData.pointerDrag != null)
                {
                    Destroy(eventData.pointerDrag.gameObject);
                }
            }

            // End the drag operation in the central system.
            DragAndMergeSystem.Instance.EndCardDrag();
            
            Debug.Log($"[CardInteractionHandler] OnEndDrag completed - world placement success: {success}");
        }

        public void OnDrop(PointerEventData eventData)
        {
            Debug.Log($"[CardInteractionHandler] OnDrop called on {gameObject.name}");

            GameObject draggedObject = eventData.pointerDrag;
            if (draggedObject == null) return;

            CardInteractionHandler draggedCardHandler = draggedObject.GetComponent<CardInteractionHandler>();
            if (draggedCardHandler == null) return;

            CardDBObject draggedCardData = draggedCardHandler.cardDisplay.GetCardData();
            CardDBObject targetCardData = this.cardDisplay.GetCardData();

            if (draggedCardData == null || targetCardData == null) return;

            // Prevent dropping on itself
            if (draggedCardHandler == this) return;

            Debug.Log($"[CardInteractionHandler] Dragged Card: {draggedCardData.cardName} (ID: {draggedCardData.cardId}, Level: {draggedCardData.level})");
            Debug.Log($"[CardInteractionHandler] Target Card: {targetCardData.cardName} (ID: {targetCardData.cardId}, Level: {targetCardData.level})");

            // Merge condition: same cardId and same level
            if (draggedCardData.cardId == targetCardData.cardId && draggedCardData.level == targetCardData.level)
            {
                Debug.Log("[CardInteractionHandler] Merge condition met!");

                // 1. Find the upgraded card
                CardDBObject upgradedCard = CardManager.Instance.GetUpgradedCard(draggedCardData);

                if (upgradedCard != null)
                {
                    Debug.Log($"[CardInteractionHandler] Found upgraded card: {upgradedCard.cardName} (ID: {upgradedCard.cardId}, Level: {upgradedCard.level})");

                    // Perform atomic merge operation
                    CardManager.Instance.PerformMerge(draggedCardData.cardId, upgradedCard.cardId);

                    // The OnHandDataChanged event will handle the UI update, so we don't need to do anything else here.
                    // The dragged card icon will be destroyed automatically.
                }
                else
                {
                    Debug.LogWarning($"[CardInteractionHandler] No card found for upgrade from {draggedCardData.cardName} (Level: {draggedCardData.level})");
                }
            }
            else
            {
                Debug.Log("[CardInteractionHandler] Merge condition not met.");
            }
        }
    }
}