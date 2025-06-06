using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using InvaderInsider.Managers;
using InvaderInsider.Data;

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
        [SerializeField] private CardDatabase cardDatabase;
        
        [Header("Gacha Settings")]
        [SerializeField] private int singleDrawCost = 10;
        [SerializeField] private int multiDrawCost = 45;  // 5회 뽑기 (10% 할인)
        [SerializeField] private float[] rarityRates = { 0.60f, 0.30f, 0.08f, 0.02f };  // Common, Rare, Epic, Legendary

        // Events
        public UnityEvent<CardDBObject> OnCardDrawn = new UnityEvent<CardDBObject>();
        public UnityEvent<List<CardDBObject>> OnMultipleCardsDrawn = new UnityEvent<List<CardDBObject>>();

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                if (cardDatabase == null)
                {
                    Debug.LogError("Card Database Scriptable Object is not assigned in the inspector!");
                }
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

            CardDBObject drawnCard = DrawCardByRarity();
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

            List<CardDBObject> drawnCards = new List<CardDBObject>();
            for (int i = 0; i < count; i++)
            {
                drawnCards.Add(DrawCardByRarity());
            }

            OnMultipleCardsDrawn?.Invoke(drawnCards);
            return true;
        }

        // 레어도에 따른 카드 뽑기
        private CardDBObject DrawCardByRarity()
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
            var cardsOfRarity = cardDatabase.cards.Where(card => card.rarity == selectedRarity).ToList();
            if (cardsOfRarity.Count == 0)
            {
                Debug.LogWarning($"No cards found for rarity: {selectedRarity}");
                return cardDatabase.cards[0]; // Fallback to first card
            }

            return cardsOfRarity[Random.Range(0, cardsOfRarity.Count)];
        }

        // 카드 정보 조회
        public CardDBObject GetCardById(int cardId)
        {
            // 현재는 cardId가 List의 인덱스라고 가정
            if (cardId >= 0 && cardId < cardDatabase.cards.Count)
                return cardDatabase.cards[cardId];
            return null;
        }

        public List<CardDBObject> GetAllCards()
        {
            return cardDatabase.cards;
        }

        public List<CardDBObject> GetCardsByType(CardType type)
        {
            return cardDatabase.cards.Where(card => card.type == type).ToList();
        }

        public List<CardDBObject> GetCardsByRarity(CardRarity rarity)
        {
            return cardDatabase.cards.Where(card => card.rarity == rarity).ToList();
        }

        // 카드 추가 메서드 (에디터에서 사용)
        public void AddCard(CardDBObject card)
        {
            if (!cardDatabase.cards.Any(c => c.cardId == card.cardId))
            {
                cardDatabase.cards.Add(card);
            }
        }

        // 현재 뽑기 비용 조회
        public int GetSingleDrawCost() => singleDrawCost;
        public int GetMultiDrawCost() => multiDrawCost;
    }
} 