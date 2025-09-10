using UnityEngine;
using UnityEngine.UI;
using InvaderInsider.Managers;
using InvaderInsider.Cards;
using TMPro;
using System.Collections.Generic;

namespace InvaderInsider.UI
{
    public class InGamePanel : BasePanel
    {
        private const string LOG_TAG = "[InGamePanel]";

        [Header("Buttons")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button summonButton;
        [SerializeField] private Button showHandButton;

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI handCardCountText;

        private CardManager cardManager;
        private bool isInitialized = false;

        protected override void Initialize()
        {
            if (isInitialized) return;

            cardManager = CardManager.Instance;
            if (cardManager == null)
            {
                Debug.LogError($"{LOG_TAG} CardManager 인스턴스를 찾을 수 없습니다.");
                return;
            }

            SetupButtons();

            cardManager.OnHandCardsChanged += UpdateHandCardCount;
            UpdateHandCardCount(cardManager.GetHandCardKeys()); // 초기값 설정

            isInitialized = true;
        }

        private void OnDestroy()
        {
            if (cardManager != null)
            {
                cardManager.OnHandCardsChanged -= UpdateHandCardCount;
            }
        }

        private void SetupButtons()
        {
            pauseButton?.onClick.AddListener(OnPauseButtonClicked);
            summonButton?.onClick.AddListener(OnSummonButtonClicked);
            showHandButton?.onClick.AddListener(OnShowHandButtonClicked);
        }

        private void OnPauseButtonClicked()
        {
            GameManager.Instance?.PauseGame(true);
        }

        private void OnSummonButtonClicked()
        {
            cardManager?.Summon();
        }

        private void OnShowHandButtonClicked()
        {
            // UIManager를 통해 HandDisplayPanel을 보여주는 로직
            // 예: UIManager.Instance.ShowPanel("HandDisplay");
        }

        private void UpdateHandCardCount(List<string> handCardKeys)
        {
            if (handCardCountText != null)
            {
                handCardCountText.text = $"핸드: {handCardKeys.Count}";
            }
        }
    }
}