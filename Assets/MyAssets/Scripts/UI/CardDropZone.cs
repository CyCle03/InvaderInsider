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
        private const string LOG_PREFIX = "[UI] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "CardDrop: Equipment card {0} dropped. Target type: {1}",
            "CardDrop: Equipment {0} applied to Character {1}",
            "CardDrop: Equipment {0} applied to Tower {1}",
            "CardDrop: Card {0} upgraded. (Old: {1}, New: {2})",
            "CardDrop: Card {0} already exists, but cannot upgrade with different rarity",
            "CardDrop: New card {0} placed",
            "CardDrop: Card {0} removed from drop zone"
        };

        [SerializeField] private bool isPlayableZone = false; // 이 드롭 존이 카드를 낼 수 있는 필드인지
        [SerializeField] private Transform cardPlacementParent; // 카드가 놓여질 부모 Transform (선택 사항)
        [SerializeField] private List<CardDisplay> placedCards = new List<CardDisplay>(); // 이 존에 현재 놓여있는 카드들

        // 카드가 성공적으로 필드에 놓였을 때 발생 (새로운 카드 배치)
        public UnityEvent<CardDBObject> OnCardSuccessfullyPlayed = new UnityEvent<CardDBObject>();
        // 카드가 성공적으로 업그레이드되었을 때 발생 (새로운 카드 데이터, 업그레이드된 기존 카드 Display)
        public UnityEvent<CardDBObject, CardDisplay> OnCardSuccessfullyUpgraded = new UnityEvent<CardDBObject, CardDisplay>();

        public bool IsPlayableZone => isPlayableZone;
        public Transform CardPlacementParent => cardPlacementParent;

        private void OnDestroy()
        {
            // C# 이벤트는 메모리 누수 방지를 위해 수동 해제
            OnCardSuccessfullyPlayed.RemoveAllListeners();
            OnCardSuccessfullyUpgraded.RemoveAllListeners();
            placedCards.Clear();
        }

        public void OnDrop(PointerEventData eventData)
        {
            // CardInteractionHandler에서 드롭 로직을 처리할 것이므로 여기서는 추가적인 로직 불필요
        }

        // 카드를 놓으려 할 때 호출되는 메서드
        public CardPlacementResult TryPlaceCard(CardDisplay droppedCardDisplay)
        {
            if (!isPlayableZone || droppedCardDisplay == null) return CardPlacementResult.Failed_InvalidZone;

            CardDBObject droppedCardData = droppedCardDisplay.GetCardData();
            if (droppedCardData == null) return CardPlacementResult.Failed_OtherReason;

            if (droppedCardData.type == CardType.Equipment)
            {
                if (Application.isPlaying)
                {
                    Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[0], droppedCardData.cardName, droppedCardData.equipmentTarget));
                }

                bool applied = false;
                foreach (var placedCard in placedCards)
                {
                    if (placedCard == null) continue;

                    CardDBObject placedCardData = placedCard.GetCardData();
                    if (placedCardData == null) continue;

                    if (placedCardData.type == CardType.Character && droppedCardData.equipmentTarget == EquipmentTargetType.Character)
                    {
                        BaseCharacter character = placedCard.GetComponent<BaseCharacter>();
                        if (character != null)
                        {
                            character.ApplyEquipment(droppedCardData);
                            if (Application.isPlaying)
                            {
                                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[1], droppedCardData.cardName, placedCardData.cardName));
                            }
                            applied = true;
                            break;
                        }
                    }
                    else if (placedCardData.type == CardType.Tower && droppedCardData.equipmentTarget == EquipmentTargetType.Tower)
                    {
                        Tower tower = placedCard.GetComponent<Tower>();
                        if (tower != null)
                        {
                            tower.ApplyEquipment(droppedCardData);
                            if (Application.isPlaying)
                            {
                                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[2], droppedCardData.cardName, placedCardData.cardName));
                            }
                            applied = true;
                            break;
                        }
                    }
                }

                if (applied)
                {
                    OnCardSuccessfullyPlayed.Invoke(droppedCardData);
                    return CardPlacementResult.Success_Place;
                }
                return CardPlacementResult.Failed_InvalidTarget;
            }
            else if (droppedCardData.type == CardType.Character || droppedCardData.type == CardType.Tower)
            {
                foreach (var placedCard in placedCards)
                {
                    if (placedCard == null) continue;

                    CardDBObject existingCardData = placedCard.GetCardData();
                    if (existingCardData == null) continue;

                    if (existingCardData.cardId == droppedCardData.cardId)
                    {
                        if (existingCardData.rarity == droppedCardData.rarity)
                        {
                            if (Application.isPlaying)
                            {
                                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[3], droppedCardData.cardName, existingCardData.rarity, droppedCardData.rarity));
                            }
                            placedCard.SetupCard(droppedCardData);
                            OnCardSuccessfullyUpgraded.Invoke(droppedCardData, placedCard);
                            return CardPlacementResult.Success_Upgrade;
                        }
                        else
                        {
                            if (Application.isPlaying)
                            {
                                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[4], droppedCardData.cardName));
                            }
                            return CardPlacementResult.Failed_AlreadyExists;
                        }
                    }
                }

                if (Application.isPlaying)
                {
                    Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[5], droppedCardData.cardName));
                }
                placedCards.Add(droppedCardDisplay);
                OnCardSuccessfullyPlayed.Invoke(droppedCardData);
                return CardPlacementResult.Success_Place;
            }

            return CardPlacementResult.Failed_OtherReason;
        }

        // 존에서 카드를 제거할 때 호출 (예: 카드가 필드를 떠날 때)
        public void RemoveCard(CardDisplay cardToRemove)
        {
            if (cardToRemove == null || !placedCards.Contains(cardToRemove)) return;

            placedCards.Remove(cardToRemove);
            if (Application.isPlaying)
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[6], cardToRemove.GetCardData()?.cardName ?? "Unknown"));
            }
        }
    }
} 