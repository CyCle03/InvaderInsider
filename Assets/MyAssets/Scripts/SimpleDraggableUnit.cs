using UnityEngine;
using UnityEngine.EventSystems;

namespace InvaderInsider
{
    /// <summary>
    /// 새로운 통합 시스템을 사용하는 간단한 드래그 가능 유닛
    /// </summary>
    [RequireComponent(typeof(BaseCharacter))]
    public class SimpleDraggableUnit : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private BaseCharacter character;
        
        private void Awake()
        {
            character = GetComponent<BaseCharacter>();
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            // 통합 시스템을 통해 드래그 시작
            bool success = DragAndMergeSystem.Instance.StartUnitDrag(character);
            
            if (!success)
            {
                // 드래그 시작 실패 시 이벤트 취소
                eventData.pointerDrag = null;
            }
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            // 통합 시스템을 통해 위치 업데이트
            DragAndMergeSystem.Instance.UpdateUnitDrag(eventData.position);
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            // 통합 시스템을 통해 드롭 시도
            bool success = DragAndMergeSystem.Instance.TryDropUnit(eventData.position);
            
            // 드래그 종료
            DragAndMergeSystem.Instance.EndUnitDrag();
        }
    }
}