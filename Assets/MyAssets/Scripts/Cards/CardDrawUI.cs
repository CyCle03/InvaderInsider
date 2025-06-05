using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using InvaderInsider.Data;
using InvaderInsider.Managers;
using UnityEngine.Events;

namespace InvaderInsider.Cards
{
    public class CardDrawUI : MonoBehaviour
    {
        [Header("Draw Buttons")]
        [SerializeField] private Button singleDrawButton;
        [SerializeField] private Button multiDrawButton;
        [SerializeField] private TextMeshProUGUI singleDrawCostText;
        [SerializeField] private TextMeshProUGUI multiDrawCostText;

        [Header("Card Display")]
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Transform cardContainer;
        [SerializeField] private GameObject drawResultPanel;

        private CardManager _cardManager;
        private GameManager _gameManager;

        private void Awake()
        {
            _cardManager = CardManager.Instance;
            _gameManager = GameManager.Instance;

            if (_cardManager == null)
            {
                Debug.LogError("CardManager instance not found!");
            }
            if (_gameManager == null)
            {
                Debug.LogError("GameManager instance not found!");
            }
        }

        private void Start()
        {
            InitializeUI();
            SubscribeToEvents();
        }

        private void InitializeUI()
        {
            // 비용 표시 업데이트
            UpdateCostTexts();

            // 버튼 이벤트 연결
            singleDrawButton.onClick.AddListener(() => _cardManager.DrawSingleCard());
            multiDrawButton.onClick.AddListener(() => _cardManager.DrawMultipleCards());

            // 초기에는 결과 패널 숨기기
            drawResultPanel.SetActive(false);
        }

        private void SubscribeToEvents()
        {
            // 카드 매니저 이벤트 구독
            _cardManager.OnCardDrawn.AddListener(new UnityEngine.Events.UnityAction<InvaderInsider.Data.CardDBObject>(ShowSingleCardResult));
            _cardManager.OnMultipleCardsDrawn.AddListener(new UnityEngine.Events.UnityAction<System.Collections.Generic.List<InvaderInsider.Data.CardDBObject>>(ShowMultipleCardResults));

            // 리소스 변경 이벤트 구독
            _gameManager.AddResourcePointsListener(UpdateButtonStates);
        }

        private void UpdateCostTexts()
        {
            singleDrawCostText.text = $"Draw 1 ({_cardManager.GetSingleDrawCost()} eData)";
            multiDrawCostText.text = $"Draw 5 ({_cardManager.GetMultiDrawCost()} eData)";
        }

        private void UpdateButtonStates(int currentEData)
        {
            singleDrawButton.interactable = currentEData >= _cardManager.GetSingleDrawCost();
            multiDrawButton.interactable = currentEData >= _cardManager.GetMultiDrawCost();
        }

        private void ShowSingleCardResult(CardDBObject card)
        {
            ClearCardContainer();
            CreateCardDisplay(card);
            drawResultPanel.SetActive(true);
        }

        private void ShowMultipleCardResults(List<CardDBObject> cards)
        {
            ClearCardContainer();
            foreach (var card in cards)
            {
                CreateCardDisplay(card);
            }
            drawResultPanel.SetActive(true);
        }

        private void CreateCardDisplay(CardDBObject card)
        {
            GameObject cardObj = Instantiate(cardPrefab, cardContainer);
            CardDisplay display = cardObj.GetComponent<CardDisplay>();
            if (display != null)
            {
                display.SetupCard(card);
            }
        }

        private void ClearCardContainer()
        {
            foreach (Transform child in cardContainer)
            {
                Destroy(child.gameObject);
            }
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (_cardManager != null)
            {
                _cardManager.OnCardDrawn.RemoveListener(new UnityEngine.Events.UnityAction<InvaderInsider.Data.CardDBObject>(ShowSingleCardResult));
                _cardManager.OnMultipleCardsDrawn.RemoveListener(new UnityEngine.Events.UnityAction<System.Collections.Generic.List<InvaderInsider.Data.CardDBObject>>(ShowMultipleCardResults));
            }

            if (_gameManager != null)
            {
                _gameManager.RemoveResourcePointsListener(UpdateButtonStates);
            }
        }
    }
} 