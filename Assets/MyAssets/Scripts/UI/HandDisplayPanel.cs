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
            UpdatePopupContent(cardManager.GetHandCardIds());
            LogManager.Log($"{LOG_TAG} Popup opened.");
        }

        public void ClosePopup()
        {
            LogManager.Log($"{LOG_TAG} ClosePopup called. isPopupOpen: {isPopupOpen}");
            if (!isPopupOpen) return;

            Hide(); // BasePanel의 Hide() 호출
            isPopupOpen = false;
            ClearHandItems();
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

        private void OnHandDataChanged(List<int> handCardIds)
        {
            if (isPopupOpen)
            {
                UpdatePopupContent(handCardIds);
            }
        }

        private void UpdatePopupContent(List<int> handCardIds)
        {
            if (!isInitialized) return;

            ClearHandItems();
            UpdateTitle(handCardIds.Count);

            if (handContainer == null || cardPrefab == null) return;

            var sortedCardIds = SortCardIds(handCardIds, currentSortType);

            foreach (int cardId in sortedCardIds)
            {
                var cardData = cardDatabase.GetCardById(cardId);
                if (cardData == null) continue;

                var cardObj = GetPooledCard();
                if (cardObj == null) continue;

                cardObj.transform.SetParent(handContainer, false);
                var display = cardObj.GetComponent<CardDisplay>();
                display?.SetupCard(cardData);

                // Handle interaction via a simple button click to show details
                var cardButton = cardObj.GetComponent<Button>();
                if (cardButton != null)
                {
                    cardButton.onClick.RemoveAllListeners();
                    cardButton.onClick.AddListener(() => ShowCardDetails(cardData));
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
                UpdatePopupContent(cardManager.GetHandCardIds());
            }
        }

        private List<int> SortCardIds(List<int> cardIds, HandSortType sortType)
        {
            var sortedIds = new List<int>(cardIds);
            if (sortType == HandSortType.None) return sortedIds;

            Comparison<int> comparison = (id1, id2) =>
            {
                var card1 = cardDatabase.GetCardById(id1);
                var card2 = cardDatabase.GetCardById(id2);
                if (card1 == null || card2 == null) return 0;

                switch (sortType)
                {
                    case HandSortType.ByType: return card1.type.CompareTo(card2.type);
                    case HandSortType.ByCost: return card1.cost.CompareTo(card2.cost);
                    case HandSortType.ByRarity: return card1.rarity.CompareTo(card2.rarity);
                    case HandSortType.ByName: return string.Compare(card1.cardName, card2.cardName, StringComparison.Ordinal);
                    default: return 0;
                }
            };

            sortedIds.Sort(comparison);
            return sortedIds;
        }

        private void ClearHandItems()
        {
            foreach (var item in currentHandItems)
            {
                if (item != null)
                {
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
