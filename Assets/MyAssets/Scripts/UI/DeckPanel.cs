using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using InvaderInsider.Data;
using InvaderInsider.Cards;

namespace InvaderInsider.UI
{
    public class DeckPanel : BasePanel
    {
        [Header("Card Containers")]
        [SerializeField] private Transform deckCardContainer;
        [SerializeField] private Transform ownedCardContainer;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Button backButton;

        private List<CardUI> activeCardUIs = new List<CardUI>();

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        protected override void Initialize()
        {
            backButton?.onClick.AddListener(() => UIManager.Instance.GoBack());
        }

        protected override void OnShow()
        {
            RefreshDeckDisplay();
        }

        protected override void OnHide()
        {
            ClearCardDisplays();
        }

        private void RefreshDeckDisplay()
        {
            ClearCardDisplays();

            var saveData = SaveDataManager.Instance.CurrentSaveData;

            // 덱에 있는 카드 표시
            foreach (int cardId in saveData.deckData.deckCardIds)
            {
                CreateCardUI(cardId, deckCardContainer, true);
            }

            // 보유 중인 카드 표시
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
            GameObject cardObj = Instantiate(cardPrefab, container);
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            if (cardUI != null)
            {
                CardData cardData = CardManager.Instance.GetCardById(cardId);
                cardUI.SetCard(cardData);
                cardUI.OnCardClicked += () => HandleCardClick(cardId, isInDeck);
                activeCardUIs.Add(cardUI);
            }
        }

        private void HandleCardClick(int cardId, bool isInDeck)
        {
            if (isInDeck)
            {
                SaveDataManager.Instance.RemoveCardFromDeck(cardId);
            }
            else
            {
                SaveDataManager.Instance.AddCardToDeck(cardId);
            }
            RefreshDeckDisplay();
        }

        private void ClearCardDisplays()
        {
            foreach (var cardUI in activeCardUIs)
            {
                if (cardUI != null)
                {
                    Destroy(cardUI.gameObject);
                }
            }
            activeCardUIs.Clear();
        }
    }
} 