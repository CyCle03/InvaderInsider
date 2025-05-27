using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InvaderInsider.Cards
{
    public class CardDisplay : MonoBehaviour
    {
        [Header("Card UI Elements")]
        [SerializeField] private Image cardImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Image rarityBorder;
        [SerializeField] private Image typeIcon;

        private CardData cardData;

        public void SetupCard(CardData data)
        {
            cardData = data;
            UpdateCardDisplay();
        }

        private void UpdateCardDisplay()
        {
            if (cardData == null) return;

            if (cardImage != null && cardData.cardImage != null)
                cardImage.sprite = cardData.cardImage;

            if (nameText != null)
                nameText.text = cardData.cardName;

            if (descriptionText != null)
                descriptionText.text = cardData.description;

            if (costText != null)
                costText.text = cardData.eDataCost.ToString();

            // 레어도에 따른 테두리 색상 설정
            if (rarityBorder != null)
            {
                Color rarityColor = GetRarityColor(cardData.rarity);
                rarityBorder.color = rarityColor;
            }

            // 카드 타입에 따른 아이콘 설정
            if (typeIcon != null)
            {
                // TODO: 카드 타입별 아이콘 스프라이트 설정
            }
        }

        private Color GetRarityColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common:
                    return Color.gray;
                case CardRarity.Rare:
                    return Color.blue;
                case CardRarity.Epic:
                    return new Color(0.5f, 0f, 0.5f); // Purple
                case CardRarity.Legendary:
                    return Color.yellow;
                default:
                    return Color.white;
            }
        }

        public CardData GetCardData()
        {
            return cardData;
        }
    }
} 