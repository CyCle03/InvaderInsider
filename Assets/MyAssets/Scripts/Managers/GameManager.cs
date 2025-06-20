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
        private static int requestedStartStage = -1; // 메인 메뉴에서 요청한 시작 스테이지

        private static bool isHandlingStateChange = false; // 중복 호출 방지 플래그

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
        
        // 게임 시작 상태 플래그들
        private bool isStartingGame = false;
        private bool isLoadingScene = false;

        // 메인 메뉴에서 호출할 스테이지 설정 메서드
        public static void SetRequestedStartStage(int stageIndex)
        {
            requestedStartStage = stageIndex;
        }

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
            
            // ResourceManager 이벤트 구독
            var resourceManager = ResourceManager.Instance;
            if (resourceManager != null)
            {
                resourceManager.OnEDataChanged -= OnEDataChanged; // 중복 구독 방지
                resourceManager.OnEDataChanged += OnEDataChanged;
            }
            
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
        }

        private void UpdateCachedComponents()
        {
            cachedStageManager = FindObjectOfType<StageManager>();
            cachedBottomBarPanel = FindObjectOfType<BottomBarPanel>(true); // 비활성화된 객체도 포함
            cachedPlayer = FindObjectOfType<Player>();
            cachedTopBarPanel = FindObjectOfType<TopBarPanel>(true); // 비활성화된 객체도 포함

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
            
            // ResourceManager 이벤트 구독 해제
            var resourceManager = ResourceManager.Instance;
            if (resourceManager != null)
            {
                resourceManager.OnEDataChanged -= OnEDataChanged;
            }
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
            // 이제 InitializeGame()은 실제 게임 시작 시점(NewGame/Continue)에서만 호출됩니다.
            // 게임 씬 로드 후 외부에서 명시적으로 호출해야 합니다.
        }

        private void HandleGameStateChanged(GameState newState)
        {
            // 중복 호출 방지 (무한 루프 방지)
            if (isHandlingStateChange)
            {
                return;
            }
            
            isHandlingStateChange = true;
            
            try
            {
                switch (newState)
                {
                    case GameState.MainMenu:
                    // MainMenu 패널은 Main 씬에만 존재하므로 직접 패널을 찾지 않음
                    // 씬 전환은 호출하는 곳(PausePanel 등)에서 직접 처리
                        break;

                    case GameState.Playing:
                        Time.timeScale = 1f;
                        break;

                    case GameState.Paused:
                        Time.timeScale = 0f;
                        // Pause 패널 표시는 PauseGame() 메서드에서 제어
                        break;

                    case GameState.GameOver:
                        Time.timeScale = 0f;
                        break;

                    case GameState.Loading:
                        break;

                    case GameState.Settings:
                        Time.timeScale = 0f;
                        break;
                }

                if (OnGameStateChanged != null)
                {
                    OnGameStateChanged.Invoke(newState);
                }
            }
            finally
            {
                isHandlingStateChange = false;
            }
        }

        public void SetGameState(GameState newState)
        {
            // 동일한 상태로의 중복 변경 방지 (무한 루프 방지)
            if (CurrentGameState == newState)
            {
                return;
            }
            
            CurrentGameState = newState;
        }

        public bool TrySpendEData(int amount)
        {
            // ResourceManager로 위임
            var resourceManager = ResourceManager.Instance;
            if (resourceManager != null)
            {
                bool success = resourceManager.TrySpendEData(amount);
                if (success)
                {
                    // UI 업데이트
                    UpdateEDataUI();
                }
                return success;
            }
            
            // ResourceManager가 없으면 기존 방식 사용
            if (amount <= 0) return false;

            if (saveDataManager?.GetCurrentEData() >= amount)
            {
                saveDataManager.UpdateEDataWithoutSave(-amount);
                UpdateEDataUI();
                return true;
            }
            return false;
        }

        public void AddEData(int amount)
        {
            AddEData(amount, true); // 기본적으로 저장
        }
        
        public void AddEData(int amount, bool saveImmediately)
        {
            // ResourceManager로 위임
            var resourceManager = ResourceManager.Instance;
            if (resourceManager != null)
            {
                resourceManager.AddEData(amount, saveImmediately);
                UpdateEDataUI();
                return;
            }
            
            // ResourceManager가 없으면 기존 방식 사용
            if (amount <= 0) return;

            if (saveImmediately)
            {
                saveDataManager?.UpdateEData(amount);
            }
            else
            {
                saveDataManager?.UpdateEDataWithoutSave(amount);
            }
            UpdateEDataUI();
        }

        // StageManager에서 호출하여 TopBarPanel의 Stage/Wave UI를 업데이트
        public void UpdateStageWaveUI(int currentStage, int spawnedMonsters, int maxMonsters)
        {
            if (cachedTopBarPanel != null && cachedStageManager != null)
            {
                int totalStages = cachedStageManager.GetStageCount();
                cachedTopBarPanel.UpdateStageInfo(currentStage, totalStages, spawnedMonsters, maxMonsters);
            }
        }

        // New Game 시작 시 초기 eData를 TopBarPanel에 설정
        public void InitializeEDataDisplay()
        {
            UpdateEDataUI();
        }

        // ResourceManager 이벤트 핸들러
        private void OnEDataChanged(int newEDataAmount)
        {
            UpdateEDataUI(newEDataAmount);
        }

        // UI 업데이트 헬퍼 메서드
        private void UpdateEDataUI()
        {
            // ResourceManager에서 현재 EData 가져오기
            var resourceManager = ResourceManager.Instance;
            int currentEData = resourceManager?.GetCurrentEData() ?? 
                              saveDataManager?.GetCurrentEData() ?? 0;
            
            UpdateEDataUI(currentEData);
        }

        // UI 업데이트 헬퍼 메서드 (오버로드)
        private void UpdateEDataUI(int currentEData)
        {
            // TopBarPanel 업데이트
            if (cachedTopBarPanel != null)
            {
                cachedTopBarPanel.UpdateEData(currentEData);
            }
            
            // 카드 뽑기 UI는 제거됨 (단순화)
            // 필요시 단일 카드 뽑기 버튼을 다른 UI에 통합 가능
        }

        public void StageCleared(int stageNum, int stars)
        {
            OnStageClearedEvent?.Invoke();
            // UpdateStageProgress는 HandleStageCleared에서만 호출 (중복 방지)
        }

        // eData 이벤트 구독 메서드들은 더 이상 사용되지 않음 (직접 호출 방식으로 전환)
        [System.Obsolete("eData는 이제 직접 호출 방식으로 업데이트됩니다.")]
        public void AddResourcePointsListener(Action<int> listener)
        {
            // 더 이상 사용되지 않음
        }

        [System.Obsolete("eData는 이제 직접 호출 방식으로 업데이트됩니다.")]
        public void RemoveResourcePointsListener(Action<int> listener)
        {
            // 더 이상 사용되지 않음
        }

        private bool stageClearedProcessed = false; // 스테이지 클리어 중복 처리 방지

        private void Update()
        {
            if (CurrentGameState == GameState.Playing)
            {
                // 스테이지 클리어 체크만 유지 (UI 자동 업데이트 제거)
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
            
            // 스테이지 클리어 시 축적된 EData와 스테이지 진행을 한 번에 저장
            saveDataManager?.UpdateStageProgress(clearedStageIndex + 1, stars);
            
            // 스테이지 클리어 이벤트 호출
            StageCleared(clearedStageIndex + 1, stars);
            OnStageClearedEvent?.Invoke();
            
            // 모든 스테이지 완료 체크
            CheckAllStagesCompleted(clearedStageIndex);
        }

        // 모든 스테이지 완료 시 일시정지 패널 활성화
        private void CheckAllStagesCompleted(int clearedStageIndex)
        {
            if (cachedStageManager == null) return;
            
            int totalStages = cachedStageManager.GetStageCount();
            bool allStagesCompleted = (clearedStageIndex + 1) >= totalStages;
            
            if (allStagesCompleted)
            {
            #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "모든 스테이지 완료! 일시정지 패널을 활성화합니다.");
            #endif
                
                // 게임을 일시정지 상태로 변경하고 일시정지 패널 표시
                SetGameState(GameState.Paused);
                uiManager?.ShowPanel("Pause");
            }
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

            // 먼저 모든 기존 패널들을 강제로 숨기고 정리
            uiManager.Cleanup();
            
            // UI 패널 등록 (한 번에 모든 패널을 찾아서 캐싱)
            CacheAndRegisterAllPanels();

            // 나머지 패널 숨기기 (등록된 패널만)
            HideNonGameplayPanels();

            // InGame, TopBar, BottomBar만 표시
            SetupGameplayPanels();
        }

        private void CacheAndRegisterAllPanels()
        {
            // 한 번에 모든 패널을 찾아서 캐싱
            var allPanels = FindObjectsOfType<BasePanel>(true);
            foreach (var panel in allPanels)
            {
                if (panel != null && panel.gameObject != null)
                {
                    panel.gameObject.SetActive(false);
                    panel.ForceHide();
                }
            }

            // UI 패널 등록
            var pausePanel = FindObjectOfType<InvaderInsider.UI.PausePanel>(true);
            if (pausePanel != null)
                uiManager.RegisterPanel("Pause", pausePanel);
            
            var settingsPanel = FindObjectOfType<InvaderInsider.UI.SettingsPanel>(true);
            if (settingsPanel != null)
                uiManager.RegisterPanel("Settings", settingsPanel);

            var deckPanel = FindObjectOfType<InvaderInsider.UI.DeckPanel>(true);
            if (deckPanel != null)
                uiManager.RegisterPanel("Deck", deckPanel);

            var summonChoicePanel = FindObjectOfType<InvaderInsider.UI.SummonChoicePanel>(true);
            if (summonChoicePanel != null)
                uiManager.RegisterPanel("SummonChoice", summonChoicePanel);

            var handDisplayPanel = FindObjectOfType<InvaderInsider.UI.HandDisplayPanel>(true);
            if (handDisplayPanel != null)
                uiManager.RegisterPanel("HandDisplay", handDisplayPanel);

            var shopPanel = FindObjectOfType<InvaderInsider.UI.ShopPanel>(true);
            if (shopPanel != null)
                uiManager.RegisterPanel("Shop", shopPanel);

            var stageSelectPanel = FindObjectOfType<InvaderInsider.UI.StageSelectPanel>(true);
            if (stageSelectPanel != null)
                uiManager.RegisterPanel("StageSelect", stageSelectPanel);
        }

        private void HideNonGameplayPanels()
        {
            // 나머지 패널 숨기기 (등록된 패널만)
            string[] panelsToHide = { "Pause", "Settings", "Deck", "Achievements", "HandDisplay", "Shop", "StageSelect", "SummonChoice" };
            foreach (var panelName in panelsToHide)
            {
                if (uiManager.IsPanelRegistered(panelName))
                {
                    uiManager.HidePanel(panelName);
                }
            }
        }

        private void SetupGameplayPanels()
        {
            // InGame, TopBar, BottomBar만 표시
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
                inGamePanel.Show(); // 패널 보이기
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
                        inGamePanel.Show(); // 패널 보이기
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
                topBarPanel.Show(); // 패널 보이기
                
                // 실제 스테이지 수로 초기화
                if (cachedStageManager != null)
                {
                    int totalStages = cachedStageManager.GetStageCount();
                    cachedTopBarPanel.UpdateStageInfo(1, totalStages, 0, 0);
                }
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
                        topBarPanel.Show(); // 패널 보이기
                        
                        // 실제 스테이지 수로 초기화
                        if (cachedStageManager != null)
                        {
                            int totalStages = cachedStageManager.GetStageCount();
                            cachedTopBarPanel.UpdateStageInfo(1, totalStages, 0, 0);
                        }
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
                bottomBarPanel.Show(); // 패널 보이기
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
                        bottomBarPanel.Show(); // 패널 보이기
                    }
                }
                #if UNITY_EDITOR
                else
                {
                    Debug.LogError(LOG_PREFIX + "BottomBar 패널을 전혀 찾을 수 없습니다!");
                }
                #endif
            }

            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + "게임플레이 패널들 표시 완료 - InGame, TopBar, BottomBar");
            #endif

            // Player HP 초기화 (100%로 설정)
            if (cachedPlayer != null)
            {
                cachedPlayer.ResetHealth(); // HP를 최대값으로 리셋
                if (cachedBottomBarPanel != null)
                {
                    cachedBottomBarPanel.UpdateHealth(cachedPlayer.CurrentHealth, cachedPlayer.MaxHealth);
                    cachedBottomBarPanel.UpdateHealthDisplay(cachedPlayer.CurrentHealth / cachedPlayer.MaxHealth);
                }
            }

            // 카드 매니저 초기화
            if (cardManager != null)
                cardManager.LoadSummonData();
            
            // Continue Game 감지 (requestedStartStage 초기화 전에 확인)
            bool isContinueGame = requestedStartStage > 0;
            
            // Continue Game 시 SaveDataManager에서 eData 로드 및 UI 업데이트
            // SaveDataManager가 null이면 다시 찾기 시도 (여러 번 시도)
            if (saveDataManager == null)
            {
                saveDataManager = SaveDataManager.Instance;
                
                // 첫 번째 시도가 실패하면 FindObjectOfType으로 직접 찾기
                if (saveDataManager == null)
                {
                    saveDataManager = FindObjectOfType<SaveDataManager>();
                }
            }
            
            if (saveDataManager != null && isContinueGame)
            {
                // Continue Game인 경우 SaveDataManager에서 실제 eData 값을 가져와서 UI에 반영
                int savedEData = saveDataManager.GetCurrentEData();
                
                // ResourceManager에도 eData 값 동기화
                var resourceManager = ResourceManager.Instance;
                if (resourceManager != null)
                {
                    resourceManager.SetEData(savedEData);
                }
                
                // TopBarPanel에 올바른 eData 값 업데이트
                if (cachedTopBarPanel != null)
                {
                    cachedTopBarPanel.UpdateEData(savedEData);
                }
                
                            // CardDrawUI 제거로 인한 업데이트 코드 제거
            }
            else if (saveDataManager != null)
            {
                // New Game 시에도 초기 eData 값을 TopBarPanel에 설정
                InitializeEDataDisplay();
                
                // ResourceManager에도 초기 eData 값 동기화
                var resourceManager = ResourceManager.Instance;
                if (resourceManager != null)
                {
                    int currentEData = saveDataManager.GetCurrentEData();
                    resourceManager.SetEData(currentEData);
                }
            }

            // StageManager 초기화 및 스테이지 시작
            if (cachedStageManager != null)
            {
                int startingStage = 0; // 기본값은 0 (첫 번째 스테이지)
                
                // 메인 메뉴에서 요청한 스테이지가 있다면 우선 사용
                if (requestedStartStage >= 0)
                {
                    startingStage = requestedStartStage;
                    requestedStartStage = -1; // 사용 후 초기화
                }
                // 그렇지 않으면 저장된 진행상황 확인
                else if (saveDataManager != null && saveDataManager.HasSaveData())
                {
                    var saveData = saveDataManager.CurrentSaveData;
                    if (saveData != null)
                    {
                        // 클리어한 최고 스테이지의 다음 스테이지부터 시작
                        int highestCleared = saveData.progressData.highestStageCleared;
                        startingStage = highestCleared; // 이미 클리어한 스테이지부터 다시 시작 (플레이어 선택권 제공)
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

        public void PauseGame(bool showPauseUI = true)
        {
            // 로그 제거 - 메모리 할당 최적화
            Time.timeScale = 0f;
            CurrentGameState = GameState.Paused;
            
            // 스테이지 클리어 시에만 저장하므로 일시정지 시 저장 제거
            
            if (showPauseUI)
            {
                uiManager?.ShowPanel("Pause");
            }
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
            
            // 스테이지 클리어 시에만 저장하므로 게임 종료 시 저장 제거
            
            // 게임 종료 로직
        }

        // 씬 전환 및 게임 시작 메서드들
        public void StartNewGame()
        {
            // 이미 게임 시작 중이거나 씬 로딩 중이면 무시
            if (isStartingGame || isLoadingScene)
            {
                return;
            }
            
            isStartingGame = true;
            
            if (saveDataManager != null)
            {
                saveDataManager.ResetGameData();
            }
            
            // 새 게임은 항상 첫 번째 스테이지(인덱스 0)부터 시작
            SetRequestedStartStage(0);
            
            // 게임 씬으로 전환
            LoadGameScene();
        }

        public void StartContinueGame()
        {
            // 이미 게임 시작 중이거나 씬 로딩 중이면 무시
            if (isStartingGame || isLoadingScene)
            {
                return;
            }
            
            isStartingGame = true;
            
            if (saveDataManager != null && saveDataManager.HasSaveData())
            {
                saveDataManager.LoadGameData();
                var saveData = saveDataManager.CurrentSaveData;
                if (saveData != null)
                {
                    // Continue는 클리어한 최고 스테이지의 다음 스테이지부터 시작
                    int highestCleared = saveData.progressData.highestStageCleared;
                    int nextStage = highestCleared + 1; // 클리어한 스테이지의 다음 스테이지
                    
                    // GameManager에 시작할 스테이지 설정 (인덱스는 0부터 시작하므로 nextStage - 1)
                    SetRequestedStartStage(nextStage - 1);
                }
                
                // 게임 씬으로 전환
                LoadGameScene();
            }
            else
            {
                isStartingGame = false; // 실패 시 플래그 리셋
            }
        }

        public void LoadMainMenuScene()
        {
            if (isLoadingScene)
            {
                return;
            }
            
            Time.timeScale = 1f;
            CurrentGameState = GameState.MainMenu;
            
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
        }

        private void LoadGameScene()
        {
            if (isLoadingScene)
            {
                return;
            }
            
            isLoadingScene = true;
            
            Time.timeScale = 1f;
            CurrentGameState = GameState.Loading;

            StartCoroutine(LoadGameSceneAsync());
        }

        private System.Collections.IEnumerator LoadGameSceneAsync()
        {
            // UI 정리
            if (uiManager != null)
            {
                uiManager.Cleanup();
            }
            
            yield return null; // 한 프레임 대기
            
            // 비동기로 Game 씬 로드
            var asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Game");
            
            // 씬 로딩 완료까지 대기
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            yield return new WaitForEndOfFrame(); // 모든 오브젝트 초기화 대기
            
            // 게임 씬 로드 완료 후 자동으로 게임 초기화
            InitializeGame();
            
            // 플래그 리셋
            isStartingGame = false;
            isLoadingScene = false;
        }
    }
} 