using UnityEngine;
using System;
using InvaderInsider.Data;
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

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            SetGameState(GameState.MainMenu);
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
            // UI 표시 로직은 각 UI Panel에서 OnGameStateChanged 이벤트를 받아 처리
        }

        public void ResumeGame()
        {
            if (CurrentGameState != GameState.Paused) return;
            Time.timeScale = 1f;
            SetGameState(GameState.Playing);
        }

        public void GameOver()
        {
            Time.timeScale = 0f;
            SetGameState(GameState.GameOver);
            Debug.Log($"{LOG_PREFIX} 게임 오버");
            // GameOver UI 표시 로직 필요
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
            // TopBarPanel 같은 UI가 이 정보를 표시하도록 이벤트를 만들거나 직접 참조하여 업데이트 할 수 있습니다.
            // 예: UIManager.Instance.UpdateWaveInfo(stage, currentWave, maxWave);
        }

        public void AddEData(int amount, bool saveImmediately)
        {
            SaveDataManager.Instance?.UpdateEData(amount, saveImmediately);
        }
    }
}