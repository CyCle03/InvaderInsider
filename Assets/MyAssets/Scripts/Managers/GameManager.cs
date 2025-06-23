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
            "스테이지 {0} 클리어 완료" // 7
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

        // 성능 최적화: Update 체크 주기 조정
        private const float STATE_CHECK_INTERVAL = 0.2f; // 0.2초마다 상태 체크
        private float nextStateCheckTime = 0f;
        
        private bool stageClearedProcessed = false; // 스테이지 클리어 중복 처리 방지

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

        public void StageCleared(int stageNum)
        {
            OnStageClearedEvent?.Invoke();
            // UpdateStageProgress는 HandleStageCleared에서만 호출 (중복 방지)
        }

        private void Update()
        {
            if (CurrentGameState != GameState.Playing) return;

            // 상태 체크 주기 최적화
            if (Time.time >= nextStateCheckTime)
            {
                CheckStageCompletion();
                nextStateCheckTime = Time.time + STATE_CHECK_INTERVAL;
            }
        }

        private void CheckStageCompletion()
        {
            if (cachedStageManager == null) return;

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
            
            // 스테이지 클리어 시 축적된 EData와 스테이지 진행을 한 번에 저장
            saveDataManager?.UpdateStageProgress(clearedStageIndex + 1);
            
            // 스테이지 클리어 이벤트 호출
            StageCleared(clearedStageIndex + 1);
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
            
            // 스테이지 시작 (요청된 스테이지가 있으면 해당 스테이지, 없으면 첫 번째 스테이지)
            var stageManager = StageManager.Instance;
            if (stageManager != null)
            {
                if (requestedStartStage >= 0)
                {
                    stageManager.StartStageFrom(requestedStartStage);
                    requestedStartStage = -1; // 사용한 후 리셋
                }
                else
                {
                    stageManager.InitializeStage(); // 기본적으로 첫 번째 스테이지 시작
                }
                
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "StageManager를 통해 스테이지를 시작했습니다.");
                #endif
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + "StageManager를 찾을 수 없습니다!");
                #endif
                
                // StageManager가 없어도 게임 상태는 Playing으로 설정
                Time.timeScale = 1f;
                CurrentGameState = GameState.Playing;
            }
            
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + "게임 초기화 완료. 게임 상태를 Playing으로 설정했습니다.");
            #endif
        }

        // 성능 최적화: FindObjectOfType 호출을 한 번에 처리
        private void CacheAndRegisterAllPanels()
        {
            // 모든 BasePanel을 한 번에 찾아서 처리
            var allPanels = FindObjectsOfType<BasePanel>(true);
            
            // 딕셔너리로 빠른 검색을 위한 임시 매핑
            var panelsByType = new System.Collections.Generic.Dictionary<System.Type, BasePanel>();
            
            foreach (var panel in allPanels)
            {
                if (panel != null && panel.gameObject != null)
                {
                    panel.gameObject.SetActive(false);
                    panel.ForceHide();
                    panelsByType[panel.GetType()] = panel;
                }
            }

            // 타입별로 등록 (FindObjectOfType 제거)
            RegisterPanelByType<InvaderInsider.UI.PausePanel>("Pause", panelsByType);
            RegisterPanelByType<InvaderInsider.UI.SettingsPanel>("Settings", panelsByType);
            RegisterPanelByType<InvaderInsider.UI.DeckPanel>("Deck", panelsByType);
            RegisterPanelByType<InvaderInsider.UI.SummonChoicePanel>("SummonChoice", panelsByType);
            RegisterPanelByType<InvaderInsider.UI.HandDisplayPanel>("HandDisplay", panelsByType);
            RegisterPanelByType<InvaderInsider.UI.ShopPanel>("Shop", panelsByType);
            RegisterPanelByType<InvaderInsider.UI.StageSelectPanel>("StageSelect", panelsByType);
        }

        private void RegisterPanelByType<T>(string panelName, System.Collections.Generic.Dictionary<System.Type, BasePanel> panelsByType) where T : BasePanel
        {
            if (panelsByType.TryGetValue(typeof(T), out BasePanel panel))
            {
                uiManager.RegisterPanel(panelName, panel);
                
                #if UNITY_EDITOR
                Debug.Log($"{LOG_PREFIX}패널 등록 성공: {panelName} ({typeof(T).Name})");
                #endif
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogWarning($"{LOG_PREFIX}패널을 찾을 수 없습니다: {panelName} ({typeof(T).Name})");
                #endif
            }
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
            // InGame 패널 설정
            var inGamePanel = FindCachedOrNewPanel<InvaderInsider.UI.InGamePanel>("InGame", "InGamePanel");
            if (inGamePanel != null)
            {
                ActivatePanelHierarchy(inGamePanel.transform);
                uiManager.RegisterPanel("InGame", inGamePanel);
                inGamePanel.Show();
            }

            // TopBar 패널 설정  
            var topBarPanel = FindCachedOrNewPanel<InvaderInsider.UI.TopBarPanel>("TopBar", "TopBarPanel");
            if (topBarPanel != null)
            {
                ActivatePanelHierarchy(topBarPanel.transform);
                uiManager.RegisterPanel("TopBar", topBarPanel);
                topBarPanel.Show();
            }

            // BottomBar 패널 설정
            var bottomBarPanel = FindCachedOrNewPanel<InvaderInsider.UI.BottomBarPanel>("BottomBar", "BottomBarPanel");
            if (bottomBarPanel != null)
            {
                ActivatePanelHierarchy(bottomBarPanel.transform);
                uiManager.RegisterPanel("BottomBar", bottomBarPanel);
                bottomBarPanel.Show();
            }
        }

        // 중복 코드 제거: 패널 찾기 로직 통합
        private T FindCachedOrNewPanel<T>(params string[] possibleNames) where T : BasePanel
        {
            // 먼저 FindObjectOfType으로 찾기
            T panel = FindObjectOfType<T>(true);
            if (panel != null) return panel;

            // 이름으로 GameObject 찾아서 컴포넌트 가져오기
            foreach (string name in possibleNames)
            {
                var gameObject = GameObject.Find(name);
                if (gameObject != null)
                {
                    panel = gameObject.GetComponent<T>();
                    if (panel != null) return panel;
                }
            }

            return null;
        }

        private void ActivatePanelHierarchy(Transform panel)
        {
            Transform current = panel;
            while (current != null)
            {
                current.gameObject.SetActive(true);
                current = current.parent;
            }
        }

        public void PauseGame(bool showPauseUI = true)
        {
            #if UNITY_EDITOR
            Debug.Log($"{LOG_PREFIX}게임 일시정지 요청 - showPauseUI: {showPauseUI}");
            #endif
            
            Time.timeScale = 0f;
            CurrentGameState = GameState.Paused;
            
            if (showPauseUI)
            {
                if (uiManager != null)
                {
                    if (uiManager.IsPanelRegistered("Pause"))
                    {
                        uiManager.ShowPanel("Pause");
                        #if UNITY_EDITOR
                        Debug.Log($"{LOG_PREFIX}Pause 패널을 표시했습니다.");
                        #endif
                    }
                    else
                    {
                        #if UNITY_EDITOR
                        Debug.LogWarning($"{LOG_PREFIX}Pause 패널이 UIManager에 등록되지 않았습니다.");
                        #endif
                    }
                }
                else
                {
                    #if UNITY_EDITOR
                    Debug.LogError($"{LOG_PREFIX}UIManager가 없습니다.");
                    #endif
                }
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