using UnityEngine;
using System;
using InvaderInsider;
using InvaderInsider.Data;
using InvaderInsider.UI;
using UnityEngine.SceneManagement;
using InvaderInsider.Cards;
using InvaderInsider.ScriptableObjects;
using InvaderInsider.Core;

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

    public class GameManager : SingletonManager<GameManager>
    {
        #region Constants
        
        private const string LOG_PREFIX = "[GameManager] ";
        
        #endregion
        
        #region Static Members
        
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
        
        private static int requestedStartStage = -1; // 메인 메뉴에서 요청한 시작 스테이지
        private static bool isHandlingStateChange = false; // 중복 호출 방지 플래그

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

        [Header("Manager References")]
        [SerializeField] private StageManager stageManagerReference; // Inspector에서 할당
        [SerializeField] private BottomBarPanel bottomBarPanelReference; // Inspector에서 할당  
        [SerializeField] private Player playerReference; // Inspector에서 할당
        [SerializeField] private TopBarPanel topBarPanelReference; // Inspector에서 할당
        [SerializeField] private CardManager cardManagerReference; // Inspector에서 할당

        public event Action<GameState> OnGameStateChanged;

        private SaveDataManager saveDataManager;
        private UIManager uiManager;
        private UICoordinator uiCoordinator;

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

        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            // 로깅 시스템 초기화 (개발 환경별 설정)
            #if UNITY_EDITOR
            DebugUtils.EnableDevelopmentLogging();
            Debug.Log($"{LOG_PREFIX}개발 모드 로깅 활성화");
            #elif DEVELOPMENT_BUILD
            DebugUtils.EnableMinimalLogging();
            Debug.Log($"{LOG_PREFIX}최소 로깅 모드 활성화");
            #else
            DebugUtils.EnableProductionLogging();
            #endif
            
            Debug.Log($"{LOG_PREFIX}GameManager 초기화 시작");
            
            // Config 로딩 (가장 먼저)
            LoadConfig();
            
            // 매니저들 초기화
            InitializeManagers();

            // 씬 전환 이벤트 등록
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            Debug.Log($"{LOG_PREFIX}GameManager 초기화 완료 - 모든 매니저 초기화 완료");
        }

        private void LoadConfig()
        {
            Debug.Log($"{LOG_PREFIX}GameConfig 로딩 시작");
            
            var configManager = ConfigManager.Instance;
            if (configManager == null)
            {
                Debug.LogError($"{LOG_PREFIX}ConfigManager가 null입니다.");
            }
            
            if (configManager != null && configManager.GameConfig != null)
            {
                gameConfig = configManager.GameConfig;
                Debug.Log($"{LOG_PREFIX}GameConfig 로딩 성공 - enableStageClearDuplicatePrevention: {gameConfig.enableStageClearDuplicatePrevention}");
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX}ConfigManager 또는 GameConfig를 찾을 수 없습니다. 기본값을 사용합니다.");
                // 기본값으로 폴백
                gameConfig = ScriptableObject.CreateInstance<GameConfigSO>();
                Debug.Log($"{LOG_PREFIX}기본 GameConfig 생성 완료");
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

            uiCoordinator = UICoordinator.Instance;
            if (uiCoordinator == null)
            {
                Debug.LogError($"{LOG_PREFIX}UICoordinator 초기화에 실패했습니다.");
            }

            cardManagerReference = FindObjectOfType<CardManager>();
            if (cardManagerReference == null)
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

        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            CleanupEventListeners();
            base.OnDestroy();
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
            stageManagerReference = FindObjectOfType<StageManager>();
            bottomBarPanelReference = FindObjectOfType<BottomBarPanel>(true); // 비활성화된 객체도 포함
            playerReference = FindObjectOfType<Player>();
            topBarPanelReference = FindObjectOfType<TopBarPanel>(true); // 비활성화된 객체도 포함

            #if UNITY_EDITOR
            // Game 씬에서만 이 컴포넌트들이 필요하므로, Game 씬이 아닌 경우 경고하지 않음
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentSceneName == gameConfig.gameSceneName)
            {
                if (stageManagerReference == null) 
                    Debug.LogWarning($"{LOG_PREFIX}{string.Format(LOG_MESSAGES[11], "StageManager")}");
                if (bottomBarPanelReference == null) 
                    Debug.LogWarning($"{LOG_PREFIX}{string.Format(LOG_MESSAGES[11], "BottomBarPanel")}");
                if (playerReference == null) 
                    Debug.LogWarning($"{LOG_PREFIX}{string.Format(LOG_MESSAGES[11], "Player")}");
                if (topBarPanelReference == null) 
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

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
        }

        private void Start()
        {
            Debug.Log($"{LOG_PREFIX}GameManager Start() 호출됨!");
            Debug.Log($"{LOG_PREFIX}현재 씬: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            Debug.Log($"{LOG_PREFIX}GameObject 이름: {gameObject.name}");
            
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

        public int GetCurrentEData()
        {
            return saveDataManager?.GetCurrentEData() ?? 0;
        }

        /// <summary>
        /// 총 스테이지 수를 반환합니다.
        /// StageManager를 통해 동적으로 가져옵니다.
        /// </summary>
        /// <returns>총 스테이지 수</returns>
        public int GetTotalStageCount()
        {
            var stageManager = StageManager.Instance;
            return stageManager?.GetStageCount() ?? 1; // 기본값 1
        }

        public void UpdateStageWaveUI(int currentStage, int spawnedMonsters, int maxMonsters)
        {
            // UICoordinator 참조 확인 및 재참조
            if (uiCoordinator == null)
            {
                uiCoordinator = UICoordinator.Instance;
                if (uiCoordinator == null)
                {
                    uiCoordinator = FindObjectOfType<UICoordinator>();
                    if (uiCoordinator == null)
                    {
                        #if UNITY_EDITOR
                        Debug.LogWarning($"{LOG_PREFIX}UICoordinator를 찾을 수 없습니다. Stage Wave UI 업데이트를 건너뜁니다.");
                        #endif
                        return;
                    }
                }
            }
            
            uiCoordinator.UpdateStageWaveUI(currentStage, spawnedMonsters, maxMonsters, GetTotalStageCount());
        }

        public void InitializeEDataDisplay()
        {
            UpdateEDataUI();
        }

        /// <summary>
        /// TopBarPanel의 초기 데이터를 설정합니다.
        /// </summary>
        private void InitializeTopBarDisplay()
        {
            // UICoordinator 참조 확인 및 재참조
            if (uiCoordinator == null)
            {
                uiCoordinator = UICoordinator.Instance;
                if (uiCoordinator == null)
                {
                    uiCoordinator = FindObjectOfType<UICoordinator>();
                    if (uiCoordinator == null)
                    {
                        #if UNITY_EDITOR
                        Debug.LogError($"{LOG_PREFIX}UICoordinator를 찾을 수 없습니다. TopBarPanel 초기화를 건너뜁니다.");
                        #endif
                        return;
                    }
                }
            }
            
            // 초기 스테이지 정보 설정
            int currentStage = requestedStartStage + 1; // 0-based to 1-based
            int totalStages = GetTotalStageCount();
            
            // StageManager에서 초기 웨이브 정보 가져오기
            var stageManager = StageManager.Instance;
            int spawnedMonsters = 0;
            int maxMonsters = 0;
            
            if (stageManager != null)
            {
                maxMonsters = stageManager.GetStageWaveCount(requestedStartStage);
            }
            
            // TopBarPanel 업데이트
            uiCoordinator.UpdateStageWaveUI(currentStage, spawnedMonsters, maxMonsters, totalStages);
            
            // 초기 EData 설정
            var resourceManager = ResourceManager.Instance;
            if (resourceManager != null)
            {
                int currentEData = resourceManager.GetCurrentEData();
                uiCoordinator.UpdateEDataUI(currentEData);
                
                #if UNITY_EDITOR
                Debug.Log($"{LOG_PREFIX}TopBarPanel 초기 데이터 설정 완료 - 스테이지: {currentStage}/{totalStages}, 웨이브: {spawnedMonsters}/{maxMonsters}, eData: {currentEData}");
                #endif
            }
        }

        private void OnEDataChanged(int newEDataAmount)
        {
            // #if UNITY_EDITOR
            // Debug.Log(LOG_PREFIX + $"EData changed: {newEDataAmount}");
            // #endif
            
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
            // UICoordinator 참조 확인 및 재참조
            if (uiCoordinator == null)
            {
                uiCoordinator = UICoordinator.Instance;
                if (uiCoordinator == null)
                {
                    uiCoordinator = FindObjectOfType<UICoordinator>();
                    if (uiCoordinator == null)
                    {
                        #if UNITY_EDITOR
                        Debug.LogWarning($"{LOG_PREFIX}UICoordinator를 찾을 수 없습니다. EData UI 업데이트를 건너뜁니다.");
                        #endif
                        return;
                    }
                }
            }
            
            uiCoordinator.UpdateEDataUI(currentEData);
        }

        public void StageCleared(int stageNum)
        {
            if (saveDataManager != null)
            {
                // 스테이지 클리어 시 진행상황 저장
                saveDataManager.UpdateStageProgress(stageNum, true);
                
                var currentData = saveDataManager.CurrentSaveData;
                if (currentData != null)
                {
                    // #if UNITY_EDITOR
                    // Debug.Log($"{LOG_PREFIX}스테이지 {stageNum} 클리어됨! 최고 클리어 스테이지: {currentData.progressData.highestStageCleared}");
                    // #endif
                }
            }
        }

        private void Update()
        {
            // ESC 키 입력 감지 (모든 상태에서 체크)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log($"{LOG_PREFIX}ESC 키 눌림! 현재 상태: {CurrentGameState}");
                
                if (CurrentGameState == GameState.Playing)
                {
                    Debug.Log($"{LOG_PREFIX}게임 중 ESC - PauseGame 호출");
                    PauseGame(true);
                }
                else if (CurrentGameState == GameState.Paused)
                {
                    Debug.Log($"{LOG_PREFIX}일시정지 중 ESC - ResumeGame 호출");
                    ResumeGame();
                }
                else
                {
                    Debug.Log($"{LOG_PREFIX}현재 상태({CurrentGameState})에서는 ESC 무시");
                }
            }
            
            if (CurrentGameState != GameState.Playing) return;

            // 상태 체크 주기 최적화
            if (Time.time >= nextStateCheckTime)
            {
                CheckStageCompletion();
                nextStateCheckTime = Time.time + gameConfig.stateCheckInterval;
            }
        }

        private void CheckStageCompletion()
        {
            if (stageManagerReference == null) return;

            bool allEnemiesSpawned = AllEnemiesSpawned();
            int activeEnemyCount = stageManagerReference.ActiveEnemyCount;
            
            if (allEnemiesSpawned && activeEnemyCount == 0)
            {
                if (!stageClearedProcessed)
                {
                    // #if UNITY_EDITOR
                    // Debug.Log(LOG_PREFIX + $"스테이지 클리어 조건 만족 - 모든 적 스폰됨: {allEnemiesSpawned}, 활성 적 수: {activeEnemyCount}");
                    // #endif
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
            if (stageManagerReference == null) return false;
            
            // 메모리 할당 최적화 - 메서드 호출 최소화하고 변수로 캐시
            int currentStageIndex = stageManagerReference.GetCurrentStageIndex();
            int spawnedCount = stageManagerReference.GetSpawnedEnemyCount();
            int maxCount = stageManagerReference.GetStageWaveCount(currentStageIndex);
            
            return spawnedCount >= maxCount;
        }

        private void HandleStageCleared()
        {
            if (stageManagerReference == null) 
            {
                LogManager.Error("GameManager", "stageManagerReference가 null입니다!");
                return;
            }

            // 스테이지 클리어 처리
            int clearedStageIndex = stageManagerReference.GetCurrentStageIndex(); // 0-based 인덱스
            
            // 방어 코드: 잘못된 스테이지 인덱스 체크
            if (clearedStageIndex < 0)
            {
                LogManager.Error("GameManager", $"잘못된 스테이지 인덱스: {clearedStageIndex}. 스테이지 클리어 처리를 중단합니다.");
                return;
            }
            
            int stageNumber = clearedStageIndex + 1; // 1-based 스테이지 번호
            
            // 스테이지 클리어 시 축적된 EData와 스테이지 진행을 한 번에 저장
            if (saveDataManager != null)
            {
                // SaveDataManager는 1-based 스테이지 번호를 기대함
                saveDataManager.UpdateStageProgress(stageNumber);
            }
            else
            {
                LogManager.Error("GameManager", "SaveDataManager를 찾을 수 없어 스테이지 진행을 저장할 수 없습니다!");
            }
            
            // 스테이지 클리어 이벤트 호출 (1-based 스테이지 번호로)
            StageCleared(stageNumber);
            OnStageClearedEvent?.Invoke();
            
            // 모든 스테이지 완료 체크 (0-based 인덱스로)
            CheckAllStagesCompleted(clearedStageIndex);
        }

        // 모든 스테이지 완료 시 일시정지 패널 활성화
        private void CheckAllStagesCompleted(int clearedStageIndex)
        {
            if (stageManagerReference == null) return;
            
            int totalStages = stageManagerReference.GetStageCount();
            bool allStagesCompleted = (clearedStageIndex + 1) >= totalStages;
            
            if (allStagesCompleted)
            {
                Debug.Log(LOG_PREFIX + "모든 스테이지 완료! PausePanel을 표시합니다.");
                
                // 게임 일시정지 및 PausePanel 표시
                PauseGame(true);
                
                // 3초 후 자동으로 메인 메뉴로 돌아가는 옵션을 제공하는 대신
                // 사용자가 직접 선택할 수 있도록 PausePanel만 표시
            }
        }
        
        private System.Collections.IEnumerator ReturnToMainMenuAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            LoadMainMenuScene();
        }

        private void InitializeGame()
        {
            // UI 패널 캐싱 및 등록
            CacheAndRegisterAllPanels();
            
            // 게임플레이 패널 설정
            SetupGameplayPanels();
            
            // TopBarPanel 초기 데이터 업데이트
            InitializeTopBarDisplay();
            
            // StageManager 참조 찾기 및 스테이지 시작
            var stageManager = StageManager.Instance;
            if (stageManager != null)
            {
                // Continue Game 시 저장된 스테이지 정보를 기반으로 시작
                stageManager.StartStageFrom(requestedStartStage);
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + $"StageManager를 통해 스테이지 {requestedStartStage + 1}부터 시작했습니다. (인덱스: {requestedStartStage})");
                #endif
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + "StageManager를 찾을 수 없습니다.");
                #endif
            }
            
            // 게임 상태를 Playing으로 설정
            SetGameState(GameState.Playing);
            
            // #if UNITY_EDITOR
            // Debug.Log(LOG_PREFIX + "게임 초기화 완료. 게임 상태를 Playing으로 설정했습니다.");
            // #endif
        }

        // 성능 최적화: FindObjectOfType 호출을 한 번에 처리
        private void CacheAndRegisterAllPanels()
        {
            // 모든 BasePanel을 한 번에 찾아서 처리
            var allPanels = FindObjectsOfType<BasePanel>(true);
            
            // Debug.Log($"{LOG_PREFIX}찾은 패널 수: {allPanels.Length}");
            // foreach (var panel in allPanels)
            // {
            //     Debug.Log($"{LOG_PREFIX}찾은 패널: {panel.GetType().Name} - {panel.gameObject.name}");
            // }
            
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
                // Debug.Log($"{LOG_PREFIX}패널 등록 성공: {panelName} ({typeof(T).Name})");
            }
            else
            {
                // Debug.LogWarning($"{LOG_PREFIX}패널을 찾을 수 없습니다: {panelName} ({typeof(T).Name})");
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
            Debug.Log($"{LOG_PREFIX}게임 일시정지 요청 - showPauseUI: {showPauseUI}");
            
            Time.timeScale = 0f;
            CurrentGameState = GameState.Paused;
            
            if (showPauseUI)
            {
                if (uiManager != null)
                {
                    Debug.Log($"{LOG_PREFIX}UIManager 확인됨. 등록된 패널 수: {uiManager.panels?.Count ?? 0}");
                    if (uiManager.panels != null)
                    {
                        foreach (var panel in uiManager.panels)
                        {
                            Debug.Log($"{LOG_PREFIX}등록된 패널: {panel.Key} - {(panel.Value != null ? "존재" : "null")}");
                        }
                    }
                    
                    if (uiManager.IsPanelRegistered("Pause"))
                    {
                        uiManager.ShowPanel("Pause");
                        Debug.Log($"{LOG_PREFIX}Pause 패널을 표시했습니다.");
                    }
                    else
                    {
                        Debug.LogWarning($"{LOG_PREFIX}Pause 패널이 UIManager에 등록되지 않았습니다.");
                        // 수동으로 Pause 패널 찾기 시도
                        var pausePanel = FindObjectOfType<InvaderInsider.UI.PausePanel>(true);
                        if (pausePanel != null)
                        {
                            Debug.Log($"{LOG_PREFIX}Pause 패널을 수동으로 찾았습니다: {pausePanel.gameObject.name}");
                            pausePanel.gameObject.SetActive(true);
                            pausePanel.Show();
                        }
                        else
                        {
                            Debug.LogError($"{LOG_PREFIX}Pause 패널을 찾을 수 없습니다!");
                        }
                    }
                }
                else
                {
                    Debug.LogError($"{LOG_PREFIX}UIManager가 없습니다.");
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
            CurrentGameState = GameState.GameOver;
            
            // 게임 종료 로직 (일반적인 종료)
            Debug.Log(LOG_PREFIX + "게임이 종료되었습니다.");
        }

        public void GameOver()
        {
            Debug.Log(LOG_PREFIX + "게임 오버! PausePanel을 표시합니다.");
            
            Time.timeScale = 0f;
            CurrentGameState = GameState.GameOver;
            
            // PausePanel을 표시하여 사용자가 선택할 수 있도록 함
            if (uiManager != null)
            {
                if (uiManager.IsPanelRegistered("Pause"))
                {
                    uiManager.ShowPanel("Pause");
                    Debug.Log(LOG_PREFIX + "게임 오버 시 Pause 패널을 표시했습니다.");
                }
                else
                {
                    Debug.LogWarning(LOG_PREFIX + "Pause 패널이 등록되지 않았습니다. 메인 메뉴로 이동합니다.");
                    LoadMainMenuScene();
                }
            }
            else
            {
                Debug.LogError(LOG_PREFIX + "UIManager가 없습니다. 메인 메뉴로 이동합니다.");
                LoadMainMenuScene();
            }
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
            UnityEngine.Debug.Log("=== FORCE LOG: StartContinueGame 호출됨! ===");
            Debug.Log(LOG_PREFIX + "=== StartContinueGame 호출됨! ===");
            
            // 이미 게임 시작 중이거나 씬 로딩 중이면 무시
            if (isStartingGame || isLoadingScene)
            {
                UnityEngine.Debug.Log("=== FORCE LOG: StartContinueGame 무시됨 - 이미 진행 중 ===");
                Debug.Log(LOG_PREFIX + "StartContinueGame 무시됨 - 이미 진행 중");
                return;
            }
            
            isStartingGame = true;
            
            UnityEngine.Debug.Log("=== FORCE LOG: Continue 게임 시작 시도 ===");
            Debug.Log(LOG_PREFIX + "Continue 게임 시작 시도");
            
            if (saveDataManager != null)
            {
                Debug.Log(LOG_PREFIX + "SaveDataManager 확인됨, HasSaveData 체크 중...");
                
                if (saveDataManager.HasSaveData())
                {
                    saveDataManager.LoadGameData();
                    var saveData = saveDataManager.CurrentSaveData;
                    if (saveData != null)
                    {
                        // Continue는 클리어한 다음 스테이지부터 시작
                        int highestCleared = saveData.progressData.highestStageCleared;
                        
                        Debug.Log(LOG_PREFIX + $"Continue 게임 시작 - 최고 클리어 스테이지: {highestCleared}, EData: {saveData.progressData.currentEData}");
                        
                        // 스테이지 결정 로직 - StageData를 Resources에서 로드하여 총 스테이지 수 확인
                        int totalStages = 1; // 기본값
                        
                        // 먼저 StageList를 시도 (여러 스테이지용)
                        var stageList = Resources.Load<StageList>("StageList1");
                        if (stageList != null)
                        {
                            totalStages = stageList.StageCount;
                            Debug.Log(LOG_PREFIX + $"[Continue Debug] StageList에서 총 스테이지 수 로드: {totalStages}");
                        }
                        else
                        {
                            // StageList가 없으면 StageDBObject를 시도 (단일 스테이지용)
                            var stageData = Resources.Load<StageDBObject>("Stage1 Database");
                            if (stageData != null)
                            {
                                totalStages = stageData.StageCount;
                                Debug.Log(LOG_PREFIX + $"[Continue Debug] StageDBObject에서 총 스테이지 수 로드: {totalStages}");
                            }
                            else
                            {
                                Debug.LogWarning(LOG_PREFIX + "[Continue Debug] StageData를 로드할 수 없어 기본값 1 사용");
                            }
                        }
                        
                        Debug.Log(LOG_PREFIX + $"[Continue Debug] 총 스테이지 수 (하드코딩): {totalStages}");
                        
                        int startStage;
                        if (highestCleared <= 0)
                        {
                            // 한 번도 클리어하지 않았다면 1스테이지부터
                            startStage = 1;
                            Debug.Log(LOG_PREFIX + $"[Continue Debug] 조건1: 한 번도 클리어 안함 → 1스테이지부터 시작");
                        }
                        else if (highestCleared >= totalStages)
                        {
                            // 모든 스테이지를 클리어했다면 마지막 스테이지부터 재시작
                            startStage = totalStages;
                            Debug.Log(LOG_PREFIX + $"[Continue Debug] 조건2: 모든 스테이지 클리어됨 ({highestCleared} >= {totalStages}) → 마지막 스테이지({totalStages})부터 재시작");
                        }
                        else
                        {
                            // 클리어한 다음 스테이지부터 시작
                            startStage = highestCleared + 1;
                            Debug.Log(LOG_PREFIX + $"[Continue Debug] 조건3: 다음 스테이지부터 시작 ({highestCleared} + 1 = {startStage})");
                        }
                        
                        UnityEngine.Debug.Log($"=== FORCE LOG: Continue - 최고 클리어: {highestCleared}, 총 스테이지: {totalStages}, 시작할 스테이지: {startStage} (인덱스: {startStage - 1}) ===");
                        Debug.Log(LOG_PREFIX + $"Continue - 최고 클리어: {highestCleared}, 총 스테이지: {totalStages}, 시작할 스테이지: {startStage} (인덱스: {startStage - 1})");
                        
                        // GameManager에 시작할 스테이지 설정 (인덱스는 0부터 시작하므로 startStage - 1)
                        UnityEngine.Debug.Log($"=== FORCE LOG: SetRequestedStartStage({startStage - 1}) 호출 ===");
                        SetRequestedStartStage(startStage - 1);
                        
                        // 게임 씬으로 전환
                        LoadGameScene();
                    }
                    else
                    {
                        #if UNITY_EDITOR
                        Debug.LogError(LOG_PREFIX + "Continue 실패 - SaveData가 null");
                        #endif
                        isStartingGame = false;
                    }
                }
                else
                {
                    #if UNITY_EDITOR
                    Debug.LogWarning(LOG_PREFIX + "Continue 실패 - HasSaveData가 false 반환");
                    #endif
                    isStartingGame = false;
                }
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + "Continue 실패 - SaveDataManager가 null");
                #endif
                isStartingGame = false;
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
            
            // 씬 전환 전 싱글톤 정리
            CleanupSingletonsForSceneChange();
            
            // 상태 플래그 리셋
            isStartingGame = false;
            isLoadingScene = false;
            
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
            
            // 씬 로드 후 MainMenuPanel의 Continue 버튼 갱신
            StartCoroutine(RefreshMainMenuAfterLoad());
        }
        
        private System.Collections.IEnumerator RefreshMainMenuAfterLoad()
        {
            // 씬 로드 완료까지 대기
            yield return new WaitForEndOfFrame();
            yield return null;
            
            Debug.Log("[FORCE LOG] RefreshMainMenuAfterLoad 시작");
            
            // SaveDataManager 강제 재로드
            var saveDataManager = SaveDataManager.Instance;
            if (saveDataManager != null)
            {
                Debug.Log("[FORCE LOG] SaveDataManager 발견됨, 데이터 강제 재로드");
                saveDataManager.LoadGameData();
                Debug.Log("[FORCE LOG] SaveDataManager 데이터 재로드 완료");
            }
            else
            {
                Debug.LogWarning("[FORCE LOG] SaveDataManager를 찾을 수 없음");
            }
            
            // MainMenuPanel 찾기 및 Continue 버튼 갱신
            var mainMenuPanel = FindObjectOfType<InvaderInsider.UI.MainMenuPanel>();
            if (mainMenuPanel != null)
            {
                Debug.Log("[FORCE LOG] MainMenuPanel 발견됨, Continue 버튼 갱신 시작");
                mainMenuPanel.RefreshContinueButton();
                Debug.Log("[FORCE LOG] Continue 버튼 갱신 완료");
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "Main 씬 로드 후 Continue 버튼 갱신 완료");
                #endif
            }
            else
            {
                Debug.LogWarning("[FORCE LOG] MainMenuPanel을 찾을 수 없음");
                #if UNITY_EDITOR
                Debug.LogWarning(LOG_PREFIX + "MainMenuPanel을 찾을 수 없어 Continue 버튼을 갱신할 수 없습니다.");
                #endif
            }
            
            Debug.Log("[FORCE LOG] RefreshMainMenuAfterLoad 완료");
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
            // 씬 전환 전 싱글톤 정리
            CleanupSingletonsForSceneChange();
            
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

        /// <summary>
        /// 에러 복구 처리 - ExceptionHandler에서 호출
        /// </summary>
        public void HandleErrorRecovery()
        {
            DebugUtils.Log(GameConstants.LOG_PREFIX_GAME, "GameManager 에러 복구 시작");

            try
            {
                // 1. 게임 상태 체크
                if (CurrentGameState == GameState.Playing)
                {
                    // 게임이 진행 중이었다면 일시 정지
                    PauseGame();
                }

                // 2. UI 상태 복구
                if (uiManager != null)
                {
                    uiManager.RestoreUIState();
                }

                // 3. 카드 시스템 상태 점검
                if (cardManagerReference != null)
                {
                    // 기본 상태 점검 (추후 IsCardSystemHealthy 메서드 구현 시 대체)
                    DebugUtils.Log(GameConstants.LOG_PREFIX_GAME, "CardManager 상태 확인");
                }

                // 4. 스테이지 시스템 상태 점검
                if (stageManagerReference != null)
                {
                    // 기본 상태 점검 (추후 IsSystemHealthy 메서드 구현 시 대체)
                    DebugUtils.Log(GameConstants.LOG_PREFIX_GAME, "StageManager 상태 확인");
                }

                // 5. 게임 재개 (안전한 상태라면)
                if (CurrentGameState == GameState.Paused)
                {
                    ResumeGame();
                }

                DebugUtils.Log(GameConstants.LOG_PREFIX_GAME, "GameManager 에러 복구 완료");
            }
            catch (Exception e)
            {
                DebugUtils.LogError(GameConstants.LOG_PREFIX_GAME, 
                    $"GameManager 에러 복구 중 예외 발생: {e.Message}");
                
                // 복구 실패 시 긴급 리셋 시도
                EmergencyReset();
            }
        }

        /// <summary>
        /// 긴급 리셋 - 치명적 오류 발생 시 호출
        /// </summary>
        public void EmergencyReset()
        {
            DebugUtils.LogWarning(GameConstants.LOG_PREFIX_GAME, "긴급 리셋 실행");

            try
            {
                // 1. 모든 시스템 정지
                Time.timeScale = 0f;
                
                // 2. 핵심 시스템 리셋
                ResetCoreSystem();
                
                // 3. 메모리 정리
                Resources.UnloadUnusedAssets();
                System.GC.Collect();
                
                // 4. 게임 시간 복원
                Time.timeScale = 1f;
                
                // 5. 새 게임 시작
                StartNewGame();
                
                DebugUtils.Log(GameConstants.LOG_PREFIX_GAME, "긴급 리셋 완료");
            }
            catch (Exception e)
            {
                DebugUtils.LogError(GameConstants.LOG_PREFIX_GAME, 
                    $"긴급 리셋 실패: {e.Message}");
                
                // 마지막 수단: 씬 재로드
                LoadMainMenuScene();
            }
        }

        /// <summary>
        /// 씬 전환 시 싱글톤 정리
        /// </summary>
        private void CleanupSingletonsForSceneChange()
        {
            DebugUtils.LogInfo(GameConstants.LOG_PREFIX_GAME, "씬 전환을 위한 싱글톤 정리 시작");

            try
            {
                // 중요하지 않은 싱글톤들만 정리 (GameManager, SaveDataManager는 유지)
                ConfigManager.PrepareForSceneChange();
                ObjectPoolManager.PrepareForSceneChange();
                
                // 메모리 정리
                Resources.UnloadUnusedAssets();
                
                DebugUtils.LogInfo(GameConstants.LOG_PREFIX_GAME, "싱글톤 정리 완료");
            }
            catch (System.Exception e)
            {
                DebugUtils.LogError(GameConstants.LOG_PREFIX_GAME, 
                    $"싱글톤 정리 중 오류 발생: {e.Message}");
            }
        }

        /// <summary>
        /// 핵심 시스템 리셋
        /// </summary>
        private void ResetCoreSystem()
        {
            // 게임 상태 초기화
            SetGameState(GameState.MainMenu);
            
            // 플래그 리셋
            isStartingGame = false;
            isLoadingScene = false;
            stageClearedProcessed = false;
            
            // 스테이지 시스템 리셋
            if (stageManagerReference != null)
            {
                // 기본 리셋 (추후 ResetToFirstStage 메서드 구현 시 대체)
                DebugUtils.Log(GameConstants.LOG_PREFIX_GAME, "StageManager 리셋");
            }
            
            // 카드 시스템 리셋
            if (cardManagerReference != null)
            {
                // 기본 리셋 (추후 ResetCardSystem 메서드 구현 시 대체)
                DebugUtils.Log(GameConstants.LOG_PREFIX_GAME, "CardManager 리셋");
            }
            
            // UI 시스템 리셋
            if (uiManager != null)
            {
                uiManager.ResetUISystem();
            }
            
            // 오브젝트 풀 정리
            var poolManager = ObjectPoolManager.Instance;
            poolManager?.ClearAllPools();
            
            // 리소스 매니저 리셋
            var resourceManager = ResourceManager.Instance;
            if (resourceManager != null)
            {
                // 기본 리셋 (추후 ResetResources 메서드 구현 시 대체)
                DebugUtils.Log(GameConstants.LOG_PREFIX_GAME, "ResourceManager 리셋");
            }
        }
        
        #endregion
    }
} 