using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InvaderInsider.Data;
using InvaderInsider.Managers;

namespace InvaderInsider.UI
{
    public class MainMenuPanel : BasePanel
    {
        [Header("Main Menu Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button deckButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button settingsButton;

        [Header("Optional Buttons")]
        [SerializeField] private Button achievementsButton;
        [SerializeField] private Button quitButton;

        [Header("Animation")]
        [SerializeField] private float buttonAnimationDelay = 0.1f;
        [SerializeField] private Animator menuAnimator;

        protected override void Initialize()
        {
            base.Initialize();
            SetupButtons();
        }

        private void SetupButtons()
        {
            // 필수 버튼들
            playButton?.onClick.AddListener(OnPlayButtonClicked);
            deckButton?.onClick.AddListener(OnDeckButtonClicked);
            shopButton?.onClick.AddListener(OnShopButtonClicked);
            settingsButton?.onClick.AddListener(OnSettingsButtonClicked);

            // 선택적 버튼들
            if (achievementsButton != null)
                achievementsButton.onClick.AddListener(OnAchievementsButtonClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitButtonClicked);
        }

        #region Button Click Handlers
        private void OnPlayButtonClicked()
        {
            menuAnimator?.SetTrigger("FadeOut");
            UIManager.Instance.ShowPanel("StageSelect");
        }

        private void OnDeckButtonClicked()
        {
            UIManager.Instance.ShowPanel("Deck");
        }

        private void OnShopButtonClicked()
        {
            UIManager.Instance.ShowPanel("Shop");
        }

        private void OnSettingsButtonClicked()
        {
            UIManager.Instance.ShowPanel("Settings");
        }

        private void OnAchievementsButtonClicked()
        {
            UIManager.Instance.ShowPanel("Achievements");
        }

        private void OnQuitButtonClicked()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        #endregion

        protected override void OnShow()
        {
            base.OnShow();
            menuAnimator?.SetTrigger("FadeIn");
        }

        protected override void OnHide()
        {
            base.OnHide();
            menuAnimator?.SetTrigger("FadeOut");
        }
    }
} 