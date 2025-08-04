using UnityEngine;
using UnityEngine.EventSystems; // EventSystems 네임스페이스 추가
using UnityEngine.UI; // ScrollRect 참조를 위해 추가
using InvaderInsider.Cards; // CardDisplay 참조를 위해 추가
using System; // Action 이벤트를 위해 추가
using InvaderInsider.UI; // CardDropZone 참조를 위해 추가
using UnityEngine.Events; // UnityEvent 참조를 위해 추가
using InvaderInsider.Managers; // GameManager 참조를 위해 추가

namespace InvaderInsider.Cards
{
    [RequireComponent(typeof(CardDisplay))] // CardDisplay 요구
    public class CardInteractionHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private CardDisplay cardDisplay; // CardDisplay 컴포넌트 참조
        public event Action OnCardClicked; // 카드 클릭 이벤트

        private RectTransform rectTransform;
        private Canvas canvas;
        private ScrollRect parentScrollRect;
        private bool isDraggingCard = false; // 현재 카드 드래그 중인지, 아니면 스크롤 중인지 상태를 저장

        private void Awake()
        {
            cardDisplay = GetComponent<CardDisplay>();
            if (cardDisplay == null)
            {
                Debug.LogError("CardDisplay component not found on this GameObject!", this);
            }
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Canvas not found in parents! Dragging may not work correctly.", this);
            }
            parentScrollRect = GetComponentInParent<ScrollRect>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (cardDisplay != null)
            {
                cardDisplay.SetHighlight(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (cardDisplay != null)
            {
                cardDisplay.SetHighlight(false);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging == false)
            {
                OnCardClicked?.Invoke();
                Debug.Log($"Card Clicked: {cardDisplay?.GetCardData()?.cardName}");
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (parentScrollRect == null) return;

            // 드래그 방향을 확인합니다.
            if (Mathf.Abs(eventData.delta.x) < Mathf.Abs(eventData.delta.y))
            {
                // 수직 드래그가 더 크면 => 카드 배치 시작
                isDraggingCard = true;
                parentScrollRect.OnBeginDrag(eventData); // 스크롤뷰의 드래그 시작을 막기 위해 이벤트를 한번 넘겨주고 비활성화
                parentScrollRect.enabled = false;

                GameManager.Instance.StartPlacementPreview(cardDisplay.GetCardData());
            }
            else
            {
                // 수평 드래그가 더 크면 => 스크롤 시작
                isDraggingCard = false;
                parentScrollRect.OnBeginDrag(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDraggingCard && parentScrollRect != null)
            {
                // 스크롤 중일 때만 이벤트를 전달합니다.
                parentScrollRect.OnDrag(eventData);
            }
            // 카드 드래그 중에는 GameManager가 프리뷰를 움직이므로 아무것도 하지 않습니다.
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (isDraggingCard)
            {
                // 카드 배치 시도
                GameManager.Instance.ConfirmPlacement();
            }
            else if (parentScrollRect != null)
            {
                // 스크롤 종료
                parentScrollRect.OnEndDrag(eventData);
            }

            // 상태 초기화 및 스크롤뷰 활성화
            isDraggingCard = false;
            if (parentScrollRect != null) parentScrollRect.enabled = true;
        }
    }
} 