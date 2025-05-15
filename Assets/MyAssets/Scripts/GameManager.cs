using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    public static GameManager gm = null;

    private void Awake()
    {
        if (gm == null)
        {
            gm = this;
        }
    }
    public TextMeshProUGUI stageText;
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI eDataText;

    public float eData = 0f;

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
        int stn = StageManager.sm.stageNum;
        stageText.text = "Stage " + (stn+1) + " / " + StageManager.sm.stageList.stages.Length;
    }

    public void UpdateWave(int eCnt)
    {
        int stn = StageManager.sm.stageNum;
        waveText.text = "Wave " + eCnt + " / " + StageManager.sm.stageList.stages[stn].Container.Length;
    }

    public void UpdateEData(float _eData)
    {
        eData += _eData;
        eDataText.text = "eData " + eData;
    }
}
