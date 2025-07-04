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
    public class CardManager : InvaderInsider.Core.SingletonManager<CardManager>
    {
        private const string LOG_TAG = "CardManager";

        

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

        // 소환 선택에서 선택된 카드 추적을 위한 필드
        private HashSet<int> selectedCardIds = new HashSet<int>();

        protected override void Awake()
        {
            base.Awake();
            LoadSummonData();
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (cardDatabase == null)
            {
                cardDatabase = Resources.Load<CardDatabase>("CardDatabase");
                if (cardDatabase == null)
                {
                    cardDatabase = Resources.Load<CardDatabase>("ScriptableObjects/CardSystem/CardDatabase");
                    if (cardDatabase == null)
                    {
                        LogManager.Error(LOG_TAG, "카드 데이터베이스가 설정되지 않았습니다.");
                        cardDatabase = ScriptableObject.CreateInstance<CardDatabase>();
                        LogManager.Warning(LOG_TAG, "빈 CardDatabase를 생성했습니다. Inspector에서 올바른 CardDatabase를 할당하세요.");
                    }
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
        }

        #region Summon System
        public void LoadSummonData()
        {
            if (SaveDataManager.Instance != null && SaveDataManager.Instance.CurrentSaveData != null)
            {
                summonCount = SaveDataManager.Instance.CurrentSaveData.progressData.summonCount;
                currentSummonCost = initialSummonCost + summonCount * summonCostIncrease;
                LogManager.Info(LOG_TAG, "소환 데이터 로드 완료. 소환 횟수: {0}, 현재 비용: {1}", summonCount, currentSummonCost);
            }
            else
            {
                summonCount = 0;
                currentSummonCost = initialSummonCost;
                LogManager.Info(LOG_TAG, "소환 데이터 로드 완료. 소환 횟수: {0}, 현재 비용: {1}", summonCount, currentSummonCost);
            }
        }

        public void SaveSummonData()
        {
            if (SaveDataManager.Instance != null && SaveDataManager.Instance.CurrentSaveData != null)
            {
                SaveDataManager.Instance.CurrentSaveData.progressData.summonCount = summonCount;
                // 메모리에만 업데이트, 저장하지 않음 (스테이지 클리어/게임 종료 시에만 저장)
                LogManager.Info(LOG_TAG, "소환 데이터 저장 완료. 소환 횟수: {0}", summonCount);
            }
            else
            {
                LogManager.Error(LOG_TAG, "SaveDataManager가 없습니다.");
            }
        }

        public void Summon()
        {
            // 중복 호출 방지
            if (isSummonInProgress)
            {
                LogManager.Warning(LOG_TAG, "소환 진행 중입니다. 잠시 후 다시 시도하세요.");
                return;
            }

            if (SaveDataManager.Instance == null)
            {
                LogManager.Error(LOG_TAG, "SaveDataManager가 없습니다.");
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
                    LogManager.Error(LOG_TAG, "리소스가 부족합니다. 필요 리소스: {0}, 현재 리소스: {1}", currentSummonCost, currentEData);
                    isSummonInProgress = false;
                    return;
                }

                // 소환 횟수 증가 및 다음 비용 계산
                summonCount++;
                currentSummonCost = initialSummonCost + summonCount * summonCostIncrease;

                LogManager.Info(LOG_TAG, "소환 비용이 {0}으로 증가했습니다.", currentSummonCost);

                // 랜덤 카드 3장 선택
                List<CardDBObject> randomCards = SelectRandomCards(3);
                
                // 선택된 카드들을 UI로 표시
                DisplaySummonChoices(randomCards); // UI 표시는 UIManager를 통해서만
            }
            else
            {
                LogManager.Warning(LOG_TAG, "리소스가 부족합니다. 현재 리소스: {0}, 필요 리소스: {1}", currentEData, currentSummonCost);
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
                    LogManager.Info(LOG_TAG, "UIManager를 통해 SummonChoice 패널을 표시했습니다.");
                    return;
                }
            }

            LogManager.Error(LOG_TAG, "카드 데이터베이스에 카드가 없습니다.");
        }

        // 소환 선택에서 카드가 선택되었는지 확인하는 메서드
        public bool IsCardSelectedInSummonChoice(int cardId)
        {
            return selectedCardIds.Contains(cardId);
        }

        // 소환 선택에서 카드 선택 시 호출되는 메서드
        public void MarkCardAsSelected(int cardId)
        {
            selectedCardIds.Add(cardId);
        }

        // 소환 선택 후 선택된 카드 초기화
        public void ClearSelectedCards()
        {
            selectedCardIds.Clear();
        }

        // 카드 선택 메서드 수정
        public void OnCardChoiceSelected(CardDBObject selectedCard)
        {
            if (selectedCard == null) return;

            // 선택된 카드를 핸드에 추가
            SaveDataManager.Instance.AddCardToHandAndOwned(selectedCard.cardId);

            // 선택된 카드 ID 마킹
            MarkCardAsSelected(selectedCard.cardId);

            // UI 업데이트 및 로깅
            LogManager.Info(LOG_TAG, $"카드 선택됨: {selectedCard.cardName} (ID: {selectedCard.cardId})");

            // 소환 선택 패널 숨기기
            var summonChoicePanel = UIManager.Instance.GetPanel("SummonChoice") as SummonChoicePanel;
            summonChoicePanel?.Hide();
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
                LogManager.Info(LOG_TAG, "카드가 핸드에서 제거되었습니다: ID {0}", cardId);
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
                    LogManager.Info(LOG_TAG, "카드가 핸드에 추가되었습니다: '{0}' (ID: {1})", randomCard.cardName, randomCard.cardId);
                }
            }
        }

        public void DemoShowHandStatus()
        {
            if (SaveDataManager.Instance != null)
            {
                var handCards = GetHandCards();
                LogManager.Info(LOG_TAG, "현재 핸드 상태: {0}장의 카드", handCards.Count);
                foreach (var card in handCards)
                {
                    LogManager.Info(LOG_TAG, "- {0} (ID: {1}, 타입: {2}, 등급: {3})", card.cardName, card.cardId, card.type, card.rarity);
                }
            }
        }
    }
} 