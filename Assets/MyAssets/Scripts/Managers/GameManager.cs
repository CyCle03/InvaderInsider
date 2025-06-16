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
            "State changed to: {0}", // 0
            "Game started", // 1
            "Game paused", // 2
            "Settings opened", // 3
            "게임 일시정지", // 4
            "게임 재개", // 5
            "EData changed: {0}", // 6
            "스테이지 {0} 클리어 완료 (별 {1}개)" // 7
        };
        
        
        private static GameManager instance;
        private static readonly object _lock = new object();
        private static bool isQuitting = false;

        public static GameManager Instance
        {
            get
            {
                if (isQuitting) return null;
                
                // 에디터에서 플레이 모드가 아닐 때는 인스턴스 생성하지 않음
                #if UNITY_EDITOR
                if (!UnityEngine.Application.isPlaying) return null;
                #endif

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
        private TopBarPanel cachedTopBarPanel;

        public event System.Action OnStageClearedEvent;

        private void Awake()
        {
            // 에디터 모드에서는 초기화하지 않음
            #if UNITY_EDITOR
            if (!Application.isPlaying) return;
            #endif
            
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
            // 매니저들 재초기화 (씬 전환 시 인스턴스가 변경될 수 있음)
            InitializeManagers();
            
            // 컴포넌트 캐시 업데이트
            UpdateCachedComponents();
            
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + $"씬 로드 후 매니저 재초기화 완료: {scene.name}, UIManager: {uiManager != null}");
            #endif
        }

        private void UpdateCachedComponents()
        {
            cachedStageManager = FindObjectOfType<StageManager>();
            cachedBottomBarPanel = FindObjectOfType<BottomBarPanel>();
            cachedPlayer = FindObjectOfType<Player>();
            cachedTopBarPanel = FindObjectOfType<TopBarPanel>();

            #if UNITY_EDITOR
            // Game 씬에서만 이 컴포넌트들이 필요하므로, Game 씬이 아닌 경우 경고하지 않음
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentSceneName == "Game")
            {
                if (cachedStageManager == null) Debug.LogWarning(LOG_PREFIX + "StageManager not found in the scene");
                if (cachedBottomBarPanel == null) Debug.LogWarning(LOG_PREFIX + "BottomBarPanel not found in the scene");
                if (cachedPlayer == null) Debug.LogWarning(LOG_PREFIX + "Player not found in the scene");
                if (cachedTopBarPanel == null) Debug.LogWarning(LOG_PREFIX + "TopBarPanel not found in the scene");
            }
            #endif
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
            // UI 매니저가 없다면 재초기화 시도
            if (uiManager == null)
            {
                #if UNITY_EDITOR
                Debug.LogWarning(LOG_PREFIX + "UIManager가 없어 재초기화를 시도합니다.");
                #endif
                
                // 매니저들 재초기화 시도
                InitializeManagers();
                
                // 여전히 null이면 포기
                if (uiManager == null)
                {
                    #if UNITY_EDITOR
                    Debug.LogError(LOG_PREFIX + "UIManager 재초기화 실패 - UI 상태 변경을 건너뜁니다.");
                    #endif
                    return;
                }
                
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "UIManager 재초기화 성공!");
                #endif
            }

            // 에디터에서만 상태 변경 로그 출력 (메모리 할당 최적화)
            #if UNITY_EDITOR && ENABLE_STATE_LOGS
            if (Application.isPlaying)
            {
                Debug.Log(LOG_PREFIX + string.Format(LOG_MESSAGES[0], newState));
            }
            #endif
            
            // 현재 씬 이름을 한 번만 가져옴
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            
            switch (newState)
            {
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    // Main 씬에서만 MainMenu 패널 표시
                    if (currentSceneName == "Main")
                    {
                        uiManager?.ShowPanel("MainMenu");
                    }
                    break;

                case GameState.Loading:
                    Time.timeScale = 1f;
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + LOG_MESSAGES[0]);
                    uiManager.DebugPrintRegisteredPanels();
                    #endif
                    break;

                case GameState.Playing:
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + LOG_MESSAGES[1]);
                    #endif
                    // Settings 패널이 등록되어 있는 경우에만 숨기기
                    if (uiManager.IsPanelRegistered("Settings"))
                    {
                        uiManager.HidePanel("Settings");
                    }
                    break;

                case GameState.Paused:
                    uiManager?.ShowPanel("Pause");
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + LOG_MESSAGES[2]);
                    #endif
                    break;

                case GameState.Settings:
                    Time.timeScale = 0f; // 설정 중에는 게임 일시정지
                    uiManager?.ShowPanel("Settings");
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + LOG_MESSAGES[3]);
                    #endif
                    break;

                default:
                    break;
            }
        }

        public void SetGameState(GameState newState)
        {
            CurrentGameState = newState;
        }

        public bool TrySpendEData(int amount)
        {
            if (amount <= 0) return false;

            if (saveDataManager?.GetCurrentEData() >= amount)
            {
                // eData 소모 시에는 저장하지 않음 (스테이지 클리어 시 저장됨)
                saveDataManager.UpdateEDataWithoutSave(-amount);
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + string.Format(LOG_MESSAGES[6], amount));
                #endif
                return true;
            }
            return false;
        }

        public void AddEData(int amount)
        {
            if (amount <= 0) return;

            // eData 추가 시에는 저장하지 않음 (스테이지 클리어 시 저장됨)
            saveDataManager?.UpdateEDataWithoutSave(amount);
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + string.Format(LOG_MESSAGES[6], amount));
            #endif
        }

        public void StageCleared(int stageNum, int stars)
        {
            OnStageClearedEvent?.Invoke();
            saveDataManager?.UpdateStageProgress(stageNum, stars);
        }

        public void AddResourcePointsListener(Action<int> listener)
        {
            if (listener != null && saveDataManager != null)
            {
                saveDataManager.OnEDataChanged += listener;
            }
        }

        public void RemoveResourcePointsListener(Action<int> listener)
        {
            if (listener != null && saveDataManager != null)
            {
                saveDataManager.OnEDataChanged -= listener;
            }
        }

        private int lastEData = -1;
        private int lastActiveEnemyCount = -1;
        private float lastPlayerHealth = -1f;
        private int lastStageIndex = -1;
        private bool stageClearedProcessed = false; // 스테이지 클리어 중복 처리 방지
        
        // UI 업데이트 최적화
        private float lastUIUpdateTime = 0f;
        private const float UI_UPDATE_INTERVAL = 0.2f; // 0.2초마다 UI 업데이트 (메모리 할당 최적화)

        private void Update()
        {
            if (CurrentGameState == GameState.Playing)
            {
                // UI 업데이트 제한 (0.2초마다만 업데이트)
                float currentTime = Time.unscaledTime;
                if (currentTime - lastUIUpdateTime >= UI_UPDATE_INTERVAL)
                {
                    UpdateUIOnChange();
                    lastUIUpdateTime = currentTime;
                }
                
                // 스테이지 클리어 체크 최적화
                if (cachedStageManager != null)
                {
                    bool allEnemiesSpawned = AllEnemiesSpawned();
                    int activeEnemyCount = cachedStageManager.ActiveEnemyCount;
                    
                    if (allEnemiesSpawned && activeEnemyCount == 0)
                    {
                        if (!stageClearedProcessed)
                        {
                            HandleStageCleared();
                            stageClearedProcessed = true;
                        }
                    }
                    else if (activeEnemyCount > 0)
                    {
                        // 적이 다시 생기면 플래그 리셋 (다음 스테이지를 위해)
                        stageClearedProcessed = false;
                    }
                }
            }
        }

        private void UpdateUIOnChange()
        {
            // EData 업데이트
            if (saveDataManager != null)
            {
                int currentEData = saveDataManager.GetCurrentEData();
                if (currentEData != lastEData)
                {
                    if (cachedTopBarPanel != null)
                    {
                        cachedTopBarPanel.UpdateEData(currentEData);
                    }
                    lastEData = currentEData;
                }
            }

            // 스테이지 정보 및 활성 적 수 업데이트 (메모리 할당 최적화)
            if (cachedStageManager != null)
            {
                // 한 번에 모든 필요한 값들을 가져와서 캐시하여 메모리 할당 최소화
                int currentStageIndex = cachedStageManager.GetCurrentStageIndex();
                int currentActiveEnemyCount = cachedStageManager.ActiveEnemyCount;
                
                bool stageChanged = currentStageIndex != lastStageIndex;
                bool enemyCountChanged = currentActiveEnemyCount != lastActiveEnemyCount;
                
                // 활성 적 수 업데이트
                if (enemyCountChanged)
                {
                    if (cachedBottomBarPanel != null)
                    {
                        cachedBottomBarPanel.UpdateMonsterCountDisplay(currentActiveEnemyCount);
                    }
                    lastActiveEnemyCount = currentActiveEnemyCount;
                }

                // TopBar 업데이트 (스테이지 변경 또는 적 수 변경 시에만)
                if ((stageChanged || enemyCountChanged) && cachedTopBarPanel != null)
                {
                    int totalStages = cachedStageManager.GetStageCount();
                    int maxMonsters = cachedStageManager.GetStageWaveCount(currentStageIndex);
                    
                    // TopBar의 Wave에는 활성 적 수/총 적 수 표시
                    cachedTopBarPanel.UpdateStageInfo(currentStageIndex + 1, totalStages, currentActiveEnemyCount, maxMonsters);
                }
                
                // 스테이지 인덱스 캐시 업데이트
                if (stageChanged)
                {
                    lastStageIndex = currentStageIndex;
                }
            }

            // 플레이어 체력 업데이트
            if (cachedPlayer != null)
            {
                float currentHealth = cachedPlayer.CurrentHealth;
                if (Mathf.Abs(currentHealth - lastPlayerHealth) > 0.01f)
                {
                    UpdatePlayerHealthUI(currentHealth, cachedPlayer.MaxHealth);
                    lastPlayerHealth = currentHealth;
                }
            }
        }

        private void UpdatePlayerHealthUI(float currentHealth, float maxHealth)
        {
            if (cachedTopBarPanel != null)
            {
                cachedTopBarPanel.UpdateHealth(currentHealth, maxHealth);
            }

            if (cachedBottomBarPanel != null)
            {
                cachedBottomBarPanel.UpdateHealthDisplay(currentHealth / maxHealth);
            }
        }

        private bool AllEnemiesSpawned()
        {
            if (cachedStageManager == null) return false;
            
            // 메모리 할당 최적화 - 메서드 호출 최소화하고 변수로 캐시
            int currentStageIndex = cachedStageManager.GetCurrentStageIndex();
            int spawnedCount = cachedStageManager.GetSpawnedEnemyCount();
            int maxCount = cachedStageManager.GetStageWaveCount(currentStageIndex);
            
            return spawnedCount >= maxCount;
        }

        private void HandleStageCleared()
        {
            if (cachedStageManager == null) return;

            // 스테이지 클리어 처리
            int clearedStageIndex = cachedStageManager.GetCurrentStageIndex();
            int stars = CalculateStageStars(); // 별점 계산 로직
            
            // 스테이지 진행 저장 (UpdateStageProgress에서 저장됨)
            saveDataManager?.UpdateStageProgress(clearedStageIndex + 1, stars);
            
            // 스테이지 클리어 이벤트 호출
            StageCleared(clearedStageIndex + 1, stars);
            OnStageClearedEvent?.Invoke();
            
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + string.Format(LOG_MESSAGES[7], clearedStageIndex + 1, stars));
            Debug.Log(LOG_PREFIX + "스테이지 클리어 시 eData와 함께 게임 데이터 저장");
            #endif
        }

        public void InitializeGame()
        {
            // 이미 Playing 상태인 경우 중복 초기화 방지
            if (CurrentGameState == GameState.Playing)
            {
                return;
            }

            // 매니저들 재초기화 (씬 로드 후 인스턴스들이 변경되었을 수 있음)
            InitializeManagers();
            
            // UIManager가 없다면 에러 로그 출력 후 대기
            if (uiManager == null)
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + "UIManager를 찾을 수 없어 게임 초기화를 중단합니다!");
                #endif
                return;
            }

            // UI 패널 등록 - 더 강력한 검색
            var inGamePanel = FindObjectOfType<InvaderInsider.UI.InGamePanel>(true);
            if (inGamePanel != null)
            {
                // Canvas나 부모 오브젝트도 활성화
                Transform current = inGamePanel.transform;
                while (current != null)
                {
                    current.gameObject.SetActive(true);
                    current = current.parent;
                }
                uiManager.RegisterPanel("InGame", inGamePanel);
            }
            else
            {
                var inGameGO = GameObject.Find("InGame") ?? GameObject.Find("InGamePanel");
                if (inGameGO != null)
                {
                    inGamePanel = inGameGO.GetComponent<InvaderInsider.UI.InGamePanel>();
                    if (inGamePanel != null)
                    {
                        uiManager.RegisterPanel("InGame", inGamePanel);
                    }
                }
                #if UNITY_EDITOR
                else
                {
                    Debug.LogError(LOG_PREFIX + "InGame 패널을 전혀 찾을 수 없습니다!");
                }
                #endif
            }

            var topBarPanel = FindObjectOfType<InvaderInsider.UI.TopBarPanel>(true);
            if (topBarPanel != null)
            {
                // Canvas나 부모 오브젝트도 활성화
                Transform current = topBarPanel.transform;
                while (current != null)
                {
                    current.gameObject.SetActive(true);
                    current = current.parent;
                }
                uiManager.RegisterPanel("TopBar", topBarPanel);
                cachedTopBarPanel = topBarPanel; // 캐시 업데이트
            }
            else
            {
                var topBarGO = GameObject.Find("TopBar") ?? GameObject.Find("TopBarPanel");
                if (topBarGO != null)
                {
                    topBarPanel = topBarGO.GetComponent<InvaderInsider.UI.TopBarPanel>();
                    if (topBarPanel != null)
                    {
                        uiManager.RegisterPanel("TopBar", topBarPanel);
                        cachedTopBarPanel = topBarPanel; // 캐시 업데이트
                    }
                }
                #if UNITY_EDITOR
                else
                {
                    Debug.LogError(LOG_PREFIX + "TopBar 패널을 전혀 찾을 수 없습니다!");
                }
                #endif
            }

            var bottomBarPanel = FindObjectOfType<InvaderInsider.UI.BottomBarPanel>(true);
            if (bottomBarPanel != null)
            {
                // Canvas나 부모 오브젝트도 활성화
                Transform current = bottomBarPanel.transform;
                while (current != null)
                {
                    current.gameObject.SetActive(true);
                    current = current.parent;
                }
                uiManager.RegisterPanel("BottomBar", bottomBarPanel);
                cachedBottomBarPanel = bottomBarPanel; // 캐시 업데이트
            }
            else
            {
                var bottomBarGO = GameObject.Find("BottomBar") ?? GameObject.Find("BottomBarPanel");
                if (bottomBarGO != null)
                {
                    bottomBarPanel = bottomBarGO.GetComponent<InvaderInsider.UI.BottomBarPanel>();
                    if (bottomBarPanel != null)
                    {
                        uiManager.RegisterPanel("BottomBar", bottomBarPanel);
                        cachedBottomBarPanel = bottomBarPanel; // 캐시 업데이트
                    }
                }
                #if UNITY_EDITOR
                else
                {
                    Debug.LogError(LOG_PREFIX + "BottomBar 패널을 전혀 찾을 수 없습니다!");
                }
                #endif
            }

            var pausePanel = FindObjectOfType<InvaderInsider.UI.PausePanel>();
            if (pausePanel != null)
                uiManager.RegisterPanel("Pause", pausePanel);

            var settingsPanel = FindObjectOfType<InvaderInsider.UI.SettingsPanel>();
            if (settingsPanel != null)
                uiManager.RegisterPanel("Settings", settingsPanel);

            var deckPanel = FindObjectOfType<InvaderInsider.UI.DeckPanel>();
            if (deckPanel != null)
                uiManager.RegisterPanel("Deck", deckPanel);

            var achievementsPanel = FindObjectOfType<InvaderInsider.UI.AchievementsPanel>();
            if (achievementsPanel != null)
                uiManager.RegisterPanel("Achievements", achievementsPanel);

            var handDisplayPanel = FindObjectOfType<InvaderInsider.UI.HandDisplayPanel>();
            if (handDisplayPanel != null)
                uiManager.RegisterPanel("HandDisplay", handDisplayPanel);

            var shopPanel = FindObjectOfType<InvaderInsider.UI.ShopPanel>();
            if (shopPanel != null)
                uiManager.RegisterPanel("Shop", shopPanel);

            var stageSelectPanel = FindObjectOfType<InvaderInsider.UI.StageSelectPanel>();
            if (stageSelectPanel != null)
                uiManager.RegisterPanel("StageSelect", stageSelectPanel);

            // 나머지 패널 숨기기 (등록된 패널만)
            string[] panelsToHide = { "Pause", "Settings", "Deck", "Achievements", "HandDisplay", "Shop", "StageSelect" };
            foreach (var panelName in panelsToHide)
            {
                if (uiManager.IsPanelRegistered(panelName))
                {
                    uiManager.HidePanel(panelName);
                }
            }

            // InGame, TopBar, BottomBar만 표시
            if (inGamePanel != null)
            {
                inGamePanel.gameObject.SetActive(true);
                inGamePanel.Show();
                uiManager.ShowPanelConcurrent("InGame");
            }
            #if UNITY_EDITOR
            else
            {
                Debug.LogError(LOG_PREFIX + "InGame 패널을 찾을 수 없습니다!");
            }
            #endif
            
            if (topBarPanel != null)
            {
                topBarPanel.gameObject.SetActive(true);
                topBarPanel.Show();
                uiManager.ShowPanelConcurrent("TopBar");
                
                // 스테이지 정보 업데이트 (현재/최대 형식)
                int currentStageIndex = cachedStageManager?.GetCurrentStageIndex() ?? 0;
                int totalStages = cachedStageManager?.GetStageCount() ?? 1;
                int activeMonsters = 0; // 초기값은 0 (아직 활성 몬스터 없음)
                int maxMonsters = cachedStageManager?.GetStageWaveCount(currentStageIndex) ?? 1;
                lastStageIndex = currentStageIndex; // 초기값 설정
                topBarPanel.UpdateStageInfo(currentStageIndex + 1, totalStages, activeMonsters, maxMonsters);
                
                // 실제 저장된 eData 사용 (초기값 0)
                int currentEData = saveDataManager?.GetCurrentEData() ?? 0;
                lastEData = currentEData; // 초기값 설정
                topBarPanel.UpdateEData(currentEData);
                
                // HP 초기화
                if (cachedPlayer != null)
                {
                    topBarPanel.UpdateHealth(cachedPlayer.CurrentHealth, cachedPlayer.MaxHealth);
                }
            }
            
            if (bottomBarPanel != null)
            {
                bottomBarPanel.gameObject.SetActive(true);
                bottomBarPanel.Show();
                uiManager.ShowPanelConcurrent("BottomBar");
                lastActiveEnemyCount = 0; // 초기값 설정
                bottomBarPanel.UpdateMonsterCountDisplay(0);
            }

            // Player HP 초기화 (100%로 설정)
            if (cachedPlayer != null)
            {
                cachedPlayer.ResetHealth(); // HP를 최대값으로 리셋
                lastPlayerHealth = cachedPlayer.CurrentHealth; // 초기값 설정
                UpdatePlayerHealthUI(cachedPlayer.CurrentHealth, cachedPlayer.MaxHealth);
            }

            // 카드 매니저 초기화
            if (cardManager != null)
                cardManager.LoadSummonData();

            // StageManager 초기화 및 스테이지 시작
            if (cachedStageManager != null)
            {
                // 저장된 진행상황이 있는지 확인하여 적절한 스테이지부터 시작
                int startingStage = 0; // 기본값은 0 (첫 번째 스테이지)
                if (saveDataManager != null && saveDataManager.HasSaveData())
                {
                    var saveData = saveDataManager.CurrentSaveData;
                    if (saveData != null)
                    {
                        // 클리어한 최고 스테이지의 다음 스테이지부터 시작
                        startingStage = saveData.progressData.highestStageCleared;
                    }
                }
                
                // InitializeStage() 대신 StartStageFrom()만 호출 (중복 방지)
                cachedStageManager.StartStageFrom(startingStage);
            }

            // 게임 상태를 Playing으로 설정
            CurrentGameState = GameState.Playing;
            Time.timeScale = 1f;
            
            // 스테이지 클리어 플래그 초기화
            stageClearedProcessed = false;
        }

        // 별점 계산 로직 (임시)
        private int CalculateStageStars()
        {
            if (cachedPlayer == null) return 1;
            
            float healthPercent = cachedPlayer.CurrentHealth / cachedPlayer.MaxHealth;
            
            if (healthPercent >= 0.8f)
                return 3;
            else if (healthPercent >= 0.5f)
                return 2;
            else
                return 1;
        }

        public void PauseGame()
        {
            // 로그 제거 - 메모리 할당 최적화
            Time.timeScale = 0f;
            CurrentGameState = GameState.Paused;
            
            // 일시정지 시 현재 진행상황 저장
            saveDataManager?.ForceSave();
            
            uiManager?.ShowPanel("Pause");
        }

        public void ResumeGame()
        {
            // 로그 제거 - 메모리 할당 최적화
            Time.timeScale = 1f;
            CurrentGameState = GameState.Playing;
            
            // 순환 참조 방지를 위해 조건부로 패널 숨기기
            if (uiManager != null && uiManager.IsPanelActive("Pause"))
            {
                // 직접 패널 숨기기 (HidePanel 호출하지 않음)
                var pausePanel = FindObjectOfType<InvaderInsider.UI.PausePanel>();
                if (pausePanel != null)
                {
                    pausePanel.gameObject.SetActive(false);
                }
            }
        }

        public void EndGame()
        {
            Time.timeScale = 0f;
            
            // 게임 종료 시 현재 진행상황 저장
            saveDataManager?.ForceSave();
            
            // 게임 종료 로직
        }
    }
} 