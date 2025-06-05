using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InvaderInsider.Data;

namespace InvaderInsider.Cards
{
    public class CardDisplay : MonoBehaviour
    {
        [Header("Card Elements")]
        [SerializeField] private Image artworkImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI powerText;
        [SerializeField] private Image cardFrame;
        [SerializeField] private Image typeIcon;

        [Header("Visual Settings")]
        [SerializeField] private Color[] rarityColors;  // Common, Rare, Epic, Legendary
        [SerializeField] private Sprite[] typeIcons;    // Unit, Spell, Trap

        private CardDBObject cardData;

        public void SetupCard(CardDBObject data)
        {
            cardData = data;
            UpdateCardVisuals();
        }

        private void UpdateCardVisuals()
        {
            if (cardData == null) return;

            // 기본 정보 표시
            if (artworkImage != null && cardData.artwork != null)
                artworkImage.sprite = cardData.artwork;
            
            if (nameText != null)
                nameText.text = cardData.cardName;
            
            if (descriptionText != null)
                descriptionText.text = cardData.description;
            
            if (costText != null)
                costText.text = cardData.cost.ToString();
            
            if (powerText != null)
                powerText.text = cardData.power.ToString();

            // 레어도에 따른 프레임 색상 변경
            if (cardFrame != null && rarityColors.Length > (int)cardData.rarity)
                cardFrame.color = rarityColors[(int)cardData.rarity];

            // 카드 타입 아이콘 설정
            if (typeIcon != null && typeIcons.Length > (int)cardData.type)
                typeIcon.sprite = typeIcons[(int)cardData.type];
        }

        public CardDBObject GetCardData()
        {
            return cardData;
        }
    }
} 