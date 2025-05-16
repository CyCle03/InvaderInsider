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
        int stn = StageManager.sgm.stageNum;
        stageText.text = "Stage " + (stn+1) + " / " + StageManager.sgm.stageList.stages.Length;
    }

    public void UpdateWave(int eCnt)
    {
        int stn = StageManager.sgm.stageNum;
        waveText.text = "Wave " + eCnt + " / " + StageManager.sgm.stageList.stages[stn].Container.Length;
    }

    public void UpdateEData(float _eData)
    {
        eData += _eData;
        eDataText.text = "eData " + eData;
    }
}
