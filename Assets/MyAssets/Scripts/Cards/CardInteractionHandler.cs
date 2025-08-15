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

            GameManager.Instance.DraggedCardData = cardDisplay.GetCardData();
            Debug.Log($"[CardInteractionHandler] OnBeginDrag - DraggedCardData set to: {GameManager.Instance.DraggedCardData?.cardName}");
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
            // UI 상태를 마지막에 원래대로 되돌립니다。
            if (gameObject != null)
            {
                transform.position = originalPosition;
                transform.SetParent(originalParent, true);
                canvasGroup.alpha = 1.0f;
                canvasGroup.blocksRaycasts = true;
            }

            bool droppedOnUnit = false;
            RaycastHit hit;
            // 마우스 위치에서 3D 월드로 레이캐스트 수행
            Ray ray = Camera.main.ScreenPointToRay(eventData.position);
            
            Debug.Log($"[CardInteractionHandler] Performing raycast from {ray.origin} in direction {ray.direction}");

            // We need to check what we hit. A single raycast is enough.
            // The layers should be set up so that Units are prioritized over Tiles if they occupy the same space.
            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log($"[CardInteractionHandler] Raycast hit: {hit.collider.gameObject.name} on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                
                UnitMergeTarget mergeTarget = hit.collider.GetComponent<UnitMergeTarget>();

                // Case 1: We directly hit a unit that can be merged with.
                if (mergeTarget != null)
                {
                    Debug.Log($"[CardInteractionHandler] UnitMergeTarget found directly on {hit.collider.gameObject.name}. Calling OnDrop.");
                    mergeTarget.OnDrop(eventData);
                    droppedOnUnit = GameManager.Instance.WasCardDroppedOnTower;
                }
                // Case 2: We hit something else (like a Tile). We don't check for nearby units.
                else
                {
                    Debug.Log($"[CardInteractionHandler] Raycast hit {hit.collider.gameObject.name}, which is not a UnitMergeTarget. Treating as placement attempt.");
                    // droppedOnUnit remains false
                }
            }
            else
            {
                Debug.Log("[CardInteractionHandler] Raycast hit nothing.");
            }

            if (droppedOnUnit)
            {
                // 유닛에 성공적으로 드롭(업그레이드)되었다면, UI 카드 아이콘을 파괴
                if (eventData.pointerDrag != null)
                {
                    Destroy(eventData.pointerDrag.gameObject); 
                }
                GameManager.Instance.CancelPlacement(); // 배치 미리보기 제거
            }
            else
            {
                // 유닛에 드롭되지 않았다면, 일반적인 배치 로직 진행
                Tile targetTile = hit.collider != null ? hit.collider.GetComponent<Tile>() : null;
                bool placementSuccessful = GameManager.Instance.ConfirmPlacement(targetTile);
                if (placementSuccessful)
                {
                    if (eventData.pointerDrag != null)
                    {
                        Destroy(eventData.pointerDrag.gameObject);
                    }
                }
            }
            
            // 드래그 상태 초기화
            GameManager.Instance.DraggedCardData = null;
            GameManager.Instance.WasCardDroppedOnTower = false;

            // 드롭 대상이 UI가 아니었다면 미리보기 취소
            if (!eventData.pointerEnter.GetComponentInParent<Canvas>())
            {
                GameManager.Instance.CancelPlacement();
            }
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

                    // 2. Remove the two old cards from hand
                    CardManager.Instance.RemoveCardFromHand(draggedCardData.cardId);
                    CardManager.Instance.RemoveCardFromHand(targetCardData.cardId);

                    // 3. Add the new upgraded card to hand
                    CardManager.Instance.AddCardToHand(upgradedCard.cardId);

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