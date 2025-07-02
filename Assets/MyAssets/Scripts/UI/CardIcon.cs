using UnityEngine;
using UnityEngine.UI;
using InvaderInsider.Data;
using InvaderInsider.Cards;

namespace InvaderInsider.UI
{
    public class CardIcon : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image cardImage;
        [SerializeField] private Image rarityGlow;
        [SerializeField] private Button cardButton;

        private CardDBObject cardData;
        public System.Action<CardDBObject> OnCardClicked;

        private void Start()
        {
            if (cardButton != null)
                cardButton.onClick.AddListener(HandleCardClick);
        }

        public void InitializeIcon(CardDBObject card)
        {
            cardData = card;
            
            // 카드 이미지 설정
            if (cardImage != null && card.cardIcon != null)
                cardImage.sprite = card.cardIcon;

            // 등급별 빛 효과
            if (rarityGlow != null)
                SetRarityGlow(card.rarity);
        }

        private void SetRarityGlow(CardRarity rarity)
        {
            Color glowColor = Color.white;
            switch (rarity)
            {
                case CardRarity.Common: glowColor = Color.gray; break;
                case CardRarity.Rare: glowColor = Color.blue; break;
                case CardRarity.Epic: glowColor = Color.magenta; break;
                case CardRarity.Legendary: glowColor = Color.yellow; break;
            }
            rarityGlow.color = glowColor;
        }

        private void HandleCardClick()
        {
            OnCardClicked?.Invoke(cardData);
        }

        private void OnDestroy()
        {
            if (cardButton != null)
                cardButton.onClick.RemoveListener(HandleCardClick);
        }
    }
} 