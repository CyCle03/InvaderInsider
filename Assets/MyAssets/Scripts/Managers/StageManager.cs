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
            "Stage data is null - initialization failed",
            "Invalid stage index: {0}",
            "Invalid enemy spawn - Stage: {0}, Count: {1}"
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
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            if (!isInitialized)
            {
                PerformInitialization();
            }
        }

        private void PerformInitialization()
        {
            if (isInitialized) return;

            if (stageDataObject != null)
            {
                stageData = stageDataObject;
            }
            else
            {
                var defaultStageList = Resources.Load<StageList>("StageList1") ?? 
                                      Resources.Load<StageList>("ScriptableObjects/StageSystem/StageList1");
                
                if (defaultStageList != null)
                {
                    stageDataObject = defaultStageList;
                    stageData = stageDataObject;
                }
                else
                {
                    #if UNITY_EDITOR
                    Debug.LogError(LOG_PREFIX + "기본 StageList를 찾을 수 없습니다. StageList1.asset 파일을 확인하세요.");
                    #endif
                    return;
                }
            }

            InitializeComponents();
            isInitialized = true;
        }

        private void InitializeComponents()
        {
            bottomBarPanel = FindObjectOfType<BottomBarPanel>();
            // BottomBarPanel은 선택사항이므로 경고 제거

            uiManager = UIManager.Instance;
            gameManager = GameManager.Instance;
            saveDataManager = SaveDataManager.Instance;

            activeTowers.Clear();
            activeTowers.AddRange(FindObjectsOfType<Tower>());

            // 웨이포인트가 설정되지 않은 경우 자동으로 찾기 시도
            if (wayPointsList.Count == 0)
            {
                AutoFindWaypoints();
            }
        }

        private void AutoFindWaypoints()
        {
            // "Waypoint" 태그나 "EnemyPath" 태그를 가진 오브젝트들을 찾아서 자동으로 웨이포인트 설정
            GameObject[] waypoints = GameObject.FindGameObjectsWithTag("Waypoint");
            if (waypoints.Length == 0)
            {
                waypoints = GameObject.FindGameObjectsWithTag("EnemyPath");
            }

            if (waypoints.Length > 0)
            {
                wayPointsList.Clear();
                // 이름순으로 정렬하여 올바른 순서로 웨이포인트 설정
                System.Array.Sort(waypoints, (a, b) => a.name.CompareTo(b.name));
                
                foreach (GameObject wp in waypoints)
                {
                    wayPointsList.Add(wp.transform);
                }
                
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + $"자동으로 {wayPointsList.Count}개의 웨이포인트를 찾았습니다.");
                #endif
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + "웨이포인트를 찾을 수 없습니다. 'Waypoint' 또는 'EnemyPath' 태그를 가진 오브젝트가 필요합니다.");
                #endif
            }
        }

        private void Start()
        {
            if (!isInitialized)
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[0]);
                #endif
                
                // 강제로 초기화 시도
                if (stageDataObject != null)
                {
                    stageData = stageDataObject;
                    InitializeComponents();
                    isInitialized = true;
                }
                return;
            }
            
            if (stageData == null)
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[0]);
                #endif
                
                // stageDataObject가 있는데 stageData가 null인 경우 다시 할당
                if (stageDataObject != null)
                {
                    stageData = stageDataObject;
                }
                
                if (stageData == null)
                {
                    return;
                }
            }
        }

        public void InitializeStage()
        {
            if (!isInitialized) return;
            StartStageInternal(0);
        }

        public void StartStageFrom(int stageIndex)
        {
            if (!isInitialized) return;

            if (stageData != null && stageIndex >= 0 && stageIndex < stageData.StageCount)
            {
                StartStageInternal(stageIndex);
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogError(string.Format(LOG_PREFIX + LOG_MESSAGES[1], stageIndex));
                #endif
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
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[0]);
                #endif
                return;
            }

            stageWave = stageData.GetStageWaveCount(stageNum);
            currentState = StageState.Ready;
            
            stageCoroutine = StartCoroutine(StageLoopCoroutine());
            ResetAllTowersRotation();
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

            while (currentState != StageState.Over && !isQuitting)
            {
                yield return HandleStageState();
            }

            // 스테이지 루프 완료
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
            if (uiManager != null)
            {
                uiManager.UpdateStage(stageNum, GetStageCount());
            }

            // 스테이지가 준비되었으므로 게임 상태를 Playing으로 변경
            if (gameManager != null)
            {
                gameManager.SetGameState(GameState.Playing);
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
            }

            yield return null;
        }

        private IEnumerator HandleEndState()
        {
            if (gameManager != null)
            {
                int stars = CalculateStageStars();
                gameManager.StageCleared(clearedStageIndex, stars);
            }

            stageNum++;
            currentTime = 0f;

            if (stageNum >= GetStageCount())
            {
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
            if (wayPointsList == null || wayPointsList.Count == 0)
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + "웨이포인트가 설정되지 않았습니다. 적을 스폰할 수 없습니다.");
                #endif
                return;
            }

            if (defaultEnemyPrefab == null)
            {
                #if UNITY_EDITOR
                Debug.LogError(string.Format(LOG_PREFIX + LOG_MESSAGES[2], stageNum, enemyCount));
                #endif
                return;
            }

            Vector3 spawnPosition = wayPointsList[0].position;
            GameObject enemy = Instantiate(defaultEnemyPrefab, spawnPosition, Quaternion.identity);
            
            EnemyObject enemyObject = enemy.GetComponent<EnemyObject>();
            if (enemyObject != null)
            {
                enemyObject.SetupEnemy(stageNum, enemyCount);
                activeEnemies.Add(enemyObject);
                IncrementEnemyCount();
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
        }

        public void EnemyReachedEnd()
        {
            if (!isInitialized) return;

            DecreaseActiveEnemyCount();
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
        }

        public void DecrementEnemyCount()
        {
            activeEnemyCountValue = Mathf.Max(0, activeEnemyCountValue - 1);
        }

        public int GetCurrentStageIndex()
        {
            return stageNum;
        }
    }
} 