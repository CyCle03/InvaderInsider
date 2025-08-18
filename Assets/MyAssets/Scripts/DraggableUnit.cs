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
        private Collider[] unitColliders; // 배열로 변경
        private Rigidbody unitRigidbody;

        private void Awake()
        {
            draggedCharacter = GetComponent<BaseCharacter>();
            unitColliders = GetComponentsInChildren<Collider>(); // 모든 자식 콜라이더를 가져오도록 변경
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
            
            // 드래그 중에는 레이캐스트에 걸리지 않도록 모든 콜라이더 비활성화
            foreach (var col in unitColliders)
            {
                col.enabled = false;
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

            // 드래그 종료 시 모든 콜라이더 복원
            foreach (var col in unitColliders)
            {
                col.enabled = true;
            }

            // 드롭 성공 여부와 관계없이, 유닛이 파괴되지 않았다면 항상 원래 위치로 되돌아가도록 수정합니다.
            // 이는 UnitMergeTarget에서 유닛 파괴가 실패하는 예외적인 경우에도 유닛이 공중에 떠다니는 버그를 방지합니다.
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
            GameManager.Instance.DroppedOnUnitTarget = null; // 플래그 초기화

            Debug.Log($"[DraggableUnit] Ended dragging: {draggedCharacter.gameObject.name}");
        }
    }
}