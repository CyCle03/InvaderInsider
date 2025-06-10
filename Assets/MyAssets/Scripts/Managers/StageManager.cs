using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InvaderInsider.Data;
using InvaderInsider.UI;
using InvaderInsider.Managers;

namespace InvaderInsider.Managers
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
        [SerializeField] private GameObject defaultEnemyPrefab;
        public int stageNum = 0;
        public int stageWave = 20;
        [Tooltip("Time between enemy spawns")]
        public float createTime = 1f;

        private float currentTime = 0f;
        private int enemyCount = 0;
        public int activeEnemyCount = 0;
        private Coroutine stageCoroutine = null;

        public enum StageState
        {
            Ready,
            Run,
            Wait,
            End,
            Over
        }

        public StageState currentState;
        private int clearedStageIndex;

        private BottomBarPanel bottomBarPanel;

        private List<Tower> _activeTowers;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                
                stageData = stageDataObject;
                if (stageData == null)
                {
                    Debug.LogError("Stage data is not assigned in the inspector! Please assign a StageList ScriptableObject.");
                }
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }

            bottomBarPanel = FindObjectOfType<BottomBarPanel>();
            if (bottomBarPanel == null)
            {
                Debug.LogWarning("BottomBarPanel not found in the scene. UI updates may not work.");
            }
            _activeTowers = new List<Tower>(FindObjectsOfType<Tower>());
        }

        void Start()
        {
            if (stageData == null)
            {
                Debug.LogError("Stage data is not set!");
                return;
            }

            // 씬 로드 후 새 게임을 시작하기 위해 StageManager 초기화 (자동 초기화 제거)
            // InitializeStage();
        }

        public void InitializeStage()
        {
            Debug.Log("StageManager InitializeStage called");
            StartStageInternal(0);
        }

        public void StartStageFrom(int stageIndex)
        {
             Debug.Log($"StageManager StartStageFrom called for stage {stageIndex}");
            if (stageData != null && stageIndex >= 0 && stageIndex < stageData.StageCount)
            {
                 StartStageInternal(stageIndex);
            }
            else
            {
                Debug.LogError($"Invalid stage index {stageIndex} provided for StartStageFrom.");
                 StartStageInternal(0);
            }
        }

        private void StartStageInternal(int startStageIndex)
        {
             currentTime = 0f;
            enemyCount = 0;
            activeEnemyCount = 0;
            stageNum = startStageIndex;
            if (stageData == null)
            {
                 Debug.LogError("Stage data is not set in StartStageInternal!");
                 return;
            }
            stageWave = stageData.GetStageWaveCount(stageNum);
            currentState = StageState.Ready;
            
            if (stageCoroutine != null)
            {
                StopCoroutine(stageCoroutine);
            }
            stageCoroutine = StartCoroutine(StageLoopCoroutine());

            ResetAllTowersRotation();
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
                        if (enemyCount >= stageWave && activeEnemyCount <= 0)
                        {
                            clearedStageIndex = stageNum;
                            currentState = StageState.End;
                            Debug.Log($"Stage {clearedStageIndex + 1} cleared: All enemies spawned and defeated.");
                        }
                        yield return null;
                        break;

                    case StageState.End:
                        Debug.Log($"Stage {stageNum + 1} End");
                        if (GameManager.Instance != null)
                        {
                            int stars = 0; 
                            GameManager.Instance.StageCleared(clearedStageIndex, stars);
                            Debug.Log($"Stage {clearedStageIndex} progress updated and saved.");
                        }

                        stageNum++;
                        currentTime = 0f;
                        enemyCount = 0;

                        if (stageNum < stageData.StageCount)
                        {
                            stageWave = stageData.GetStageWaveCount(stageNum);
                            yield return new WaitForSeconds(STAGE_END_DELAY);
                            ResetAllTowersRotation();
                            currentState = StageState.Ready;
                        }
                        else
                        {
                            stageWave = 0;
                            currentState = StageState.Over;
                            Debug.Log("All stages completed!");
                            yield return new WaitForSeconds(STAGE_END_DELAY);
                            if (UIManager.Instance != null)
                            {
                                UIManager.Instance.ShowPanel("MainMenu");
                            }
                            ResetAllTowersRotation();
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

            GameObject enemyToSpawn = stageData?.GetStageObject(stageNum, enemyCount);
            if (enemyToSpawn == null)
            {
                Debug.LogWarning($"StageData did not provide an enemy prefab for stage {stageNum}, enemy {enemyCount}. Using defaultEnemyPrefab.");
                enemyToSpawn = defaultEnemyPrefab;
            }

            if (enemyToSpawn != null)
            {
                GameObject enemy = Instantiate(enemyToSpawn);
                enemy.transform.position = wayPoints[0].position;
                enemyCount++;
                activeEnemyCount++;
                UIManager.Instance.UpdateWave(enemyCount, stageWave);
                if (bottomBarPanel != null)
                {
                     bottomBarPanel.UpdateMonsterCountDisplay(activeEnemyCount);
                }
                currentTime = 0f;
            }
            else
            {
                Debug.LogError($"Failed to spawn enemy: No prefab assigned or retrieved for stage {stageNum}, enemy {enemyCount}.");
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

        public void DecreaseActiveEnemyCount()
        {
            activeEnemyCount--;
            Debug.Log($"Active enemies: {activeEnemyCount}");
            if (bottomBarPanel != null)
            {
                bottomBarPanel.UpdateMonsterCountDisplay(activeEnemyCount);
            }
        }

        public void OnEnemyDied(int eDataAmount)
        {
            DecreaseActiveEnemyCount();
            SaveDataManager.Instance.UpdateEData(eDataAmount);
            Debug.Log($"Enemy Died! eData reward: {eDataAmount}");
        }

        public void EnemyReachedEnd()
        {
            DecreaseActiveEnemyCount();
            Debug.Log("Enemy reached end waypoint. Active enemies: " + activeEnemyCount);
        }

        public void InitializeStageFromLoadedData(int stageIndex)
        {
            Debug.Log($"StageManager: Initializing for stage {stageIndex} from loaded data.");

            StartStageFrom(stageIndex);

            if (UIManager.Instance != null)
            {
                 UIManager.Instance.UpdateStage(stageNum, GetStageCount());
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateWave(1, stageWave);
            }

            if (bottomBarPanel != null)
            {
                bottomBarPanel.UpdateMonsterCountDisplay(activeEnemyCount);
                Debug.Log($"StageManager Initialized: Stage {stageNum + 1}, Total Waves: {stageWave}, Active Enemies: {activeEnemyCount}");
            }
        }

        private void ResetAllTowersRotation()
        {
            foreach (Tower tower in _activeTowers)
            {
                if (tower != null)
                {
                    tower.ResetTowerRotation();
                }
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
                Debug.Log("StageManager instance cleared on destroy.");
            }
        }
    }
} 