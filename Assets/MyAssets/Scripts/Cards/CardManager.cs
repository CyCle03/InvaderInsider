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
        private bool isSummonInProgress = false; // 소환 진행 중 플래그

        // Events
        public UnityEvent<CardDBObject> OnCardDrawn = new UnityEvent<CardDBObject>();
        public UnityEvent<List<CardDBObject>> OnMultipleCardsDrawn = new UnityEvent<List<CardDBObject>>();

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
            // 중복 호출 방지
            if (isSummonInProgress)
            {
                #if UNITY_EDITOR
                Debug.LogWarning(LOG_PREFIX + "소환이 이미 진행 중입니다. 중복 호출을 무시합니다.");
                #endif
                return;
            }

            if (SaveDataManager.Instance == null)
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[5]);
                #endif
                return;
            }

            if (SaveDataManager.Instance.CurrentSaveData.progressData.currentEData >= currentSummonCost)
            {
                isSummonInProgress = true; // 소환 시작
                
                // 소환 시에는 저장하지 않고 eData만 감소 (스테이지 클리어 시 저장됨)
                SaveDataManager.Instance.UpdateEDataWithoutSave(-currentSummonCost);

                // 소환 횟수 증가 및 다음 비용 계산
                summonCount++;
                currentSummonCost = initialSummonCost + summonCount * summonCostIncrease;

                #if UNITY_EDITOR
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[7], summonCount, currentSummonCost));
                #endif

                // 랜덤 카드 3장 선택
                List<CardDBObject> randomCards = SelectRandomCards(3);
                
                // 카드 선택 UI 표시
                DisplaySummonChoices(randomCards);
            }
            else
            {
                #if UNITY_EDITOR
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[6], 
                    SaveDataManager.Instance.CurrentSaveData.progressData.currentEData, currentSummonCost));
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
            // UIManager를 통해 등록된 SummonChoice 패널 사용
            if (InvaderInsider.UI.UIManager.Instance != null && InvaderInsider.UI.UIManager.Instance.IsPanelRegistered("SummonChoice"))
            {
                // 등록된 패널을 직접 가져와서 사용
                var summonChoicePanel = InvaderInsider.UI.UIManager.Instance.GetPanel("SummonChoice") as SummonChoicePanel;
                if (summonChoicePanel != null)
                {
                    currentSummonChoicePanel = summonChoicePanel;
                    currentSummonChoicePanel.SetupCards(choices);
                    InvaderInsider.UI.UIManager.Instance.ShowPanel("SummonChoice");
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + "UIManager를 통해 SummonChoice 패널을 표시했습니다.");
                    #endif
                    return;
                }
            }
            
            // 백업: UIManager에 등록되지 않은 경우 씬에서 직접 찾기
            if (currentSummonChoicePanel == null)
            {
                currentSummonChoicePanel = FindObjectOfType<SummonChoicePanel>(true);
            }
            
            if (currentSummonChoicePanel != null)
            {
                currentSummonChoicePanel.SetupCards(choices);
                currentSummonChoicePanel.Show();
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "씬에서 SummonChoice 패널을 찾아 표시했습니다.");
                #endif
                return;
            }
            
            // 최후의 백업: 동적 생성
            #if UNITY_EDITOR
            Debug.LogWarning(LOG_PREFIX + "SummonChoice 패널을 찾을 수 없어 동적 생성을 시도합니다.");
            #endif
            
            if (summonChoicePanelPrefab == null)
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[8]);
                #endif
                return;
            }

            // 적절한 Canvas 찾기
            Canvas targetCanvas = null;
            Canvas[] allCanvases = FindObjectsOfType<Canvas>();
            
            foreach (Canvas canvas in allCanvases)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    string canvasName = canvas.name.ToLower();
                    if (!canvasName.Contains("topbar") && !canvasName.Contains("bottombar"))
                    {
                        targetCanvas = canvas;
                        break;
                    }
                }
            }
            
            if (targetCanvas != null)
            {
                GameObject panelObj = Instantiate(summonChoicePanelPrefab, targetCanvas.transform);
                currentSummonChoicePanel = panelObj.GetComponent<SummonChoicePanel>();
                if (currentSummonChoicePanel != null)
                {
                    currentSummonChoicePanel.SetupCards(choices);
                    currentSummonChoicePanel.Show();
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + $"Canvas({targetCanvas.name})에 소환 선택 패널을 동적 생성했습니다.");
                    #endif
                }
            }
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
            else if (currentSummonChoicePanel != null)
            {
                // 백업: 동적 생성된 패널 제거
                currentSummonChoicePanel.Hide();
                Destroy(currentSummonChoicePanel.gameObject);
                currentSummonChoicePanel = null;
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "동적 생성된 소환 선택 패널을 제거했습니다.");
                #endif
            }
            
            // 패널 참조 정리
            currentSummonChoicePanel = null;
            
            // 선택된 카드를 이벤트로 전달 및 핸드에 추가
            if (selectedCard != null)
            {
                // 핸드에 카드 추가 (SaveDataManager를 통해)
                if (SaveDataManager.Instance != null)
                {
                    SaveDataManager.Instance.AddCardToHandAndOwned(selectedCard.cardId);
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + $"카드가 핸드에 추가되었습니다: {selectedCard.cardName} (ID: {selectedCard.cardId})");
                    #endif
                }
                else
                {
                    #if UNITY_EDITOR
                    Debug.LogError(LOG_PREFIX + "SaveDataManager 인스턴스가 없어 카드를 핸드에 추가할 수 없습니다.");
                    #endif
                }

                OnCardDrawn?.Invoke(selectedCard);
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + $"플레이어가 카드를 선택했습니다: {selectedCard.cardName}");
                #endif
            }
            else
            {
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "카드 선택이 취소되었습니다.");
                #endif
            }
            
            SaveSummonData();
        }

        public void ShowSummonChoicePanel()
        {
            if (currentSummonChoicePanel != null)
            {
                // UIManager를 통해 패널 보이기
                if (InvaderInsider.UI.UIManager.Instance != null && InvaderInsider.UI.UIManager.Instance.IsPanelRegistered("SummonChoice"))
                {
                    InvaderInsider.UI.UIManager.Instance.ShowPanel("SummonChoice");
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + "UIManager를 통해 SummonChoice 패널을 다시 표시했습니다.");
                    #endif
                }
                else
                {
                    currentSummonChoicePanel.Show();
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + "SummonChoice 패널을 다시 표시했습니다.");
                    #endif
                }
            }
        }

        public void HideSummonChoicePanel()
        {
            if (currentSummonChoicePanel != null)
            {
                // UIManager를 통해 패널 숨기기
                if (InvaderInsider.UI.UIManager.Instance != null && InvaderInsider.UI.UIManager.Instance.IsPanelRegistered("SummonChoice"))
                {
                    InvaderInsider.UI.UIManager.Instance.HidePanel("SummonChoice");
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + "UIManager를 통해 SummonChoice 패널을 임시로 숨겼습니다.");
                    #endif
                }
                else
                {
                    currentSummonChoicePanel.Hide();
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + "SummonChoice 패널을 임시로 숨겼습니다.");
                    #endif
                }
            }
        }

        public bool IsSummonChoicePanelActive()
        {
            if (currentSummonChoicePanel == null) return false;
            
            if (InvaderInsider.UI.UIManager.Instance != null && InvaderInsider.UI.UIManager.Instance.IsPanelRegistered("SummonChoice"))
            {
                return InvaderInsider.UI.UIManager.Instance.IsPanelActive("SummonChoice");
            }
            
            return currentSummonChoicePanel.gameObject.activeInHierarchy;
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
            if (cardDatabase != null && cardDatabase.cards.Count > 0)
            {
                var randomCard = cardDatabase.cards[UnityEngine.Random.Range(0, cardDatabase.cards.Count)];
                if (SaveDataManager.Instance != null)
                {
                    SaveDataManager.Instance.AddCardToHandAndOwned(randomCard.cardId);
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + $"디모: 랜덤 카드를 핸드에 추가했습니다: {randomCard.cardName} (ID: {randomCard.cardId})");
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