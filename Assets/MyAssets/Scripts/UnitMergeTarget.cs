using UnityEngine;
using UnityEngine.EventSystems;
using InvaderInsider.Data;
using InvaderInsider.Managers;
using InvaderInsider.Cards;

namespace InvaderInsider
{
    public class UnitMergeTarget : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private void Awake()
        {
            Debug.Log($"[UnitMergeTarget] Awake called on {gameObject.name}");
            
            // Collider가 있는지 확인하고 없으면 추가
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                // BoxCollider 추가
                BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
                boxCol.isTrigger = true;
                Debug.Log($"[UnitMergeTarget] {gameObject.name}에 BoxCollider를 추가했습니다.");
            }
            else
            {
                // 기존 Collider가 있으면 isTrigger 설정
                col.isTrigger = true;
                Debug.Log($"[UnitMergeTarget] {gameObject.name}의 기존 Collider를 isTrigger=true로 설정했습니다.");
            }
        }

        private void Start()
        {
            Debug.Log($"[UnitMergeTarget] Start called on {gameObject.name}");
            
            // 컴포넌트 상태 확인
            Collider col = GetComponent<Collider>();
            BaseCharacter character = GetComponent<BaseCharacter>();
            
            Debug.Log($"[UnitMergeTarget] {gameObject.name} 상태 - " +
                     $"Collider: {col != null}, " +
                     $"IsTrigger: {col?.isTrigger}, " +
                     $"BaseCharacter: {character != null}, " +
                     $"Layer: {gameObject.layer}");
        }

        public void OnDrop(PointerEventData eventData)
        {
            Debug.Log($"[UnitMergeTarget] *** OnDrop called on {gameObject.name}! ***");
            
            CardDBObject draggedCard = GameManager.Instance.DraggedCardData;
            BaseCharacter draggedUnit = GameManager.Instance.DraggedUnit;

            Debug.Log($"[UnitMergeTarget] OnDrop - DraggedCardData: {(draggedCard != null ? draggedCard.cardName : "null")}, DraggedUnit: {(draggedUnit != null ? draggedUnit.gameObject.name : "null")}");

            BaseCharacter targetCharacter = GetComponent<BaseCharacter>();
            if (targetCharacter == null)
            {
                Debug.LogError($"[UnitMergeTarget] {gameObject.name} requires a BaseCharacter component. OnDrop cannot proceed.");
                return; // 타겟 캐릭터가 없으면 처리하지 않음
            }

            // 1. 카드 아이콘을 드래그한 경우
            if (draggedCard != null)
            {
                Debug.Log($"[UnitMergeTarget] OnDrop called on {gameObject.name} with a Card.");
                Debug.Log($"[UnitMergeTarget] Dragged Card: {draggedCard.cardName} (ID: {draggedCard.cardId}, Level: {draggedCard.level})");
                
                if (!targetCharacter.IsInitialized)
                {
                    Debug.LogWarning($"[UnitMergeTarget] Target character {targetCharacter.gameObject.name} is not initialized. Cannot upgrade with card.");
                    GameManager.Instance.WasCardDroppedOnTower = false; // 업그레이드 실패
                    return;
                }

                Debug.Log($"[UnitMergeTarget] Target Character: {targetCharacter.gameObject.name} (ID: {targetCharacter.CardId}, Level: {targetCharacter.Level})");

                // 업그레이드 조건 확인: ID와 레벨이 모두 같아야 함
                if (targetCharacter.CardId == draggedCard.cardId && targetCharacter.Level == draggedCard.level)
                {
                    Debug.Log($"[UnitMergeTarget] Upgrade conditions met! Upgrading {targetCharacter.gameObject.name} with card.");
                    targetCharacter.LevelUp();

                    // 카드 소모
                    CardManager.Instance.RemoveCardFromHand(draggedCard.cardId);

                    GameManager.Instance.WasCardDroppedOnTower = true; // 성공적으로 유닛에 드롭되었음을 알림
                }
                else
                {
                    Debug.Log($"[UnitMergeTarget] Upgrade conditions NOT met for card. ID Match: {targetCharacter.CardId == draggedCard.cardId}, Level Match: {targetCharacter.Level == draggedCard.level}");
                    GameManager.Instance.WasCardDroppedOnTower = false; // 업그레이드 실패
                }
            }
            // 2. 유닛을 드래그한 경우
            else if (draggedUnit != null && draggedUnit != targetCharacter) // 자기 자신에게 드롭하는 것 방지
            {
                Debug.Log($"[UnitMergeTarget] OnDrop called on {gameObject.name} with a Unit.");
                Debug.Log($"[UnitMergeTarget] Dragged Unit: {draggedUnit.gameObject.name} (ID: {draggedUnit.CardId}, Level: {draggedUnit.Level})");
                Debug.Log($"[UnitMergeTarget] Target Unit: {targetCharacter.gameObject.name} (ID: {targetCharacter.CardId}, Level: {targetCharacter.Level})");

                if (!draggedUnit.IsInitialized || !targetCharacter.IsInitialized)
                {
                    Debug.LogWarning($"[UnitMergeTarget] Dragged or target unit is not initialized. Cannot merge.");
                    GameManager.Instance.DroppedOnUnitTarget = null; // 드롭 실패
                    return;
                }

                // 합치기 조건 확인: ID와 레벨이 모두 같아야 함
                if (draggedUnit.CardId == targetCharacter.CardId && draggedUnit.Level == targetCharacter.Level)
                {
                    Debug.Log($"[UnitMergeTarget] Merge conditions met! Merging {draggedUnit.gameObject.name} into {targetCharacter.gameObject.name}.");
                    targetCharacter.LevelUp();
                    Destroy(draggedUnit.gameObject); // 드래그된 유닛 파괴
                    GameManager.Instance.DroppedOnUnitTarget = targetCharacter; // 성공적으로 드롭되었음을 알림
                }
                else
                {
                    Debug.Log($"[UnitMergeTarget] Merge conditions NOT met for units. ID Match: {draggedUnit.CardId == targetCharacter.CardId}, Level Match: {draggedUnit.Level == targetCharacter.Level}");
                    GameManager.Instance.DroppedOnUnitTarget = null; // 드롭 실패
                }
            }
            else
            {
                Debug.Log($"[UnitMergeTarget] No valid dragged item (card or unit) or target character is null.");
                GameManager.Instance.WasCardDroppedOnTower = false; // 카드 드롭 실패
                GameManager.Instance.DroppedOnUnitTarget = null; // 유닛 드롭 실패
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Debug.Log($"[UnitMergeTarget] OnPointerEnter called on {gameObject.name}");
            
            // 드래그 중인 유닛이 있으면 잠재적 드롭 타겟으로 설정
            if (GameManager.Instance.DraggedUnit != null)
            {
                Debug.Log($"[UnitMergeTarget] Potential drop target: {gameObject.name}");
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Debug.Log($"[UnitMergeTarget] OnPointerExit called on {gameObject.name}");
        }

        // OnTriggerEnter를 사용한 대체 드롭 감지
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[UnitMergeTarget] OnTriggerEnter with {other.name}");
            
            // 드래그 중인 유닛이 트리거에 들어왔는지 확인
            BaseCharacter draggedUnit = GameManager.Instance.DraggedUnit;
            if (draggedUnit != null && other.gameObject == draggedUnit.gameObject)
            {
                Debug.Log($"[UnitMergeTarget] Dragged unit entered trigger area of {gameObject.name}");
                
                // 수동으로 드롭 처리 시뮬레이션
                SimulateDropEvent();
            }
        }

        private void SimulateDropEvent()
        {
            Debug.Log($"[UnitMergeTarget] Simulating drop event on {gameObject.name}");
            
            // 가짜 PointerEventData 생성
            PointerEventData fakeEventData = new PointerEventData(EventSystem.current);
            
            // OnDrop 직접 호출
            OnDrop(fakeEventData);
        }
    }
}