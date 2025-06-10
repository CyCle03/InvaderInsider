using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using InvaderInsider.Managers;
using InvaderInsider.UI;

namespace InvaderInsider.UI
{
    public class PausePanel : BasePanel
    {
        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        protected override void Initialize()
        {
            resumeButton?.onClick.AddListener(ResumeGame);
            restartButton?.onClick.AddListener(RestartGame);
            mainMenuButton?.onClick.AddListener(() => UIManager.Instance.ShowPanel("MainMenu"));
        }

        private void ResumeGame()
        {
            Time.timeScale = 1f;
            Debug.Log($"Time.timeScale set to: {Time.timeScale} in ResumeGame");
            GameManager.Instance.CurrentGameState = GameState.Playing;
            Hide();
        }

        private void RestartGame()
        {
            Debug.Log("[PausePanel] RestartGame called!");
            // Time.timeScale = 1f; // GameplayPanel.OnShow에서 처리
            Debug.Log($"Time.timeScale set to: {Time.timeScale} in RestartGame");
            StageManager.Instance.InitializeStage(); // 스테이지 초기화
            UIManager.Instance.ShowPanel("Gameplay"); // 게임 플레이 패널 표시
            // UIManager.Instance.SetControlButtonsActive(true); // GameplayPanel.OnShow에서 처리
            // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        protected override void OnShow()
        {
            Time.timeScale = 0f;
            Debug.Log($"Time.timeScale set to: {Time.timeScale} in PausePanel.OnShow");
            GameManager.Instance.CurrentGameState = GameState.Paused;
            MenuInputHandler inputHandler = FindObjectOfType<MenuInputHandler>();
            if (inputHandler != null)
            {
                inputHandler.enabled = false;
            }
        }

        protected override void OnHide()
        {
            MenuInputHandler inputHandler = FindObjectOfType<MenuInputHandler>();
            if (inputHandler != null)
            {
                inputHandler.enabled = true;
            }
        }
    }
} 