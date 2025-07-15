using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using InvaderInsider.Data;
using InvaderInsider.Cards;
using TMPro;
using System;

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

        private readonly List<GameObject> currentHandItems = new List<GameObject>();
        private readonly Queue<GameObject> cardDisplayPool = new Queue<GameObject>();
        private CardManager cardManager;
        private bool isInitialized = false;
        private bool isPopupOpen = false;
        private HandSortType currentSortType = HandSortType.None;

        public enum HandSortType { None, ByType, ByCost, ByRarity, ByName }

        protected override void Initialize()
        {
            if (isInitialized) return;

            cardManager = CardManager.Instance;
            if (cardManager == null)
            {
                Debug.LogError($"{LOG_TAG} CardManager 인스턴스를 찾을 수 없습니다.");
                return;
            }

            if (cardDatabase == null)
            {
                Debug.LogError($"{LOG_TAG} CardDatabase가 할당되지 않았습니다.");
                return;
            }

            InitializeCardDisplayPool();
            SetupButtons();
            popupOverlay.SetActive(false);

            cardManager.OnHandCardsChanged += OnHandDataChanged;

            isInitialized = true;
        }

        private void OnDestroy()
        {
            if (cardManager != null)
            {
                cardManager.OnHandCardsChanged -= OnHandDataChanged;
            }
        }

        private void SetupButtons()
        {
            closeButton?.onClick.AddListener(ClosePopup);
            sortByTypeButton?.onClick.AddListener(() => SortHand(HandSortType.ByType));
            sortByCostButton?.onClick.AddListener(() => SortHand(HandSortType.ByCost));
            sortByRarityButton?.onClick.AddListener(() => SortHand(HandSortType.ByRarity));
            sortByNameButton?.onClick.AddListener(() => SortHand(HandSortType.ByName));
        }

        public void OpenPopup()
        {
            if (isPopupOpen || !isInitialized) return;

            popupOverlay.SetActive(true);
            isPopupOpen = true;
            UpdatePopupContent(cardManager.GetHandCardIds());
        }

        public void ClosePopup()
        {
            if (!isPopupOpen) return;

            popupOverlay.SetActive(false);
            isPopupOpen = false;
            ClearHandItems();
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

                var handler = cardObj.GetComponent<CardInteractionHandler>();
                if (handler != null)
                {
                    handler.OnCardPlayInteractionCompleted.RemoveAllListeners(); // 기존 리스너 제거
                    handler.OnCardPlayInteractionCompleted.AddListener(HandleCardPlayInteractionCompleted);
                }

                currentHandItems.Add(cardObj);
            }
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

        private void HandleCardPlayInteractionCompleted(CardDisplay playedCardDisplay, CardPlacementResult result)
        {
            if (!isInitialized || cardManager == null) return;

            var playedCardData = playedCardDisplay.GetCardData();
            if (playedCardData == null) return;

            if (result == CardPlacementResult.Success_Place || result == CardPlacementResult.Success_Upgrade)
            {
                // 핸드에서 카드를 제거하는 로직은 이제 필요 없습니다.
                // 카드는 항상 핸드에 남아있습니다.
            }
        }

        private void ClearHandItems()
        {
            foreach (var item in currentHandItems)
            {
                if (item != null)
                {
                    var handler = item.GetComponent<CardInteractionHandler>();
                    if (handler != null)
                    {
                        handler.OnCardPlayInteractionCompleted.RemoveAllListeners();
                    }
                    ReturnPooledCard(item);
                }
            }
            currentHandItems.Clear();
        }

        protected override void OnShow()
        {
            base.OnShow();
            OpenPopup();
        }

        protected override void OnHide()
        {
            base.OnHide();
            ClosePopup();
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
