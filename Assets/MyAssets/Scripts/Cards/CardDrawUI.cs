using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using InvaderInsider.Data;
using InvaderInsider.Managers;

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
            singleDrawButton.onClick.AddListener(() => CardManager.Instance.DrawSingleCard());
            multiDrawButton.onClick.AddListener(() => CardManager.Instance.DrawMultipleCards());

            // 초기에는 결과 패널 숨기기
            drawResultPanel.SetActive(false);
        }

        private void SubscribeToEvents()
        {
            // 카드 매니저 이벤트 구독
            CardManager.Instance.OnCardDrawn.AddListener(ShowSingleCardResult);
            CardManager.Instance.OnMultipleCardsDrawn.AddListener(ShowMultipleCardResults);

            // 리소스 변경 이벤트 구독
            GameManager.Instance.AddResourcePointsListener(UpdateButtonStates);
        }

        private void UpdateCostTexts()
        {
            singleDrawCostText.text = $"Draw 1 ({CardManager.Instance.GetSingleDrawCost()} eData)";
            multiDrawCostText.text = $"Draw 5 ({CardManager.Instance.GetMultiDrawCost()} eData)";
        }

        private void UpdateButtonStates(int currentEData)
        {
            singleDrawButton.interactable = currentEData >= CardManager.Instance.GetSingleDrawCost();
            multiDrawButton.interactable = currentEData >= CardManager.Instance.GetMultiDrawCost();
        }

        private void ShowSingleCardResult(CardData card)
        {
            ClearCardContainer();
            CreateCardDisplay(card);
            drawResultPanel.SetActive(true);
        }

        private void ShowMultipleCardResults(List<CardData> cards)
        {
            ClearCardContainer();
            foreach (var card in cards)
            {
                CreateCardDisplay(card);
            }
            drawResultPanel.SetActive(true);
        }

        private void CreateCardDisplay(CardData card)
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
            if (CardManager.Instance != null)
            {
                CardManager.Instance.OnCardDrawn.RemoveListener(ShowSingleCardResult);
                CardManager.Instance.OnMultipleCardsDrawn.RemoveListener(ShowMultipleCardResults);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RemoveResourcePointsListener(UpdateButtonStates);
            }
        }
    }
} 