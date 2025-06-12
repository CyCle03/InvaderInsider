using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InvaderInsider.Data;
using InvaderInsider.Cards;

namespace InvaderInsider.UI
{
    public class CardButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image cardImage;
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI cardDescriptionText;
        [SerializeField] private TextMeshProUGUI cardCostText;
        [SerializeField] private Image rarityFrame;

        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "CardButton: 필수 UI 요소가 할당되지 않았습니다."
        };

        private CardDBObject cardData;

        public void Initialize(CardDBObject card)
        {
            if (!ValidateReferences())
            {
                return;
            }

            cardData = card;
            UpdateUI();
        }

        private bool ValidateReferences()
        {
            if (cardImage == null || cardNameText == null || cardDescriptionText == null || 
                cardCostText == null || rarityFrame == null)
            {
                Debug.LogError(LOG_MESSAGES[0]);
                return false;
            }
            return true;
        }

        private void UpdateUI()
        {
            if (cardData == null) return;

            // 카드 이미지
            if (cardData.artwork != null)
            {
                cardImage.sprite = cardData.artwork;
            }

            // 카드 이름
            cardNameText.text = cardData.cardName;

            // 카드 설명
            cardDescriptionText.text = cardData.description;

            // 카드 비용
            cardCostText.text = cardData.cost.ToString();

            // 레어도에 따른 프레임 색상 설정
            rarityFrame.color = GetRarityColor(cardData.rarity);
        }

        private Color GetRarityColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common:
                    return new Color(0.7f, 0.7f, 0.7f); // 회색
                case CardRarity.Rare:
                    return new Color(0.0f, 0.5f, 1.0f); // 파란색
                case CardRarity.Epic:
                    return new Color(0.6f, 0.0f, 0.8f); // 보라색
                case CardRarity.Legendary:
                    return new Color(1.0f, 0.5f, 0.0f); // 주황색
                default:
                    return Color.white;
            }
        }
    }
} 