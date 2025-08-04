using UnityEngine;
using UnityEngine.EventSystems; // EventSystems 네임스페이스 추가
using InvaderInsider.Cards; // CardDisplay 참조를 위해 추가
using System; // Action 이벤트를 위해 추가
using InvaderInsider.UI; // CardDropZone 참조를 위해 추가
using UnityEngine.Events; // UnityEvent 참조를 위해 추가
using InvaderInsider.Managers; // GameManager 참조를 위해 추가

namespace InvaderInsider.Cards
{
    [RequireComponent(typeof(RectTransform))] // RectTransform 요구
    [RequireComponent(typeof(CardDisplay))] // CardDisplay 요구
    public class CardInteractionHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private CardDisplay cardDisplay; // CardDisplay 컴포넌트 참조
        public event Action OnCardClicked; // 카드 클릭 이벤트

        

        private RectTransform rectTransform; // 카드 UI의 RectTransform
        private Canvas canvas; // 상위 Canvas 참조 (드래그 위치 계산용)
        private Transform originalParent; // 드래그 시작 시 원래 부모
        private Vector3 originalLocalPosition; // 드래그 시작 시 원래 로컬 위치

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
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // 마우스 오버 시 하이라이트 활성화
            if (cardDisplay != null)
            {
                cardDisplay.SetHighlight(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // 마우스 아웃 시 하이라이트 비활성화
            if (cardDisplay != null)
            {
                cardDisplay.SetHighlight(false);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 카드 클릭 이벤트 발생 (드래그가 발생하지 않았을 경우에만 클릭으로 처리)
            if (eventData.dragging == false)
            {
                OnCardClicked?.Invoke();
                Debug.Log($"Card Clicked: {cardDisplay?.GetCardData()?.cardName}");
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (cardDisplay == null || cardDisplay.GetCardData() == null) return;

            // GameManager에 프리뷰 생성 요청
            GameManager.Instance.StartPlacementPreview(cardDisplay.GetCardData());

            // 드래그 중에도 카드 UI는 그대로 보이도록 비활성화 코드를 제거합니다.
        }

        public void OnDrag(PointerEventData eventData)
        {
            // 이 메소드는 이제 GameManager가 처리하므로 비워둡니다.
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // GameManager에 배치 확정 요청
            bool success = GameManager.Instance.ConfirmPlacement();

            // 성공 시 GameManager가 CardManager를 통해 핸드에서 카드를 제거하고 UI를 갱신합니다.
            // 실패 시에는 프리뷰만 사라지고 핸드의 카드는 그대로 유지됩니다.
            // 따라서 이 메소드에서는 추가적인 처리가 필요 없습니다.
        }
    }
} 