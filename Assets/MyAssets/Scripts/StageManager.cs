using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public Transform[] wayPoints = new Transform[2];
    public GameObject[] enemyPool;
    public GameObject enemyPrefab;
    public StageList stageList;
    public int stageNum = 0;
    public int stageWave = 20;
    public float createTime = 1f;

    float currentTime = 0f;
    int enemyCount = 0;

    public static EnemyManager Instance = null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public enum StageState
    {
        Ready,
        Run,
        End,
    }

    public StageState s_state;

    // Start is called before the first frame update
    void Start()
    {
        int stageLength = stageList.stages[stageNum].Container.Length;
        enemyPool = new GameObject[stageLength];
        for (int i = 0; i < enemyPool.Length; i++)
        {
            GameObject enemy = Instantiate(stageList.stages[stageNum].Container[i]);
            enemyPool[i] = enemy;
            enemy.SetActive(false);
        }

        currentTime = 0f;
        enemyCount = 0;

        s_state = StageState.Ready;
    }

    // Update is called once per frame
    void Update()
    {
        switch (s_state)
        {
            case StageState.Ready:
                ReadyEnemy();
                break;
            case StageState.Run:
                RunEnemy();
                break;
            case StageState.End:
                break;
        }
    }

    
    void ReadyEnemy()
    {
        StartCoroutine(StartStatge());
    }
    IEnumerator StartStatge()
    {
        yield return new WaitForSeconds(1f);
        s_state = StageState.Run;
    }

    void RunEnemy()
    {
        currentTime += Time.deltaTime;

        if (currentTime > createTime)
        {
            SpawnEnemy();
        }
    }
    public void SpawnEnemy()
    {
        if (enemyCount < stageWave)
        {
            GameObject enemy = enemyPool[enemyCount];

            if (enemy.activeSelf == false)
            {
                enemy.transform.position = wayPoints[0].position;
                enemy.SetActive(true);
                enemyCount++;
                currentTime = 0f;
            }
        }
        else
        {
            s_state = StageState.End;
        }
    }

}
