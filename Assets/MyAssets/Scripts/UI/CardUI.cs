using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using InvaderInsider.Cards;
using InvaderInsider.Data;

namespace InvaderInsider.UI
{
    [RequireComponent(typeof(CardDisplay))] // CardDisplay 요구
    [RequireComponent(typeof(CardInteractionHandler))] // CardInteractionHandler 요구
    public class CardUI : MonoBehaviour
    {
        private const string LOG_PREFIX = "[UI] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "Card: CardDisplay component not found",
            "Card: CardInteractionHandler component not found",
            "Card: Card data set - ID {0}"
        };

        // [Header("Card Interaction")] // Button 필드를 제거했으므로 주석 처리하거나 제거
        // [SerializeField] private Button cardButton; // 제거
        private CardDisplay cardDisplay; // CardDisplay 컴포넌트 참조
        private CardInteractionHandler cardInteractionHandler; // CardInteractionHandler 컴포넌트 참조

        private CardDBObject cardData;
        private readonly string[] cachedStrings = new string[3];

        public int CardId => cardData?.cardId ?? -1;
        public event Action OnCardClicked; // 카드 클릭 이벤트

        private void Awake()
        {
            cardDisplay = GetComponent<CardDisplay>();
            if (cardDisplay == null)
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[0], this);
                return;
            }

            cardInteractionHandler = GetComponent<CardInteractionHandler>(); // 같은 GameObject에서 CardInteractionHandler 컴포넌트 가져오기
            if (cardInteractionHandler == null)
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[1], this);
                return;
            }
            else
            {
                // CardInteractionHandler의 OnCardClicked 이벤트 구독
                cardInteractionHandler.OnCardClicked += HandleCardInteractionClicked;
            }
            // cardButton?.onClick.AddListener(() => OnCardClicked?.Invoke()); // 제거
        }

        public void SetCard(CardDBObject data)
        {
            if (data == null) return;

            cardData = data;
            if (cardDisplay != null)
            {
                cardDisplay.SetupCard(cardData); // CardDisplay를 통해 시각적 요소 설정
                if (cachedStrings[2] == null)
                {
                    cachedStrings[2] = string.Format(LOG_PREFIX + LOG_MESSAGES[2], cardData.cardId);
                }
            }
        }

        public CardDBObject GetCardData()
        {
            return cardData;
        }

        private void OnDestroy()
        {
            // cardButton?.onClick.RemoveAllListeners(); // 제거
            if (cardInteractionHandler != null)
            {
                cardInteractionHandler.OnCardClicked -= HandleCardInteractionClicked;
            }
        }

        private void HandleCardInteractionClicked()
        {
            OnCardClicked?.Invoke(); // CardUI의 이벤트를 발생시켜 외부에서 구독할 수 있도록 함
        }
    }
} 