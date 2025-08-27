using UnityEngine;
using UnityEngine.EventSystems;
using InvaderInsider.Managers;
using System.Collections.Generic; // Required for Dictionary

namespace InvaderInsider
{
    [RequireComponent(typeof(BaseCharacter))]
    public class DraggableUnit : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private BaseCharacter draggedCharacter;
        private Vector3 originalPosition;
        private Rigidbody unitRigidbody;

        // Store original layers to restore them after dragging
        private Dictionary<GameObject, int> originalLayers;
        private const int IGNORE_RAYCAST_LAYER = 2;

        private void Awake()
        {
            InitializeDraggableUnit();
        }

        private void Start()
        {
            // Awake에서 컴포넌트를 찾지 못한 경우 Start에서 다시 시도
            if (draggedCharacter == null)
            {
                InitializeDraggableUnit();
            }
        }

        private void InitializeDraggableUnit()
        {
            draggedCharacter = GetComponent<BaseCharacter>();
            unitRigidbody = GetComponent<Rigidbody>();

            // Store the original layer of this object and all its children
            originalLayers = new Dictionary<GameObject, int>();
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                originalLayers[t.gameObject] = t.gameObject.layer;
            }

            if (draggedCharacter == null)
            {
                Debug.LogWarning($"[DraggableUnit] {gameObject.name}에서 BaseCharacter 컴포넌트를 찾을 수 없습니다.");
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // 카드 드래그 중이면 유닛 드래그 차단
            if (GameManager.Instance.IsCardDragInProgress) 
            {
                eventData.pointerDrag = null;
                return;
            }

            // BaseCharacter 컴포넌트가 없거나 초기화되지 않았으면 드래그 불가
            if (draggedCharacter == null)
            {
                Debug.LogWarning($"[DraggableUnit] {gameObject.name}: BaseCharacter 컴포넌트가 없어 드래그할 수 없습니다.");
                eventData.pointerDrag = null;
                return;
            }

            if (!draggedCharacter.IsInitialized)
            {
                Debug.LogWarning($"[DraggableUnit] {gameObject.name}: 초기화되지 않은 캐릭터는 드래그할 수 없습니다.");
                eventData.pointerDrag = null;
                return;
            }

            originalPosition = transform.position;

            if (unitRigidbody != null)
            {
                unitRigidbody.isKinematic = true;
            }
            
            // Set layer to "Ignore Raycast" for the object and all children
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = IGNORE_RAYCAST_LAYER;
            }

            GameManager.Instance.DraggedUnit = draggedCharacter;
            Debug.Log($"[DraggableUnit] Started dragging: {draggedCharacter.gameObject.name} (ID: {draggedCharacter.CardId}, Level: {draggedCharacter.Level})");
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (draggedCharacter == null) return;

            Ray ray = Camera.main.ScreenPointToRay(eventData.position);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, GameManager.Instance.TileLayerMask))
            {
                transform.position = hit.point + new Vector3(0, GameManager.Instance.PlacementYOffset, 0);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (draggedCharacter == null) return;

            if (unitRigidbody != null)
            {
                unitRigidbody.isKinematic = false;
            }

            // Restore the original layers
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (originalLayers.ContainsKey(t.gameObject))
                {
                    t.gameObject.layer = originalLayers[t.gameObject];
                }
            }

            transform.position = originalPosition;

            if (GameManager.Instance.DroppedOnUnitTarget != null)
            {
                Debug.Log($"[DraggableUnit] Dropped on a valid target. Returned to original position as a fallback in case destruction fails.");
            }
            else
            {
                Debug.Log($"[DraggableUnit] Dropped on an invalid target or merge failed. Returned to original position.");
            }

            GameManager.Instance.DraggedUnit = null;
            GameManager.Instance.DroppedOnUnitTarget = null;

            Debug.Log($"[DraggableUnit] Ended dragging: {draggedCharacter.gameObject.name}");
        }
    }
}
