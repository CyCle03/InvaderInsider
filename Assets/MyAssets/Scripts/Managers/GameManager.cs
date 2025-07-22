using UnityEngine;
using System;
using InvaderInsider.Data;
using UnityEngine.SceneManagement;
using InvaderInsider.Cards;
using InvaderInsider.UI; // UIManager 네임스페이스 추가

namespace InvaderInsider.Managers
{
    public enum GameState
    {
        None,
        MainMenu,
        Loading,
        Playing,
        Paused,
        GameOver
    }

    public class GameManager : MonoBehaviour
    {
        private const string LOG_PREFIX = "[GameManager] ";
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        public GameState CurrentGameState { get; private set; }
        public event Action<GameState> OnGameStateChanged;

        private UIManager uiManager; // UIManager 참조 추가

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded; // 씬 로드 이벤트 구독
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded; // 이벤트 구독 해제
        }

        private void Start()
        {
            SetGameState(GameState.MainMenu);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // UIManager 인스턴스를 직접 찾습니다.
            uiManager = FindObjectOfType<UIManager>();
            if (uiManager == null)
            {
                Debug.LogError($"{LOG_PREFIX}UIManager를 찾을 수 없습니다.");
                return;
            }

            InitializeUIForScene(scene.name); // 씬에 맞는 UI 초기화
        }

        private void InitializeUIForScene(string sceneName)
        {
            // 모든 BasePanel 컴포넌트를 찾습니다.
            BasePanel[] allPanels = FindObjectsOfType<BasePanel>(true);

            // UIManager에 패널들을 등록하고 초기 상태를 설정합니다.
            foreach (BasePanel panel in allPanels)
            {
                if (panel != null)
                {
                    uiManager.RegisterPanel(panel.name, panel); // 패널의 GameObject 이름을 키로 사용
                }
            }

            // 모든 패널을 비활성화합니다.
            foreach (BasePanel panel in allPanels)
            {
                if (panel != null)
                {
                    panel.gameObject.SetActive(false);
                }
            }

            // 씬에 따라 필요한 패널만 활성화합니다.
            if (sceneName == "Main")
            {
                uiManager.ShowPanel("MainMenu");
            }
            else if (sceneName == "Game")
            {
                uiManager.ShowPanelConcurrent("TopBar");
                uiManager.ShowPanelConcurrent("BottomBar");
                uiManager.ShowPanelConcurrent("InGame");
            }
        }

        public void SetGameState(GameState newState)
        {
            if (CurrentGameState == newState) return;
            CurrentGameState = newState;
            OnGameStateChanged?.Invoke(newState);
            Debug.Log($"{LOG_PREFIX}Game state changed to: {newState}");
        }

        public void StartNewGame()
        {
            SaveDataManager.Instance?.ResetGameData();
            StartGame(0);
        }

        public void StartContinueGame()
        {
            var saveData = SaveDataManager.Instance?.CurrentSaveData;
            int startStage = 0;
            if (saveData != null)
            {
                startStage = saveData.progressData.highestStageCleared;
            }
            StartGame(startStage);
        }

        public void StartGame(int startStageIndex)
        {
            SetGameState(GameState.Loading);
            SceneManager.LoadSceneAsync("Game").completed += (asyncOperation) =>
            {
                var stageManager = FindObjectOfType<StageManager>();
                if (stageManager != null)
                {
                    stageManager.StartStageFrom(startStageIndex);
                    SetGameState(GameState.Playing);
                    // 스테이지 시작 후 UI를 즉시 업데이트
                    UpdateStageWaveUI(stageManager.GetCurrentStageIndex() + 1, stageManager.GetSpawnedEnemyCount(), stageManager.GetStageWaveCount(stageManager.GetCurrentStageIndex()));
                }
                else
                {
                    Debug.LogError($"{LOG_PREFIX}StageManager를 찾을 수 없습니다.");
                }
            };
        }

        public void PauseGame(bool showPauseUI = true)
        {
            if (CurrentGameState != GameState.Playing) return;
            Time.timeScale = 0f;
            SetGameState(GameState.Paused);
            if (showPauseUI) uiManager?.ShowPanelConcurrent("Pause");
        }

        public void ResumeGame()
        {
            if (CurrentGameState != GameState.Paused) return;
            Time.timeScale = 1f;
            SetGameState(GameState.Playing);
            uiManager?.HidePanel("Pause");
        }

        public void GameOver()
        {
            Time.timeScale = 0f;
            SetGameState(GameState.GameOver);
            Debug.Log($"{LOG_PREFIX} 게임 오버");
            // 게임 오버 UI 표시 로직 추가
        }

        public void StageCleared(int clearedStageNumber)
        {
            Debug.Log($"{LOG_PREFIX}스테이지 {clearedStageNumber} 클리어! 데이터 저장을 시작합니다.");
            CardManager.Instance?.SaveCards();
            SaveDataManager.Instance?.UpdateStageProgress(clearedStageNumber, true);
        }

        public void LoadMainMenuScene()
        {
            Time.timeScale = 1f;
            SetGameState(GameState.Loading);
            SceneManager.LoadScene("Main");
            SetGameState(GameState.MainMenu);
        }

        public void UpdateStageWaveUI(int stage, int currentWave, int maxWave)
        {
            // UIManager를 통해 TopBarPanel의 UI를 업데이트하는 예시
            // TopBarPanel의 GameObject 이름이 "TopBarPanel"이라고 가정
            var topBarPanel = uiManager?.GetPanel("TopBar") as TopBarPanel;
            if (topBarPanel != null)
            {
                topBarPanel.UpdateStageWaveUI(stage, currentWave, maxWave);
            }
        }

        public void AddEData(int amount, bool saveImmediately)
        {
            SaveDataManager.Instance?.UpdateEData(amount, saveImmediately);
        }

        public void UpdateEDataUI(int amount)
        {
            var topBarPanel = uiManager?.GetPanel("TopBar") as TopBarPanel;
            if (topBarPanel != null)
            {
                topBarPanel.UpdateEData(amount);
            }
        }
    }
}