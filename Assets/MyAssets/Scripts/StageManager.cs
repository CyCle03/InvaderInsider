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
                DontDestroyOnLoad(gameObject);
                
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

            InitializeStage();
        }

        private void InitializeStage()
        {
            currentTime = 0f;
            enemyCount = 0;
            stageNum = 0;
            stageWave = stageData.GetStageWaveCount(stageNum);
            currentState = StageState.Ready;
        }

        void Update()
        {
            if (stageData == null) return;

            switch (currentState)
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
            if (stageNum < stageData.StageCount)
            {
                if (stageCoroutine == null)
                {
                    stageCoroutine = StartCoroutine(StartStage());
                }
            }
            else
            {
                currentState = StageState.Over;
            }
        }

        IEnumerator StartStage()
        {
            yield return new WaitForSeconds(STAGE_START_DELAY);
            currentState = StageState.Run;
            UIManager.Instance.UpdateStage(stageNum, GetStageCount());
            stageCoroutine = null;
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
                currentState = StageState.End;
                stageNum++;
                currentTime = 0f;
                enemyCount = 0;

                if (stageNum < stageData.StageCount)
                {
                    stageWave = stageData.GetStageWaveCount(stageNum);
                }
                else
                {
                    stageWave = 0;
                }
            }
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

        void EndStage()
        {
            if(stageCoroutine == null)
            {
                stageCoroutine = StartCoroutine(WaitStage());
            }
        }

        IEnumerator WaitStage()
        {
            yield return new WaitForSeconds(STAGE_END_DELAY);
            currentState = StageState.Ready;
            stageCoroutine = null;
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
