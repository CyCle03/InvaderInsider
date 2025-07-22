using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InvaderInsider.Data;
using InvaderInsider.Managers; // LogManager 사용을 위해 추가

namespace InvaderInsider.Cards
{
    [RequireComponent(typeof(RectTransform))]
    public class CardDisplay : MonoBehaviour
    {
        [Header("Card Elements")]
        [SerializeField] private Image artworkImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI powerText;
        [SerializeField] private TextMeshProUGUI typeText; // CardType 표시용
        [SerializeField] private Image rarityFrame;
        [SerializeField] private Image rarityIcon;
        [SerializeField] private Image typeIcon;
        [SerializeField] private Image cardFrame;
        [SerializeField] private GameObject highlightPanel;
        [SerializeField] private GameObject equipmentBonusPanel;
        [SerializeField] private TextMeshProUGUI equipmentAttackBonusText;
        [SerializeField] private TextMeshProUGUI equipmentHealthBonusText;

        [Header("Visual Settings")]
        [SerializeField] private Color[] rarityColors;  // Common, Rare, Epic, Legendary
        [SerializeField] private Sprite commonIcon;
        [SerializeField] private Sprite rareIcon;
        [SerializeField] private Sprite epicIcon;
        [SerializeField] private Sprite legendaryIcon;
        [SerializeField] private Sprite[] typeIcons;    // Unit, Spell, Trap (CardType 순서와 일치해야 함)
        [SerializeField] private Sprite[] typeFrames;   // 타입별 카드 프레임 스프라이트 (CardType 순서와 일치해야 함)
        [SerializeField] private int maxDescriptionLength = 100; // 설명 텍스트 최대 길이

        private CardDBObject cardData;

        public void SetupCard(CardDBObject data)
        {
            if (data == null)
            {
                LogManager.Error("CardDisplay", "SetupCard에 전달된 카드 데이터가 null입니다.");
                return;
            }
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
            {
                string description = cardData.description;
                if (description.Length > maxDescriptionLength)
                {
                    description = description.Substring(0, maxDescriptionLength) + "...";
                }
                descriptionText.text = description;
            }

            if (costText != null)
                costText.text = cardData.cost.ToString();

            if (powerText != null)
                powerText.text = cardData.power.ToString();

            if (typeText != null)
                typeText.text = GetCardTypeDisplayName(cardData.type);

            // 레어도에 따른 프레임 색상 변경
            if (rarityFrame != null && rarityColors != null && rarityColors.Length > (int)cardData.rarity)
                rarityFrame.color = rarityColors[(int)cardData.rarity];

            // 레어도 아이콘 설정
            if (rarityIcon != null)
            {
                rarityIcon.sprite = GetRarityIcon(cardData.rarity);
                rarityIcon.gameObject.SetActive(rarityIcon.sprite != null);
            }

            // 카드 타입 아이콘 설정
            if (typeIcon != null && typeIcons != null && typeIcons.Length > (int)cardData.type)
                typeIcon.sprite = typeIcons[(int)cardData.type];

            // 카드 타입에 따른 프레임 변경
            if (cardFrame != null && typeFrames != null && typeFrames.Length > (int)cardData.type)
                cardFrame.sprite = typeFrames[(int)cardData.type];

            // 장비 카드 보너스 패널
            if (equipmentBonusPanel != null)
            {
                equipmentBonusPanel.SetActive(cardData.type == CardType.Equipment);
                if (cardData.type == CardType.Equipment)
                {
                    if (equipmentAttackBonusText != null)
                        equipmentAttackBonusText.text = $"+{cardData.equipmentBonusAttack}";
                    if (equipmentHealthBonusText != null)
                        equipmentHealthBonusText.text = $"+{cardData.equipmentBonusHealth}";
                }
            }

            // 초기에는 하이라이트 비활성화
            SetHighlight(false);
        }

        public void SetHighlight(bool isHighlighted)
        {
            if (highlightPanel != null)
            {
                highlightPanel.SetActive(isHighlighted);
            }
        }

        public CardDBObject GetCardData()
        {
            return cardData;
        }

        private string GetCardTypeDisplayName(CardType type)
        {
            switch (type)
            {
                case CardType.Character:
        return "캐릭터";
                case CardType.Equipment:
        return "장비";
                case CardType.Tower:
        return "타워";
                default:
        return "알 수 없음";
            }
        }

        private Color GetRarityColor(CardRarity rarity)
        {
            if (rarityColors == null || rarityColors.Length == 0) return Color.white;
            return rarityColors.Length > (int)rarity ? rarityColors[(int)rarity] : Color.white;
        }
        private Sprite GetRarityIcon(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common:
        return commonIcon;
                case CardRarity.Rare:
        return rareIcon;
                case CardRarity.Epic:
        return epicIcon;
                case CardRarity.Legendary:
        return legendaryIcon;
                default:
        return null;
            }
        }
    }
}