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
            draggedCharacter = GetComponent<BaseCharacter>();
            unitRigidbody = GetComponent<Rigidbody>();

            // Store the original layer of this object and all its children
            originalLayers = new Dictionary<GameObject, int>();
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                originalLayers[t.gameObject] = t.gameObject.layer;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (GameManager.Instance.IsCardDragInProgress) 
            {
                eventData.pointerDrag = null;
                return;
            }

            if (draggedCharacter == null) return;

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
            Debug.Log($"[DraggableUnit] Started dragging: {draggedCharacter.gameObject.name}");
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
