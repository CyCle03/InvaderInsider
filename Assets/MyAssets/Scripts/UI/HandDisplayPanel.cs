using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using InvaderInsider.Data;
using InvaderInsider.Cards;
using TMPro;
using System;
using InvaderInsider.Managers;

namespace InvaderInsider.UI
{
    public class HandDisplayPanel : BasePanel
    {
        private const string LOG_TAG = "HandDisplay";

        [Header("UI Elements")]
        [SerializeField] private GameObject popupOverlay;
        [SerializeField] private Transform handContainer;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Button closeButton;

        [Header("Sorting Buttons")]
        [SerializeField] private Button sortByTypeButton;
        [SerializeField] private Button sortByCostButton;
        [SerializeField] private Button sortByRarityButton;
        [SerializeField] private Button sortByNameButton;

        [Header("Data References")]
        [SerializeField] private CardDatabase cardDatabase;

        [Header("Panel References")]
        [SerializeField] private CardDetailView cardDetailView;

        private readonly List<GameObject> currentHandItems = new List<GameObject>();
        private readonly Queue<GameObject> cardDisplayPool = new Queue<GameObject>();
        private CardManager cardManager;
        private bool isInitialized = false;
        private bool isPopupOpen = false;
        private HandSortType currentSortType = HandSortType.None;

        public enum HandSortType { None, ByType, ByCost, ByRarity, ByName }

        public void OpenPopup()
        {
            LogManager.Log($"{LOG_TAG} OpenPopup called. isPopupOpen: {isPopupOpen}, isInitialized: {isInitialized}");
            if (isPopupOpen || !isInitialized) return;

            Show(); // BasePanel의 Show() 호출
            isPopupOpen = true;
            UpdatePopupContent(cardManager.GetHandCardKeys());
            LogManager.Log($"{LOG_TAG} Popup opened. CloseButton interactable: {closeButton?.interactable}, PopupOverlay active: {popupOverlay?.activeSelf}");
            CanvasGroup popupCanvasGroup = popupOverlay?.GetComponent<CanvasGroup>();
            if (popupCanvasGroup == null && popupOverlay != null)
            {
                popupCanvasGroup = popupOverlay.AddComponent<CanvasGroup>();
                LogManager.Log($"{LOG_TAG} Added CanvasGroup to popupOverlay.");
            }
            if (popupCanvasGroup != null)
            {
                popupCanvasGroup.blocksRaycasts = true;
                LogManager.Log($"{LOG_TAG} PopupOverlay CanvasGroup blocksRaycasts set to true.");
            }
        }

        public void ClosePopup()
        {
            LogManager.Log($"{LOG_TAG} ClosePopup called. isPopupOpen: {isPopupOpen}");
            if (!isPopupOpen) return;

            Hide(); // BasePanel의 Hide() 호출
            isPopupOpen = false;
            ClearHandItems();

            CanvasGroup popupCanvasGroup = popupOverlay?.GetComponent<CanvasGroup>();
            if (popupCanvasGroup != null)
            {
                popupCanvasGroup.blocksRaycasts = false;
                LogManager.Log($"{LOG_TAG} PopupOverlay CanvasGroup blocksRaycasts set to false.");
            }
            LogManager.Log($"{LOG_TAG} Popup closed.");
        }

        protected override void Initialize()
        {
            if (isInitialized) return;

            base.Initialize();
            cardManager = CardManager.Instance;
            if (cardManager == null)
            {
                LogManager.LogError($"{LOG_TAG} CardManager 인스턴스를 찾을 수 없습니다.");
                return;
            }

            if (cardDatabase == null)
            {
                LogManager.LogError($"{LOG_TAG} CardDatabase가 할당되지 않았습니다.");
                return;
            }

            // UIManager를 통해 CardDetailView 가져오기
            cardDetailView = UIManager.Instance?.GetPanel("CardDetailView") as CardDetailView;
            if (cardDetailView == null)
            {
                // CardDetailView를 찾지 못하면 비활성화된 오브젝트를 포함해서 다시 찾기
                cardDetailView = FindObjectOfType<CardDetailView>(true);
                if (cardDetailView == null)
            {
                LogManager.LogError($"{LOG_TAG} CardDetailView를 찾을 수 없습니다.");
                return;
            }
            }

            InitializeCardDisplayPool();
            SetupButtons();

            cardManager.OnHandCardsChanged += OnHandDataChanged;

            isInitialized = true;
        }

        private void SetupButtons()
        {
            if (closeButton == null)
            {
                LogManager.LogError($"{LOG_TAG} closeButton이 할당되지 않았습니다. Inspector를 확인하세요.");
            }
            else
            {
                closeButton.onClick.AddListener(ClosePopup);
                LogManager.Log($"{LOG_TAG} closeButton 이벤트 리스너 추가됨.");
            }
            sortByTypeButton?.onClick.AddListener(() => SortHand(HandSortType.ByType));
            sortByCostButton?.onClick.AddListener(() => SortHand(HandSortType.ByCost));
            sortByRarityButton?.onClick.AddListener(() => SortHand(HandSortType.ByRarity));
            sortByNameButton?.onClick.AddListener(() => SortHand(HandSortType.ByName));
        }

        private void OnHandDataChanged(List<string> handCardKeys)
        {
            if (isPopupOpen)
            {
                UpdatePopupContent(handCardKeys);
            }
        }

        private void UpdatePopupContent(List<string> handCardKeys)
        {
            if (!isInitialized) return;

            ClearHandItems();
            UpdateTitle(handCardKeys.Count);

            if (handContainer == null || cardPrefab == null) return;

            var sortedCardKeys = SortCardKeys(handCardKeys, currentSortType);

            foreach (string key in sortedCardKeys)
            {
                var parts = key.Split('_');
                if (parts.Length != 2 || !int.TryParse(parts[0], out int cardId) || !int.TryParse(parts[1], out int level)) continue;

                var cardData = cardManager.GetCard(cardId, level);
                if (cardData == null) continue;

                var cardObj = GetPooledCard();
                if (cardObj == null) continue;

                cardObj.transform.SetParent(handContainer, false);
                var display = cardObj.GetComponent<CardDisplay>();
                display?.SetupCard(cardData);

                // Handle interaction via CardInteractionHandler
                var cardInteractionHandler = cardObj.GetComponent<CardInteractionHandler>();
                if (cardInteractionHandler != null)
                {
                    cardInteractionHandler.OnCardClicked += () => ShowCardDetails(cardData);
                }
                else
                {
                    LogManager.LogWarning($"{LOG_TAG} CardInteractionHandler를 찾을 수 없습니다. 카드 클릭 이벤트가 작동하지 않을 수 있습니다.");
                }

                currentHandItems.Add(cardObj);
            }
        }

        private void ShowCardDetails(CardDBObject cardData)
        {
            cardDetailView.ShowCard(cardData);
            ClosePopup();
        }

        private void UpdateTitle(int handCount)
        {
            if (titleText != null)
            {
                titleText.text = $"보유 카드 ({handCount})";
            }
        }

        public void SortHand(HandSortType sortType)
        {
            currentSortType = sortType;
            if (isPopupOpen)
            {
                UpdatePopupContent(cardManager.GetHandCardKeys());
            }
        }

        private List<string> SortCardKeys(List<string> cardKeys, HandSortType sortType)
        {
            var sortedKeys = new List<string>(cardKeys);
            if (sortType == HandSortType.None) return sortedKeys;

            sortedKeys.Sort((key1, key2) => {
                var parts1 = key1.Split('_');
                var parts2 = key2.Split('_');
                if (parts1.Length != 2 || parts2.Length != 2) return 0;

                var card1 = cardManager.GetCard(int.Parse(parts1[0]), int.Parse(parts1[1]));
                var card2 = cardManager.GetCard(int.Parse(parts2[0]), int.Parse(parts2[1]));
                if (card1 == null || card2 == null) return 0;

                switch (sortType)
                {
                    case HandSortType.ByType: return card1.type.CompareTo(card2.type);
                    case HandSortType.ByCost: return card1.cost.CompareTo(card2.cost);
                    case HandSortType.ByRarity: return card1.rarity.CompareTo(card2.rarity);
                    case HandSortType.ByName: return string.Compare(card1.cardName, card2.cardName, StringComparison.Ordinal);
                    default: return 0;
                }
            });

            return sortedKeys;
        }

        private void ClearHandItems()
        {
            foreach (var item in currentHandItems)
            {
                if (item != null)
                {
                    var cardInteractionHandler = item.GetComponent<CardInteractionHandler>();
                    if (cardInteractionHandler != null)
                    {
                        cardInteractionHandler.ClearClickListeners();
                    }

                    var button = item.GetComponent<Button>();
                    if (button != null)
                    {
                        button.onClick.RemoveAllListeners();
                    }
                    ReturnPooledCard(item);
                }
            }
            currentHandItems.Clear();
        }

        #region Object Pool
        private void InitializeCardDisplayPool()
        {
            if (cardPrefab == null) return;
            for (int i = 0; i < 10; i++)
            {
                GameObject cardObj = Instantiate(cardPrefab, transform);
                cardObj.SetActive(false);
                cardDisplayPool.Enqueue(cardObj);
            }
        }

        private GameObject GetPooledCard()
        {
            if (cardDisplayPool.Count > 0)
            {
                var cardObj = cardDisplayPool.Dequeue();
                cardObj.SetActive(true);
                return cardObj;
            }
            return Instantiate(cardPrefab);
        }

        private void ReturnPooledCard(GameObject cardObj)
        {
            if (cardObj != null)
            {
                cardObj.SetActive(false);
                cardObj.transform.SetParent(transform);
                cardDisplayPool.Enqueue(cardObj);
            }
        }
        #endregion
    }
}
