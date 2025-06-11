using UnityEngine;
using System;
using InvaderInsider.Data;
using InvaderInsider.UI;
using UnityEngine.SceneManagement;

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
        private const string LOG_PREFIX = "[Game] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "State changed to: {0}",
            "Game started",
            "Game paused",
            "Game resumed",
            "Game ended",
            "Settings opened",
            "Settings closed",
            "Game data loaded. Updating game state and UI...",
            "UI update initiated based on loaded data",
            "StageManager not found. Cannot update Stage/Wave/Enemy Count UI",
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

            if (cachedStageManager == null) Debug.LogWarning(LOG_PREFIX + LOG_MESSAGES[10]);
            if (cachedBottomBarPanel == null) Debug.LogWarning(LOG_PREFIX + LOG_MESSAGES[11]);
            if (cachedPlayer == null) Debug.LogWarning(LOG_PREFIX + LOG_MESSAGES[12]);
        }

        private void CleanupEventListeners()
        {
            if (saveDataManager != null)
            {
                saveDataManager.OnGameDataLoaded -= HandleGameDataLoaded;
            }
            OnGameStateChanged -= HandleGameStateChanged;
        }

        private void OnEnable()
        {
            if (saveDataManager != null)
            {
                saveDataManager.OnGameDataLoaded -= HandleGameDataLoaded;
                saveDataManager.OnGameDataLoaded += HandleGameDataLoaded;
            }
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
            CurrentGameState = GameState.MainMenu;
        }

        private void HandleGameDataLoaded()
        {
            if (saveDataManager == null) return;

            Debug.Log(LOG_PREFIX + LOG_MESSAGES[7]);
            SaveData loadedData = saveDataManager.CurrentSaveData;

            if (cachedStageManager != null)
            {
                uiManager.UpdateStage(loadedData.progressData.highestStageCleared, cachedStageManager.GetStageCount());
                cachedStageManager.InitializeStageFromLoadedData(loadedData.progressData.highestStageCleared);

                if (cachedBottomBarPanel != null)
                {
                    cachedBottomBarPanel.UpdateMonsterCountDisplay(cachedStageManager.ActiveEnemyCount);
                }
            }
            else
            {
                Debug.LogWarning(LOG_PREFIX + LOG_MESSAGES[9]);
            }

            if (cachedPlayer != null)
            {
                cachedPlayer.ResetHealth();
            }

            Debug.Log(LOG_PREFIX + LOG_MESSAGES[8]);
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
                    if (uiManager != null)
                    {
                        uiManager.ShowPanel("MainMenu");
                        uiManager.HideCurrentPanel();
                        uiManager.HidePanel("InGame");
                        uiManager.HidePanel("Pause");
                    }
                    break;

                case GameState.Playing:
                    if (uiManager != null)
                    {
                        uiManager.ShowPanel("InGame");
                        if (uiManager.IsCurrentPanel("Pause"))
                        {
                            uiManager.GoBack();
                        }
                        else
                        {
                            uiManager.HideCurrentPanel();
                        }
                    }
                    if (Application.isPlaying)
                    {
                        Debug.Log(LOG_PREFIX + LOG_MESSAGES[1]);
                    }
                    break;

                case GameState.Paused:
                    if (uiManager != null && !uiManager.IsPanelActive("Pause"))
                    {
                        uiManager.ShowPanel("Pause");
                    }
                    if (Application.isPlaying)
                    {
                        Debug.Log(LOG_PREFIX + LOG_MESSAGES[2]);
                    }
                    break;

                case GameState.Settings:
                    if (uiManager != null)
                    {
                        uiManager.ShowPanel("Settings");
                        uiManager.HideCurrentPanel();
                    }
                    if (Application.isPlaying)
                    {
                        Debug.Log(LOG_PREFIX + LOG_MESSAGES[5]);
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
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[13], amount));
                return true;
            }
            return false;
        }

        public void AddEData(int amount)
        {
            if (saveDataManager != null && amount > 0)
            {
                saveDataManager.UpdateEData(amount);
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[14], amount));
            }
        }

        public void StageCleared(int stageNum, int stars)
        {
            if (saveDataManager != null && stageNum > 0 && stars > 0)
            {
                saveDataManager.UpdateStageProgress(stageNum, stars);
                saveDataManager.SaveGameData();
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[15], stageNum, stars));
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
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[16]);
            }
            
            OnStageClearedEvent?.Invoke();
        }
    }
} 