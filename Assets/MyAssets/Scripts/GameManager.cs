using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using InvaderInsider;
using InvaderInsider.UI;
using InvaderInsider.Data;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

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

    [Header("Game Resources")]
    [Tooltip("Points earned from defeating enemies")]
    public int resourcePoints = 0;

    private event Action<int> OnResourcePointsChanged;

    [Header("UI Elements")]
    public TextMeshProUGUI stageText;
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI resourceText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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
        UpdateStage();
        resourcePoints = SaveDataManager.Instance.CurrentSaveData.progressData.currentEData;
        UpdateResourceDisplay();
    }

    private void LoadGameData()
    {
        SaveDataManager.Instance.LoadGameData();
    }

    public void UpdateStage()
    {
        var stageManager = StageManager.Instance;
        if (stageText != null)
            stageText.text = $"Stage {stageManager.stageNum + 1} / {stageManager.GetStageCount()}";
    }

    public void UpdateWave(int eCnt)
    {
        var stageManager = StageManager.Instance;
        if (waveText != null)
            waveText.text = $"Wave {eCnt} / {stageManager.GetStageWaveCount(stageManager.stageNum)}";
    }

    private void UpdateResourceDisplay()
    {
        if (resourceText != null)
        {
            resourceText.text = $"eData: {resourcePoints}";
        }
    }

    public void UpdateEData(int amount)
    {
        resourcePoints += amount;
        SaveDataManager.Instance.UpdateEData(amount);
        OnResourcePointsChanged?.Invoke(resourcePoints);
        UpdateResourceDisplay();
    }

    public bool TrySpendEData(int amount)
    {
        if (SaveDataManager.Instance.TrySpendEData(amount))
        {
            resourcePoints -= amount;
            OnResourcePointsChanged?.Invoke(resourcePoints);
            UpdateResourceDisplay();
            return true;
        }
        return false;
    }

    public void StageCleared(int stageNum, int stars)
    {
        SaveDataManager.Instance.UpdateStageProgress(stageNum, stars);
    }

    // 이벤트 구독 메서드
    public void AddResourcePointsListener(Action<int> listener)
    {
        OnResourcePointsChanged += listener;
    }

    // 이벤트 구독 해제 메서드
    public void RemoveResourcePointsListener(Action<int> listener)
    {
        OnResourcePointsChanged -= listener;
    }
}
