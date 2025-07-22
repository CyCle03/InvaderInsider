using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using InvaderInsider.Data;
using InvaderInsider.Cards;
using InvaderInsider.Managers;

namespace InvaderInsider.UI
{
    public class HandIconDisplay : MonoBehaviour
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

        void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            if (cardManager != null)
            {
                cardManager.OnHandCardsChanged -= OnHandDataChanged;
            }
            openHandPanelButton?.onClick.RemoveListener(OpenHandDisplayPanel);
        }

        private void Initialize()
        {
            if (isInitialized) return;

            cardManager = CardManager.Instance;
            if (cardManager == null)
            {
                Debug.LogError($"{LOG_TAG} CardManager 인스턴스를 찾을 수 없습니다.");
                return;
            }

            if (handDisplayPanel == null)
            {
                handDisplayPanel = FindObjectOfType<HandDisplayPanel>(true);
                 if (handDisplayPanel == null)
                 {
                    Debug.LogError($"{LOG_TAG} HandDisplayPanel을 찾을 수 없습니다.");
                    return;
                 }
            }

            openHandPanelButton?.onClick.AddListener(OpenHandDisplayPanel);
            cardManager.OnHandCardsChanged += OnHandDataChanged;

            // Initial population
            OnHandDataChanged(cardManager.GetHandCardIds());

            isInitialized = true;
        }

        private void OnHandDataChanged(List<int> handCardIds)
        {
            if (!isInitialized || cardManager == null) return;
            
            foreach (var item in currentIconItems)
            {
                Destroy(item);
            }
            currentIconItems.Clear();

            if (iconContainer == null || cardIconPrefab == null) return;

            foreach (int cardId in handCardIds)
            {
                var cardData = cardManager.GetCardById(cardId);
                if (cardData == null) continue;

                GameObject iconObj = Instantiate(cardIconPrefab, iconContainer);
                var iconImage = iconObj.GetComponent<Image>();
                if (iconImage != null && cardData.artwork != null)
                {
                    iconImage.sprite = cardData.artwork; 
                }
                currentIconItems.Add(iconObj);
            }
        }

        private void OpenHandDisplayPanel()
        {
            handDisplayPanel?.OpenPopup();
        }
    }
}
