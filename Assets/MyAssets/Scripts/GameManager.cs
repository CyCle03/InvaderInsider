using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using InvaderInsider;

public class GameManager : MonoBehaviour
{
    public static GameManager gm;

    [Header("Game Resources")]
    public int enemyData = 0;

    public event Action<int> OnEnemyDataChanged;

    public TextMeshProUGUI stageText;
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI eDataText;

    private void Awake()
    {
        if (gm == null)
        {
            gm = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        UpdateStage();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateStage()
    {
        var stageManager = StageManager.Instance;
        stageText.text = $"Stage {stageManager.stageNum + 1} / {stageManager.GetStageCount()}";
    }

    public void UpdateWave(int eCnt)
    {
        var stageManager = StageManager.Instance;
        waveText.text = $"Wave {eCnt} / {stageManager.GetStageWaveCount(stageManager.stageNum)}";
    }

    public void UpdateEData(int amount)
    {
        enemyData += amount;
        OnEnemyDataChanged?.Invoke(enemyData);
        if (eDataText != null)
        {
            eDataText.text = $"eData: {enemyData}";
        }
    }

    public bool TrySpendEData(int amount)
    {
        if (enemyData >= amount)
        {
            enemyData -= amount;
            OnEnemyDataChanged?.Invoke(enemyData);
            if (eDataText != null)
            {
                eDataText.text = $"eData: {enemyData}";
            }
            return true;
        }
        return false;
    }
}
