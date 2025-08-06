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
            using UnityEngine;
using UnityEngine.EventSystems;
using InvaderInsider.Data;
using InvaderInsider.Managers;
using InvaderInsider.Towers; // TowerDropZone 사용을 위해 추가

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
            // UI 상태를 먼저 원래대로 되돌립니다.
            if (gameObject != null)
            {
                transform.position = originalPosition;
                transform.SetParent(originalParent, true);
                canvasGroup.alpha = 1.0f;
                canvasGroup.blocksRaycasts = true;
            }

            bool droppedOnTower = false;
            RaycastHit hit;
            // 마우스 위치에서 3D 월드로 레이캐스트 수행
            Ray ray = Camera.main.ScreenPointToRay(eventData.position);
            if (Physics.Raycast(ray, out hit))
            {
                TowerDropZone dropZone = hit.collider.GetComponent<TowerDropZone>();
                if (dropZone != null)
                {
                    // TowerDropZone의 OnDrop 메서드를 수동으로 호출
                    dropZone.OnDrop(eventData);
                    // OnDrop이 성공적으로 업그레이드를 처리했는지 GameManager 플래그로 확인
                    droppedOnTower = GameManager.Instance.WasCardDroppedOnTower;
                }
            }

            if (droppedOnTower)
            {
                // 타워에 성공적으로 드롭(업그레이드)되었다면, UI 카드 아이콘을 파괴
                // (TowerDropZone.OnDrop에서 이미 처리될 수 있지만, 안전장치)
                if (eventData.pointerDrag != null)
                {
                    Destroy(eventData.pointerDrag.gameObject); // .gameObject 추가
                }
                GameManager.Instance.CancelPlacement(); // 배치 미리보기 제거
            }
            else
            {
                // 타워에 드롭되지 않았다면, 일반적인 배치 로직 진행
                GameManager.Instance.ConfirmPlacement();
            }
            
            // 드래그 상태 초기화
            GameManager.Instance.DraggedCardData = null;
            GameManager.Instance.WasCardDroppedOnTower = false; // 플래그 초기화
        }
    }
}
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

            // 타워에 드롭되지 않았을 때만 배치 로직 실행
            if (!GameManager.Instance.WasCardDroppedOnTower)
            {
                GameManager.Instance.ConfirmPlacement(); // 배치 확정 또는 취소
            }
            else
            {
                // 타워에 드롭된 경우 미리보기만 취소
                GameManager.Instance.CancelPlacement();
            }
            
            // 드래그 상태 초기화
            GameManager.Instance.DraggedCardData = null;
            GameManager.Instance.WasCardDroppedOnTower = false; // 플래그 초기화
        }
    }
}

 