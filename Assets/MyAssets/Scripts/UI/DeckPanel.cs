using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using InvaderInsider.Cards;
using InvaderInsider.Data;
using InvaderInsider.Managers;

namespace InvaderInsider.UI
{
    public class DeckPanel : BasePanel
    {
        private const string LOG_TAG = "DeckPanel";

        [Header("UI Elements")]
        [SerializeField] private Transform cardContainer;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Button backButton;

        private readonly List<GameObject> activeCardObjects = new List<GameObject>();
        private CardManager cardManager;
        private bool isInitialized = false;

        protected override void Initialize()
        {
            if (isInitialized) return;

            cardManager = CardManager.Instance;
            if (cardManager == null)
            {
                Debug.LogError($"{LOG_TAG}: CardManager 인스턴스를 찾을 수 없습니다.");
                return;
            }

            backButton?.onClick.AddListener(OnBackButtonClicked);
            isInitialized = true;
        }

        private void OnBackButtonClicked()
        {
            // UIManager를 통해 이전 패널로 돌아가는 로직 (필요 시 UIManager에 구현)
            // 예: UIManager.Instance.GoBack();
            gameObject.SetActive(false);
        }

        protected override void OnShow()
        {
            base.OnShow();
            if (!isInitialized) Initialize();
            RefreshCardDisplay();
        }

        protected override void OnHide()
        {
            base.OnHide();
            ClearCardDisplays();
        }

        private void RefreshCardDisplay()
        {
            if (!isInitialized) return;

            ClearCardDisplays();

            List<CardDBObject> allCards = cardManager.GetHandCards();

            foreach (var cardData in allCards)
            {
                CreateCardUI(cardData, cardContainer);
            }
        }

        private void CreateCardUI(CardDBObject cardData, Transform container)
        {
            if (container == null || cardPrefab == null) return;

            GameObject cardObj = Instantiate(cardPrefab, container);
            var cardUI = cardObj.GetComponent<CardUI>();
            
            if (cardUI != null)
            {
                cardUI.SetCard(cardData);
                // 카드 클릭 시 상세 정보를 보여주거나 하는 로직을 여기에 추가할 수 있습니다.
                // cardUI.OnCardClicked += () => ShowCardDetails(cardData);
            }
            activeCardObjects.Add(cardObj);
        }

        private void ClearCardDisplays()
        {
            foreach (var cardObj in activeCardObjects)
            {
                Destroy(cardObj);
            }
            activeCardObjects.Clear();
        }
    }
}