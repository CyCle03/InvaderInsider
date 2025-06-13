using UnityEngine;
using UnityEngine.UI; // UI 요소 사용을 위해 추가
using System.Collections.Generic;
using InvaderInsider.Data; // CardDBObject 사용을 위해 추가
using InvaderInsider.Cards; // CardManager 사용을 위해 추가
using TMPro; // TextMeshPro 사용 시 추가

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
            "카드 선택 UI 닫기"
        };

        [Header("UI References")]
        [SerializeField] private Transform cardContainer;
        [SerializeField] private GameObject cardButtonPrefab;
        [SerializeField] private Button closeButton;

        private List<CardDBObject> availableCards;
        private List<Button> cardButtons;

        protected override void Initialize()
        {
            if (cardContainer == null || closeButton == null)
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + "필수 UI 요소가 할당되지 않았습니다.");
                #endif
                return;
            }

            closeButton.onClick.AddListener(HandleCloseClick);
            cardButtons = new List<Button>();
        }

        public void Initialize(List<CardDBObject> cards)
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
            Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[1], cards.Count));
            #endif

            // cardButtons 리스트가 null인 경우 초기화
            if (cardButtons == null)
            {
                cardButtons = new List<Button>();
            }

            // cardContainer null 체크 추가
            if (cardContainer == null)
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + "cardContainer가 할당되지 않았습니다.");
                #endif
                return;
            }

            // 기존 카드 버튼 제거
            foreach (var button in cardButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            cardButtons.Clear();

            // 새로운 카드 버튼 생성
            foreach (var card in cards)
            {
                if (cardButtonPrefab == null)
                {
                    #if UNITY_EDITOR
                    Debug.LogError(LOG_PREFIX + "cardButtonPrefab이 할당되지 않았습니다.");
                    #endif
                    continue;
                }

                GameObject buttonObj = Instantiate(cardButtonPrefab, cardContainer);
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
        }

        private void HandleCardSelect(CardDBObject selectedCard)
        {
            #if UNITY_EDITOR
            Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[2], selectedCard.cardName));
            #endif
            if (CardManager.Instance != null)
            {
                CardManager.Instance.OnCardChoiceSelected(selectedCard);
            }
            HandleCloseClick();
        }

        private void HandleCloseClick()
        {
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[3]);
            #endif
            if (CardManager.Instance != null)
            {
                CardManager.Instance.OnCardChoiceSelected(null);
            }
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleCloseClick);
            }

            if (cardButtons != null)
            {
                foreach (var button in cardButtons)
                {
                    if (button != null)
                    {
                        button.onClick.RemoveAllListeners();
                    }
                }
            }
        }
    }
} 