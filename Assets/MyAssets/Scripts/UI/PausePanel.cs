using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using InvaderInsider.Managers;
using InvaderInsider.UI;

namespace InvaderInsider.UI
{
    public class PausePanel : BasePanel
    {
        private const string LOG_PREFIX = "[Pause] ";
        
        [Header("Pause Panel UI")]
        public Button resumeButton;
        public Button settingsButton;
        public Button mainMenuButton;

        private bool wasPaused = false;

        private void Start()
        {
            SetupButtons();
        }

        private void SetupButtons()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeClicked);
            
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
            
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        public override void Show()
        {
            base.Show();
            PauseGame();
        }

        public override void Hide()
        {
            base.Hide();
            ResumeGame();
        }

        private void PauseGame()
        {
            wasPaused = Time.timeScale == 0f;
            if (!wasPaused)
            {
                Time.timeScale = 0f;
            }
        }

        private void ResumeGame()
        {
            if (!wasPaused)
            {
                Time.timeScale = 1f;
            }
        }

        private void OnResumeClicked()
        {
            Hide();
        }

        private void OnSettingsClicked()
        {
            // 설정 패널 표시 로직
            UIManager.Instance?.ShowPanel("Settings");
        }

        private void OnMainMenuClicked()
        {
            ResumeGame();
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        private void OnDestroy()
        {
            // 게임이 종료될 때 시간 스케일 복구
            if (Time.timeScale == 0f)
            {
                Time.timeScale = 1f;
            }
        }
    }
} 