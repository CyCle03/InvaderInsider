using UnityEngine;
using System;
using InvaderInsider.Data;
using InvaderInsider.UI;

namespace InvaderInsider.Managers
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        Settings
    }

    public class GameManager : MonoBehaviour
    {
        private static GameManager instance;
        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<GameManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        instance = go.AddComponent<GameManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        [Header("Game State")]
        private GameState currentGameState = GameState.MainMenu;
        public GameState CurrentGameState
        {
            get => currentGameState;
            set
            {
                currentGameState = value;
                OnGameStateChanged?.Invoke(value);
            }
        }

        public event Action<GameState> OnGameStateChanged;

        // 캐싱할 매니저 및 플레이어 참조
        private StageManager cachedStageManager;
        private BottomBarPanel cachedBottomBarPanel;
        private Player cachedPlayer;

        private void OnEnable()
        {
            // SaveDataManager의 데이터 로드 완료 이벤트 구독
            if (SaveDataManager.Instance != null)
            {
                SaveDataManager.Instance.OnGameDataLoaded += HandleGameDataLoaded;
            }
        }

        private void OnDisable()
        {
            // SaveDataManager의 데이터 로드 완료 이벤트 구독 해지
            if (SaveDataManager.Instance != null)
            {
                SaveDataManager.Instance.OnGameDataLoaded -= HandleGameDataLoaded;
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            // 필요한 참조들을 Awake에서 캐싱
            cachedStageManager = FindObjectOfType<StageManager>();
            cachedBottomBarPanel = FindObjectOfType<BottomBarPanel>();
            cachedPlayer = FindObjectOfType<Player>();

            if (cachedStageManager == null) Debug.LogWarning("StageManager not found in the scene.");
            if (cachedBottomBarPanel == null) Debug.LogWarning("BottomBarPanel not found in the scene.");
            if (cachedPlayer == null) Debug.LogWarning("Player not found in the scene.");
        }

        void Start()
        {
        }

        private void LoadGameData()
        {
            SaveDataManager.Instance.LoadGameData();
        }

        public bool TrySpendEData(int amount)
        {
            var currentEData = SaveDataManager.Instance.GetCurrentEData();
            if (currentEData >= amount)
            {
                SaveDataManager.Instance.UpdateEData(-amount);
                return true;
            }
            return false;
        }

        public void AddEData(int amount)
        {
            SaveDataManager.Instance.UpdateEData(amount);
        }

        public void StageCleared(int stageNum, int stars)
        {
            SaveDataManager.Instance.UpdateStageProgress(stageNum, stars);
            SaveDataManager.Instance.SaveGameData();
        }

        public void AddResourcePointsListener(Action<int> listener)
        {
            SaveDataManager.Instance.OnEDataChanged += listener;
            // 현재 값으로 즉시 호출
            listener?.Invoke(SaveDataManager.Instance.GetCurrentEData());
        }

        public void RemoveResourcePointsListener(Action<int> listener)
        {
            SaveDataManager.Instance.OnEDataChanged -= listener;
        }

        private void HandleGameDataLoaded()
        {
            Debug.Log("[GameManager] Game data loaded. Updating game state and UI...");

            // SaveDataManager에서 로드된 데이터를 가져옵니다.
            SaveData loadedData = SaveDataManager.Instance.CurrentSaveData;

            // --- 로드된 데이터를 기반으로 게임 상태 및 UI 업데이트 --- //

            // StageManager 인스턴스를 캐싱된 참조로 사용
            if (cachedStageManager != null)
            {
                // UIManager의 UpdateStage 함수 호출. 총 스테이지 수는 StageManager에서 가져옵니다。
                UIManager.Instance.UpdateStage(loadedData.progressData.highestStageCleared, cachedStageManager.GetStageCount());
                
                // 로드된 스테이지에 맞춰 웨이브 관리자 초기화 함수 호출
                cachedStageManager.InitializeStageFromLoadedData(loadedData.progressData.highestStageCleared);

                // StageManager 초기화 완료 후 BottomBarPanel의 적 수 업데이트
                if (cachedBottomBarPanel != null)
                {
                    cachedBottomBarPanel.UpdateMonsterCountDisplay(cachedStageManager.activeEnemyCount);
                }
            } else {
                Debug.LogWarning("StageManager not found. Cannot update Stage/Wave/Enemy Count UI.");
            }

            // 플레이어 체력 업데이트 (Player 스크립트가 체력을 관리하고 Health UI는 BottomBarPanel이 표시)
            if (cachedPlayer != null)
            {
                cachedPlayer.ResetHealth();
            }

            // eData UI 업데이트는 SaveDataManager의 OnEDataChanged 이벤트에 의해 이미 처리됩니다。
            // 여기서는 별도 호출 필요 없습니다.

            Debug.Log("[GameManager] UI update initiated based on loaded data.");
        }
    }
} 