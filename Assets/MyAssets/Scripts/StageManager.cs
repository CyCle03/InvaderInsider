using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InvaderInsider.Data;
using InvaderInsider.UI;

namespace InvaderInsider
{
    public class StageManager : MonoBehaviour
    {
        private static StageManager instance;
        public static StageManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<StageManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("StageManager");
                        instance = go.AddComponent<StageManager>();
                    }
                }
                return instance;
            }
        }

        [Header("Stage Data")]
        [SerializeField] private StageList stageDataObject;
        private IStageData stageData;

        [Header("Stage Settings")]
        private const float STAGE_START_DELAY = 1f;
        private const float STAGE_END_DELAY = 3f;
        public List<Transform> wayPoints = new List<Transform>();
        public GameObject enemyPrefab;
        public int stageNum = 0;
        public int stageWave = 20;
        [Tooltip("Time between enemy spawns")]
        public float createTime = 1f;

        private float currentTime = 0f;
        private int enemyCount = 0;
        private Coroutine stageCoroutine = null;

        public enum StageState
        {
            Ready,
            Run,
            End,
            Over
        }

        public StageState currentState;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                
                stageData = stageDataObject;
                if (stageData == null)
                {
                    Debug.LogError("Stage data is not assigned in the inspector!");
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            if (stageData == null)
            {
                Debug.LogError("Stage data is not set!");
                return;
            }
        }

        public void InitializeStage()
        {
            Debug.Log("StageManager InitializeStage called");
            currentTime = 0f;
            enemyCount = 0;
            stageNum = 0;
            if (stageData == null)
            {
                 Debug.LogError("Stage data is not set in InitializeStage!");
                 return;
            }
            stageWave = stageData.GetStageWaveCount(stageNum);
            currentState = StageState.Ready;
            
            if (stageCoroutine != null)
            {
                StopCoroutine(stageCoroutine);
            }
            stageCoroutine = StartCoroutine(StageLoopCoroutine());
        }

        private IEnumerator StageLoopCoroutine()
        {
            Debug.Log("StageLoopCoroutine started");
            while (currentState != StageState.Over)
            {
                switch (currentState)
                {
                    case StageState.Ready:
                        Debug.Log($"Stage {stageNum + 1} Ready");
                        UIManager.Instance.UpdateStage(stageNum, GetStageCount());
                        yield return new WaitForSeconds(STAGE_START_DELAY);
                        currentState = StageState.Run;
                        break;

                    case StageState.Run:
                        currentTime += Time.deltaTime;
                        if(enemyCount < stageWave)
                        {
                            if (currentTime > createTime)
                            {
                                SpawnEnemy();
                                currentTime = 0f;
                            }
                        }
                        else
                        {
                            currentState = StageState.End;
                            Debug.Log($"Stage {stageNum + 1} Wave {stageWave} completed");
                        }
                        yield return null;
                        break;

                    case StageState.End:
                        Debug.Log($"Stage {stageNum + 1} End");
                        stageNum++;
                        currentTime = 0f;
                        enemyCount = 0;

                        if (stageNum < stageData.StageCount)
                        {
                            stageWave = stageData.GetStageWaveCount(stageNum);
                            yield return new WaitForSeconds(STAGE_END_DELAY);
                            currentState = StageState.Ready;
                        }
                        else
                        {
                            stageWave = 0;
                            currentState = StageState.Over;
                            Debug.Log("All stages completed!");
                        }
                        yield return null;
                        break;
                }
            }
             Debug.Log("StageLoopCoroutine finished");
        }

        public void SpawnEnemy()
        {
            if (wayPoints.Count == 0)
            {
                Debug.LogError("No waypoints set for enemy path!");
                return;
            }

            GameObject enemyPrefab = stageData.GetStageObject(stageNum, enemyCount);
            if (enemyPrefab != null)
            {
                GameObject enemy = Instantiate(enemyPrefab);
                enemy.transform.position = wayPoints[0].position;
                enemyCount++;
                UIManager.Instance.UpdateWave(enemyCount, stageWave);
                currentTime = 0f;
            }
        }

        public int GetStageCount()
        {
            return stageData?.StageCount ?? 0;
        }

        public int GetStageWaveCount(int stageIndex)
        {
            return stageData?.GetStageWaveCount(stageIndex) ?? 0;
        }
    }
}
