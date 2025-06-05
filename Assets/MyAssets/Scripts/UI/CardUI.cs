using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using InvaderInsider.Cards;
using InvaderInsider.Data;

namespace InvaderInsider.UI
{
    public class CardUI : MonoBehaviour
    {
        [Header("Card Elements")]
        [SerializeField] private Image cardImage;
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI cardDescriptionText;
        [SerializeField] private Button cardButton;

        private CardDBObject cardData;
        public event Action OnCardClicked;

        private void Awake()
        {
            cardButton?.onClick.AddListener(() => OnCardClicked?.Invoke());
        }

        public void SetCard(CardDBObject data)
        {
            cardData = data;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (cardData == null) return;

            if (cardImage != null)
                cardImage.sprite = cardData.artwork;
            
            if (cardNameText != null)
                cardNameText.text = cardData.cardName;
            
            if (cardDescriptionText != null)
                cardDescriptionText.text = cardData.description;
        }

        public CardDBObject GetCardData()
        {
            return cardData;
        }

        private void OnDestroy()
        {
            cardButton?.onClick.RemoveAllListeners();
        }
    }
} 