using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InvaderInsider.Data;
using InvaderInsider.Managers;
using InvaderInsider;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement; // 씬 관리를 위해 추가
using InvaderInsider.UI;
using System.Collections.Generic;

namespace InvaderInsider.UI
{
    public class MainMenuPanel : BasePanel
    {
        private const string LOG_PREFIX = "[UI] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "MainMenu: Time scale {0}",
            "MainMenu: Load button {0}",
            "MainMenu: Loading stage {0}",
            "MainMenu: No saved game data found"
        };

        [Header("Components")]
        [SerializeField] private MainMenuCanvasSetup canvasSetup;
        [SerializeField] private MainMenuLayout layout;
        [SerializeField] private MainMenuButtonHandler buttonHandler;
        [SerializeField] private Animator menuAnimator;

        private UIManager uiManager;
        private GameManager gameManager;
        private SaveDataManager saveDataManager;
        private StageManager stageManager;
        private Player cachedPlayer;
        private bool hasAnimator;

        protected override void Awake()
        {
            base.Awake();
            panelName = "MainMenu";
            
            InitializeManagers();
            ValidateComponents();
            SetupEventHandlers();
            
            uiManager.RegisterPanel("MainMenu", this);
        }

        private void InitializeManagers()
        {
            uiManager = UIManager.Instance;
            gameManager = GameManager.Instance;
            saveDataManager = SaveDataManager.Instance;
            stageManager = StageManager.Instance;
            cachedPlayer = FindObjectOfType<Player>();
        }

        private void ValidateComponents()
        {
            if (canvasSetup == null)
            {
                canvasSetup = GetComponent<MainMenuCanvasSetup>();
                if (canvasSetup == null)
                {
                    canvasSetup = gameObject.AddComponent<MainMenuCanvasSetup>();
                }
            }

            if (layout == null)
            {
                layout = GetComponent<MainMenuLayout>();
                if (layout == null)
                {
                    layout = gameObject.AddComponent<MainMenuLayout>();
                }
            }

            if (buttonHandler == null)
            {
                buttonHandler = GetComponent<MainMenuButtonHandler>();
                if (buttonHandler == null)
                {
                    buttonHandler = gameObject.AddComponent<MainMenuButtonHandler>();
                }
            }

            hasAnimator = menuAnimator != null;
        }

        private void SetupEventHandlers()
        {
            if (buttonHandler != null)
            {
                buttonHandler.OnNewGameClicked += HandleNewGame;
                buttonHandler.OnLoadGameClicked += HandleLoadGame;
                buttonHandler.OnDeckClicked += HandleDeck;
                buttonHandler.OnShopClicked += HandleShop;
                buttonHandler.OnSettingsClicked += HandleSettings;
                buttonHandler.OnAchievementsClicked += HandleAchievements;
                buttonHandler.OnQuitClicked += HandleQuit;
            }
        }

        private void HandleNewGame()
        {
            if (gameManager != null)
            {
                gameManager.SetGameState(GameState.Loading);
                if (stageManager != null)
                {
                    stageManager.StartStageFrom(0);
                    Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[2], 1));
                }
            }
        }

        private void HandleLoadGame()
        {
            if (saveDataManager != null && stageManager != null)
            {
                var saveData = saveDataManager.CurrentSaveData;
                if (saveData != null)
                {
                    gameManager.SetGameState(GameState.Loading);
                    stageManager.StartStageFrom(saveData.progressData.highestStageCleared);
                    Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[2], saveData.progressData.highestStageCleared + 1));
                }
                else
                {
                    Debug.Log(LOG_PREFIX + LOG_MESSAGES[3]);
                }
            }
        }

        private void HandleDeck()
        {
            if (uiManager != null)
            {
                uiManager.ShowPanel("Deck");
            }
        }

        private void HandleShop()
        {
            if (uiManager != null)
            {
                uiManager.ShowPanel("Shop");
            }
        }

        private void HandleSettings()
        {
            if (uiManager != null)
            {
                uiManager.ShowPanel("Settings");
            }
        }

        private void HandleAchievements()
        {
            if (uiManager != null)
            {
                uiManager.ShowPanel("Achievements");
            }
        }

        private void HandleQuit()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        protected override void OnShow()
        {
            base.OnShow();
            
            if (buttonHandler != null)
            {
                buttonHandler.SetButtonsInteractable(true);
            }

            if (hasAnimator)
            {
                menuAnimator.SetTrigger("Show");
            }

            Time.timeScale = 1f;
            Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[0], Time.timeScale));
        }

        protected override void OnHide()
        {
            base.OnHide();
            
            if (buttonHandler != null)
            {
                buttonHandler.SetButtonsInteractable(false);
            }

            if (hasAnimator)
            {
                menuAnimator.SetTrigger("Hide");
            }
        }

        private void OnDestroy()
        {
            if (buttonHandler != null)
            {
                buttonHandler.OnNewGameClicked -= HandleNewGame;
                buttonHandler.OnLoadGameClicked -= HandleLoadGame;
                buttonHandler.OnDeckClicked -= HandleDeck;
                buttonHandler.OnShopClicked -= HandleShop;
                buttonHandler.OnSettingsClicked -= HandleSettings;
                buttonHandler.OnAchievementsClicked -= HandleAchievements;
                buttonHandler.OnQuitClicked -= HandleQuit;
            }
        }
    }
} 