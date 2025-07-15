using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using InvaderInsider.Cards;
using InvaderInsider.Data;
using InvaderInsider.Managers;

namespace InvaderInsider.UI
{
    public class SummonChoicePanel : BasePanel
    {
        private const string LOG_TAG = "[SummonChoice]";

        [Header("UI References")]
        [SerializeField] private Transform cardContainer;
        [SerializeField] private GameObject cardButtonPrefab;
        [SerializeField] private Button closeButton;

        private List<CardDBObject> availableCards;
        private List<GameObject> cardButtonObjects = new List<GameObject>();
        private CardManager cardManager;
        private bool wasGamePaused = false;
        private bool isInitialized = false;

        protected override void Initialize()
        {
            if (isInitialized) return;

            base.Initialize();
            cardManager = CardManager.Instance;
            if (cardManager == null)
            {
                Debug.LogError($"{LOG_TAG} CardManager 인스턴스를 찾을 수 없습니다.");
                return;
            }

            closeButton?.onClick.AddListener(HandleCloseClick);
            isInitialized = true;
        }

        public void SetupCards(List<CardDBObject> cards)
        {
            if (!isInitialized) Initialize();

            ClearCardButtons();
            availableCards = cards;

            if (cards == null || cardButtonPrefab == null || cardContainer == null) return;

            foreach (var card in cards)
            {
                CreateCardButton(card);
            }
        }

        private void CreateCardButton(CardDBObject card)
        {
            GameObject buttonObj = Instantiate(cardButtonPrefab, cardContainer);
            var cardButtonComponent = buttonObj.GetComponent<CardButton>();
            var button = buttonObj.GetComponent<Button>();

            if (cardButtonComponent != null && button != null)
            {
                cardButtonComponent.Initialize(card);
                button.onClick.AddListener(() => HandleCardSelect(card));
                cardButtonObjects.Add(buttonObj);
            }
            else
            {
                Destroy(buttonObj);
            }
        }

        private void HandleCardSelect(CardDBObject selectedCard)
        {
            cardManager?.OnCardChoiceSelected(selectedCard);
        }

        private void HandleCloseClick()
        {
            cardManager?.OnCardChoiceSelected(null);
        }

        protected override void OnShow()
        {
            base.OnShow();
            if (GameManager.Instance != null)
            {
                wasGamePaused = GameManager.Instance.CurrentGameState == GameState.Paused;
                if (!wasGamePaused)
                {
                    GameManager.Instance.PauseGame();
                }
            }
        }

        protected override void OnHide()
        {
            base.OnHide();
            if (GameManager.Instance != null && !wasGamePaused)
            {
                GameManager.Instance.ResumeGame();
            }
            ClearCardButtons();
        }

        private void ClearCardButtons()
        {
            foreach (var buttonObj in cardButtonObjects)
            {
                Destroy(buttonObj);
            }
            cardButtonObjects.Clear();
        }
    }
}