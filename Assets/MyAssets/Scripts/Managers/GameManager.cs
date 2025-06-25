using UnityEngine;
using System;
using InvaderInsider.Data;
using InvaderInsider.UI;
using UnityEngine.SceneManagement;
using InvaderInsider.Cards;
using InvaderInsider.ScriptableObjects;

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
            "스테이지 {0} 클리어 완료", // 7
            "ResourceManager를 찾을 수 없습니다.", // 8
            "SaveDataManager 초기화에 실패했습니다.", // 9
            "UIManager 초기화에 실패했습니다.", // 10
            "필수 컴포넌트를 찾을 수 없습니다: {0}" // 11
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
        private float nextStateCheckTime = 0f;
        
        private bool stageClearedProcessed = false; // 스테이지 클리어 중복 처리 방지

        // 설정 참조
        private GameConfigSO gameConfig;

        // 메인 메뉴에서 호출할 스테이지 설정 메서드
        public static void SetRequestedStartStage(int stageIndex)
        {
            requestedStartStage = stageIndex;
        }

        private void Awake()
        {
            Debug.Log("[FORCE LOG] GameManager Awake 시작");
            
            if (isQuitting) return;

            lock (_lock)
            {
                if (instance == null)
                {
                    instance = this;
                    DontDestroyOnLoad(gameObject);
                    
                    Debug.Log("[FORCE LOG] GameManager 인스턴스 설정 완료, Config 로딩 시작");
                    
                    // Config 로딩 (가장 먼저)
                    LoadConfig();
                    
                    Debug.Log("[FORCE LOG] Config 로딩 완료, 매니저 초기화 시작");
                    
                    // 매니저들 초기화
                    InitializeManagers();

                    // 씬 전환 이벤트 등록
                    SceneManager.sceneLoaded += OnSceneLoaded;
                    
                    Debug.Log("[FORCE LOG] GameManager Awake 완료");
                }
                else if (instance != this)
                {
                    Debug.Log("[FORCE LOG] 중복 GameManager 제거");
                    Destroy(gameObject);
                }
            }
        }

        private void LoadConfig()
        {
            Debug.Log("[FORCE LOG] LoadConfig 시작");
            
            var configManager = ConfigManager.Instance;
            Debug.Log($"[FORCE LOG] ConfigManager 인스턴스: {(configManager != null ? "존재" : "null")}");
            
            if (configManager != null && configManager.GameConfig != null)
            {
                gameConfig = configManager.GameConfig;
                Debug.Log($"[FORCE LOG] GameConfig 로딩 성공 - enableStageClearDuplicatePrevention: {gameConfig.enableStageClearDuplicatePrevention}");
            }
            else
            {
                Debug.LogError($"[FORCE LOG] ConfigManager 또는 GameConfig를 찾을 수 없습니다. 기본값을 사용합니다.");
                Debug.LogError($"{LOG_PREFIX}ConfigManager 또는 GameConfig를 찾을 수 없습니다. 기본값을 사용합니다.");
                // 기본값으로 폴백
                gameConfig = ScriptableObject.CreateInstance<GameConfigSO>();
                Debug.Log($"[FORCE LOG] 기본 GameConfig 생성 완료");
            }
        }

        private void InitializeManagers()
        {
            saveDataManager = SaveDataManager.Instance;
            if (saveDataManager == null)
            {
                Debug.LogError($"{LOG_PREFIX}{LOG_MESSAGES[9]}");
            }

            uiManager = UIManager.Instance;
            if (uiManager == null)
            {
                Debug.LogError($"{LOG_PREFIX}{LOG_MESSAGES[10]}");
            }

            cardManager = FindObjectOfType<CardManager>();
            if (cardManager == null)
            {
                Debug.LogWarning($"{LOG_PREFIX}{string.Format(LOG_MESSAGES[11], "CardManager")}");
            }
            
            // ResourceManager 이벤트 구독
            var resourceManager = ResourceManager.Instance;
            if (resourceManager != null)
            {
                resourceManager.OnEDataChanged -= OnEDataChanged; // 중복 구독 방지
                resourceManager.OnEDataChanged += OnEDataChanged;
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX}{LOG_MESSAGES[8]}");
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
            if (scene.name == null)
            {
                Debug.LogError($"{LOG_PREFIX}로드된 씬의 이름이 null입니다.");
                return;
            }

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
            if (currentSceneName == gameConfig.gameSceneName)
            {
                if (cachedStageManager == null) 
                    Debug.LogWarning($"{LOG_PREFIX}{string.Format(LOG_MESSAGES[11], "StageManager")}");
                if (cachedBottomBarPanel == null) 
                    Debug.LogWarning($"{LOG_PREFIX}{string.Format(LOG_MESSAGES[11], "BottomBarPanel")}");
                if (cachedPlayer == null) 
                    Debug.LogWarning($"{LOG_PREFIX}{string.Format(LOG_MESSAGES[11], "Player")}");
                if (cachedTopBarPanel == null) 
                    Debug.LogWarning($"{LOG_PREFIX}{string.Format(LOG_MESSAGES[11], "TopBarPanel")}");
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
            if (isHandlingStateChange && gameConfig.enableStateChangeDuplicatePrevention)
            {
                Debug.LogWarning($"{LOG_PREFIX}HandleGameStateChanged가 이미 처리 중입니다. 중복 호출을 방지합니다.");
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
                        Time.timeScale = gameConfig.defaultTimeScale;
                        break;

                    case GameState.Paused:
                        Time.timeScale = gameConfig.pausedTimeScale;
                        // Pause 패널 표시는 PauseGame() 메서드에서 제어
                        break;

                    case GameState.GameOver:
                        Time.timeScale = gameConfig.pausedTimeScale;
                        break;

                    case GameState.Loading:
                        break;

                    case GameState.Settings:
                        Time.timeScale = gameConfig.pausedTimeScale;
                        break;
                }

                Debug.Log($"{LOG_PREFIX}{string.Format(LOG_MESSAGES[0], newState)}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX}게임 상태 변경 중 오류가 발생했습니다: {ex.Message}");
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
                Debug.LogWarning($"{LOG_PREFIX}이미 {newState} 상태입니다. 중복 변경을 방지합니다.");
                return;
            }
            
            CurrentGameState = newState;
        }

        public bool TrySpendEData(int amount)
        {
            if (gameConfig.enableEDataValidation)
            {
                if (amount <= gameConfig.minEDataValue)
                {
                    Debug.LogWarning($"{LOG_PREFIX}유효하지 않은 EData 소비량입니다: {amount}");
                    return false;
                }

                if (amount > gameConfig.maxEDataValue)
                {
                    Debug.LogWarning($"{LOG_PREFIX}EData 소비량이 최대값을 초과합니다: {amount}");
                    return false;
                }
            }

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
            else
            {
                Debug.LogError($"{LOG_PREFIX}{LOG_MESSAGES[8]}");
                return false;
            }
        }

        public void AddEData(int amount)
        {
            AddEData(amount, true);
        }

        public void AddEData(int amount, bool saveImmediately)
        {
            if (gameConfig.enableEDataValidation)
            {
                if (amount <= gameConfig.minEDataValue)
                {
                    Debug.LogWarning($"{LOG_PREFIX}유효하지 않은 EData 추가량입니다: {amount}");
                    return;
                }

                if (amount > gameConfig.maxEDataValue)
                {
                    Debug.LogWarning($"{LOG_PREFIX}EData 추가량이 최대값을 초과합니다: {amount}");
                    return;
                }
            }

            // ResourceManager로 위임
            var resourceManager = ResourceManager.Instance;
            if (resourceManager != null)
            {
                resourceManager.AddEData(amount, saveImmediately);
                UpdateEDataUI();
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX}{LOG_MESSAGES[8]}");
            }
        }

        public void UpdateStageWaveUI(int currentStage, int spawnedMonsters, int maxMonsters)
        {
            if (cachedTopBarPanel != null && cachedStageManager != null)
            {
                // TopBarPanel에서 Stage와 Wave 정보를 함께 표시
                int totalStages = cachedStageManager.GetStageCount();
                cachedTopBarPanel.UpdateStageInfo(currentStage, totalStages, spawnedMonsters, maxMonsters);
            }
            else
            {
                Debug.LogWarning($"{LOG_PREFIX}TopBarPanel 또는 StageManager가 null입니다. UI 업데이트를 건너뜁니다.");
            }
        }

        public void InitializeEDataDisplay()
        {
            UpdateEDataUI();
        }

        private void OnEDataChanged(int newEDataAmount)
        {
            Debug.Log($"{LOG_PREFIX}{string.Format(LOG_MESSAGES[6], newEDataAmount)}");
            UpdateEDataUI(newEDataAmount);
        }

        private void UpdateEDataUI()
        {
            var resourceManager = ResourceManager.Instance;
            if (resourceManager != null)
            {
                int currentEData = resourceManager.GetCurrentEData();
                UpdateEDataUI(currentEData);
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX}{LOG_MESSAGES[8]}");
            }
        }

        private void UpdateEDataUI(int currentEData)
        {
            if (cachedTopBarPanel != null)
            {
                // TopBarPanel에는 UpdateEDataDisplay 메서드가 없으므로 UpdateEData 사용
                cachedTopBarPanel.UpdateEData(currentEData);
            }
            else
            {
                Debug.LogWarning($"{LOG_PREFIX}TopBarPanel이 null입니다. EData UI 업데이트를 건너뜁니다.");
            }
        }

        public void StageCleared(int stageNum)
        {
            Debug.Log($"[FORCE LOG] StageCleared 호출됨 - 스테이지: {stageNum}");
            Debug.Log($"[FORCE LOG] stageClearedProcessed: {stageClearedProcessed}");
            Debug.Log($"[FORCE LOG] gameConfig null 여부: {gameConfig == null}");
            
            if (gameConfig != null)
            {
                Debug.Log($"[FORCE LOG] enableStageClearDuplicatePrevention: {gameConfig.enableStageClearDuplicatePrevention}");
            }
            
            if (stageClearedProcessed && gameConfig.enableStageClearDuplicatePrevention)
            {
                Debug.LogWarning($"[FORCE LOG] 스테이지 클리어 중복 처리 방지 - 메서드 종료");
                Debug.LogWarning($"{LOG_PREFIX}스테이지 클리어가 이미 처리되었습니다. 중복 처리를 방지합니다.");
                return;
            }

            stageClearedProcessed = true;
            Debug.Log($"[FORCE LOG] stageClearedProcessed를 true로 설정");

            Debug.Log($"{LOG_PREFIX}{string.Format(LOG_MESSAGES[7], stageNum)}");

            // StageManager에는 OnStageCleared 메서드가 없으므로 다른 방식으로 처리
            if (cachedStageManager != null)
            {
                // 스테이지 클리어 처리는 StageManager의 내부 로직으로 처리됨
                // 여기서는 로그만 출력하고 실제 처리는 StageManager가 담당
                Debug.Log($"{LOG_PREFIX}StageManager를 통해 스테이지 {stageNum} 클리어 처리 완료");
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX}StageManager가 null입니다. 스테이지 클리어 처리를 완료할 수 없습니다.");
            }

            OnStageClearedEvent?.Invoke();
            Debug.Log($"[FORCE LOG] OnStageClearedEvent 호출 완료");
        }

        private void Update()
        {
            // 10초마다 한 번씩 상태 체크
            if (Time.time % 10f < 0.1f)
            {
                Debug.Log($"[GAME STATE] CurrentGameState: {CurrentGameState}, gameConfig null: {gameConfig == null}");
                if (gameConfig != null)
                {
                    Debug.Log($"[GAME STATE] stateCheckInterval: {gameConfig.stateCheckInterval}");
                }
            }
            
            if (CurrentGameState != GameState.Playing) return;

            // 상태 체크 주기 최적화
            if (Time.time >= nextStateCheckTime)
            {
                // 20초마다 한 번씩 CheckStageCompletion 호출 로그
                if (Time.time % 20f < 0.1f)
                {
                    Debug.Log($"[FORCE LOG] CheckStageCompletion 호출 예정 - nextStateCheckTime: {nextStateCheckTime}");
                }
                
                CheckStageCompletion();
                nextStateCheckTime = Time.time + gameConfig.stateCheckInterval;
            }
        }

        private void CheckStageCompletion()
        {
            if (cachedStageManager == null) return;

            bool allEnemiesSpawned = AllEnemiesSpawned();
            int activeEnemyCount = cachedStageManager.ActiveEnemyCount;
            
            // 주기적 로그 - 10초마다 한 번씩만 출력
            if (Time.time % 10f < 0.1f)
            {
                Debug.Log($"[STAGE CHECK] 모든 적 스폰됨: {allEnemiesSpawned}, 활성 적 수: {activeEnemyCount}, 스테이지 클리어 처리됨: {stageClearedProcessed}");
            }
            
            if (allEnemiesSpawned && activeEnemyCount == 0)
            {
                if (!stageClearedProcessed)
                {
                    Debug.Log("[FORCE LOG] 스테이지 클리어 조건 만족! HandleStageCleared 호출");
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + $"스테이지 클리어 조건 만족 - 모든 적 스폰됨: {allEnemiesSpawned}, 활성 적 수: {activeEnemyCount}");
                    #endif
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
            
            // 디버깅을 위한 강제 로그
            Debug.Log($"[FORCE LOG] AllEnemiesSpawned - 스테이지: {currentStageIndex}, 스폰된 적: {spawnedCount}, 최대 적: {maxCount}, 결과: {spawnedCount >= maxCount}");
            
            return spawnedCount >= maxCount;
        }

        private void HandleStageCleared()
        {
            // 강제 로그 - 무조건 출력
            Debug.Log("[FORCE LOG] HandleStageCleared 메서드 호출됨!");
            
            if (cachedStageManager == null) 
            {
                Debug.LogError("[FORCE LOG] cachedStageManager가 null입니다!");
                return;
            }

            // 스테이지 클리어 처리
            int clearedStageIndex = cachedStageManager.GetCurrentStageIndex();
            
            Debug.Log($"[FORCE LOG] 클리어된 스테이지 인덱스: {clearedStageIndex}");
            
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + $"HandleStageCleared 호출됨 - 스테이지 {clearedStageIndex} 클리어 처리 시작");
            #endif
            
            // 스테이지 클리어 시 축적된 EData와 스테이지 진행을 한 번에 저장
            if (saveDataManager != null)
            {
                Debug.Log($"[FORCE LOG] SaveDataManager 존재 - UpdateStageProgress 호출 예정");
                
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + $"UpdateStageProgress 호출 - 스테이지 {clearedStageIndex + 1}");
                #endif
                
                // 저장 전 상태 확인
                var beforeSaveData = saveDataManager.CurrentSaveData;
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + $"저장 전 - 최고 클리어 스테이지: {beforeSaveData?.progressData?.highestStageCleared}");
                #endif
                
                saveDataManager.UpdateStageProgress(clearedStageIndex + 1);
                
                Debug.Log($"[FORCE LOG] UpdateStageProgress 호출 완료");
                
                // 저장 후 상태 확인
                var afterSaveData = saveDataManager.CurrentSaveData;
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + $"저장 후 - 최고 클리어 스테이지: {afterSaveData?.progressData?.highestStageCleared}");
                Debug.Log(LOG_PREFIX + $"SaveDataManager.HasSaveData(): {saveDataManager.HasSaveData()}");
                #endif
            }
            else
            {
                Debug.LogError("[FORCE LOG] SaveDataManager가 null입니다!");
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + "SaveDataManager를 찾을 수 없어 스테이지 진행을 저장할 수 없습니다!");
                #endif
            }
            
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
                
                // PauseGame을 호출하여 일시정지 상태로 변경하고 패널 표시
                PauseGame(true);
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
                    // Continue는 클리어한 최고 스테이지부터 다시 시작 (다음 스테이지가 아닌)
                    int highestCleared = saveData.progressData.highestStageCleared;
                    
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + $"Continue 게임 시작 - 최고 클리어 스테이지: {highestCleared}");
                    #endif
                    
                    // 최소 1스테이지는 보장 (아무것도 클리어하지 않은 경우)
                    int startStage = Mathf.Max(1, highestCleared);
                    
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + $"시작할 스테이지: {startStage} (인덱스: {startStage - 1})");
                    #endif
                    
                    // GameManager에 시작할 스테이지 설정 (인덱스는 0부터 시작하므로 startStage - 1)
                    SetRequestedStartStage(startStage - 1);
                }
                
                // 게임 씬으로 전환
                LoadGameScene();
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogWarning(LOG_PREFIX + "Continue 실패 - SaveDataManager 없거나 저장 데이터 없음");
                #endif
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
            
            // 상태 플래그 리셋
            isStartingGame = false;
            isLoadingScene = false;
            
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