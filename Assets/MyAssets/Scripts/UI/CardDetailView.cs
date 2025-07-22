using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InvaderInsider.Data;
using InvaderInsider.Cards;

namespace InvaderInsider.UI
{
    public class CardDetailView : MonoBehaviour
    {
        private const string LOG_TAG = "[CardDetailView]";

        [Header("UI Elements")]
        [SerializeField] private GameObject detailViewOverlay;
        [SerializeField] private Image cardArtwork;
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI cardDescriptionText;
        [SerializeField] private TextMeshProUGUI cardStatsText; // For Cost, Rarity etc.
        [SerializeField] private Button closeButton;
        [SerializeField] private Button playButton;

        private CardDBObject currentCard;
        private CardManager cardManager;

        void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            closeButton?.onClick.RemoveListener(HideView);
            playButton?.onClick.RemoveListener(PlayCard);
        }

        private void Initialize()
        {
            cardManager = CardManager.Instance;
            if (cardManager == null)
            {
                Debug.LogError($"{LOG_TAG} CardManager 인스턴스를 찾을 수 없습니다.");
            }

            closeButton?.onClick.AddListener(HideView);
            playButton?.onClick.AddListener(PlayCard);
            detailViewOverlay.SetActive(false);
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

            detailViewOverlay.SetActive(true);
        }

        private void HideView()
        {
            detailViewOverlay.SetActive(false);
            currentCard = null;
        }

        private void PlayCard()
        {
            if (currentCard == null || cardManager == null)
            {
                Debug.LogWarning($"{LOG_TAG} 카드를 사용하기 위한 정보가 부족합니다.");
                return;
            }

            // CardManager에 카드 사용 요청
            // 이 부분은 CardManager의 기능에 따라 구현이 달라질 수 있습니다.
            // 예를 들어, OnCardChoiceSelected를 재사용하거나 새로운 메서드를 호출할 수 있습니다.
            cardManager.OnCardChoiceSelected(currentCard);

            HideView();
        }
    }
}
