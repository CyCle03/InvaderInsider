using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using InvaderInsider.Cards; // CardData, CardDBObject 참조를 위해 추가
using InvaderInsider.Data; // CardDBObject 참조를 위해 추가
using UnityEngine.Events; // UnityEvent를 사용하기 위해 추가
using InvaderInsider; // BaseCharacter, Tower 참조를 위해 추가

namespace InvaderInsider.UI
{
    public enum CardPlacementResult
    {
        Success_Place,          // 카드가 성공적으로 배치됨
        Success_Upgrade,        // 카드가 성공적으로 업그레이드됨
        Failed_InvalidZone,     // 유효하지 않은 존에 드롭됨
        Failed_AlreadyExists,   // 이미 같은 카드가 존재하지만 업그레이드 불가능 (다른 등급이거나 조건 불충족)
        Failed_InvalidTarget,   // 장비 카드의 대상이 유효하지 않음
        Failed_OtherReason      // 기타 실패
    }

    public class CardDropZone : MonoBehaviour, IDropHandler
    {
        [SerializeField] private bool isPlayableZone = false; // 이 드롭 존이 카드를 낼 수 있는 필드인지
        [SerializeField] private Transform cardPlacementParent; // 카드가 놓여질 부모 Transform (선택 사항)
        [SerializeField] private List<CardDisplay> placedCards = new List<CardDisplay>(); // 이 존에 현재 놓여있는 카드들

        // 카드가 성공적으로 필드에 놓였을 때 발생 (새로운 카드 배치)
        public UnityEvent<InvaderInsider.Data.CardDBObject> OnCardSuccessfullyPlayed = new UnityEvent<InvaderInsider.Data.CardDBObject>();
        // 카드가 성공적으로 업그레이드되었을 때 발생 (새로운 카드 데이터, 업그레이드된 기존 카드 Display)
        public UnityEvent<InvaderInsider.Data.CardDBObject, CardDisplay> OnCardSuccessfullyUpgraded = new UnityEvent<InvaderInsider.Data.CardDBObject, CardDisplay>();

        public bool IsPlayableZone => isPlayableZone;
        public Transform CardPlacementParent => cardPlacementParent;

        public void OnDrop(PointerEventData eventData)
        {
            // CardInteractionHandler에서 드롭 로직을 처리할 것이므로 여기서는 추가적인 로직 불필요
        }

        // 카드를 놓으려 할 때 호출되는 메서드
        public CardPlacementResult TryPlaceCard(CardDisplay droppedCardDisplay)
        {
            if (!isPlayableZone) return CardPlacementResult.Failed_InvalidZone; // 플레이 불가능한 존이면 실패

            InvaderInsider.Data.CardDBObject droppedCardData = droppedCardDisplay.GetCardData(); // 완전한 네임스페이스 명시

            // 장비 아이템 처리 (EquipmentTargetType에 따라)
            if (droppedCardData.type == CardType.Equipment)
            {
                Debug.Log($"Equipment card {droppedCardData.cardName} dropped. Target type: {droppedCardData.equipmentTarget}");
                
                bool applied = false;
                // 현재 존에 캐릭터/타워 카드가 있는지 확인
                foreach (var placedCard in placedCards)
                {
                    InvaderInsider.Data.CardDBObject placedCardData = placedCard.GetCardData(); // 완전한 네임스페이스 명시

                    if (placedCardData.type == CardType.Character && droppedCardData.equipmentTarget == EquipmentTargetType.Character)
                    {
                        BaseCharacter character = placedCard.GetComponent<BaseCharacter>(); // 가정: BaseCharacter가 CardDisplay와 같은 GameObject에 있음
                        if (character != null)
                        {
                            character.ApplyEquipment(droppedCardData);
                            Debug.Log($"Equipment {droppedCardData.cardName} applied to Character {placedCardData.cardName}.");
                            applied = true; 
                            break; // 적용 후 루프 종료
                        }
                    }
                    else if (placedCardData.type == CardType.Tower && droppedCardData.equipmentTarget == EquipmentTargetType.Tower)
                    {
                        Tower tower = placedCard.GetComponent<Tower>(); // 가정: Tower가 CardDisplay와 같은 GameObject에 있음
                        if (tower != null)
                        {
                            tower.ApplyEquipment(droppedCardData);
                            Debug.Log($"Equipment {droppedCardData.cardName} applied to Tower {placedCardData.cardName}.");
                            applied = true; 
                            break; // 적용 후 루프 종료
                        }
                    }
                }
                if (applied)
                {
                    OnCardSuccessfullyPlayed.Invoke(droppedCardData); // 장비도 플레이로 간주
                    return CardPlacementResult.Success_Place;
                }
                else return CardPlacementResult.Failed_InvalidTarget; // 적용 가능한 대상이 없는 경우
            }
            else if (droppedCardData.type == CardType.Character || droppedCardData.type == CardType.Tower)
            {
                // 캐릭터/타워 카드 처리 (업그레이드 또는 신규 배치)
                foreach (var placedCard in placedCards)
                {
                    InvaderInsider.Data.CardDBObject existingCardData = placedCard.GetCardData(); // 완전한 네임스페이스 명시

                    // 같은 ID의 카드가 존재하고 등급이 같으면 업그레이드
                    if (existingCardData.cardId == droppedCardData.cardId)
                    {
                        if (existingCardData.rarity == droppedCardData.rarity)
                        {
                            Debug.Log($"Card {droppedCardData.cardName} upgraded. (Old: {existingCardData.rarity}, New: {droppedCardData.rarity})");
                            placedCard.SetupCard(droppedCardData); // 기존 카드의 비주얼을 업그레이드된 데이터로 업데이트
                            OnCardSuccessfullyUpgraded.Invoke(droppedCardData, placedCard); // 업그레이드 이벤트 발생
                            return CardPlacementResult.Success_Upgrade;
                        }
                        else
                        {
                            // 같은 카드인데 등급이 다른 경우 (업그레이드 불가능)
                            Debug.Log($"Card {droppedCardData.cardName} already exists, but cannot upgrade with different rarity.");
                            return CardPlacementResult.Failed_AlreadyExists;
                        }
                    }
                }
                // 기존 같은 카드가 없는 경우, 새로운 카드 배치
                Debug.Log($"New card {droppedCardData.cardName} placed.");
                placedCards.Add(droppedCardDisplay); // 새로운 카드 추가
                OnCardSuccessfullyPlayed.Invoke(droppedCardData); // 배치 이벤트 발생
                return CardPlacementResult.Success_Place;
            }
            return CardPlacementResult.Failed_OtherReason; // 기타 타입의 카드 (예: Spell)는 여기에 로직 추가
        }

        // 존에서 카드를 제거할 때 호출 (예: 카드가 필드를 떠날 때)
        public void RemoveCard(CardDisplay cardToRemove)
        {
            if (placedCards.Contains(cardToRemove))
            {
                placedCards.Remove(cardToRemove);
                Debug.Log($"Card {cardToRemove.GetCardData()?.cardName} removed from drop zone.");
            }
        }
    }
} 