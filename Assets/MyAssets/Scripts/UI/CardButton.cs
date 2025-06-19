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
        [SerializeField] private TextMeshProUGUI cardPowerText;
        [SerializeField] private TextMeshProUGUI cardTypeText;
        [SerializeField] private Image rarityFrame;
        [SerializeField] private Image rarityIcon;
        [SerializeField] private GameObject equipmentBonusPanel;
        [SerializeField] private TextMeshProUGUI equipmentAttackBonusText;
        [SerializeField] private TextMeshProUGUI equipmentHealthBonusText;

        [Header("Rarity Sprites")]
        [SerializeField] private Sprite commonIcon;
        [SerializeField] private Sprite rareIcon;
        [SerializeField] private Sprite epicIcon;
        [SerializeField] private Sprite legendaryIcon;

        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "CardButton: 필수 UI 요소가 할당되지 않았습니다.",
            "CardButton: 카드 데이터가 업데이트되었습니다: {0}",
            "CardButton: 장비 카드로 설정되었습니다.",
            "CardButton: 일반 카드로 설정되었습니다."
        };

        private CardDBObject cardData;
        private Button cardButton;

        private void Awake()
        {
            cardButton = GetComponent<Button>();
        }

        public void Initialize(CardDBObject card)
        {
            if (card == null)
            {
                #if UNITY_EDITOR
                Debug.LogError("CardButton: 초기화에 전달된 CardDBObject가 null입니다.");
                #endif
                return;
            }

            cardData = card;
            
            // 필수 요소 검증
            bool hasRequiredElements = ValidateRequiredReferences();
            
            // 필수 요소가 없어도 기본 동작은 수행 (에러 방지)
            if (hasRequiredElements)
            {
                UpdateUI();
                #if UNITY_EDITOR
                Debug.Log(string.Format(LOG_MESSAGES[1], card.cardName));
                #endif
            }
            else
            {
                // 필수 요소가 없는 경우에도 가능한 업데이트 수행
                UpdateUIWithNullCheck();
                #if UNITY_EDITOR
                Debug.LogWarning($"CardButton: 필수 요소 누락으로 제한된 초기화를 수행했습니다. 카드: {card.cardName}");
                #endif
            }
        }

        /// <summary>
        /// 카드 데이터를 동적으로 변경
        /// </summary>
        public void UpdateCardData(CardDBObject newCard)
        {
            if (newCard == null) return;
            
            cardData = newCard;
            UpdateUI();
            
            #if UNITY_EDITOR
            Debug.Log(string.Format(LOG_MESSAGES[1], newCard.cardName));
            #endif
        }

        /// <summary>
        /// 특정 카드 속성만 업데이트
        /// </summary>
        public void UpdateCardCost(int newCost)
        {
            if (cardData != null)
            {
                cardData.cost = newCost;
                if (cardCostText != null)
                {
                    cardCostText.text = newCost.ToString();
                }
            }
        }

        public void UpdateCardPower(int newPower)
        {
            if (cardData != null)
            {
                cardData.power = newPower;
                if (cardPowerText != null)
                {
                    cardPowerText.text = newPower.ToString();
                }
            }
        }

        public void UpdateCardImage(Sprite newSprite)
        {
            if (cardData != null && cardImage != null)
            {
                cardData.artwork = newSprite;
                cardImage.sprite = newSprite;
            }
        }

        public void UpdateCardDescription(string newDescription)
        {
            if (cardData != null)
            {
                cardData.description = newDescription;
                if (cardDescriptionText != null)
                {
                    cardDescriptionText.text = newDescription;
                }
            }
        }

        public CardDBObject GetCardData()
        {
            return cardData;
        }

        private bool ValidateRequiredReferences()
        {
            // 필수 UI 요소들을 개별적으로 체크하고 누락된 요소 알려주기
            bool isValid = true;
            string missingElements = "";

            if (cardImage == null)
            {
                missingElements += "cardImage, ";
                isValid = false;
            }
            
            if (cardNameText == null)
            {
                missingElements += "cardNameText, ";
                isValid = false;
            }
            
            if (cardDescriptionText == null)
            {
                missingElements += "cardDescriptionText, ";
                isValid = false;
            }
            
            if (cardCostText == null)
            {
                missingElements += "cardCostText, ";
                isValid = false;
            }
            
            if (rarityFrame == null)
            {
                missingElements += "rarityFrame, ";
                isValid = false;
            }

            if (!isValid)
            {
                missingElements = missingElements.TrimEnd(' ', ',');
                Debug.LogError($"CardButton: 다음 필수 UI 요소들이 할당되지 않았습니다: {missingElements}");
                Debug.LogError($"CardButton: 프리팹 경로를 확인하세요. GameObject 이름: {gameObject.name}");
            }
            
            // 선택적 UI 요소들에 대한 경고 (새로 추가된 요소들)
            #if UNITY_EDITOR
            if (cardPowerText == null)
            {
                Debug.LogWarning("CardButton: cardPowerText가 할당되지 않았습니다. 파워 정보가 표시되지 않습니다.");
            }
            
            if (cardTypeText == null)
            {
                Debug.LogWarning("CardButton: cardTypeText가 할당되지 않았습니다. 타입 정보가 표시되지 않습니다.");
            }
            
            if (rarityIcon == null)
            {
                Debug.LogWarning("CardButton: rarityIcon이 할당되지 않았습니다. 등급 아이콘이 표시되지 않습니다.");
            }
            
            if (equipmentBonusPanel == null)
            {
                Debug.LogWarning("CardButton: equipmentBonusPanel이 할당되지 않았습니다. 장비 보너스 정보가 표시되지 않습니다.");
            }
            #endif
            
            return isValid;
        }

        private void UpdateUI()
        {
            if (cardData == null) return;

            // 기본 카드 정보
            UpdateBasicCardInfo();
            
            // 레어도 관련 UI
            UpdateRarityDisplay();
            
            // 카드 타입에 따른 특별 UI
            UpdateTypeSpecificUI();
        }

        private void UpdateBasicCardInfo()
        {
            // 카드 이미지
            if (cardData.artwork != null && cardImage != null)
            {
                cardImage.sprite = cardData.artwork;
            }

            // 카드 이름
            if (cardNameText != null)
            {
                cardNameText.text = cardData.cardName;
            }

            // 카드 설명
            if (cardDescriptionText != null)
            {
                cardDescriptionText.text = cardData.description;
            }

            // 카드 비용
            if (cardCostText != null)
            {
                cardCostText.text = cardData.cost.ToString();
            }

            // 카드 능력치
            if (cardPowerText != null)
            {
                cardPowerText.text = cardData.power.ToString();
            }

            // 카드 타입
            if (cardTypeText != null)
            {
                cardTypeText.text = GetCardTypeDisplayName(cardData.type);
            }
        }

        private void UpdateRarityDisplay()
        {
            // 레어도에 따른 프레임 색상 설정
            if (rarityFrame != null)
            {
                rarityFrame.color = GetRarityColor(cardData.rarity);
            }

            // 레어도 아이콘 설정
            if (rarityIcon != null)
            {
                rarityIcon.sprite = GetRarityIcon(cardData.rarity);
                rarityIcon.gameObject.SetActive(rarityIcon.sprite != null);
            }
        }

        private void UpdateTypeSpecificUI()
        {
            // 장비 카드인 경우 보너스 정보 표시
            if (cardData.type == CardType.Equipment)
            {
                if (equipmentBonusPanel != null)
                {
                    equipmentBonusPanel.SetActive(true);
                    
                    if (equipmentAttackBonusText != null)
                    {
                        equipmentAttackBonusText.text = $"+{cardData.equipmentBonusAttack}";
                    }
                    
                    if (equipmentHealthBonusText != null)
                    {
                        equipmentHealthBonusText.text = $"+{cardData.equipmentBonusHealth}";
                    }
                    
                    #if UNITY_EDITOR
                    Debug.Log(LOG_MESSAGES[2]);
                    #endif
                }
                else
                {
                    #if UNITY_EDITOR
                    Debug.LogWarning("CardButton: 장비 카드이지만 equipmentBonusPanel이 할당되지 않았습니다.");
                    #endif
                }
            }
            else
            {
                // 장비가 아닌 카드의 경우 장비 보너스 패널 숨김
                if (equipmentBonusPanel != null)
                {
                    equipmentBonusPanel.SetActive(false);
                }
                
                #if UNITY_EDITOR
                Debug.Log(LOG_MESSAGES[3]);
                #endif
            }
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
            switch (rarity)
            {
                case CardRarity.Common:
                    return new Color(0.7f, 0.7f, 0.7f, 1f); // 회색
                case CardRarity.Rare:
                    return new Color(0.0f, 0.5f, 1.0f, 1f); // 파란색
                case CardRarity.Epic:
                    return new Color(0.6f, 0.0f, 0.8f, 1f); // 보라색
                case CardRarity.Legendary:
                    return new Color(1.0f, 0.5f, 0.0f, 1f); // 주황색
                default:
                    return Color.white;
            }
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

        /// <summary>
        /// 카드 버튼 상태 설정 (클릭 가능/불가능)
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (cardButton != null)
            {
                cardButton.interactable = interactable;
            }
        }

        /// <summary>
        /// 카드가 선택된 상태 표시
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (rarityFrame != null)
            {
                // 선택된 카드는 프레임을 더 밝게 표시
                Color baseColor = GetRarityColor(cardData?.rarity ?? CardRarity.Common);
                rarityFrame.color = selected ? baseColor * 1.5f : baseColor;
            }
        }

        /// <summary>
        /// Null 체크를 통한 안전한 UI 업데이트 (필수 요소가 누락된 경우용)
        /// </summary>
        private void UpdateUIWithNullCheck()
        {
            if (cardData == null) return;

            try
            {
                // 기본 카드 정보 (null 체크 포함)
                if (cardImage != null && cardData.artwork != null)
                {
                    cardImage.sprite = cardData.artwork;
                }

                if (cardNameText != null)
                {
                    cardNameText.text = cardData.cardName;
                }

                if (cardDescriptionText != null)
                {
                    cardDescriptionText.text = cardData.description;
                }

                if (cardCostText != null)
                {
                    cardCostText.text = cardData.cost.ToString();
                }

                if (cardPowerText != null)
                {
                    cardPowerText.text = cardData.power.ToString();
                }

                if (cardTypeText != null)
                {
                    cardTypeText.text = GetCardTypeDisplayName(cardData.type);
                }

                // 레어도 관련
                if (rarityFrame != null)
                {
                    rarityFrame.color = GetRarityColor(cardData.rarity);
                }

                if (rarityIcon != null)
                {
                    rarityIcon.sprite = GetRarityIcon(cardData.rarity);
                    rarityIcon.gameObject.SetActive(rarityIcon.sprite != null);
                }

                // 타입별 특별 UI
                UpdateTypeSpecificUI();
            }
            catch (System.Exception ex)
            {
                #if UNITY_EDITOR
                Debug.LogError($"CardButton: UI 업데이트 중 예외 발생: {ex.Message}");
                #endif
            }
        }

        /// <summary>
        /// 디버그용: UI 요소 할당 상태 확인
        /// </summary>
        #if UNITY_EDITOR
        [System.Obsolete("디버그용 메서드입니다. 프로덕션에서는 제거하세요.")]
        public void DebugUIAssignmentStatus()
        {
            Debug.Log("=== CardButton UI 할당 상태 ===");
            Debug.Log($"cardImage: {(cardImage != null ? "할당됨" : "할당 안됨")}");
            Debug.Log($"cardNameText: {(cardNameText != null ? "할당됨" : "할당 안됨")}");
            Debug.Log($"cardDescriptionText: {(cardDescriptionText != null ? "할당됨" : "할당 안됨")}");
            Debug.Log($"cardCostText: {(cardCostText != null ? "할당됨" : "할당 안됨")}");
            Debug.Log($"cardPowerText: {(cardPowerText != null ? "할당됨" : "할당 안됨")}");
            Debug.Log($"cardTypeText: {(cardTypeText != null ? "할당됨" : "할당 안됨")}");
            Debug.Log($"rarityFrame: {(rarityFrame != null ? "할당됨" : "할당 안됨")}");
            Debug.Log($"rarityIcon: {(rarityIcon != null ? "할당됨" : "할당 안됨")}");
            Debug.Log($"equipmentBonusPanel: {(equipmentBonusPanel != null ? "할당됨" : "할당 안됨")}");
            Debug.Log($"equipmentAttackBonusText: {(equipmentAttackBonusText != null ? "할당됨" : "할당 안됨")}");
            Debug.Log($"equipmentHealthBonusText: {(equipmentHealthBonusText != null ? "할당됨" : "할당 안됨")}");
            Debug.Log("================================");
        }
        #endif
    }
} 