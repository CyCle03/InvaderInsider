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

            // StageManager 인스턴스를 찾습니다.
            StageManager stageManager = FindObjectOfType<StageManager>();

            // 1. Stage UI 업데이트 (Stage 정보를 표시하는 UI 스크립트의 함수를 호출)
            // Stage 정보는 GameManager 또는 StageManager가 관리할 수 있습니다.
            // 로드된 데이터에서 최고 클리어 스테이지 정보를 가져와 현재 스테이지를 설정하거나 UI에 표시합니다。
            int currentStage = loadedData.progressData.highestStageCleared; // 로드된 최고 클리어 스테이지
            // 로드된 최고 클리어 스테이지를 기반으로 Stage UI 업데이트 (UIManager)
            if (UIManager.Instance != null)
            {
                // UIManager의 UpdateStage 함수 호출. 총 스테이지 수는 StageManager에서 가져옵니다。
                // StageManager의 GetStageCount() 함수 사용
                if (stageManager != null)
                {
                    UIManager.Instance.UpdateStage(currentStage, stageManager.GetStageCount()); // currentStage는 0-based 인덱스, GetStageCount()는 총 스테이지 수
                } else {
                    Debug.LogWarning("StageManager not found for Stage UI update.");
                }
            }

            // 2. 웨이브 및 적 수 정보 업데이트 (StageManager가 관리)
            // StageManager 인스턴스를 찾아 로드된 스테이지 정보로 초기화합니다.
            if (stageManager != null)
            {
                // 로드된 스테이지에 맞춰 웨이브 관리자 초기화 함수 호출 (StageManager에 구현 필요)
                // 이 함수 내부에서 해당 스테이지의 첫 웨이브 설정 및 적 수 UI 초기 업데이트를 수행할 것으로 예상
                stageManager.InitializeStageFromLoadedData(currentStage); // StageManager에 구현 필요

                // StageManager 초기화 완료 후 BottomBarPanel의 적 수 업데이트
                // StageManager에서 현재 적 수를 가져와 BottomBarPanel 함수를 호출합니다。
                BottomBarPanel bottomBarPanel = FindObjectOfType<BottomBarPanel>();
                if (bottomBarPanel != null)
                {
                    // BottomBarPanel의 적 수 업데이트 함수 호출. StageManager에서 현재 적 수를 가져옵니다。
                    bottomBarPanel.UpdateMonsterCountDisplay(stageManager.activeEnemyCount); // StageManager의 activeEnemyCount 변수 사용
                }
            } else {
                Debug.LogWarning("StageManager not found. Cannot update Wave and Enemy Count UI.");
            }

            // 3. 플레이어 체력 업데이트 (Player 스크립트가 체력을 관리하고 Health UI는 BottomBarPanel이 표시)
            // 일반적으로 게임 로드 시 플레이어 체력은 최대치로 설정됩니다.
            // Player 스크ript가 체력을 관리하고 있다면 해당 인스턴스를 찾아 체력을 설정합니다.
            Player player = FindObjectOfType<Player>();
            if (player != null)
            {
                // 로드된 데이터에 특정 체력 값이 있다면 사용, 없다면 최대 체력으로 설정
                // 현재 SaveData에는 체력 정보가 없으므로 최대 체력으로 초기화하는 함수 호출
                player.ResetHealth(); // Player 스크립트에 구현됨. 내부적으로 BottomBarPanel 체력 UI 업데이트 호출。

                // 체력 UI 업데이트는 Player 스크립트의 ResetHealth 또는 TakeDamage 함수에서
                // BottomBarPanel을 찾아 직접 업데이트하도록 이미 구현되어 있습니다.
                // 여기서는 Player.ResetHealth() 호출만으로 충분합니다。
            }

            // 4. eData UI 업데이트
            // eData UI 업데이트는 SaveDataManager의 OnEDataChanged 이벤트에 의해 이미 처리됩니다。
            // LoadGameData 함수에서 이미 이벤트를 발생시키고 있으므로 여기서는 별도 호출 필요 없습니다.

            // 5. 추가 UI 요소 업데이트 (필요하다면)
            // 예: Hand UI 업데이트 (SaveDataManager의 OnHandDataChanged 이벤트나 별도 이벤트를 구독하여 처리)
            // 예: Deck UI 업데이트
            // 예: Resource UI 업데이트 (SaveDataManager 또는 별도 리소스 관리자의 이벤트를 구독하여 처리)

            // TODO: 로드된 데이터를 기반으로 게임 씬 상태를 정확히 복원하는 추가 로직 구현
            // (예: 특정 스테이지의 특정 웨이브에서 시작, 남아있던 적 종류 및 수 등) - SaveData에 정보가 있어야 함

            Debug.Log("[GameManager] UI update initiated based on loaded data.");
        }
    }
} 