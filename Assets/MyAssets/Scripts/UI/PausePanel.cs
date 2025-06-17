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
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + "Resume 버튼 클릭됨");
            #endif
            
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

        private void OnSettingsClicked()
        {
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + "설정 버튼 클릭됨");
            #endif
            
            // Pause 패널 숨기고 Settings 패널 표시
            Hide();
            UIManager.Instance?.ShowPanel("Settings");
        }

        private void OnMainMenuClicked()
        {
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + "메인 메뉴로 이동");
            #endif
            
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
            // 게임이 종료될 때 시간 스케일 복구
            if (Time.timeScale == 0f)
            {
                Time.timeScale = 1f;
            }
        }
    }
} 