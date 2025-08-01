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
        [SerializeField] private Button playButton;

        private CardDBObject currentCard;
        private CardManager cardManager;

        protected override void Initialize()
        {
            base.Initialize();
            cardManager = CardManager.Instance;
            if (cardManager == null)
            {
                Debug.LogError($"{LOG_TAG} CardManager 인스턴스를 찾을 수 없습니다.");
            }

            closeButton?.onClick.AddListener(HideView);
            playButton?.onClick.AddListener(PlayCard);
        }

        private void OnDestroy()
        {
            closeButton?.onClick.RemoveListener(HideView);
            playButton?.onClick.RemoveListener(PlayCard);
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

        private void PlayCard()
        {
            if (currentCard == null || cardManager == null)
            {
                Debug.LogWarning($"{LOG_TAG} 카드를 사용하기 위한 정보가 부족합니다.");
                return;
            }

            // GameManager를 통해 카드를 사용하도록 변경
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayCard(currentCard);
            }
            else
            {
                Debug.LogError($"{LOG_TAG} GameManager 인스턴스를 찾을 수 없습니다.");
            }

            // 핸드에서 카드 제거
            cardManager.RemoveCardFromHand(currentCard.cardId);

            HideView();
        }
    }
}
