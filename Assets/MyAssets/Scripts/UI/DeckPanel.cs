using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using InvaderInsider.Data;
using InvaderInsider.Cards;
using InvaderInsider.Managers;

namespace InvaderInsider.UI
{
    public class DeckPanel : BasePanel
    {
        private const string LOG_PREFIX = "[Deck] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "Deck: Panel shown",
            "Deck: Panel hidden",
            "Deck: Card {0} added to deck",
            "Deck: Card {0} removed from deck",
            "Deck: Card {0} created",
            "Deck: Card {0} destroyed",
            "Deck: Card data not found for ID {0}"
        };

        [Header("Card Containers")]
        [SerializeField] private Transform deckCardContainer;
        [SerializeField] private Transform ownedCardContainer;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Button backButton;

        private readonly List<CardUI> activeCardUIs = new List<CardUI>();
        private readonly Dictionary<int, CardUI> cardUIDict = new Dictionary<int, CardUI>();
        private UIManager uiManager;
        private SaveDataManager saveManager;
        private CardManager cardManager;
        private readonly string[] cachedStrings = new string[7];
        private bool isInitialized = false;

        protected override void Awake()
        {
            base.Awake();
            
            uiManager = UIManager.Instance;
            saveManager = SaveDataManager.Instance;
            cardManager = CardManager.Instance;
            
            Initialize();
        }

        protected override void Initialize()
        {
            if (isInitialized)
            {
                LogManager.Info("DeckPanel", "Deck 패널이 이미 초기화되었습니다. 중복 초기화를 방지합니다.");
                return;
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(OnBackButtonClicked);
            }
            
            isInitialized = true;
        }

        private void OnBackButtonClicked()
        {
            uiManager.GoBack();
        }

        protected override void OnShow()
        {
            base.OnShow();
            if (cachedStrings[0] == null)
            {
                cachedStrings[0] = LOG_PREFIX + LOG_MESSAGES[0];
            }
            Debug.Log(cachedStrings[0]);
            RefreshDeckDisplay();
        }

        protected override void OnHide()
        {
            base.OnHide();
            if (cachedStrings[1] == null)
            {
                cachedStrings[1] = LOG_PREFIX + LOG_MESSAGES[1];
            }
            Debug.Log(cachedStrings[1]);
            ClearCardDisplays();
        }

        private void RefreshDeckDisplay()
        {
            if (saveManager == null) return;
            
            ClearCardDisplays();
            var saveData = saveManager.CurrentSaveData;

            foreach (int cardId in saveData.deckData.deckCardIds)
            {
                CreateCardUI(cardId, deckCardContainer, true);
            }

            foreach (int cardId in saveData.deckData.ownedCardIds)
            {
                if (!saveData.deckData.deckCardIds.Contains(cardId))
                {
                    CreateCardUI(cardId, ownedCardContainer, false);
                }
            }
        }

        private void CreateCardUI(int cardId, Transform container, bool isInDeck)
        {
            if (container == null || cardPrefab == null || cardManager == null) return;

            var cardData = cardManager.GetCardById(cardId);
            if (cardData == null)
            {
                if (cachedStrings[6] == null)
                {
                    cachedStrings[6] = string.Format(LOG_PREFIX + LOG_MESSAGES[6], cardId);
                }
                Debug.LogWarning(cachedStrings[6]);
                return;
            }

            if (cardUIDict.TryGetValue(cardId, out var existingCardUI))
            {
                existingCardUI.transform.SetParent(container);
                existingCardUI.transform.localScale = Vector3.one;
                activeCardUIs.Add(existingCardUI);
                return;
            }

            var cardObj = Instantiate(cardPrefab, container);
            var cardUI = cardObj.GetComponent<CardUI>();
            
            if (cardUI != null)
            {
                cardUI.SetCard(cardData);
                cardUI.OnCardClicked += () => HandleCardClick(cardId, isInDeck);
                activeCardUIs.Add(cardUI);
                cardUIDict[cardId] = cardUI;

                if (cachedStrings[4] == null)
                {
                    cachedStrings[4] = string.Format(LOG_PREFIX + LOG_MESSAGES[4], cardId);
                }
                Debug.Log(cachedStrings[4]);
            }
        }

        private void HandleCardClick(int cardId, bool isInDeck)
        {
            if (saveManager == null) return;

            if (isInDeck)
            {
                saveManager.RemoveCardFromDeck(cardId);
                if (cachedStrings[3] == null)
                {
                    cachedStrings[3] = string.Format(LOG_PREFIX + LOG_MESSAGES[3], cardId);
                }
                Debug.Log(cachedStrings[3]);
            }
            else
            {
                saveManager.AddCardToDeck(cardId);
                if (cachedStrings[2] == null)
                {
                    cachedStrings[2] = string.Format(LOG_PREFIX + LOG_MESSAGES[2], cardId);
                }
                Debug.Log(cachedStrings[2]);
            }
            
            RefreshDeckDisplay();
        }

        private void ClearCardDisplays()
        {
            foreach (var cardUI in activeCardUIs)
            {
                if (cardUI != null)
                {
                    if (cachedStrings[5] == null)
                    {
                        cachedStrings[5] = string.Format(LOG_PREFIX + LOG_MESSAGES[5], cardUI.CardId);
                    }
                    Debug.Log(cachedStrings[5]);
                    cardUI.transform.SetParent(null);
                    cardUI.gameObject.SetActive(false);
                }
            }
            activeCardUIs.Clear();
        }

        private void OnDestroy()
        {
            foreach (var cardUI in cardUIDict.Values)
            {
                if (cardUI != null)
                {
                    Destroy(cardUI.gameObject);
                }
            }
            cardUIDict.Clear();
            activeCardUIs.Clear();
        }
    }
} 