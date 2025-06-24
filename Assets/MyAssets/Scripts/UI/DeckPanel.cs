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
        private const string LOG_TAG = "Deck";

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
                LogManager.Info(LOG_TAG, "패널이 이미 초기화되었습니다. 중복 초기화를 방지합니다.");
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
            LogManager.Info(LOG_TAG, "패널 표시됨");
            RefreshDeckDisplay();
        }

        protected override void OnHide()
        {
            base.OnHide();
            LogManager.Info(LOG_TAG, "패널 숨김");
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
                LogManager.Warning(LOG_TAG, "카드 데이터를 찾을 수 없음 - ID: {0}", cardId);
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

                LogManager.Info(LOG_TAG, "카드 생성됨 - ID: {0}", cardId);
            }
        }

        private void HandleCardClick(int cardId, bool isInDeck)
        {
            if (saveManager == null) return;

            if (isInDeck)
            {
                saveManager.RemoveCardFromDeck(cardId);
                LogManager.Info(LOG_TAG, "카드가 덱에서 제거됨 - ID: {0}", cardId);
            }
            else
            {
                saveManager.AddCardToDeck(cardId);
                LogManager.Info(LOG_TAG, "카드가 덱에 추가됨 - ID: {0}", cardId);
            }
            
            RefreshDeckDisplay();
        }

        private void ClearCardDisplays()
        {
            foreach (var cardUI in activeCardUIs)
            {
                if (cardUI != null)
                {
                    LogManager.Info(LOG_TAG, "카드 제거됨 - ID: {0}", cardUI.CardId);
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