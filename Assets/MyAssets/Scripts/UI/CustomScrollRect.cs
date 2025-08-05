using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InvaderInsider.UI
{
    public class CustomScrollRect : ScrollRect
    {
        private bool forbidDrag = false;

        public override void OnBeginDrag(PointerEventData eventData)
        {
            Debug.Log($"[CustomScrollRect] OnBeginDrag called. EventData.pointerPress: {eventData.pointerPress?.gameObject.name}, EventData.pointerDrag: {eventData.pointerDrag?.gameObject.name}");

            // 드래그 대상이 IDragHandler를 가지고 있는지 확인합니다.
            // 자기 자신(ScrollRect) 외의 다른 핸들러를 찾습니다.
            var dragHandlers = eventData.pointerPress.GetComponents<IDragHandler>();
            bool shouldPassEvent = false;
            foreach (var handler in dragHandlers)
            {
                if ((MonoBehaviour)handler != this)
                {
                    shouldPassEvent = true;
                    break;
                }
            }

            if (shouldPassEvent)
            {
                forbidDrag = true;
                // 이벤트를 다른 핸들러에게 전달합니다.
                ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.beginDragHandler);
            }
            else
            {
                forbidDrag = false;
                base.OnBeginDrag(eventData);
            }
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (forbidDrag)
            {
                ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.dragHandler);
            }
            else
            {
                base.OnDrag(eventData);
            }
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (forbidDrag)
            {
                ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.endDragHandler);
                forbidDrag = false;
            }
            else
            {
                base.OnEndDrag(eventData);
            }
        }
    }
}
