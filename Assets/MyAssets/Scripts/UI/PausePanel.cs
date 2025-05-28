using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
            mainMenuButton?.onClick.AddListener(() => LoadMainMenu());
        }

        private void ResumeGame()
        {
            Time.timeScale = 1f;
            GameManager.Instance.CurrentGameState = GameState.Playing;
            UIManager.Instance.GoBack();
        }

        private void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void LoadMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        protected override void OnShow()
        {
            Time.timeScale = 0f;
            GameManager.Instance.CurrentGameState = GameState.Paused;
        }

        protected override void OnHide()
        {
            if (GameManager.Instance.CurrentGameState == GameState.Paused)
            {
                Time.timeScale = 1f;
                GameManager.Instance.CurrentGameState = GameState.Playing;
            }
        }
    }
} 