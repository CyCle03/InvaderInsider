using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using InvaderInsider.Managers;
using InvaderInsider.UI;
using InvaderInsider.Managers;

namespace InvaderInsider.UI
{
    public class PausePanel : BasePanel
    {
        private new const string LOG_PREFIX = "[Pause] ";
        
        [Header("Pause Panel UI")]
        public Button resumeButton;
        public Button restartButton;
        public Button settingsButton;
        public Button mainMenuButton;

        private bool wasPaused = false;
        private bool buttonsSetup = false; // 버튼 이벤트 등록 완료 플래그

        private void Start()
        {
            if (!buttonsSetup)
            {
                SetupButtons();
                buttonsSetup = true;
            }
        }

        private void SetupButtons()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeClicked);
            
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);
            
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
            // ResumeGame() 호출 제거 - GameManager에서 직접 처리하여 순환 참조 방지
        }

        private void PauseGame()
        {
            wasPaused = Time.timeScale == 0f;
            if (!wasPaused)
            {
                if (GameManager.Instance != null)
                {
                    // GameManager에서 일시정지 처리하지만 패널 표시는 이미 되어있음
                    Time.timeScale = 0f;
                    GameManager.Instance.SetGameState(InvaderInsider.Managers.GameState.Paused);
                }
                else
                {
                    Time.timeScale = 0f;
                }
            }
        }

        private void ResumeGame()
        {
            if (!wasPaused)
            {
                // 순환 참조 방지를 위해 GameManager.ResumeGame() 호출하지 않음
                Time.timeScale = 1f;
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetGameState(InvaderInsider.Managers.GameState.Playing);
                }
            }
        }

        private void OnResumeClicked()
        {
            LogManager.Info(LOG_PREFIX, "Resume 버튼이 클릭되었습니다.");
            
            // 순환 참조 방지를 위해 먼저 패널 숨기고 게임 재시작
            gameObject.SetActive(false);
            
            if (GameManager.Instance != null)
            {
                Time.timeScale = 1f;
                GameManager.Instance.SetGameState(InvaderInsider.Managers.GameState.Playing);
            }
            else
            {
                Time.timeScale = 1f;
            }
        }

        private void OnRestartClicked()
        {
            LogManager.Info(LOG_PREFIX, "Restart 버튼이 클릭되었습니다.");
            
            // GameManager를 통해 새 게임 시작
            if (GameManager.Instance != null)
            {
                // 시간 스케일 복구
                Time.timeScale = 1f;
                
                // 새 게임 시작 (현재 게임을 처음부터 다시)
                GameManager.Instance.StartNewGame();
            }
            else
            {
                LogManager.Error(LOG_PREFIX, "GameManager를 찾을 수 없습니다!");
                
                // GameManager가 없다면 직접 Game 씬 재로드
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
            }
        }

        private void OnSettingsClicked()
        {
            LogManager.Info(LOG_PREFIX, "Settings 버튼이 클릭되었습니다.");
            
            // Pause 패널 숨기고 Settings 패널 표시
            Hide();
            UIManager.Instance?.ShowPanel("Settings");
        }

        private void OnMainMenuClicked()
        {
            LogManager.Info(LOG_PREFIX, "Main Menu 버튼이 클릭되었습니다.");
            
            // GameManager가 씬 전환과 상태 관리를 모두 담당
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadMainMenuScene();
            }
            else
            {
                // 만약 GameManager가 없다면 직접 씬 전환
                Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
            }
        }

        private void OnDestroy()
        {
            // 버튼 이벤트 정리
            CleanupButtonEvents();
            
            // 게임이 종료될 때 시간 스케일 복구
            if (Time.timeScale == 0f)
            {
                Time.timeScale = 1f;
            }
        }
        
        private void CleanupButtonEvents()
        {
            if (buttonsSetup)
            {
                if (resumeButton != null)
                    resumeButton.onClick.RemoveListener(OnResumeClicked);
                if (restartButton != null)
                    restartButton.onClick.RemoveListener(OnRestartClicked);
                if (settingsButton != null)
                    settingsButton.onClick.RemoveListener(OnSettingsClicked);
                if (mainMenuButton != null)
                    mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
                
                buttonsSetup = false;
            }
        }
    }
} 