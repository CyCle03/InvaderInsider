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
            Time.timeScale = 1f;
            Debug.Log($"Time.timeScale set to: {Time.timeScale} in RestartGame");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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

            if (GameManager.Instance.CurrentGameState == GameState.Paused)
            {
                Time.timeScale = 1f;
                Debug.Log($"Time.timeScale set to: {Time.timeScale} in PausePanel.OnHide");
                GameManager.Instance.CurrentGameState = GameState.Playing;
            }
        }
    }
} 