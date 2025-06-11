using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using InvaderInsider.Managers;
using InvaderInsider.UI;

namespace InvaderInsider.UI
{
    public class PausePanel : BasePanel
    {
        private const string LOG_PREFIX = "[UI] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "PausePanel: Time scale set to {0} (Resume)",
            "PausePanel: Time scale set to {0} (Restart)",
            "PausePanel: Time scale set to {0} (Show)",
            "PausePanel: Game state changed to Paused",
            "PausePanel: Input handler disabled",
            "PausePanel: Input handler enabled"
        };

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        private MenuInputHandler cachedInputHandler;
        private GameManager gameManager;
        private UIManager uiManager;

        protected override void Awake()
        {
            base.Awake();
            uiManager = UIManager.Instance;
            gameManager = GameManager.Instance;
            cachedInputHandler = FindObjectOfType<MenuInputHandler>();
            
            uiManager.RegisterPanel("Pause", this);
            Initialize();
        }

        protected override void Initialize()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(ResumeGame);
            }
            
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(RestartGame);
            }
            
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(ShowMainMenu);
            }
        }

        private void ShowMainMenu()
        {
            uiManager.ShowPanel("MainMenu");
        }

        private void ResumeGame()
        {
            Time.timeScale = 1f;
            Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[0], Time.timeScale));
            gameManager.SetGameState(GameState.Playing);
            Hide();
        }

        private void RestartGame()
        {
            Time.timeScale = 1f;
            Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[1], Time.timeScale));
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        protected override void OnShow()
        {
            base.OnShow();
            
            Time.timeScale = 0f;
            Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[2], Time.timeScale));
            
            if (gameManager.CurrentGameState != GameState.Paused)
            {
                gameManager.SetGameState(GameState.Paused);
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[3]);
            }

            if (cachedInputHandler != null)
            {
                cachedInputHandler.enabled = false;
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[4]);
            }
        }

        protected override void OnHide()
        {
            if (cachedInputHandler != null)
            {
                cachedInputHandler.enabled = true;
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[5]);
            }
        }
    }
} 