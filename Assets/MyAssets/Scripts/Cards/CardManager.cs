using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using InvaderInsider.Data;
using InvaderInsider.UI;
using System;
using System.Collections; // Required for coroutine
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

        // Hand is now represented by a list of composite keys "{cardId}_{level}"
        private List<string> ownedCardKeys = new List<string>();

        // Event now passes the list of keys
        public event Action<List<string>> OnHandCardsChanged;

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

        // LoadCards now migrates old data format
        public void LoadCards()
        {
            if (SaveDataManager.Instance != null && SaveDataManager.Instance.CurrentSaveData != null)
            {
                // For backward compatibility, assume loaded IDs are for level 1 cards.
                // This will convert the List<int> from save data to the new List<string> format.
                ownedCardKeys = SaveDataManager.Instance.CurrentSaveData.deckData.cardIds
                                .Select(id => $"{id}_1")
                                .ToList();
                LogManager.Log($"{LOG_TAG} Cards loaded and migrated: {ownedCardKeys.Count} cards.");
                OnHandCardsChanged?.Invoke(ownedCardKeys);
            }
            else
            {
                LogManager.LogWarning($"{LOG_TAG} SaveDataManager instance or CurrentSaveData is null. Skipping card load.");
            }
        }

        // SaveCards now converts back to the old format, losing level info.
        // TODO: Update SaveDataManager to handle List<string> to properly save levels.
        public void SaveCards()
        { 
            if (SaveDataManager.Instance != null)
            {
                var idsToSave = ownedCardKeys.Select(key => {
                    var parts = key.Split('_');
                    return int.Parse(parts[0]);
                }).ToList();

                SaveDataManager.Instance.SetOwnedCards(idsToSave);
                SaveDataManager.Instance.SaveGameData();
                LogManager.Log($"{LOG_TAG} Card levels saved (NOTE: Level info is lost in current save format).");
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

                AddCardToHand(selectedCard);
            }
        }

        #region Card Data Access
        public CardDBObject GetCardById(int cardId)
        {
            return cardDatabase?.GetCardById(cardId);
        }

        public CardDBObject GetCard(int cardId, int level)
        {
            return cardDatabase?.GetCard(cardId, level);
        }

        public CardDBObject GetUpgradedCard(CardDBObject card)
        {
            if (card == null) return null;
            return cardDatabase?.AllCards.FirstOrDefault(c => c.cardId == card.cardId && c.level == card.level + 1);
        }

        public List<CardDBObject> GetAllCards() => cardDatabase.AllCards;
        #endregion

        #region Hand Management (In-Memory)

        public List<string> GetHandCardKeys()
        {
            return new List<string>(ownedCardKeys);
        }

        public List<CardDBObject> GetHandCards()
        {
            var handCards = new List<CardDBObject>();
            foreach (string key in ownedCardKeys)
            {
                var parts = key.Split('_');
                if (parts.Length == 2 && int.TryParse(parts[0], out int cardId) && int.TryParse(parts[1], out int level))
                {
                    var cardData = GetCard(cardId, level);
                    if (cardData != null)
                    {
                        handCards.Add(cardData);
                    }
                }
            }
            return handCards;
        }

        public void PerformMerge(CardDBObject card1, CardDBObject card2)
        {
            StartCoroutine(PerformMergeEndOfFrame(card1, card2));
        }

        private IEnumerator PerformMergeEndOfFrame(CardDBObject card1, CardDBObject card2)
        {
            // Wait until all input events of the current frame are processed
            yield return new WaitForEndOfFrame();

            var upgradedCard = GetUpgradedCard(card1);
            if (upgradedCard == null)
            {
                LogManager.LogWarning($"{LOG_TAG} No upgrade path found for {card1.cardName}.");
                yield break;
            }

            string key1 = $"{card1.cardId}_{card1.level}";
            string key2 = $"{card2.cardId}_{card2.level}";
            string newKey = $"{upgradedCard.cardId}_{upgradedCard.level}";

            ownedCardKeys.Remove(key1);
            ownedCardKeys.Remove(key2);
            ownedCardKeys.Add(newKey);

            LogManager.Log($"Merge successful: {key1} + {key2} -> {newKey}");
            OnHandCardsChanged?.Invoke(ownedCardKeys);
        }

        public void AddCardToHand(CardDBObject card)
        {
            if (card == null) return;
            string key = $"{card.cardId}_{card.level}";
            ownedCardKeys.Add(key);
            LogManager.Log($"{LOG_TAG} Card added to hand: {key}");
            OnHandCardsChanged?.Invoke(ownedCardKeys);
        }

        public void RemoveCardFromHand(CardDBObject card)
        {
            if (card == null) return;
            string key = $"{card.cardId}_{card.level}";
            if (ownedCardKeys.Remove(key))
            {
                LogManager.Log($"{LOG_TAG} Card removed from hand: {key}");
                OnHandCardsChanged?.Invoke(ownedCardKeys);
            }
        }

        public int GetHandCardCount()
        {
            return ownedCardKeys.Count;
        }

        public void ClearHand()
        {
            ownedCardKeys.Clear();
            OnHandCardsChanged?.Invoke(ownedCardKeys);
            LogManager.Log($"{LOG_TAG} Hand cleared.");
        }
        #endregion
    }
}
