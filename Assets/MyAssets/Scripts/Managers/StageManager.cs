using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InvaderInsider.Data;
using InvaderInsider.UI;
using InvaderInsider.Managers;
using System.Threading.Tasks;

namespace InvaderInsider.Managers
{
    public class StageManager : MonoBehaviour
    {
        private const string LOG_PREFIX = "[Stage] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "Stage {0} Ready",
            "Stage {0} cleared: All enemies spawned and defeated.",
            "Stage {0} End",
            "Stage {0} progress updated and saved.",
            "All stages completed!",
            "StageLoopCoroutine started",
            "StageLoopCoroutine finished",
            "Stage data is not assigned in the inspector! Please assign a StageList ScriptableObject.",
            "BottomBarPanel not found in the scene. UI updates may not work.",
            "Stage data is not set!",
            "StageManager InitializeStage called",
            "StageManager StartStageFrom called for stage {0}",
            "Invalid stage index {0} provided for StartStageFrom.",
            "Stage data is not set in StartStageInternal!",
            "No waypoints set for enemy path!",
            "StageData did not provide an enemy prefab for stage {0}, enemy {1}. Using defaultEnemyPrefab.",
            "Failed to spawn enemy: No prefab assigned or retrieved for stage {0}, enemy {1}.",
            "Stage {0} wave {1} started",
            "Stage {0} wave {1} completed",
            "Stage {0} wave {1} enemy spawned",
            "Stage {0} wave {1} enemy reached end",
            "Stage {0} wave {1} enemy died"
        };

        private static StageManager instance;
        private static readonly object _lock = new object();
        private static bool isQuitting = false;
        private static bool isInitialized = false;

        public static StageManager Instance
        {
            get
            {
                if (isQuitting) return null;

                lock (_lock)
                {
                    if (instance == null && !isQuitting)
                    {
                        instance = FindObjectOfType<StageManager>();
                        if (instance == null)
                        {
                            GameObject go = new GameObject("StageManager");
                            instance = go.AddComponent<StageManager>();
                            DontDestroyOnLoad(go);
                        }
                    }
                    
                    // 인스턴스가 있지만 초기화되지 않은 경우 강제 초기화
                    if (instance != null && !isInitialized)
                    {
                        Debug.Log(LOG_PREFIX + "Instance exists but not initialized in getter, forcing initialization");
                        instance.PerformInitialization();
                    }
                    
                    return instance;
                }
            }
        }

        [Header("Stage Data")]
        [SerializeField] private StageList stageDataObject;
        private IStageData stageData;

        [Header("Stage Settings")]
        private const float STAGE_START_DELAY = 1f;
        private const float STAGE_END_DELAY = 3f;
        private const float MIN_SPAWN_INTERVAL = 0.1f;
        private const float MAX_SPAWN_INTERVAL = 2f;
        private const int MAX_ACTIVE_ENEMIES = 50;

        [SerializeField] private List<Transform> wayPointsList;
        public IReadOnlyList<Transform> WayPoints => wayPointsList;
        [SerializeField] private GameObject defaultEnemyPrefab;
        [SerializeField] private int stageNum = 0;
        [SerializeField] private int stageWave = 20;
        [SerializeField, Range(MIN_SPAWN_INTERVAL, MAX_SPAWN_INTERVAL)] 
        private float createTime = 1f;

        private float currentTime = 0f;
        private int enemyCount = 0;
        private int activeEnemyCountValue = 0;
        private Coroutine stageCoroutine = null;
        private readonly Queue<EnemyObject> enemyPool = new Queue<EnemyObject>();
        private readonly HashSet<EnemyObject> activeEnemies = new HashSet<EnemyObject>();

        public enum StageState
        {
            Ready,
            Run,
            Wait,
            End,
            Over
        }

        private StageState currentState;
        private int clearedStageIndex;
        private BottomBarPanel bottomBarPanel;
        private readonly List<Tower> activeTowers = new List<Tower>();
        private UIManager uiManager;
        private GameManager gameManager;
        private SaveDataManager saveDataManager;

        public int ActiveEnemyCount => activeEnemyCountValue;

        private void Awake()
        {
            Debug.Log(LOG_PREFIX + "=== AWAKE CALLED ===");
            
            if (instance == null)
            {
                Debug.Log(LOG_PREFIX + "Setting as instance");
                instance = this;
                DontDestroyOnLoad(gameObject);
                PerformInitialization();
            }
            else if (instance != this)
            {
                Debug.Log(LOG_PREFIX + "Destroying duplicate StageManager");
                Destroy(gameObject);
                return;
            }
            else
            {
                Debug.Log(LOG_PREFIX + "This is already the instance");
                // 이미 instance이지만 초기화가 안 되어 있을 수 있음
                if (!isInitialized)
                {
                    Debug.Log(LOG_PREFIX + "Instance exists but not initialized, performing initialization");
                    PerformInitialization();
                }
            }
        }

        private void PerformInitialization()
        {
            Debug.Log(LOG_PREFIX + "=== PERFORMING INITIALIZATION ===");
            Debug.Log(LOG_PREFIX + "StageManager - stageDataObject: " + (stageDataObject != null ? "Assigned" : "Null"));
            if (stageDataObject != null)
            {
                Debug.Log(LOG_PREFIX + "stageDataObject name: " + stageDataObject.name);
            }
            
            if (stageDataObject == null)
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[7]);
                // 다양한 경로에서 StageList를 찾아서 할당
                stageDataObject = Resources.Load<StageList>("StageList1");
                if (stageDataObject == null)
                {
                    stageDataObject = Resources.Load<StageList>("ScriptableObjects/StageSystem/StageList1");
                    if (stageDataObject == null)
                    {
                        Debug.LogError(LOG_PREFIX + "기본 StageList를 찾을 수 없습니다. StageList1.asset 파일을 확인하세요.");
                        Debug.LogError(LOG_PREFIX + "검색한 경로: Resources/StageList1, Resources/ScriptableObjects/StageSystem/StageList1");
                        return;
                    }
                    else
                    {
                        Debug.Log(LOG_PREFIX + "StageList1을 ScriptableObjects 폴더에서 로드했습니다.");
                    }
                }
                else
                {
                    Debug.Log(LOG_PREFIX + "기본 StageList1을 로드했습니다.");
                }
            }
            else
            {
                Debug.Log(LOG_PREFIX + "StageDataObject가 Inspector에서 할당되었습니다: " + stageDataObject.name);
            }

            stageData = stageDataObject;
            Debug.Log(LOG_PREFIX + "StageData assigned: " + (stageData != null ? "Success" : "Failed"));
            
            if (stageData != null)
            {
                Debug.Log(LOG_PREFIX + "StageData StageCount: " + stageData.StageCount);
            }
            
            Debug.Log(LOG_PREFIX + "Calling InitializeComponents");
            InitializeComponents();
            
            isInitialized = true;
            Debug.Log(LOG_PREFIX + "=== INITIALIZATION COMPLETED - isInitialized: " + isInitialized + " ===");
        }

        private void InitializeComponents()
        {
            bottomBarPanel = FindObjectOfType<BottomBarPanel>();
            if (bottomBarPanel == null)
            {
                Debug.LogWarning(LOG_PREFIX + LOG_MESSAGES[8]);
            }

            uiManager = UIManager.Instance;
            gameManager = GameManager.Instance;
            saveDataManager = SaveDataManager.Instance;

            activeTowers.Clear();
            activeTowers.AddRange(FindObjectsOfType<Tower>());

            if (wayPointsList.Count == 0)
            {
                Debug.LogWarning(LOG_PREFIX + LOG_MESSAGES[14]);
            }
        }

        private void Start()
        {
            Debug.Log(LOG_PREFIX + "=== START CALLED ===");
            Debug.Log(LOG_PREFIX + "instance == this: " + (instance == this));
            Debug.Log(LOG_PREFIX + "StageManager Start - isInitialized: " + isInitialized + ", stageData: " + (stageData != null ? "Valid" : "Null"));
            Debug.Log(LOG_PREFIX + "stageDataObject: " + (stageDataObject != null ? stageDataObject.name : "null"));
            
            if (!isInitialized)
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[9]);
                Debug.LogError(LOG_PREFIX + "Awake가 제대로 호출되지 않았습니다!");
                
                // 강제로 초기화 시도
                Debug.Log(LOG_PREFIX + "강제 초기화 시도...");
                if (stageDataObject != null)
                {
                    stageData = stageDataObject;
                    InitializeComponents();
                    isInitialized = true;
                    Debug.Log(LOG_PREFIX + "강제 초기화 완료");
                }
                return;
            }
            
            if (stageData == null)
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[9]);
                Debug.LogError(LOG_PREFIX + "stageDataObject is: " + (stageDataObject != null ? stageDataObject.name : "null"));
                
                // stageDataObject가 있는데 stageData가 null인 경우 다시 할당
                if (stageDataObject != null)
                {
                    Debug.Log(LOG_PREFIX + "stageDataObject가 있으므로 다시 할당 시도");
                    stageData = stageDataObject;
                }
                
                if (stageData == null)
                {
                    return;
                }
            }
            
            Debug.Log(LOG_PREFIX + "StageManager initialized successfully with " + stageData.StageCount + " stages");
        }

        public void InitializeStage()
        {
            if (!isInitialized) return;

            if (Application.isPlaying)
            {
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[10]);
            }
            StartStageInternal(0);
        }

        public void StartStageFrom(int stageIndex)
        {
            if (!isInitialized) return;

            if (Application.isPlaying)
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[11], stageIndex));
            }

            if (stageData != null && stageIndex >= 0 && stageIndex < stageData.StageCount)
            {
                StartStageInternal(stageIndex);
            }
            else
            {
                Debug.LogError(string.Format(LOG_PREFIX + LOG_MESSAGES[12], stageIndex));
                StartStageInternal(0);
            }
        }

        private void StartStageInternal(int startStageIndex)
        {
            if (stageCoroutine != null)
            {
                StopCoroutine(stageCoroutine);
                stageCoroutine = null;
            }

            CleanupActiveEnemies();
            ResetStageState(startStageIndex);
            
            if (stageData == null)
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[13]);
                return;
            }

            stageWave = stageData.GetStageWaveCount(stageNum);
            currentState = StageState.Ready;
            
            stageCoroutine = StartCoroutine(StageLoopCoroutine());
            ResetAllTowersRotation();

            if (Application.isPlaying)
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[17], stageNum + 1, stageWave));
            }
        }

        private void ResetStageState(int startStageIndex)
        {
            currentTime = 0f;
            enemyCount = 0;
            activeEnemyCountValue = 0;
            stageNum = startStageIndex;
        }

        private void CleanupActiveEnemies()
        {
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    enemyPool.Enqueue(enemy);
                    enemy.gameObject.SetActive(false);
                }
            }
            activeEnemies.Clear();
        }

        private IEnumerator StageLoopCoroutine()
        {
            if (Application.isPlaying)
            {
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[5]);
            }

            while (currentState != StageState.Over && !isQuitting)
            {
                yield return HandleStageState();
            }

            if (Application.isPlaying)
            {
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[6]);
            }
        }

        private IEnumerator HandleStageState()
        {
            switch (currentState)
            {
                case StageState.Ready:
                    yield return HandleReadyState();
                    break;

                case StageState.Run:
                    yield return HandleRunState();
                    break;

                case StageState.End:
                    yield return HandleEndState();
                    break;

                case StageState.Wait:
                    yield return HandleWaitState();
                    break;

                default:
                    yield return null;
                    break;
            }
        }

        private IEnumerator HandleReadyState()
        {
            if (Application.isPlaying)
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[0], stageNum + 1));
            }

            if (uiManager != null)
            {
                uiManager.UpdateStage(stageNum, GetStageCount());
            }

            yield return new WaitForSeconds(STAGE_START_DELAY);
            currentState = StageState.Run;
        }

        private IEnumerator HandleRunState()
        {
            currentTime += Time.deltaTime;

            if (enemyCount < stageWave && activeEnemyCountValue < MAX_ACTIVE_ENEMIES)
            {
                if (currentTime >= createTime)
                {
                    SpawnEnemy();
                    currentTime = 0f;
                }
            }

            if (enemyCount >= stageWave && activeEnemyCountValue <= 0)
            {
                clearedStageIndex = stageNum;
                currentState = StageState.End;

                if (Application.isPlaying)
                {
                    Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[1], clearedStageIndex + 1));
                }
            }

            yield return null;
        }

        private IEnumerator HandleEndState()
        {
            if (Application.isPlaying)
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[2], stageNum + 1));
            }

            if (gameManager != null)
            {
                int stars = CalculateStageStars();
                gameManager.StageCleared(clearedStageIndex, stars);

                if (Application.isPlaying)
                {
                    Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[3], clearedStageIndex));
                }
            }

            stageNum++;
            currentTime = 0f;

            if (stageNum >= GetStageCount())
            {
                if (Application.isPlaying)
                {
                    Debug.Log(LOG_PREFIX + LOG_MESSAGES[4]);
                }
                currentState = StageState.Over;
            }
            else
            {
                currentState = StageState.Wait;
            }

            yield return new WaitForSeconds(STAGE_END_DELAY);
        }

        private IEnumerator HandleWaitState()
        {
            yield return new WaitForSeconds(STAGE_START_DELAY);
            StartStageInternal(stageNum);
        }

        private int CalculateStageStars()
        {
            // TODO: Implement star calculation logic based on performance
            return 1;
        }

        public void SpawnEnemy()
        {
            if (!isInitialized || stageData == null) return;

            GameObject enemyPrefab = stageData.GetStageObject(stageNum, enemyCount);
            if (enemyPrefab == null)
            {
                Debug.LogWarning(string.Format(LOG_PREFIX + LOG_MESSAGES[15], stageNum, enemyCount));
                enemyPrefab = defaultEnemyPrefab;
            }

            if (enemyPrefab == null)
            {
                Debug.LogError(string.Format(LOG_PREFIX + LOG_MESSAGES[16], stageNum, enemyCount));
                return;
            }

            GameObject enemyObject = Instantiate(enemyPrefab, wayPointsList[0].position, Quaternion.identity);
            EnemyObject enemy = enemyObject.GetComponent<EnemyObject>();
            if (enemy != null)
            {
                enemy.SetupEnemy(stageNum, enemyCount);
                activeEnemies.Add(enemy);
                IncrementEnemyCount();
            }

            if (Application.isPlaying)
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[19], stageNum, stageWave, enemyCount));
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
            if (activeEnemyCountValue > 0)
            {
                activeEnemyCountValue--;
            }
        }

        public void OnEnemyDied(int eDataAmount)
        {
            if (!isInitialized) return;

            DecreaseActiveEnemyCount();
            if (saveDataManager != null)
            {
                saveDataManager.UpdateEData(eDataAmount);
            }

            if (Application.isPlaying)
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[21], stageNum + 1, enemyCount));
            }
        }

        public void EnemyReachedEnd()
        {
            if (!isInitialized) return;

            DecreaseActiveEnemyCount();
            if (Application.isPlaying)
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[20], stageNum + 1, enemyCount));
            }
        }

        public void InitializeStageFromLoadedData(int stageIndex)
        {
            if (!isInitialized) return;

            if (stageIndex >= 0 && stageIndex < GetStageCount())
            {
                StartStageFrom(stageIndex);
            }
            else
            {
                StartStageFrom(0);
            }
        }

        private void ResetAllTowersRotation()
        {
            foreach (var tower in activeTowers)
            {
                if (tower != null)
                {
                    tower.ResetRotation();
                }
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                CleanupActiveEnemies();
                if (stageCoroutine != null)
                {
                    StopCoroutine(stageCoroutine);
                    stageCoroutine = null;
                }
                isInitialized = false;
            }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
            CleanupActiveEnemies();
        }

        public void IncrementEnemyCount()
        {
            activeEnemyCountValue++;
            if (Application.isPlaying)
            {
                Debug.Log($"[Stage] 활성 적 수 증가: {activeEnemyCountValue}");
            }
        }

        public void DecrementEnemyCount()
        {
            activeEnemyCountValue = Mathf.Max(0, activeEnemyCountValue - 1);
            if (Application.isPlaying)
            {
                Debug.Log($"[Stage] 활성 적 수 감소: {activeEnemyCountValue}");
            }
        }

        public int GetCurrentStageIndex()
        {
            return stageNum;
        }
    }
} 