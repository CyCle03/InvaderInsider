using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using InvaderInsider.Data;
using InvaderInsider.Managers;

namespace InvaderInsider.UI
{
    public class MainMenuPanel : BasePanel
    {
        private const string LOG_PREFIX = "[MainMenu] ";
        
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "Scene transition initiated", // 0
            "No save data found", // 1
            "Loading stage: {0}" // 2
        };

        [Header("Components")]
        [SerializeField] private MainMenuButtonHandler buttonHandler;

        [Header("Menu Buttons")]
        public Button newGameButton;
        public Button continueButton;
        public Button settingsButton;
        public Button deckButton;
        public Button achievementsButton;
        public Button exitButton;

        [Header("Version Info")]
        public Text versionText;

        private SaveDataManager saveDataManager;

        private void Start()
        {
            Initialize();
        }

        protected override void Initialize()
        {
            saveDataManager = SaveDataManager.Instance;
            
            SetupButtons();
            UpdateContinueButton();
            UpdateVersionInfo();
        }

        private void SetupButtons()
        {
            if (newGameButton != null)
                newGameButton.onClick.AddListener(OnNewGameClicked);
            
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);
            
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
            
            if (deckButton != null)
                deckButton.onClick.AddListener(OnDeckClicked);
            
            if (achievementsButton != null)
                achievementsButton.onClick.AddListener(OnAchievementsClicked);
            
            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);
        }

        private void UpdateContinueButton()
        {
            if (continueButton != null && saveDataManager != null)
            {
                bool hasSaveData = saveDataManager.HasSaveData();
                continueButton.interactable = hasSaveData;
            }
        }

        private void UpdateVersionInfo()
        {
            if (versionText != null)
            {
                versionText.text = $"v{Application.version}";
            }
        }

        // 버튼 이벤트 핸들러들
        private void OnNewGameClicked()
        {
            StartNewGame();
        }

        private void OnContinueClicked()
        {
            ContinueGame();
        }

        private void OnSettingsClicked()
        {
            UIManager.Instance?.ShowPanel("Settings");
        }

        private void OnDeckClicked()
        {
            UIManager.Instance?.ShowPanel("Deck");
        }

        private void OnAchievementsClicked()
        {
            UIManager.Instance?.ShowPanel("Achievements");
        }

        private void OnExitClicked()
        {
            ExitGame();
        }

        // 게임 로직 메서드들
        private void StartNewGame()
        {
            if (saveDataManager != null)
            {
                saveDataManager.ResetGameData();
                #if UNITY_EDITOR
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[2], 1));
                #endif
            }
            
            LoadGameScene();
        }

        private void ContinueGame()
        {
            if (saveDataManager != null && saveDataManager.HasSaveData())
            {
                saveDataManager.LoadGameData();
                var saveData = saveDataManager.CurrentSaveData;
                if (saveData != null)
                {
                    int nextStage = saveData.progressData.highestStageCleared + 1;
                    #if UNITY_EDITOR
                    Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[2], nextStage));
                    #endif
                }
            }
            else
            {
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[1]);
                #endif
                return;
            }
            
            LoadGameScene();
        }

        private void LoadGameScene()
        {
            Time.timeScale = 1f;
            
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[0]);
            #endif

            // UI 정리
            var uiManager = UIManager.Instance;
            if (uiManager != null)
            {
                uiManager.Cleanup();
            }

            // 게임 매니저 초기화
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.InitializeGame();
            }
            
            SceneManager.LoadScene("Game");
        }

        private void ExitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        // 외부에서 호출 가능한 공개 메서드들
        public void RefreshContinueButton()
        {
            UpdateContinueButton();
        }

        public void SetInteractable(bool interactable)
        {
            if (newGameButton != null) newGameButton.interactable = interactable;
            if (continueButton != null) continueButton.interactable = interactable && saveDataManager?.HasSaveData() == true;
            if (settingsButton != null) settingsButton.interactable = interactable;
            if (deckButton != null) deckButton.interactable = interactable;
            if (achievementsButton != null) achievementsButton.interactable = interactable;
            if (exitButton != null) exitButton.interactable = interactable;
        }
    }
} 