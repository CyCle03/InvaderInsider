using UnityEngine;
using System;
using InvaderInsider.Data;

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

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                LoadGameData();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            LoadGameData();
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
    }
} 