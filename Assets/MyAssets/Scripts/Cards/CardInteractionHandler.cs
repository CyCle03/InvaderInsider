using UnityEngine;
using UnityEngine.EventSystems; // EventSystems 네임스페이스 추가
using InvaderInsider.Cards; // CardDisplay 참조를 위해 추가
using System; // Action 이벤트를 위해 추가
using InvaderInsider.UI; // CardDropZone 참조를 위해 추가
using UnityEngine.Events; // UnityEvent 참조를 위해 추가

namespace InvaderInsider.Cards
{
    [RequireComponent(typeof(RectTransform))] // RectTransform 요구
    [RequireComponent(typeof(CardDisplay))] // CardDisplay 요구
    public class CardInteractionHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private CardDisplay cardDisplay; // CardDisplay 컴포넌트 참조
        public event Action OnCardClicked; // 카드 클릭 이벤트

        // 카드의 플레이/업그레이드 상호작용 완료 시 발생 (HandDisplayPanel 등에서 구독하여 처리)
        public UnityEvent<CardDisplay, InvaderInsider.UI.CardPlacementResult> OnCardPlayInteractionCompleted = new UnityEvent<CardDisplay, InvaderInsider.UI.CardPlacementResult>();

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
            // 드래그 시작 시 원래 위치와 부모 저장
            originalParent = rectTransform.parent;
            originalLocalPosition = rectTransform.localPosition;

            // 드래그 중에는 Canvas의 최상위로 이동하여 다른 UI 위에 렌더링되도록 함
            rectTransform.SetParent(canvas.transform, true);
            cardDisplay.SetHighlight(false); // 드래그 시작 시 하이라이트 해제
        }

        public void OnDrag(PointerEventData eventData)
        {
            // 마우스 포인터를 따라 카드 이동
            if (rectTransform != null && canvas != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.GetComponent<RectTransform>(),
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint
                );
                rectTransform.localPosition = localPoint;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            GameObject droppedOn = eventData.pointerCurrentRaycast.gameObject;
            CardDropZone dropZone = null;
            CardPlacementResult result = CardPlacementResult.Failed_InvalidZone; // 기본값은 유효하지 않은 존

            if (droppedOn != null)
            {
                dropZone = droppedOn.GetComponent<CardDropZone>();
            }

            if (dropZone != null && dropZone.IsPlayableZone)
            {
                // 플레이 가능한 존에 드롭된 경우, TryPlaceCard 호출하여 결과 얻기
                result = dropZone.TryPlaceCard(cardDisplay); // cardDisplay는 이 핸들러가 붙은 카드
            }

            switch (result)
            {
                case CardPlacementResult.Success_Place:
                    // 성공적으로 배치된 경우
                    rectTransform.SetParent(dropZone.CardPlacementParent != null ? dropZone.CardPlacementParent : droppedOn.transform);
                    rectTransform.localPosition = Vector3.zero; // 필드의 중앙에 위치 (필요시 조정)
                    rectTransform.localScale = Vector3.one; // 크기 초기화
                    Debug.Log($"Card {cardDisplay?.GetCardData()?.cardName} successfully placed on playable zone: {droppedOn.name}");
                    break;

                case CardPlacementResult.Success_Upgrade:
                    // 성공적으로 업그레이드된 경우, 현재 드래그된 카드는 소모됨 (풀로 반환 또는 비활성화)
                    // 기존 카드는 CardDropZone에서 이미 처리됨
                    this.gameObject.SetActive(false); // 드래그된 카드 비활성화 (풀로 반환될 준비)
                    Debug.Log($"Card {cardDisplay?.GetCardData()?.cardName} successfully upgraded an existing card.");
                    break;

                case CardPlacementResult.Failed_InvalidZone:
                case CardPlacementResult.Failed_AlreadyExists:
                case CardPlacementResult.Failed_InvalidTarget:
                case CardPlacementResult.Failed_OtherReason:
                default:
                    // 실패한 경우, 원래 위치로 복귀
                    rectTransform.SetParent(originalParent);
                    rectTransform.localPosition = originalLocalPosition;
                    Debug.Log($"Card {cardDisplay?.GetCardData()?.cardName} returned to original position. Reason: {result}");
                    break;
            }

            rectTransform.SetAsLastSibling(); // 드래그 종료 시 렌더링 순서 재조정 (옵션)

            // 카드의 플레이/업그레이드 상호작용 완료 이벤트 발생
            OnCardPlayInteractionCompleted?.Invoke(cardDisplay, result);
        }
    }
} 