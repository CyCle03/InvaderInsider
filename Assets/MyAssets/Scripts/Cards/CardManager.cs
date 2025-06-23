using UnityEngine;
using UnityEngine.UI;
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
            "카드 매니저 초기화 완료",
            "소환 진행 중입니다. 잠시 후 다시 시도하세요.",
            "리소스가 부족합니다. 필요 리소스: {0}, 현재 리소스: {1}",
            "소환 성공! 3장의 카드 중 선택하세요.",
            "소환 비용이 {0}으로 증가했습니다.",
            "카드가 핸드에 추가되었습니다: {0}",
            "카드 데이터베이스가 설정되지 않았습니다.",
            "카드 데이터베이스에 카드가 없습니다.",
            "소환 데이터 로드 완료. 소환 횟수: {0}, 현재 비용: {1}",
            "소환 데이터 저장 완료. 소환 횟수: {0}",
            "SaveDataManager가 없습니다."
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
        [SerializeField] private int drawCost = 10;
        [SerializeField] private float[] rarityRates = { 0.60f, 0.30f, 0.08f, 0.02f };  // Common, Rare, Epic, Legendary

        [Header("Summon Settings")]
        [SerializeField] private int initialSummonCost = 10;
        [SerializeField] private int summonCostIncrease = 1;

        private int currentSummonCost;
        private int summonCount = 0;
        private bool isSummonInProgress = false; // 소환 진행 중 플래그

        // Events
        public UnityEvent<CardDBObject> OnCardDrawn = new UnityEvent<CardDBObject>();

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                
                // HideFlags 명시적 설정 (에디터에서 편집 가능하도록)
                #if UNITY_EDITOR
                gameObject.hideFlags = HideFlags.None;
                #endif
                
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
                        Debug.LogError(LOG_PREFIX + LOG_MESSAGES[6]);
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
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[8], summonCount, currentSummonCost));
                #endif
            }
            else
            {
                summonCount = 0;
                currentSummonCost = initialSummonCost;
                #if UNITY_EDITOR
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[8], summonCount, currentSummonCost));
                #endif
            }
        }

        public void SaveSummonData()
        {
            if (SaveDataManager.Instance != null && SaveDataManager.Instance.CurrentSaveData != null)
            {
                SaveDataManager.Instance.CurrentSaveData.progressData.summonCount = summonCount;
                // 메모리에만 업데이트, 저장하지 않음 (스테이지 클리어/게임 종료 시에만 저장)
                #if UNITY_EDITOR
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[9], summonCount));
                #endif
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[10]);
                #endif
            }
        }

        public void Summon()
        {
            // 중복 호출 방지
            if (isSummonInProgress)
            {
                #if UNITY_EDITOR
                Debug.LogWarning(LOG_PREFIX + LOG_MESSAGES[1]);
                #endif
                return;
            }

            if (SaveDataManager.Instance == null)
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[10]);
                #endif
                return;
            }

            // ResourceManager 우선 사용, 없으면 SaveDataManager 사용
            var resourceManager = ResourceManager.Instance;
            int currentEData = resourceManager?.GetCurrentEData() ?? 
                              SaveDataManager.Instance?.GetCurrentEData() ?? 0;
            
            if (currentEData >= currentSummonCost)
            {
                isSummonInProgress = true; // 소환 시작
                
                // ResourceManager를 통한 EData 소모
                bool success = false;
                if (resourceManager != null)
                {
                    success = resourceManager.TrySpendEData(currentSummonCost);
                }
                else if (SaveDataManager.Instance != null)
                {
                    SaveDataManager.Instance.UpdateEDataWithoutSave(-currentSummonCost);
                    success = true;
                }
                
                if (!success)
                {
#if UNITY_EDITOR
                    Debug.LogError(LOG_PREFIX + LOG_MESSAGES[2]);
#endif
                    isSummonInProgress = false;
                    return;
                }

                // 소환 횟수 증가 및 다음 비용 계산
                summonCount++;
                currentSummonCost = initialSummonCost + summonCount * summonCostIncrease;

                #if UNITY_EDITOR
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[4], currentSummonCost));
                #endif

                // 랜덤 카드 3장 선택
                List<CardDBObject> randomCards = SelectRandomCards(3);
                
                // 선택된 카드들을 UI로 표시
                DisplaySummonChoices(randomCards); // UI 표시는 UIManager를 통해서만
            }
            else
            {
                #if UNITY_EDITOR
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[2], 
                    currentEData, currentSummonCost));
                #endif
            }
        }

        private List<CardDBObject> SelectRandomCards(int count)
        {
            List<CardDBObject> result = new List<CardDBObject>();
            if (cardDatabase == null || cardDatabase.AllCards.Count == 0) return result;

            List<CardDBObject> availableCards = new List<CardDBObject>(cardDatabase.AllCards);
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
            // UIManager를 통해 등록된 SummonChoice 패널 사용
            if (InvaderInsider.UI.UIManager.Instance != null && InvaderInsider.UI.UIManager.Instance.IsPanelRegistered("SummonChoice"))
            {
                // UIManager에서 SummonChoice 패널 가져오기
                var summonChoicePanel = InvaderInsider.UI.UIManager.Instance.GetPanel("SummonChoice") as SummonChoicePanel;
                if (summonChoicePanel != null)
                {
                    summonChoicePanel.SetupCards(choices);
                    InvaderInsider.UI.UIManager.Instance.ShowPanel("SummonChoice");
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + "UIManager를 통해 SummonChoice 패널을 표시했습니다.");
                    #endif
                    return;
                }
            }

            #if UNITY_EDITOR
            Debug.LogError(LOG_PREFIX + LOG_MESSAGES[7]);
            #endif
        }

        public void OnCardChoiceSelected(CardDBObject selectedCard)
        {
            // 소환 진행 플래그 해제
            isSummonInProgress = false;

            // UIManager를 통해 패널 숨기기
            if (InvaderInsider.UI.UIManager.Instance != null && InvaderInsider.UI.UIManager.Instance.IsPanelRegistered("SummonChoice"))
            {
                InvaderInsider.UI.UIManager.Instance.HidePanel("SummonChoice");
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "UIManager를 통해 SummonChoice 패널을 숨겼습니다.");
                #endif
            }
            
            if (SaveDataManager.Instance != null && selectedCard != null)
            {
                SaveDataManager.Instance.AddCardToHandAndOwned(selectedCard.cardId);
                
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[5] + $" '{selectedCard.cardName}'");
                #endif
            }
            
            OnCardDrawn?.Invoke(selectedCard);
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[5] + $" '{selectedCard.cardName}'");
            #endif
        }
        #endregion

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

            var cardsOfRarity = cardDatabase.AllCards.Where(card => card.rarity == selectedRarity).ToList();
            if (cardsOfRarity.Count == 0)
            {
                return cardDatabase.AllCards[0];
            }

            return cardsOfRarity[Random.Range(0, cardsOfRarity.Count)];
        }

        #region Card Database Access
        public CardDBObject GetCardById(int cardId)
        {
            if (cardId >= 0 && cardId < cardDatabase.AllCards.Count)
                return cardDatabase.AllCards[cardId];
            return null;
        }

        public List<CardDBObject> GetAllCards() => cardDatabase.AllCards;

        public List<CardDBObject> GetCardsByType(CardType type) =>
            cardDatabase.AllCards.Where(card => card.type == type).ToList();

        public List<CardDBObject> GetCardsByRarity(CardRarity rarity) =>
            cardDatabase.AllCards.Where(card => card.rarity == rarity).ToList();

        public void AddCard(CardDBObject card)
        {
            if (!cardDatabase.AllCards.Any(c => c.cardId == card.cardId))
            {
                cardDatabase.AddCard(card);
            }
        }

        public int GetDrawCost() => drawCost;
        public int GetCurrentSummonCost() => currentSummonCost;

        // 핸드 관련 편의 메서드들
        public List<int> GetHandCardIds()
        {
            if (SaveDataManager.Instance?.CurrentSaveData != null)
            {
                return SaveDataManager.Instance.CurrentSaveData.deckData.handCardIds;
            }
            return new List<int>();
        }

        public List<CardDBObject> GetHandCards()
        {
            var handCardIds = GetHandCardIds();
            var handCards = new List<CardDBObject>();
            
            foreach (int cardId in handCardIds)
            {
                var cardData = GetCardById(cardId);
                if (cardData != null)
                {
                    handCards.Add(cardData);
                }
            }
            return handCards;
        }

        public void RemoveCardFromHand(int cardId)
        {
            if (SaveDataManager.Instance != null)
            {
                SaveDataManager.Instance.RemoveCardFromHand(cardId);
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + $"카드가 핸드에서 제거되었습니다: ID {cardId}");
                #endif
            }
        }

        public bool IsCardInHand(int cardId)
        {
            if (SaveDataManager.Instance?.CurrentSaveData != null)
            {
                return SaveDataManager.Instance.CurrentSaveData.deckData.IsInHand(cardId);
            }
            return false;
        }

        public int GetHandCardCount()
        {
            return GetHandCardIds().Count;
        }
        #endregion

        // 디모/테스트 메서드들
        public void DemoAddRandomCardToHand()
        {
            if (cardDatabase != null && cardDatabase.AllCards.Count > 0)
            {
                var randomCard = cardDatabase.AllCards[UnityEngine.Random.Range(0, cardDatabase.AllCards.Count)];
                if (SaveDataManager.Instance != null)
                {
                    SaveDataManager.Instance.AddCardToHandAndOwned(randomCard.cardId);
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + LOG_MESSAGES[5] + $" '{randomCard.cardName}' (ID: {randomCard.cardId})");
                    #endif
                }
            }
        }

        public void DemoShowHandStatus()
        {
            if (SaveDataManager.Instance != null)
            {
                var handCards = GetHandCards();
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + $"현재 핸드 상태: {handCards.Count}장의 카드");
                foreach (var card in handCards)
                {
                    Debug.Log(LOG_PREFIX + $"- {card.cardName} (ID: {card.cardId}, 타입: {card.type}, 등급: {card.rarity})");
                }
                #endif
            }
        }
    }
} 