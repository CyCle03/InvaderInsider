using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using InvaderInsider.Data;
using InvaderInsider.UI;
using System; 
using InvaderInsider.Managers;

namespace InvaderInsider.Cards
{
    public class CardManager : MonoBehaviour
    {
        private const string LOG_TAG = "CardManager";

        private static CardManager instance;
        private static readonly object _lock = new object();
        private static bool isQuitting = false;

        public static CardManager Instance
        {
            get
            {
                if (isQuitting) return null;
                lock (_lock)
                {
                    if (instance == null)
                    {
                        instance = FindObjectOfType<CardManager>();
                        if (instance == null)
                        {
                            GameObject go = new GameObject("CardManager");
                            instance = go.AddComponent<CardManager>();
                            DontDestroyOnLoad(go);
                        }
                    }
                    return instance;
                }
            }
        }

        [Header("Card Database")]
        [SerializeField] private CardDatabase cardDatabase;

        // 메모리 내에서 관리되는 플레이어 소유 카드 목록
        private List<int> ownedCardIds = new List<int>();

        // 핸드(소유 카드) 변경 시 발생하는 이벤트
        public event Action<List<int>> OnHandCardsChanged;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void Initialize()
        {
            if (cardDatabase == null)
            {
                cardDatabase = Resources.Load<CardDatabase>("CardDatabase");
            }
            LoadCards();
        }

        // 게임 시작 시 SaveDataManager로부터 카드 데이터를 로드
        public void LoadCards()
        {
            if (SaveDataManager.Instance != null && SaveDataManager.Instance.CurrentSaveData != null)
            {
                ownedCardIds = new List<int>(SaveDataManager.Instance.CurrentSaveData.deckData.cardIds);
                LogManager.Log($"{LOG_TAG} SaveDataManager로부터 카드 로드 완료: {ownedCardIds.Count}장");
                OnHandCardsChanged?.Invoke(ownedCardIds);
            }
            else
            {
                LogManager.LogWarning($"{LOG_TAG} SaveDataManager 인스턴스 또는 CurrentSaveData가 null입니다. 카드 로드 건너뜀.");
            }
        }

        // 스테이지 클리어 등 특정 시점에 SaveDataManager에 카드 데이터를 저장
        public void SaveCards()
        {
            if (SaveDataManager.Instance != null)
            {
                SaveDataManager.Instance.SetOwnedCards(ownedCardIds);
                SaveDataManager.Instance.SaveGameData(); // 즉시 파일에 저장
                LogManager.Log($"{LOG_TAG} 카드 저장 완료: {ownedCardIds.Count}장");
            }
        }

        public void Summon()
        {
            int summonCost = 10 + SaveDataManager.Instance.CurrentSaveData.progressData.summonCount;
            if (SaveDataManager.Instance.CurrentSaveData.progressData.currentEData > summonCost)
            {
                SaveDataManager.Instance.CurrentSaveData.progressData.summonCount++;
                SaveDataManager.Instance.UpdateEData(-summonCost, true);
                List<CardDBObject> randomCards = SelectRandomCards(3);
                DisplaySummonChoices(randomCards);
            }
        }

        private List<CardDBObject> SelectRandomCards(int count)
        {
            List<CardDBObject> result = new List<CardDBObject>();
            if (cardDatabase == null || cardDatabase.AllCards.Count == 0) return result;

            // Filter for level 1 cards only
            List<CardDBObject> availableCards = cardDatabase.AllCards.Where(card => card.level == 1).ToList();

            if (availableCards.Count == 0)
            {
                LogManager.LogWarning($"{LOG_TAG} No level 1 cards found in the database for summoning.");
                return result;
            }

            System.Random rng = new System.Random();

            for (int i = 0; i < count && availableCards.Count > 0; i++)
            {
                int index = rng.Next(availableCards.Count);
                result.Add(availableCards[index]);
                availableCards.RemoveAt(index);
            }
            return result;
        }

        private void DisplaySummonChoices(List<CardDBObject> choices)
        {
            var summonChoicePanel = UIManager.Instance?.GetPanel("SummonChoice") as SummonChoicePanel;
            if (summonChoicePanel != null)
            {
                summonChoicePanel.SetupCards(choices);
                UIManager.Instance?.ShowPanel("SummonChoice");
            }
            else
            {
                LogManager.LogError($"{LOG_TAG} SummonChoicePanel을 찾을 수 없습니다.");
            }
        }

        public void OnCardChoiceSelected(CardDBObject selectedCard)
        {
            var summonChoicePanel = FindObjectOfType<SummonChoicePanel>();
            if (summonChoicePanel != null)
            {
                UIManager.Instance?.HidePanel("SummonChoice");
            }

            if (selectedCard != null)
            {
                // eData 차감
                if (SaveDataManager.Instance != null)
                {
                    SaveDataManager.Instance.UpdateEData(-selectedCard.cost, true);
                    LogManager.Log($"{LOG_TAG} eData {selectedCard.cost} 차감됨. 현재 eData: {SaveDataManager.Instance.CurrentSaveData.progressData.currentEData}");
                }
                else
                {
                    LogManager.LogError($"{LOG_TAG} SaveDataManager 인스턴스를 찾을 수 없어 eData를 차감할 수 없습니다.");
                }

                AddCardToHand(selectedCard.cardId);
            }
        }

        #region Card Data Access
        public CardDBObject GetCardById(int cardId)
        {
            return cardDatabase?.GetCardById(cardId);
        }

        public CardDBObject GetUpgradedCard(CardDBObject card)
        {
            if (card == null) return null;
            return cardDatabase?.AllCards.FirstOrDefault(c => c.cardId == card.cardId && c.level == card.level + 1);
        }

        public List<CardDBObject> GetAllCards() => cardDatabase.AllCards;
        #endregion

        #region Hand Management (In-Memory)
        public List<int> GetHandCardIds()
        {
            return new List<int>(ownedCardIds);
        }

        public List<CardDBObject> GetHandCards()
        {
            var handCards = new List<CardDBObject>();
            foreach (int cardId in ownedCardIds)
            {
                var cardData = GetCardById(cardId);
                if (cardData != null)
                {
                    handCards.Add(cardData);
                }
            }
            return handCards;
        }

        public void PerformMerge(int oldCardId, int newCardId)
        {
            // Remove two instances of the old card
            ownedCardIds.Remove(oldCardId);
            ownedCardIds.Remove(oldCardId);

            // Add one instance of the new card
            ownedCardIds.Add(newCardId);

            LogManager.Log($"Merge successful: 2x ID {oldCardId} -> 1x ID {newCardId}");

            // Notify listeners that the hand has changed
            OnHandCardsChanged?.Invoke(ownedCardIds);
        }

        public void AddCardToHand(int cardId)
        {
            ownedCardIds.Add(cardId);
            LogManager.Log($"{LOG_TAG} 카드가 핸드에 추가되었습니다: ID {cardId}");
            LogManager.Log($"{LOG_TAG} Firing OnHandCardsChanged event.");
            OnHandCardsChanged?.Invoke(ownedCardIds);
        }

        public void RemoveCardFromHand(int cardId)
        {
            if (ownedCardIds.Remove(cardId))
            {
                LogManager.Log($"{LOG_TAG} 카드가 핸드에서 제거되었습니다: ID {cardId}");
                OnHandCardsChanged?.Invoke(ownedCardIds);
            }
        }

        public bool IsCardInHand(int cardId)
        {
            return ownedCardIds.Contains(cardId);
        }

        public int GetHandCardCount()
        {
            return ownedCardIds.Count;
        }

        public void ClearHand()
        {
            ownedCardIds.Clear();
            OnHandCardsChanged?.Invoke(ownedCardIds);
            LogManager.Log($"{LOG_TAG} 핸드의 모든 카드를 제거했습니다. 현재 카드 수: {ownedCardIds.Count}");
        }
        #endregion
    }
}