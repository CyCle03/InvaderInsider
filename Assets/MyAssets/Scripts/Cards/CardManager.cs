using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using InvaderInsider.Managers;
using InvaderInsider.Data;
using InvaderInsider.UI;

namespace InvaderInsider.Cards
{
    public class CardManager : MonoBehaviour
    {
        private const string LOG_PREFIX = "[CardManager] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "Card Database Scriptable Object is not assigned in the inspector!",
            "소환 데이터 로드: 횟수 = {0}, 비용 = {1}",
            "소환 데이터 없음: 횟수 = {0}, 비용 = {1}",
            "소환 데이터 저장: 횟수 = {0}",
            "SaveDataManager 인스턴스를 찾을 수 없습니다. 소환 데이터 저장 실패.",
            "SaveDataManager 인스턴스를 찾을 수 없습니다.",
            "eData 부족! 현재 eData: {0}, 필요 비용: {1}",
            "소환 성공! 현재 횟수: {0}, 다음 소환 비용: {1}",
            "Summon Choice Panel Prefab is not assigned!"
        };

        private static CardManager instance;
        private static readonly object _lock = new object();
        private static bool isQuitting = false;
        private static bool isInitialized = false;

        public static CardManager Instance
        {
            get
            {
                if (isQuitting) return null;

                lock (_lock)
                {
                    if (instance == null && !isQuitting)
                    {
                        instance = FindObjectOfType<CardManager>();
                        if (instance == null)
                        {
                            GameObject go = new GameObject("CardManager");
                            instance = go.AddComponent<CardManager>();
                            DontDestroyOnLoad(go);
                        }
                    }
                    
                    if (instance != null && !isInitialized)
                    {
                        instance.PerformInitialization();
                    }
                    
                    return instance;
                }
            }
        }

        [Header("Card Collections")]
        [SerializeField] private CardDatabase cardDatabase;
        
        [Header("Gacha Settings")]
        [SerializeField] private int singleDrawCost = 10;
        [SerializeField] private int multiDrawCost = 45;  // 5회 뽑기 (10% 할인)
        [SerializeField] private float[] rarityRates = { 0.60f, 0.30f, 0.08f, 0.02f };  // Common, Rare, Epic, Legendary

        [Header("Summon Settings")]
        [SerializeField] private int initialSummonCost = 10;
        [SerializeField] private int summonCostIncrease = 1;
        [SerializeField] private GameObject summonChoicePanelPrefab;

        private int currentSummonCost;
        private int summonCount = 0;
        private SummonChoicePanel currentSummonChoicePanel;

        // Events
        public UnityEvent<CardDBObject> OnCardDrawn = new UnityEvent<CardDBObject>();
        public UnityEvent<List<CardDBObject>> OnMultipleCardsDrawn = new UnityEvent<List<CardDBObject>>();

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                PerformInitialization();
                LoadSummonData();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void PerformInitialization()
        {
            if (cardDatabase == null)
            {
                cardDatabase = Resources.Load<CardDatabase>("CardDatabase");
                if (cardDatabase == null)
                {
                    cardDatabase = Resources.Load<CardDatabase>("ScriptableObjects/CardSystem/CardDatabase");
                    if (cardDatabase == null)
                    {
                        #if UNITY_EDITOR
                        Debug.LogError(LOG_PREFIX + LOG_MESSAGES[0]);
                        #endif
                        cardDatabase = ScriptableObject.CreateInstance<CardDatabase>();
                        #if UNITY_EDITOR
                        Debug.LogWarning(LOG_PREFIX + "빈 CardDatabase를 생성했습니다. Inspector에서 올바른 CardDatabase를 할당하세요.");
                        #endif
                    }
                }
            }
            
            isInitialized = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                if (currentSummonChoicePanel != null)
                {
                    Destroy(currentSummonChoicePanel.gameObject);
                    currentSummonChoicePanel = null;
                }
                instance = null;
                isInitialized = false;
            }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        #region Summon System
        public void LoadSummonData()
        {
            if (SaveDataManager.Instance != null && SaveDataManager.Instance.CurrentSaveData != null)
            {
                summonCount = SaveDataManager.Instance.CurrentSaveData.progressData.summonCount;
                currentSummonCost = initialSummonCost + summonCount * summonCostIncrease;
                #if UNITY_EDITOR
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[1], summonCount, currentSummonCost));
                #endif
            }
            else
            {
                summonCount = 0;
                currentSummonCost = initialSummonCost;
                #if UNITY_EDITOR
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[2], summonCount, currentSummonCost));
                #endif
            }
        }

        public void SaveSummonData()
        {
            if (SaveDataManager.Instance != null && SaveDataManager.Instance.CurrentSaveData != null)
            {
                SaveDataManager.Instance.CurrentSaveData.progressData.summonCount = summonCount;
                SaveDataManager.Instance.SaveGameData();
                #if UNITY_EDITOR
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[3], summonCount));
                #endif
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[4]);
                #endif
            }
        }

        public void Summon()
        {
            if (SaveDataManager.Instance == null)
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[5]);
                #endif
                return;
            }

            if (SaveDataManager.Instance.CurrentSaveData.progressData.currentEData >= currentSummonCost)
            {
                // 소환 시에는 저장하지 않고 eData만 감소 (스테이지 클리어 시 저장됨)
                SaveDataManager.Instance.UpdateEDataWithoutSave(-currentSummonCost);
                summonCount++;
                currentSummonCost = initialSummonCost + summonCount * summonCostIncrease;
                #if UNITY_EDITOR
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[7], summonCount, currentSummonCost));
                #endif

                List<CardDBObject> selectedCards = SelectRandomCards(3);
                DisplaySummonChoices(selectedCards);
            }
            else
            {
                #if UNITY_EDITOR
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[6], 
                    SaveDataManager.Instance.CurrentSaveData.progressData.currentEData, 
                    currentSummonCost));
                #endif
            }
        }

        private List<CardDBObject> SelectRandomCards(int count)
        {
            List<CardDBObject> result = new List<CardDBObject>();
            if (cardDatabase == null || cardDatabase.cards.Count == 0) return result;

            List<CardDBObject> availableCards = new List<CardDBObject>(cardDatabase.cards);
            float totalWeight = availableCards.Sum(card => card.summonWeight);

            System.Random rng = new System.Random();

            for (int i = 0; i < count && availableCards.Count > 0 && totalWeight > 0; i++)
            {
                float randomWeight = (float)rng.NextDouble() * totalWeight;
                float currentWeight = 0f;

                for (int j = 0; j < availableCards.Count; j++)
                {
                    currentWeight += availableCards[j].summonWeight;
                    if (randomWeight <= currentWeight)
                    {
                        result.Add(availableCards[j]);
                        totalWeight -= availableCards[j].summonWeight;
                        availableCards.RemoveAt(j);
                        break;
                    }
                }
            }

            return result;
        }

        private void DisplaySummonChoices(List<CardDBObject> choices)
        {
            if (summonChoicePanelPrefab == null)
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[8]);
                #endif
                return;
            }

            if (currentSummonChoicePanel != null)
            {
                Destroy(currentSummonChoicePanel.gameObject);
            }

            GameObject panelObj = Instantiate(summonChoicePanelPrefab);
            currentSummonChoicePanel = panelObj.GetComponent<SummonChoicePanel>();
            if (currentSummonChoicePanel != null)
            {
                currentSummonChoicePanel.Initialize(choices);
            }
        }

        public void OnCardChoiceSelected(CardDBObject selectedCard)
        {
            if (currentSummonChoicePanel != null)
            {
                Destroy(currentSummonChoicePanel.gameObject);
                currentSummonChoicePanel = null;
            }
            
            // 선택된 카드를 이벤트로 전달
            OnCardDrawn?.Invoke(selectedCard);
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + $"플레이어가 카드를 선택했습니다: {selectedCard.cardName}");
            #endif
            
            SaveSummonData();
        }
        #endregion

        #region Gacha System
        public bool DrawSingleCard()
        {
            if (!GameManager.Instance.TrySpendEData(singleDrawCost))
            {
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "Not enough eData to draw a card!");
                #endif
                return false;
            }

            CardDBObject drawnCard = DrawCardByRarity();
            OnCardDrawn?.Invoke(drawnCard);
            return true;
        }

        public bool DrawMultipleCards(int count = 5)
        {
            if (!GameManager.Instance.TrySpendEData(multiDrawCost))
            {
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "Not enough eData to draw multiple cards!");
                #endif
                return false;
            }

            List<CardDBObject> drawnCards = new List<CardDBObject>();
            for (int i = 0; i < count; i++)
            {
                drawnCards.Add(DrawCardByRarity());
            }

            OnMultipleCardsDrawn?.Invoke(drawnCards);
            return true;
        }

        private CardDBObject DrawCardByRarity()
        {
            float randomValue = Random.value;
            float currentProbability = 0f;
            CardRarity selectedRarity = CardRarity.Common;

            for (int i = 0; i < rarityRates.Length; i++)
            {
                currentProbability += rarityRates[i];
                if (randomValue <= currentProbability)
                {
                    selectedRarity = (CardRarity)i;
                    break;
                }
            }

            var cardsOfRarity = cardDatabase.cards.Where(card => card.rarity == selectedRarity).ToList();
            if (cardsOfRarity.Count == 0)
            {
                #if UNITY_EDITOR
                Debug.LogWarning(LOG_PREFIX + $"No cards found for rarity: {selectedRarity}");
                #endif
                return cardDatabase.cards[0];
            }

            return cardsOfRarity[Random.Range(0, cardsOfRarity.Count)];
        }
        #endregion

        #region Card Database Access
        public CardDBObject GetCardById(int cardId)
        {
            if (cardId >= 0 && cardId < cardDatabase.cards.Count)
                return cardDatabase.cards[cardId];
            return null;
        }

        public List<CardDBObject> GetAllCards() => cardDatabase.cards;

        public List<CardDBObject> GetCardsByType(CardType type) =>
            cardDatabase.cards.Where(card => card.type == type).ToList();

        public List<CardDBObject> GetCardsByRarity(CardRarity rarity) =>
            cardDatabase.cards.Where(card => card.rarity == rarity).ToList();

        public void AddCard(CardDBObject card)
        {
            if (!cardDatabase.cards.Any(c => c.cardId == card.cardId))
            {
                cardDatabase.cards.Add(card);
            }
        }

        public int GetSingleDrawCost() => singleDrawCost;
        public int GetMultiDrawCost() => multiDrawCost;
        public int GetCurrentSummonCost() => currentSummonCost;
        #endregion
    }
} 