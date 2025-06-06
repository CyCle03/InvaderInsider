using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InvaderInsider.Data;

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
        [SerializeField] private Image cardFrame;
        [SerializeField] private Image typeIcon;
        [SerializeField] private GameObject highlightPanel; // 하이라이트 효과를 위한 GameObject 또는 Image
        [SerializeField] private GameObject cardBackPanel; // 카드 뒷면을 위한 GameObject

        [Header("Visual Settings")]
        [SerializeField] private Color[] rarityColors;  // Common, Rare, Epic, Legendary
        [SerializeField] private Sprite[] typeIcons;    // Unit, Spell, Trap
        [SerializeField] private Sprite[] typeFrames;   // 타입별 카드 프레임 스프라이트
        [SerializeField] private int maxDescriptionLength = 100; // 설명 텍스트 최대 길이

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

            // 레어도에 따른 프레임 색상 변경
            if (cardFrame != null && rarityColors.Length > (int)cardData.rarity)
                cardFrame.color = rarityColors[(int)cardData.rarity];

            // 카드 타입 아이콘 설정
            if (typeIcon != null && typeIcons.Length > (int)cardData.type)
                typeIcon.sprite = typeIcons[(int)cardData.type];

            // 카드 타입에 따른 프레임 변경
            if (cardFrame != null && typeFrames.Length > (int)cardData.type)
                cardFrame.sprite = typeFrames[(int)cardData.type];

            // 초기에는 하이라이트 비활성화
            SetHighlight(false);

            // 카드를 설정할 때는 앞면을 보이도록 함
            ShowCardFront(true);
        }

        public void SetHighlight(bool isHighlighted)
        {
            if (highlightPanel != null)
            {
                highlightPanel.SetActive(isHighlighted);
            }
        }

        public void ShowCardFront(bool showFront)
        {
            // 카드의 앞면 요소들을 활성화/비활성화
            artworkImage.gameObject.SetActive(showFront);
            nameText.gameObject.SetActive(showFront);
            descriptionText.gameObject.SetActive(showFront);
            costText.gameObject.SetActive(showFront);
            powerText.gameObject.SetActive(showFront);
            cardFrame.gameObject.SetActive(showFront);
            typeIcon.gameObject.SetActive(showFront);

            // 카드 뒷면 패널 활성화/비활성화
            if (cardBackPanel != null)
            {
                cardBackPanel.SetActive(!showFront);
            }
        }

        public CardDBObject GetCardData()
        {
            return cardData;
        }
    }
} 