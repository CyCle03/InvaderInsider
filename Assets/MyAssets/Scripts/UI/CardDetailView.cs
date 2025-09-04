using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InvaderInsider.Data;
using InvaderInsider.Cards;
using InvaderInsider.Managers;

namespace InvaderInsider.UI
{
    public class CardDetailView : BasePanel
    {
        private const string LOG_TAG = "[CardDetailView]";

        [Header("UI Elements")]
        [SerializeField] private Image cardArtwork;
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI cardDescriptionText;
        [SerializeField] private TextMeshProUGUI cardStatsText; // For Cost, Rarity etc.
        [SerializeField] private Button closeButton;

        private CardDBObject currentCard;
        private CardManager cardManager;

        protected override void Initialize()
        {
            base.Initialize();
            UIManager.Instance?.RegisterPanel("CardDetailView", this);

            cardManager = CardManager.Instance;
            if (cardManager == null)
            {
                Debug.LogError($"{LOG_TAG} CardManager 인스턴스를 찾을 수 없습니다.");
            }

            closeButton?.onClick.AddListener(HideView);
        }

        private void OnDestroy()
        {
            closeButton?.onClick.RemoveListener(HideView);
        }

        public void ShowCard(CardDBObject cardData)
        {
            if (cardData == null) return;

            currentCard = cardData;

            if (cardArtwork != null && cardData.artwork != null)
            {
                cardArtwork.sprite = cardData.artwork;
            }
            if (cardNameText != null)
            {
                cardNameText.text = cardData.cardName;
            }
            if (cardDescriptionText != null)
            {
                cardDescriptionText.text = cardData.description;
            }
            if (cardStatsText != null)
            { 
                cardStatsText.text = $"Cost: {cardData.cost} | Rarity: {cardData.rarity}";
            }

            Show();
        }

        private void HideView()
        {
            Hide();
            currentCard = null;
        }

        
    }
}
