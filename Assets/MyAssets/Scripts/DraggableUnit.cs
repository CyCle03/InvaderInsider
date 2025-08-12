
using UnityEngine;
using UnityEngine.EventSystems;
using InvaderInsider.Managers;

namespace InvaderInsider
{
    [RequireComponent(typeof(BaseCharacter))]
    public class DraggableUnit : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private BaseCharacter draggedCharacter;
        private Vector3 originalPosition;
        private Collider unitCollider;
        private Rigidbody unitRigidbody;

        private void Awake()
        {
            draggedCharacter = GetComponent<BaseCharacter>();
            unitCollider = GetComponent<Collider>();
            unitRigidbody = GetComponent<Rigidbody>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (draggedCharacter == null) return;

            originalPosition = transform.position;

            // 드래그 중에는 물리 영향을 받지 않도록 설정
            if (unitRigidbody != null)
            {
                unitRigidbody.isKinematic = true;
            }
            // 드래그 중에는 레이캐스트에 걸리지 않도록 설정 (드롭 존이 감지할 수 있도록)
            if (unitCollider != null)
            {
                unitCollider.enabled = false;
            }

            // GameManager에 드래그 시작을 알림
            GameManager.Instance.DraggedUnit = draggedCharacter;

            Debug.Log($"[DraggableUnit] Started dragging: {draggedCharacter.gameObject.name}");
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (draggedCharacter == null) return;

            // 마우스 위치를 3D 월드 좌표로 변환
            Ray ray = Camera.main.ScreenPointToRay(eventData.position);
            RaycastHit hit;
            // 타일 레이어만 감지하도록 설정 (GameManager의 tileLayerMask 사용)
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, GameManager.Instance.TileLayerMask))
            {
                // 타일의 위치에 유닛을 배치 (Y축 오프셋 고려)
                transform.position = hit.point + new Vector3(0, GameManager.Instance.PlacementYOffset, 0);
            }
            else
            {
                // 타일이 감지되지 않으면 현재 위치 유지 또는 다른 처리
                // transform.position = originalPosition; // 또는 화면 밖으로 이동
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (draggedCharacter == null) return;

            // 드래그 종료 시 물리 설정 복원
            if (unitRigidbody != null)
            {
                unitRigidbody.isKinematic = false;
            }
            if (unitCollider != null)
            {
                unitCollider.enabled = true;
            }

            // 드롭 성공 여부 확인 (UnitMergeTarget에서 처리)
            BaseCharacter droppedOnTarget = GameManager.Instance.DroppedOnUnitTarget; // 드롭된 타겟 유닛
            if (droppedOnTarget != null) // 유닛에 드롭 성공
            {
                // UnitMergeTarget에서 이미 처리했으므로 여기서는 추가 로직 없음
                // 이 오브젝트는 UnitMergeTarget에서 파괴될 것임
                Debug.Log($"[DraggableUnit] Dropped on unit: {droppedOnTarget.gameObject.name}");
            }
            else // 유닛에 드롭 실패 (빈 타일 또는 유효하지 않은 곳)
            {
                // 마우스 위치에서 3D 월드로 레이캐스트 수행하여 드롭된 위치 확인
                Ray ray = Camera.main.ScreenPointToRay(eventData.position);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, GameManager.Instance.TileLayerMask))
                {
                    // 타일 위에 드롭되었지만 합쳐지지 않은 경우
                    Debug.Log($"[DraggableUnit] Dropped on tile {hit.collider.gameObject.name}, but no merge occurred. Returning to original position.");
                    transform.position = originalPosition; // 원래 위치로 되돌림
                }
                else
                {
                    // 타일이 아닌 다른 곳에 드롭된 경우
                    Debug.Log($"[DraggableUnit] Dropped on empty space or invalid target. Returning to original position.");
                    transform.position = originalPosition; // 원래 위치로 되돌림
                }
            }

            GameManager.Instance.DraggedUnit = null;
            GameManager.Instance.DroppedOnUnitTarget = null; // 플래그 초기화

            Debug.Log($"[DraggableUnit] Ended dragging: {draggedCharacter.gameObject.name}");
        }
    }
}
