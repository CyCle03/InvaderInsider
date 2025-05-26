using System.Collections;
using UnityEngine;

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
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                
                // StageList를 IStageData로 캐스팅
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

        public Transform[] wayPoints = new Transform[2];
        public GameObject enemyPrefab;
        public int stageNum = 0;
        public int stageWave = 20;
        public float createTime = 1f;

        float currentTime = 0f;
        int enemyCount = 0;
        Coroutine co = null;

        public enum StageState
        {
            Ready,
            Run,
            End,
            Over
        }

        public StageState s_state;

        void Start()
        {
            if (stageData == null)
            {
                Debug.LogError("Stage data is not set!");
                return;
            }

            currentTime = 0f;
            enemyCount = 0;
            stageNum = 0;
            stageWave = stageData.GetStageWaveCount(stageNum);

            s_state = StageState.Ready;
        }

        void Update()
        {
            if (stageData == null) return;

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
            if (stageNum < stageData.StageCount)
            {
                if (co == null)
                {
                    co = StartCoroutine(StartStatge());
                }
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
            co = null;
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
            GameObject enemyPrefab = stageData.GetStageObject(stageNum, enemyCount);
            if (enemyPrefab != null)
            {
                GameObject enemy = Instantiate(enemyPrefab);
                enemy.transform.position = wayPoints[0].position;
                enemyCount++;
                GameManager.gm.UpdateWave(enemyCount);
                currentTime = 0f;
            }
        }

        void EndStage()
        {
            if(co == null)
            {
                co = StartCoroutine(WaitStage());
            }
        }

        IEnumerator WaitStage()
        {
            yield return new WaitForSeconds(3f);
            s_state = StageState.Ready;
            co = null;
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
