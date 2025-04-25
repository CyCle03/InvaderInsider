using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    public Transform[] wayPoints = new Transform[2];
    public GameObject[] enemyPool;
    public GameObject enemyPrefab;
    public int stageWave = 20;
    public float createTime = 1f;

    float currentTime = 0f;
    int enemyCount = 0;

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
        enemyPool = new GameObject[stageWave];
        for (int i = 0; i < enemyPool.Length; i++)
        {
            GameObject enemy = Instantiate(enemyPrefab);
            enemyPool[i] = enemy;
            enemy.SetActive(false);
        }

        currentTime = -1f;
        enemyCount = 0;

        s_state = StageState.Ready;
    }

    // Update is called once per frame
    void Update()
    {
        switch (s_state)
        {
            case StageState.Ready:
                break;
            case StageState.Run:
                break;
            case StageState.End:
                break;
        }

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
