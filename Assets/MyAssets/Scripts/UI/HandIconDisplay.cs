using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using InvaderInsider.Data;
using InvaderInsider.Cards;
using InvaderInsider.Managers;

namespace InvaderInsider.UI
{
    public class HandIconDisplay : BasePanel
    {
        private const string LOG_TAG = "[HandIconDisplay]";

        [Header("UI References")]
        [SerializeField] private Transform iconContainer;
        [SerializeField] private GameObject cardIconPrefab;
        [SerializeField] private Button openHandPanelButton;

        [Header("Panel References")]
        [SerializeField] private HandDisplayPanel handDisplayPanel;

        private CardManager cardManager;
        private readonly List<GameObject> currentIconItems = new List<GameObject>();
        private bool isInitialized = false;

        protected override void Initialize()
        {
            Debug.Log($"{LOG_TAG} Initialize called. HandIconDisplay active: {gameObject.activeSelf}, in hierarchy: {gameObject.activeInHierarchy}");
            base.Initialize();
            if (isInitialized) return;

            cardManager = CardManager.Instance;
            if (cardManager == null)
            {
                LogManager.LogError($"{LOG_TAG} CardManager 인스턴스를 찾을 수 없습니다.");
                return;
            }

            if (handDisplayPanel == null)
            {
                handDisplayPanel = FindObjectOfType<HandDisplayPanel>(true);
                 if (handDisplayPanel == null)
                 {
                    LogManager.LogError($"{LOG_TAG} HandDisplayPanel을 찾을 수 없습니다.");
                    return;
                 }
            }

            openHandPanelButton?.onClick.AddListener(OpenHandDisplayPanel);
            Debug.Log($"{LOG_TAG} Subscribing to OnHandCardsChanged. IconContainer active: {iconContainer?.gameObject.activeSelf}, in hierarchy: {iconContainer?.gameObject.activeInHierarchy}");
            cardManager.OnHandCardsChanged += OnHandDataChanged;

            // Initial population
            OnHandDataChanged(cardManager.GetHandCardIds());

            isInitialized = true;
            Debug.Log($"{LOG_TAG} Initialization complete.");
        }

        private void OnDestroy()
        {
            if (cardManager != null)
            {
                cardManager.OnHandCardsChanged -= OnHandDataChanged;
            }
            openHandPanelButton?.onClick.RemoveListener(OpenHandDisplayPanel);
        }

        private void OnHandDataChanged(List<int> handCardIds)
        {
            Debug.Log($"{LOG_TAG} OnHandDataChanged called with {handCardIds.Count} cards.");
            if (!isInitialized || cardManager == null) return;
            
            foreach (var item in currentIconItems)
            {
                Destroy(item);
            }
            currentIconItems.Clear();

            if (iconContainer == null || cardIconPrefab == null) return;

            if (handCardIds.Count > 0)
            {
                Show(); // 핸드에 카드가 있으면 활성화
                foreach (int cardId in handCardIds)
                {
                    var cardData = cardManager.GetCardById(cardId);
                    if (cardData == null) continue;

                    GameObject iconObj = Instantiate(cardIconPrefab, iconContainer);
                    iconObj.name = $"CardIcon_{cardData.cardId}"; // Add name for easier debugging
                    var iconRectTransform = iconObj.GetComponent<RectTransform>();
                    if (iconRectTransform != null)
                    {
                        Debug.Log($"{LOG_TAG} Created icon {iconObj.name} at position {iconRectTransform.anchoredPosition} with size {iconRectTransform.sizeDelta}");
                    }
                    var iconImage = iconObj.GetComponent<Image>();
                    if (iconImage != null && cardData.artwork != null)
                    {
                        iconImage.sprite = cardData.artwork; 
                    }
                    currentIconItems.Add(iconObj);
                }
            }
            else
            {
                Hide(); // 핸드에 카드가 없으면 비활성화
            }
            Debug.Log($"{LOG_TAG} OnHandDataChanged finished. {currentIconItems.Count} icons created. Panel active: {gameObject.activeSelf}");
        }

        private void OpenHandDisplayPanel()
        {
            handDisplayPanel?.OpenPopup();
        }
    }
}
