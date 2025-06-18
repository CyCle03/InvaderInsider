using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using InvaderInsider.Data;
using InvaderInsider.Cards;
using TMPro;

namespace InvaderInsider.UI
{
    public class SummonChoicePanel : BasePanel
    {
        private const string LOG_PREFIX = "[SummonChoice] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "카드 선택 UI 초기화 실패: 카드 목록이 비어있습니다.",
            "카드 선택 UI 초기화: {0}개의 카드",
            "카드 선택: {0}",
            "카드 선택 UI 닫기",
            "Panel shown",
            "Panel hidden",
            "필수 UI 요소가 할당되지 않았습니다.",
            "cardContainer가 할당되지 않았습니다.",
            "cardButtonPrefab이 할당되지 않았습니다."
        };

        [Header("UI References")]
        [SerializeField] private Transform cardContainer;
        [SerializeField] private GameObject cardButtonPrefab;
        [SerializeField] private Button closeButton;

        private List<CardDBObject> availableCards;
        private List<Button> cardButtons = new List<Button>();
        private UIManager uiManager;
        private CardManager cardManager;

        protected override void Awake()
        {
            base.Awake();
            
            uiManager = UIManager.Instance;
            cardManager = CardManager.Instance;
            
            Initialize();
        }

        protected override void Initialize()
        {
            if (cardContainer == null || closeButton == null)
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[6]);
                #endif
                return;
            }

            closeButton.onClick.AddListener(HandleCloseClick);
        }

        public void SetupCards(List<CardDBObject> cards)
        {
            if (cards == null || cards.Count == 0)
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[0]);
                #endif
                return;
            }

            availableCards = cards;
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + string.Format(LOG_MESSAGES[1], cards.Count));
            #endif

            if (cardContainer == null)
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[7]);
                #endif
                return;
            }

            // 기존 카드 버튼 제거
            ClearCardButtons();

            // 새로운 카드 버튼 생성
            foreach (var card in cards)
            {
                CreateCardButton(card);
            }
        }

        private void ClearCardButtons()
        {
            foreach (var button in cardButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    Destroy(button.gameObject);
                }
            }
            cardButtons.Clear();
        }

        private void CreateCardButton(CardDBObject card)
        {
            if (cardButtonPrefab == null)
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[8]);
                #endif
                return;
            }

            GameObject buttonObj = Instantiate(cardButtonPrefab, cardContainer);
            
            // HideFlags 초기화 (에디터에서 이름 변경 가능하도록)
            #if UNITY_EDITOR
            buttonObj.hideFlags = HideFlags.None;
            #endif
            
            Button cardButton = buttonObj.GetComponent<Button>();
            if (cardButton != null)
            {
                CardButton cardButtonComponent = buttonObj.GetComponent<CardButton>();
                if (cardButtonComponent != null)
                {
                    cardButtonComponent.Initialize(card);
                    cardButton.onClick.AddListener(() => HandleCardSelect(card));
                }
                cardButtons.Add(cardButton);
            }
        }

        private void HandleCardSelect(CardDBObject selectedCard)
        {
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + string.Format(LOG_MESSAGES[2], selectedCard.cardName));
            #endif
            if (cardManager != null)
            {
                cardManager.OnCardChoiceSelected(selectedCard);
            }
        }

        private void HandleCloseClick()
        {
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[3]);
            #endif
            if (cardManager != null)
            {
                cardManager.OnCardChoiceSelected(null);
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[4]);
            #endif
        }

        protected override void OnHide()
        {
            base.OnHide();
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[5]);
            #endif
            ClearCardButtons();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
            }

            ClearCardButtons();
        }
    }
} 