using UnityEngine;
using System;
using InvaderInsider.Data;
using InvaderInsider.UI;
using UnityEngine.SceneManagement;
using InvaderInsider.Cards;

namespace InvaderInsider.Managers
{
    public enum GameState
    {
        None,
        MainMenu,
        Loading,
        Playing,
        Paused,
        GameOver,
        Settings
    }

    public class GameManager : MonoBehaviour
    {
        private const string LOG_PREFIX = "[GameManager] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "State changed to: {0}",
            "Game started",
            "Game paused",
            "Settings opened",
            "StageManager not found in the scene",
            "BottomBarPanel not found in the scene",
            "Player not found in the scene",
            "EData spent: {0}",
            "EData added: {0}",
            "Stage {0} cleared with {1} stars"
        };

        private static GameManager instance;
        private static readonly object _lock = new object();
        private static bool isQuitting = false;

        public static GameManager Instance
        {
            get
            {
                if (isQuitting) return null;

                lock (_lock)
                {
                    if (instance == null)
                    {
                        instance = FindObjectOfType<GameManager>();
                        if (instance == null && !isQuitting)
                        {
                            GameObject go = new GameObject("GameManager");
                            instance = go.AddComponent<GameManager>();
                            DontDestroyOnLoad(go);
                        }
                    }
                    return instance;
                }
            }
        }

        [Header("Game State")]
        private GameState currentGameState = GameState.MainMenu;
        public GameState CurrentGameState
        {
            get => currentGameState;
            private set
            {
                if (currentGameState != value)
                {
                    currentGameState = value;
                    OnGameStateChanged?.Invoke(value);
                }
            }
        }

        public event Action<GameState> OnGameStateChanged;

        private StageManager cachedStageManager;
        private BottomBarPanel cachedBottomBarPanel;
        private Player cachedPlayer;
        private SaveDataManager saveDataManager;
        private UIManager uiManager;
        private CardManager cardManager;

        public event System.Action OnStageClearedEvent;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeManagers();
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeManagers()
        {
            saveDataManager = SaveDataManager.Instance;
            uiManager = UIManager.Instance;
            cardManager = FindObjectOfType<CardManager>();
            UpdateCachedComponents();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            CleanupEventListeners();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            UpdateCachedComponents();
        }

        private void UpdateCachedComponents()
        {
            cachedStageManager = FindObjectOfType<StageManager>();
            cachedBottomBarPanel = FindObjectOfType<BottomBarPanel>();
            cachedPlayer = FindObjectOfType<Player>();

            if (cachedStageManager == null) Debug.LogWarning(LOG_PREFIX + LOG_MESSAGES[4]);
            if (cachedBottomBarPanel == null) Debug.LogWarning(LOG_PREFIX + LOG_MESSAGES[5]);
            if (cachedPlayer == null) Debug.LogWarning(LOG_PREFIX + LOG_MESSAGES[6]);
        }

        private void CleanupEventListeners()
        {
            OnGameStateChanged -= HandleGameStateChanged;
        }

        private void OnEnable()
        {
            OnGameStateChanged -= HandleGameStateChanged;
            OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            CleanupEventListeners();
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void Start()
        {
            InitializeGame();
        }

        private void HandleGameStateChanged(GameState newState)
        {
            if (Application.isPlaying)
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[0], newState));
            }
            
            switch (newState)
            {
                case GameState.MainMenu:
                    UIManager mainMenuUIManager = UIManager.Instance;
                    if (mainMenuUIManager != null)
                    {
                        mainMenuUIManager.ShowPanel("MainMenu");
                        mainMenuUIManager.HideCurrentPanel();
                        mainMenuUIManager.HidePanel("InGame");
                        mainMenuUIManager.HidePanel("Pause");
                    }
                    break;

                case GameState.Loading:
                    if (Application.isPlaying)
                    {
                        Debug.Log(LOG_PREFIX + "게임 로딩 중 - UI 전환 시작");
                    }
                    // UIManager를 실시간으로 가져오기
                    UIManager currentUIManager = UIManager.Instance;
                    if (currentUIManager != null)
                    {
                        Debug.Log(LOG_PREFIX + "UIManager 찾음, 패널 전환 실행");
                        currentUIManager.DebugPrintRegisteredPanels();
                        Debug.Log(LOG_PREFIX + "MainMenu 패널 숨기기 시도");
                        currentUIManager.HidePanel("MainMenu");
                        Debug.Log(LOG_PREFIX + "InGame 패널 보이기 시도");
                        currentUIManager.ShowPanel("InGame");
                        Debug.Log(LOG_PREFIX + "UI 전환 완료");
                    }
                    else
                    {
                        Debug.LogError(LOG_PREFIX + "UIManager.Instance가 null입니다!");
                    }
                    break;

                case GameState.Playing:
                    UIManager playingUIManager = UIManager.Instance;
                    if (playingUIManager != null)
                    {
                        playingUIManager.ShowPanel("InGame");
                        if (playingUIManager.IsCurrentPanel("Pause"))
                        {
                            playingUIManager.GoBack();
                        }
                        else
                        {
                            playingUIManager.HideCurrentPanel();
                        }
                    }
                    if (Application.isPlaying)
                    {
                        Debug.Log(LOG_PREFIX + LOG_MESSAGES[1]);
                    }
                    break;

                case GameState.Paused:
                    UIManager pausedUIManager = UIManager.Instance;
                    if (pausedUIManager != null && !pausedUIManager.IsPanelActive("Pause"))
                    {
                        pausedUIManager.ShowPanel("Pause");
                    }
                    if (Application.isPlaying)
                    {
                        Debug.Log(LOG_PREFIX + LOG_MESSAGES[2]);
                    }
                    break;

                case GameState.Settings:
                    UIManager settingsUIManager = UIManager.Instance;
                    if (settingsUIManager != null)
                    {
                        settingsUIManager.ShowPanel("Settings");
                        settingsUIManager.HideCurrentPanel();
                    }
                    if (Application.isPlaying)
                    {
                        Debug.Log(LOG_PREFIX + LOG_MESSAGES[3]);
                    }
                    break;
            }
        }

        public void SetGameState(GameState newState)
        {
            CurrentGameState = newState;
        }

        public bool TrySpendEData(int amount)
        {
            if (saveDataManager == null || amount <= 0) return false;
            
            var currentEData = saveDataManager.GetCurrentEData();
            if (currentEData >= amount)
            {
                saveDataManager.UpdateEData(-amount);
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[7], amount));
                return true;
            }
            return false;
        }

        public void AddEData(int amount)
        {
            if (saveDataManager != null && amount > 0)
            {
                saveDataManager.UpdateEData(amount);
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[8], amount));
            }
        }

        public void StageCleared(int stageNum, int stars)
        {
            if (saveDataManager != null && stageNum > 0 && stars > 0)
            {
                saveDataManager.UpdateStageProgress(stageNum, stars);
                saveDataManager.SaveGameData();
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[9], stageNum, stars));
            }
        }

        public void AddResourcePointsListener(Action<int> listener)
        {
            if (saveDataManager != null)
            {
                saveDataManager.OnEDataChanged += listener;
            }
        }

        public void RemoveResourcePointsListener(Action<int> listener)
        {
            if (saveDataManager != null)
            {
                saveDataManager.OnEDataChanged -= listener;
            }
        }

        private void Update()
        {
            if (CurrentGameState != GameState.Playing || cachedStageManager == null) return;

            if (cachedStageManager.ActiveEnemyCount <= 0)
            {
                HandleStageCleared();
            }
        }

        private void HandleStageCleared()
        {
            if (CurrentGameState != GameState.Playing) return;

            CurrentGameState = GameState.None;
            if (Application.isPlaying)
            {
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[9]);
            }
            
            OnStageClearedEvent?.Invoke();
        }

        public void InitializeGame()
        {
            Debug.Log(LOG_PREFIX + "Initializing game...");

            // UI 패널 등록
            var inGamePanel = FindObjectOfType<InvaderInsider.UI.InGamePanel>();
            if (inGamePanel != null)
                UIManager.Instance.RegisterPanel("InGame", inGamePanel);

            var topBarPanel = FindObjectOfType<InvaderInsider.UI.TopBarPanel>();
            if (topBarPanel != null)
                UIManager.Instance.RegisterPanel("TopBar", topBarPanel);

            var bottomBarPanel = FindObjectOfType<InvaderInsider.UI.BottomBarPanel>();
            if (bottomBarPanel != null)
                UIManager.Instance.RegisterPanel("BottomBar", bottomBarPanel);

            var pausePanel = FindObjectOfType<InvaderInsider.UI.PausePanel>();
            if (pausePanel != null)
                UIManager.Instance.RegisterPanel("Pause", pausePanel);

            var settingsPanel = FindObjectOfType<InvaderInsider.UI.SettingsPanel>();
            if (settingsPanel != null)
                UIManager.Instance.RegisterPanel("Settings", settingsPanel);

            var deckPanel = FindObjectOfType<InvaderInsider.UI.DeckPanel>();
            if (deckPanel != null)
                UIManager.Instance.RegisterPanel("Deck", deckPanel);

            var achievementsPanel = FindObjectOfType<InvaderInsider.UI.AchievementsPanel>();
            if (achievementsPanel != null)
                UIManager.Instance.RegisterPanel("Achievements", achievementsPanel);

            var handDisplayPanel = FindObjectOfType<InvaderInsider.UI.HandDisplayPanel>();
            if (handDisplayPanel != null)
                UIManager.Instance.RegisterPanel("HandDisplay", handDisplayPanel);

            var shopPanel = FindObjectOfType<InvaderInsider.UI.ShopPanel>();
            if (shopPanel != null)
                UIManager.Instance.RegisterPanel("Shop", shopPanel);

            var stageSelectPanel = FindObjectOfType<InvaderInsider.UI.StageSelectPanel>();
            if (stageSelectPanel != null)
                UIManager.Instance.RegisterPanel("StageSelect", stageSelectPanel);

            // 나머지 패널 숨기기
            string[] panelsToHide = { "Pause", "Settings", "Deck", "Achievements", "HandDisplay", "Shop", "StageSelect" };
            foreach (var panelName in panelsToHide)
            {
                UIManager.Instance.HidePanel(panelName);
            }

            // InGame, TopBar, BottomBar만 표시
            if (inGamePanel != null)
                UIManager.Instance.ShowPanel("InGame");
            if (topBarPanel != null)
                UIManager.Instance.ShowPanel("TopBar");
            if (bottomBarPanel != null)
                UIManager.Instance.ShowPanel("BottomBar");

            // 카드 매니저 초기화
            if (cardManager != null)
                cardManager.LoadSummonData();

            // 게임 상태 초기화
            Time.timeScale = 1f;

            Debug.Log(LOG_PREFIX + "Game initialized");
        }

        public void PauseGame()
        {
            Time.timeScale = 0f;
            uiManager?.ShowPanel("Pause");
        }

        public void ResumeGame()
        {
            Time.timeScale = 1f;
            uiManager?.HidePanel("Pause");
        }

        public void EndGame()
        {
            Time.timeScale = 0f;
            // 게임 종료 로직
        }
    }
} 