using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using InvaderInsider.Managers;

namespace InvaderInsider.Cards
{
    public class CardManager : MonoBehaviour
    {
        private static CardManager instance;
        public static CardManager Instance
        {
            get
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

        [Header("Card Collections")]
        [SerializeField] private List<CardData> allCards = new List<CardData>();
        
        [Header("Gacha Settings")]
        [SerializeField] private int singleDrawCost = 10;
        [SerializeField] private int multiDrawCost = 45;  // 5회 뽑기 (10% 할인)
        [SerializeField] private float[] rarityRates = { 0.60f, 0.30f, 0.08f, 0.02f };  // Common, Rare, Epic, Legendary

        // Events
        public UnityEvent<CardData> OnCardDrawn = new UnityEvent<CardData>();
        public UnityEvent<List<CardData>> OnMultipleCardsDrawn = new UnityEvent<List<CardData>>();

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // 단일 카드 뽑기
        public bool DrawSingleCard()
        {
            if (!GameManager.Instance.TrySpendEData(singleDrawCost))
            {
                Debug.Log("Not enough eData to draw a card!");
                return false;
            }

            CardData drawnCard = DrawCardByRarity();
            OnCardDrawn?.Invoke(drawnCard);
            return true;
        }

        // 5회 연속 뽑기
        public bool DrawMultipleCards(int count = 5)
        {
            if (!GameManager.Instance.TrySpendEData(multiDrawCost))
            {
                Debug.Log("Not enough eData to draw multiple cards!");
                return false;
            }

            List<CardData> drawnCards = new List<CardData>();
            for (int i = 0; i < count; i++)
            {
                drawnCards.Add(DrawCardByRarity());
            }

            OnMultipleCardsDrawn?.Invoke(drawnCards);
            return true;
        }

        // 레어도에 따른 카드 뽑기
        private CardData DrawCardByRarity()
        {
            float randomValue = Random.value;
            float currentProbability = 0f;
            CardRarity selectedRarity = CardRarity.Common;

            // 레어도 결정
            for (int i = 0; i < rarityRates.Length; i++)
            {
                currentProbability += rarityRates[i];
                if (randomValue <= currentProbability)
                {
                    selectedRarity = (CardRarity)i;
                    break;
                }
            }

            // 해당 레어도의 카드들 중에서 랜덤 선택
            var cardsOfRarity = allCards.Where(card => card.rarity == selectedRarity).ToList();
            if (cardsOfRarity.Count == 0)
            {
                Debug.LogWarning($"No cards found for rarity: {selectedRarity}");
                return allCards[0]; // Fallback to first card
            }

            return cardsOfRarity[Random.Range(0, cardsOfRarity.Count)];
        }

        // 카드 정보 조회
        public CardData GetCardById(int cardId)
        {
            // 현재는 cardId가 List의 인덱스라고 가정
            if (cardId >= 0 && cardId < allCards.Count)
                return allCards[cardId];
            return null;
        }

        public List<CardData> GetAllCards()
        {
            return allCards;
        }

        public List<CardData> GetCardsByType(CardData.CardType type)
        {
            return allCards.Where(card => card.type == type).ToList();
        }

        public List<CardData> GetCardsByRarity(CardRarity rarity)
        {
            return allCards.Where(card => card.rarity == rarity).ToList();
        }

        // 카드 추가 메서드 (에디터에서 사용)
        public void AddCard(CardData card)
        {
            if (!allCards.Any(c => c.cardId == card.cardId))
            {
                allCards.Add(card);
            }
        }

        // 현재 뽑기 비용 조회
        public int GetSingleDrawCost() => singleDrawCost;
        public int GetMultiDrawCost() => multiDrawCost;
    }
} 