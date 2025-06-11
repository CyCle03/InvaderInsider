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
                    
                    // 인스턴스가 있지만 초기화되지 않은 경우 강제 초기화
                    if (instance != null && !isInitialized)
                    {
                        Debug.Log("CardManager: Instance exists but not initialized in getter, forcing initialization");
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

        // Events
        public UnityEvent<CardDBObject> OnCardDrawn = new UnityEvent<CardDBObject>();
        public UnityEvent<List<CardDBObject>> OnMultipleCardsDrawn = new UnityEvent<List<CardDBObject>>();

        private void Awake()
        {
            Debug.Log("CardManager: === AWAKE CALLED ===");
            
            if (instance == null)
            {
                Debug.Log("CardManager: Setting as instance");
                instance = this;
                DontDestroyOnLoad(gameObject);
                PerformInitialization();
            }
            else if (instance != this)
            {
                Debug.Log("CardManager: Destroying duplicate CardManager");
                Destroy(gameObject);
                return;
            }
            else
            {
                Debug.Log("CardManager: This is already the instance");
                // 이미 instance이지만 초기화가 안 되어 있을 수 있음
                if (!isInitialized)
                {
                    Debug.Log("CardManager: Instance exists but not initialized, performing initialization");
                    PerformInitialization();
                }
            }
        }

        private void PerformInitialization()
        {
            Debug.Log("CardManager: === PERFORMING INITIALIZATION ===");
            Debug.Log("CardManager: cardDatabase: " + (cardDatabase != null ? "Assigned" : "Null"));
            if (cardDatabase != null)
            {
                Debug.Log("CardManager: cardDatabase name: " + cardDatabase.name);
            }
            
            if (cardDatabase == null)
            {
                Debug.LogError("Card Database Scriptable Object is not assigned in the inspector!");
                // Resources 폴더에서 CardDatabase를 찾아서 할당
                cardDatabase = Resources.Load<CardDatabase>("CardDatabase1");
                if (cardDatabase == null)
                {
                    // ScriptableObjects 폴더에서 직접 로드 시도
                    cardDatabase = Resources.Load<CardDatabase>("ScriptableObjects/CardSystem/CardDatabase1");
                    if (cardDatabase == null)
                    {
                        Debug.LogError("기본 CardDatabase를 찾을 수 없습니다. CardDatabase1.asset 파일을 확인하세요.");
                        // 빈 CardDatabase 생성
                        cardDatabase = ScriptableObject.CreateInstance<CardDatabase>();
                        Debug.LogWarning("빈 CardDatabase를 생성했습니다. Inspector에서 올바른 CardDatabase를 할당하세요.");
                    }
                    else
                    {
                        Debug.Log("CardDatabase1을 ScriptableObjects 폴더에서 로드했습니다.");
                    }
                }
                else
                {
                    Debug.Log("기본 CardDatabase1을 로드했습니다.");
                }
            }
            else
            {
                Debug.Log("CardDatabase가 Inspector에서 할당되었습니다: " + cardDatabase.name);
                Debug.Log("CardDatabase 카드 개수: " + cardDatabase.cards.Count);
            }
            
            isInitialized = true;
            Debug.Log("CardManager: === INITIALIZATION COMPLETED - isInitialized: " + isInitialized + " ===");
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