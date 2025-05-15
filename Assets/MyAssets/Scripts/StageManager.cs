using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public Transform[] wayPoints = new Transform[2];
    public GameObject enemyPrefab;
    public StageList stageList;
    public int stageNum = 0;
    public int stageWave = 20;
    public float createTime = 1f;

    float currentTime = 0f;
    int enemyCount = 0;

    public static StageManager sm = null;

    private void Awake()
    {
        if (sm == null)
        {
            sm = this;
        }
    }

    public enum StageState
    {
        Ready,
        Run,
        End,
        Over
    }

    public StageState s_state;

    // Start is called before the first frame update
    void Start()
    {
        currentTime = 0f;
        enemyCount = 0;
        stageNum = 0;
        stageWave = stageList.stages[stageNum].Container.Length;

        s_state = StageState.Ready;
    }

    // Update is called once per frame
    void Update()
    {
        switch (s_state)
        {
            case StageState.Ready:
                ReadyStage();
                break;
            case StageState.Run:
                RunStage();
                break;
            case StageState.End:
                EndStage();
                break;
            case StageState.Over:

                break;
        }
    }
    void ReadyStage()
    {
        if (stageNum < stageList.stages.Length)
        {
            StartCoroutine(StartStatge());
        }
        else
        {
            s_state = StageState.Over;
        }

    }
    IEnumerator StartStatge()
    {
        yield return new WaitForSeconds(1f);
        s_state = StageState.Run;
        GameManager.gm.UpdateStage();
    }

    void RunStage()
    {
        currentTime += Time.deltaTime;

        if(enemyCount < stageWave)
        {
            if (currentTime > createTime)
            {
                SpawnEnemy();
            }
        }
        else
        {
            s_state = StageState.End;
            stageNum++;
            currentTime = 0f;
            enemyCount = 0;
            stageWave = stageList.stages[stageNum].Container.Length;
        }
        
    }
    public void SpawnEnemy()
    {
        int stageLength = stageList.stages[stageNum].Container.Length;
        GameObject enemy = Instantiate(stageList.stages[stageNum].Container[enemyCount]);
        enemy.transform.position = wayPoints[0].position;
        enemyCount++;
        GameManager.gm.UpdateWave(enemyCount);
        currentTime = 0f;
    }

    void EndStage()
    {
        StartCoroutine(WaitStage());
    }

    IEnumerator WaitStage()
    {
        yield return new WaitForSeconds(3f);
        s_state = StageState.Ready;
    }
}
