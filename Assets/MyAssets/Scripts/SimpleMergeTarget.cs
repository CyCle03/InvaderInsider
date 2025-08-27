using UnityEngine;
using UnityEngine.EventSystems;

namespace InvaderInsider
{
    /// <summary>
    /// 새로운 통합 시스템을 사용하는 간단한 머지 타겟
    /// </summary>
    [RequireComponent(typeof(BaseCharacter))]
    public class SimpleMergeTarget : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private BaseCharacter character;
        
        private void Awake()
        {
            character = GetComponent<BaseCharacter>();
            
            // Collider 설정
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
                boxCol.isTrigger = true;
            }
            else
            {
                col.isTrigger = true;
            }
        }
        
        public void OnDrop(PointerEventData eventData)
        {
            // 통합 시스템에서 현재 드래그 타입 확인
            if (DragAndMergeSystem.Instance.IsCardDragging)
            {
                // 카드 드롭 처리는 통합 시스템에서 자동으로 처리됨
                return;
            }
            
            if (DragAndMergeSystem.Instance.IsUnitDragging)
            {
                // 유닛 드롭 처리는 통합 시스템에서 자동으로 처리됨
                return;
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            // 시각적 피드백 (선택사항)
            if (DragAndMergeSystem.Instance.IsDragging)
            {
                // 하이라이트 효과 등을 추가할 수 있음
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            // 시각적 피드백 제거 (선택사항)
        }
    }
}