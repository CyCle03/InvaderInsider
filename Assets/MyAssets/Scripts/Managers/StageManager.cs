using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InvaderInsider.Data;
using InvaderInsider.UI;
using InvaderInsider.Managers;
using InvaderInsider.Cards;
using System.Threading.Tasks;
using System;
using UnityEditor;

namespace InvaderInsider.Managers
{
    public class StageManager : MonoBehaviour
    {
        private const string LOG_PREFIX = "[Stage] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "Invalid stage index: {0}",  // 0
            "스테이지 {0} 준비 중... (총 적: {1})", // 1  
            "2개의 웨이포인트를 자동으로 찾았습니다.", // 2
            "웨이포인트를 찾을 수 없습니다. 수동으로 설정해주세요.", // 3
            "스테이지 {0} 시작!", // 4
            "스테이지 {0} 클리어! (적 처치: {1}/{2})", // 5
            "적 프리팹을 찾을 수 없습니다. 스테이지: {0}", // 6
            "웨이포인트 수동 재설정 시작" // 7
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
                
                // 에디터에서 플레이 모드가 아닐 때는 인스턴스 생성하지 않음
                #if UNITY_EDITOR
                if (!UnityEngine.Application.isPlaying) return null;
                #endif

                // 현재 씬이 Game 씬이 아니면 null 반환
                string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (currentSceneName != "Game")
                {
                    return null;
                }

                lock (_lock)
                {
                    if (instance == null)
                    {
                        // 게임 씬에서만 자동 생성 허용
                        instance = FindObjectOfType<StageManager>();
                    }
                    return instance;
                }
            }
        }

        [Header("Stage Data")]
        [SerializeField] private ScriptableObject stageDataObject;
        private IStageData stageData;

        [Header("Stage Settings")]
        private const float STAGE_START_DELAY = 1f;
        private const float STAGE_END_DELAY = 3f;
        private const float MIN_SPAWN_INTERVAL = 0.1f;
        private const float MAX_SPAWN_INTERVAL = 2f;
        private const int MAX_ACTIVE_ENEMIES = 50;

        [SerializeField] private List<Transform> wayPointsList;
        public IReadOnlyList<Transform> WayPoints => wayPointsList;
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
            // 에디터 모드에서는 초기화하지 않음
            #if UNITY_EDITOR
            if (!Application.isPlaying) return;
            #endif
            
            // 현재 씬이 Game 씬인지 확인
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentSceneName != "Game")
            {
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + $"게임 씬이 아니므로 StageManager를 파괴합니다. 현재 씬: {currentSceneName}");
                #endif
                Destroy(gameObject);
                return;
            }
            
            // 게임 씬에서는 싱글톤 패턴 적용 (DontDestroyOnLoad 제거)
            if (instance == null)
            {
                instance = this;
                // 게임 씬 전용이므로 DontDestroyOnLoad 제거
                
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "StageManager 인스턴스 생성됨 (게임 씬 전용)");
                #endif
            }
            else if (instance != this)
            {
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "중복 StageManager 인스턴스 파괴됨");
                #endif
                
                // 기존 인스턴스가 있다면 새로운 인스턴스는 파괴
                Destroy(gameObject);
                return;
            }
            
            // 이미 초기화된 경우 중복 초기화 방지
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
                stageData = stageDataObject as IStageData;
                if (stageData == null)
                {
                    #if UNITY_EDITOR
                    Debug.LogError(LOG_PREFIX + "StageDataObject가 IStageData를 구현하지 않습니다. StageList 또는 StageDBObject를 사용하세요.");
                    #endif
                }
                else
                {
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + "StageDataObject에서 StageData를 재설정했습니다.");
                    #endif
                }
            }
            else
            {
                // Resources 폴더에서 로드 시도
                var defaultStageList = Resources.Load<StageList>("StageList1") ?? 
                                      Resources.Load<StageList>("ScriptableObjects/StageSystem/StageList1") ??
                                      Resources.Load<StageList>("StageSystem/StageList1") ??
                                      Resources.Load<StageList>("Stage1 Database") ??
                                      Resources.Load<StageList>("StageSystem/Stage1 Database");
                
                if (defaultStageList != null)
                {
                    stageDataObject = defaultStageList;
                    stageData = stageDataObject as IStageData;
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + $"Resources에서 StageData를 로드했습니다: {defaultStageList.name}");
                    #endif
                }
                else
                {
                    // StageDBObject로도 시도
                    var stageDBObject = Resources.Load<StageDBObject>("Stage1 Database") ??
                                       Resources.Load<StageDBObject>("StageSystem/Stage1 Database") ??
                                       Resources.Load<StageDBObject>("ScriptableObjects/StageSystem/Stage1 Database");
                    
                    if (stageDBObject != null)
                    {
                        stageDataObject = stageDBObject;
                        stageData = stageDataObject as IStageData;
                        #if UNITY_EDITOR
                        Debug.Log(LOG_PREFIX + $"Resources에서 StageDBObject를 로드했습니다: {stageDBObject.name}");
                        #endif
                    }
                    else
                    {
                        #if UNITY_EDITOR
                        Debug.LogError(LOG_PREFIX + "StageData를 찾을 수 없습니다. 다음 경로들을 확인하세요:");
                        Debug.LogError(LOG_PREFIX + "- Resources/StageList1");
                        Debug.LogError(LOG_PREFIX + "- Resources/StageSystem/StageList1");
                        Debug.LogError(LOG_PREFIX + "- Resources/Stage1 Database");
                        Debug.LogError(LOG_PREFIX + "- Resources/StageSystem/Stage1 Database");
                        #endif
                    }
                }
            }

            InitializeComponents();
            isInitialized = true;
        }

        private void InitializeComponents()
        {
            // Find operations을 한 번에 모아서 처리
            gameManager = gameManager ?? GameManager.Instance;
            bottomBarPanel = bottomBarPanel ?? FindObjectOfType<BottomBarPanel>();
            saveDataManager = saveDataManager ?? SaveDataManager.Instance;
            
            // GameManager 참조가 null인 경우 재시도
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
                if (gameManager == null)
                {
                    #if UNITY_EDITOR
                    Debug.LogWarning(LOG_PREFIX + "GameManager를 찾을 수 없습니다. 나중에 재시도합니다.");
                    #endif
                }
            }
            
            // StageData 로딩 상태 확인
            if (stageData == null)
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + "StageData가 null입니다. 다음 중 하나를 확인하세요:");
                Debug.LogError(LOG_PREFIX + "1. StageManager Inspector에서 StageDataObject 필드에 StageList 또는 StageDBObject 할당");
                Debug.LogError(LOG_PREFIX + "2. Resources 폴더에 StageList1.asset 또는 Stage1 Database.asset 파일 존재 확인");
                #endif
                
                // StageData 재로드 시도
                if (stageDataObject != null)
                {
                    stageData = stageDataObject as IStageData;
                    if (stageData == null)
                    {
                        #if UNITY_EDITOR
                        Debug.LogError(LOG_PREFIX + "StageDataObject가 IStageData를 구현하지 않습니다. StageList 또는 StageDBObject를 사용하세요.");
                        #endif
                    }
                    else
                    {
                        #if UNITY_EDITOR
                        Debug.Log(LOG_PREFIX + "StageDataObject에서 StageData를 재설정했습니다.");
                        #endif
                    }
                }
                else
                {
                    // Resources 폴더에서 로드 시도
                    var defaultStageList = Resources.Load<StageList>("StageList1") ?? 
                                          Resources.Load<StageList>("ScriptableObjects/StageSystem/StageList1") ??
                                          Resources.Load<StageList>("StageSystem/StageList1") ??
                                          Resources.Load<StageList>("Stage1 Database") ??
                                          Resources.Load<StageList>("StageSystem/Stage1 Database");
                    
                    if (defaultStageList != null)
                    {
                        stageDataObject = defaultStageList;
                        stageData = stageDataObject as IStageData;
                        #if UNITY_EDITOR
                        Debug.Log(LOG_PREFIX + $"Resources에서 StageData를 로드했습니다: {defaultStageList.name}");
                        #endif
                    }
                    else
                    {
                        // StageDBObject로도 시도
                        var stageDBObject = Resources.Load<StageDBObject>("Stage1 Database") ??
                                           Resources.Load<StageDBObject>("StageSystem/Stage1 Database") ??
                                           Resources.Load<StageDBObject>("ScriptableObjects/StageSystem/Stage1 Database");
                        
                        if (stageDBObject != null)
                        {
                            stageDataObject = stageDBObject;
                            stageData = stageDataObject as IStageData;
                            #if UNITY_EDITOR
                            Debug.Log(LOG_PREFIX + $"Resources에서 StageDBObject를 로드했습니다: {stageDBObject.name}");
                            #endif
                        }
                        else
                        {
                            #if UNITY_EDITOR
                            Debug.LogError(LOG_PREFIX + "StageData를 찾을 수 없습니다. 다음 경로들을 확인하세요:");
                            Debug.LogError(LOG_PREFIX + "- Resources/StageList1");
                            Debug.LogError(LOG_PREFIX + "- Resources/StageSystem/StageList1");
                            Debug.LogError(LOG_PREFIX + "- Resources/Stage1 Database");
                            Debug.LogError(LOG_PREFIX + "- Resources/StageSystem/Stage1 Database");
                            #endif
                        }
                    }
                }
            }
            
            // TopBarPanel은 GameManager를 통해 접근하므로 직접 찾지 않음
            // readonly 필드이므로 이미 초기화되어 있음 - 새로운 할당 불가

            #if UNITY_EDITOR
            string gameManagerStatus = gameManager != null ? "찾음" : "없음";
            string bottomBarStatus = bottomBarPanel != null ? "찾음" : "없음";
            string saveDataStatus = saveDataManager != null ? "찾음" : "없음";
            string stageDataStatus = stageData != null ? "찾음" : "없음";
            Debug.Log($"[Stage] 컴포넌트 초기화 - GameManager: {gameManagerStatus}, BottomBarPanel: {bottomBarStatus}, SaveDataManager: {saveDataStatus}, StageData: {stageDataStatus}");
            #endif

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
            wayPointsList.Clear();
            
            // 1. "Waypoint" 태그를 가진 오브젝트들을 찾기
            GameObject[] waypoints = new GameObject[0];
            
            try
            {
                waypoints = GameObject.FindGameObjectsWithTag("Waypoint");
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + $"Waypoint 태그로 {waypoints.Length}개 찾음");
                #endif
            }
            catch (UnityException ex)
            {
                #if UNITY_EDITOR
                Debug.LogWarning(LOG_PREFIX + $"Waypoint 태그가 정의되지 않았습니다: {ex.Message}");
                #endif
            }
            
            // 2. 태그로 찾지 못한 경우 이름으로 찾기 시도
            if (waypoints.Length == 0)
            {
                List<GameObject> foundWaypoints = new List<GameObject>();
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
                
                foreach (GameObject obj in allObjects)
                {
                    if (obj == null) continue;
                    
                    string objName = obj.name.ToLower();
                    if (objName.Contains("waypoint") || objName.Contains("way") || 
                        objName.Contains("path") || objName.Contains("point"))
                    {
                        foundWaypoints.Add(obj);
                        #if UNITY_EDITOR
                        Debug.Log(LOG_PREFIX + $"이름으로 찾은 웨이포인트: {obj.name}");
                        #endif
                    }
                }
                
                waypoints = foundWaypoints.ToArray();
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + $"이름으로 {waypoints.Length}개 찾음");
                #endif
            }
            
            // 3. 찾은 웨이포인트들을 정렬하여 추가
            if (waypoints.Length > 0)
            {
                Array.Sort(waypoints, (a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
                
                foreach (GameObject waypoint in waypoints)
                {
                    if (waypoint != null && waypoint.transform != null)
                    {
                        wayPointsList.Add(waypoint.transform);
                        #if UNITY_EDITOR
                        Debug.Log(LOG_PREFIX + $"웨이포인트 추가됨: {waypoint.name} at {waypoint.transform.position}");
                        #endif
                    }
                }
                
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + $"{wayPointsList.Count}개의 웨이포인트를 자동으로 찾았습니다.");
                #endif
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogWarning(LOG_PREFIX + "웨이포인트를 찾을 수 없습니다. 수동으로 설정해주세요.");
                #endif
            }
        }

        private void Start()
        {
            if (!isInitialized)
            {
                PerformInitialization();
            }

            // GameManager 참조 재확인
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
                if (gameManager == null)
                {
                    gameManager = FindObjectOfType<GameManager>();
                }
                
                #if UNITY_EDITOR
                if (gameManager != null)
                {
                    Debug.Log(LOG_PREFIX + "Start에서 GameManager 참조 재설정 완료");
                }
                else
                {
                    Debug.LogWarning(LOG_PREFIX + "Start에서도 GameManager를 찾을 수 없습니다.");
                }
                #endif
            }

            // 웨이포인트가 누락된 경우 자동 재설정 시도
            if (wayPointsList == null || wayPointsList.Count == 0)
            {
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "웨이포인트가 누락되어 재초기화를 시도합니다.");
                #endif
                AutoFindWaypoints();
                
                if (wayPointsList == null || wayPointsList.Count == 0)
                {
                    Debug.LogWarning(LOG_PREFIX + "웨이포인트를 찾을 수 없습니다. 게임 오브젝트에 'Waypoint' 또는 'EnemyPath' 태그를 설정하세요.");
                }
            }
        }

        public void InitializeStage()
        {
            if (!isInitialized) return;
            StartStageInternal(0);
        }

        /// <summary>
        /// 지정된 스테이지 인덱스부터 스테이지를 시작합니다.
        /// Continue Game 시 저장된 스테이지 정보를 기반으로 호출됩니다.
        /// </summary>
        /// <param name="stageIndex">시작할 스테이지 인덱스 (0부터 시작)</param>
        public void StartStageFrom(int stageIndex)
        {
            if (!isInitialized)
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + "StageManager가 초기화되지 않음");
                #endif
                return;
            }

            // 스테이지 인덱스 유효성 검사
            if (stageIndex >= 0 && stageData != null && stageIndex < stageData.StageCount)
            {
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + $"스테이지 {stageIndex + 1}부터 시작합니다. (인덱스: {stageIndex})");
                #endif
                StartStageInternal(stageIndex);
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogWarning(LOG_PREFIX + $"무효한 스테이지 인덱스 {stageIndex} (총 스테이지: {stageData?.StageCount ?? 0}) - 0으로 시작");
                #endif
                StartStageInternal(0);
            }
        }

        private void StartStageInternal(int startStageIndex)
        {
            if (!isInitialized)
            {
                Debug.LogError(LOG_PREFIX + "StageManager가 초기화되지 않았습니다.");
                return;
            }

            // 스테이지 데이터에서 실제 wave 수 가져오기
            if (stageData != null)
            {
                stageWave = stageData.GetStageWaveCount(startStageIndex);
            }
            else
            {
                stageWave = 20; // 기본값
            }

            ResetStageState(startStageIndex);
            CleanupActiveEnemies();

            currentState = StageState.Ready;

            if (stageCoroutine != null)
            {
                StopCoroutine(stageCoroutine);
            }
            stageCoroutine = StartCoroutine(StageLoopCoroutine());
        }

        private void ResetStageState(int startStageIndex)
        {
            currentTime = 0f;
            enemyCount = 0;
            activeEnemyCountValue = 0;
            stageNum = startStageIndex;
            
            // 스테이지 시작 시 UI 초기화 (GameManager를 통해)
            if (gameManager != null)
            {
                int maxMonsters = GetStageWaveCount(startStageIndex);
                gameManager.UpdateStageWaveUI(startStageIndex + 1, 0, maxMonsters);
            }
            
            if (bottomBarPanel != null)
            {
                bottomBarPanel.UpdateMonsterCountDisplay(0);
            }
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
            StageState lastState = StageState.Ready;
            
            while (currentState != StageState.Over && !isQuitting)
            {
                // 상태가 변경되었는지 확인
                if (lastState != currentState)
                {
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + $"스테이지 상태 변경: {lastState} -> {currentState}");
                    #endif
                    lastState = currentState;
                }
                
                switch (currentState)
                {
                    case StageState.Ready:
                        yield return StartCoroutine(HandleReadyState());
                        break;
                    case StageState.Run:
                        yield return StartCoroutine(HandleRunState());
                        break;
                    case StageState.Wait:
                        yield return StartCoroutine(HandleWaitState());
                        break;
                    case StageState.End:
                        yield return StartCoroutine(HandleEndState());
                        break;
                    default:
                        yield return null;
                        break;
                }
                
                // 프레임당 한 번씩 체크
                yield return null;
            }
            
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + "스테이지 상태 핸들러 종료");
            #endif
        }

        private IEnumerator HandleReadyState()
        {
            // 동적으로 현재 스테이지의 웨이브 카운트 가져오기
            int currentStageWaveCount = GetStageWaveCount(stageNum);
            
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + $"스테이지 {stageNum + 1} 준비 중... (총 적: {currentStageWaveCount})");
            #endif
            
            // TopBar UI 업데이트 (현재/최대 형식) - GameManager를 통해
            if (gameManager != null)
            {
                int spawnedMonsters = enemyCount; // 현재 소환된 몬스터 수
                int maxMonsters = currentStageWaveCount; // 현재 스테이지의 최대 몬스터 수
                gameManager.UpdateStageWaveUI(stageNum + 1, spawnedMonsters, maxMonsters);
            }
            
            // BottomBar UI 업데이트 (초기 적 수)
            if (bottomBarPanel != null)
            {
                bottomBarPanel.UpdateMonsterCountDisplay(0);
            }
            
            if (uiManager != null)
            {
                uiManager.UpdateStage(stageNum, GetStageCount());
            }

            yield return new WaitForSeconds(STAGE_START_DELAY);
            currentState = StageState.Run;
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + string.Format(LOG_MESSAGES[4], stageNum));
            #endif
        }

        private IEnumerator HandleRunState()
        {
            while (currentState == StageState.Run && !isQuitting)
            {
                currentTime += Time.deltaTime;

                // 동적으로 현재 스테이지의 웨이브 카운트 가져오기
                int currentStageWaveCount = GetStageWaveCount(stageNum);

                if (enemyCount < currentStageWaveCount && activeEnemyCountValue < MAX_ACTIVE_ENEMIES)
                {
                    if (currentTime >= createTime)
                    {
                        SpawnEnemy();
                        currentTime = 0f;
                    }
                }

                if (enemyCount >= currentStageWaveCount && activeEnemyCountValue <= 0)
                {
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + string.Format(LOG_MESSAGES[5], stageNum, enemyCount, currentStageWaveCount));
                    #endif
                    clearedStageIndex = stageNum;
                    currentState = StageState.End;
                    break; // 상태가 변경되었으므로 루프 종료
                }

                yield return null;
            }
        }

        /// <summary>
        /// 스테이지 종료 상태를 처리합니다.
        /// 스테이지 클리어 시 GameManager를 통해 진행 정보를 저장하고 다음 스테이지로 진행합니다.
        /// </summary>
        private IEnumerator HandleEndState()
        {
            // GameManager를 통해 스테이지 클리어 정보 저장
            if (gameManager != null)
            {
                // clearedStageIndex는 0-based이므로 1-based로 변환하여 전달
                gameManager.StageCleared(clearedStageIndex + 1);
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + $"스테이지 {clearedStageIndex + 1} 클리어 완료 - 진행 정보 저장됨");
                #endif
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogWarning(LOG_PREFIX + "GameManager를 찾을 수 없어 스테이지 클리어 정보를 저장할 수 없습니다.");
                #endif
            }

            // 다음 스테이지로 진행
            stageNum++;
            currentTime = 0f;

            // 모든 스테이지 완료 여부 확인
            if (stageNum >= GetStageCount())
            {
                // 모든 스테이지 완료 시 Over 상태로 변경하고 스테이지 진행 중단
                currentState = StageState.Over;
                
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "모든 스테이지 완료 - 스테이지 진행 종료");
                #endif
                
                yield break; // 코루틴 종료로 더 이상 진행하지 않음
            }
            else
            {
                // 다음 스테이지 대기 상태로 전환
                currentState = StageState.Wait;
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + $"다음 스테이지 {stageNum + 1} 대기 중...");
                #endif
            }

            // 스테이지 종료 대기 시간
            yield return new WaitForSeconds(STAGE_END_DELAY);
        }

        /// <summary>
        /// 스테이지 대기 상태를 처리합니다.
        /// 다음 스테이지 시작 전 대기 시간을 제공하고 UI를 업데이트합니다.
        /// </summary>
        private IEnumerator HandleWaitState()
        {
            // 다음 스테이지 시작 전 UI 업데이트 (GameManager를 통해)
            if (gameManager != null)
            {
                int maxMonsters = GetStageWaveCount(stageNum);
                gameManager.UpdateStageWaveUI(stageNum + 1, 0, maxMonsters); // 새 스테이지는 0부터 시작
            }
            
            // 다음 스테이지 시작 대기 시간
            yield return new WaitForSeconds(STAGE_START_DELAY);
            
            // 다음 스테이지 시작
            StartStageInternal(stageNum);
        }

        /// <summary>
        /// 현재 스테이지에서 적을 소환합니다.
        /// StageData에서 적 프리팹을 가져와서 소환하고, 스테이지 클리어 조건을 확인합니다.
        /// </summary>
        public void SpawnEnemy()
        {
            if (!isInitialized || currentState != StageState.Run) return;

            // GameManager 참조 확인 및 재참조
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
                if (gameManager == null)
                {
                    gameManager = FindObjectOfType<GameManager>();
                }
            }

            int currentStageWaveCount = GetStageWaveCount(stageNum);
            
            if (enemyCount < currentStageWaveCount && activeEnemyCountValue < MAX_ACTIVE_ENEMIES)
            {
                // 웨이포인트가 없는 경우 처리
                if (wayPointsList == null || wayPointsList.Count == 0)
                {
                    #if UNITY_EDITOR
                    Debug.LogWarning(LOG_PREFIX + "웨이포인트가 없어 적을 소환할 수 없습니다.");
                    #endif
                    return;
                }

                // StageData에서 에너미 프리팹 가져오기
                GameObject enemyPrefab = null;
                if (stageData != null)
                {
                    enemyPrefab = stageData.GetStageObject(stageNum, enemyCount);
                    // #if UNITY_EDITOR
                    // if (enemyPrefab != null)
                    // {
                    //     Debug.Log(LOG_PREFIX + $"StageData에서 에너미 프리팹을 가져왔습니다: {enemyPrefab.name} (스테이지: {stageNum}, 인덱스: {enemyCount})");
                    // }
                    // else
                    // {
                    //     Debug.LogWarning(LOG_PREFIX + $"StageData에서 에너미 프리팹을 찾을 수 없습니다. (스테이지: {stageNum}, 인덱스: {enemyCount})");
                    // }
                    // #endif
                }
                else
                {
                    #if UNITY_EDITOR
                    Debug.LogError(LOG_PREFIX + "StageData가 null입니다. 에너미 프리팹을 가져올 수 없습니다.");
                    #endif
                    return;
                }
                
                // null인 경우 처리
                if (enemyPrefab == null)
                {
                    #if UNITY_EDITOR
                    Debug.LogError(LOG_PREFIX + "StageData에서 에너미 프리팹을 찾을 수 없습니다. StageData를 확인해주세요.");
                    #endif
                    return;
                }

                // 적 스폰 로직
                Vector3 spawnPosition = wayPointsList[0].position;
                GameObject enemyObject = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
                EnemyObject enemy = enemyObject.GetComponent<EnemyObject>();
                
                if (enemy != null)
                {
                    // SetupEnemy 메서드 사용 (Initialize 대신)
                    enemy.SetupEnemy(stageNum, enemyCount);
                    activeEnemies.Add(enemy);
                    IncrementEnemyCount();
                }
                
                enemyCount++;
                
                if (enemyCount >= currentStageWaveCount && activeEnemyCountValue <= 0)
                {
                    currentState = StageState.End;
                }
                
                // #if UNITY_EDITOR
                // Debug.Log(LOG_PREFIX + string.Format(LOG_MESSAGES[5], stageNum, enemyCount, currentStageWaveCount));
                // #endif
            }
            
            // Wave 진행상황 UI 업데이트 (GameManager를 통해)
            if (gameManager != null)
            {
                int maxMonsters = GetStageWaveCount(stageNum);
                gameManager.UpdateStageWaveUI(stageNum + 1, enemyCount, maxMonsters);
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
            
            // Wave 정보를 포함한 UI 업데이트 (TopBar)
            UpdateWaveProgressUI();
            
            // Active Enemy 카운트 UI 업데이트 (BottomBar)
            if (bottomBarPanel != null)
            {
                bottomBarPanel.UpdateMonsterCountDisplay(activeEnemyCountValue);
            }
        }

        /// <summary>
        /// 적이 죽었을 때 호출됩니다.
        /// 활성 적 수를 감소시키고 eData를 추가합니다.
        /// </summary>
        /// <param name="eDataAmount">획득할 eData 양</param>
        public void OnEnemyDied(int eDataAmount)
        {
            if (!isInitialized) return;

            DecreaseActiveEnemyCount();
            
            // SaveDataManager 참조 확인 및 재참조 (Continue Game 후 안전장치)
            if (saveDataManager == null)
            {
                saveDataManager = SaveDataManager.Instance;
                if (saveDataManager == null)
                {
                    saveDataManager = FindObjectOfType<SaveDataManager>();
                }
            }
            
            // GameManager 참조 확인 및 재참조
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
                if (gameManager == null)
                {
                    gameManager = FindObjectOfType<GameManager>();
                }
            }
            
            // eData 업데이트는 GameManager를 통해 처리 (적 처치 시에는 저장하지 않음)
            if (gameManager != null)
            {
                gameManager.AddEData(eDataAmount, false); // 저장하지 않음
                // #if UNITY_EDITOR
                // Debug.Log(LOG_PREFIX + $"적 처치로 eData {eDataAmount} 획득 (총 eData: {gameManager.GetCurrentEData()})");
                // #endif
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + "GameManager를 찾을 수 없어 eData 업데이트를 건너뜁니다.");
                #endif
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

        private void OnDestroy()
        {
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + "StageManager 파괴 - 리소스 정리");
            #endif
            
            // 코루틴 정리
            StopStageCoroutine();
            
            // 적 오브젝트 정리
            CleanupActiveEnemies();
            
            // 싱글톤 인스턴스 정리 (자신이 메인 인스턴스인 경우에만)
            if (instance == this)
            {
                instance = null;
                isInitialized = false;
            }
        }

        private void OnDisable()
        {
            // 애플리케이션 종료 중이면 정리 과정 생략
            if (isQuitting) return;
            
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + "StageManager 비활성화 - 코루틴 정리");
            #endif
            
            // 씬 전환시 코루틴만 정리 (인스턴스는 유지)
            StopStageCoroutine();
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
            CleanupActiveEnemies();
            StopStageCoroutine();
        }

        private void StopStageCoroutine()
        {
            if (stageCoroutine != null)
            {
                StopCoroutine(stageCoroutine);
                stageCoroutine = null;
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "스테이지 코루틴 정리 완료");
                #endif
            }
        }

        public void IncrementEnemyCount()
        {
            activeEnemyCountValue++;
            
            // Wave 정보를 포함한 UI 업데이트 (TopBar)
            UpdateWaveProgressUI();
            
            // Active Enemy 카운트 UI 업데이트 (BottomBar)
            if (bottomBarPanel != null)
            {
                bottomBarPanel.UpdateMonsterCountDisplay(activeEnemyCountValue);
            }
        }

        public void DecrementEnemyCount()
        {
            activeEnemyCountValue = Mathf.Max(0, activeEnemyCountValue - 1);
            
            // Wave 정보를 포함한 UI 업데이트 (TopBar)
            UpdateWaveProgressUI();
            
            // Active Enemy 카운트 UI 업데이트 (BottomBar)
            if (bottomBarPanel != null)
            {
                bottomBarPanel.UpdateMonsterCountDisplay(activeEnemyCountValue);
            }
        }
        
        /// <summary>
        /// Wave 진행상황 UI를 업데이트합니다.
        /// GameManager를 통해 TopBar와 BottomBar의 UI를 동기화합니다.
        /// </summary>
        private void UpdateWaveProgressUI()
        {
            if (gameManager != null)
            {
                int maxMonsters = GetStageWaveCount(stageNum);
                gameManager.UpdateStageWaveUI(stageNum + 1, enemyCount, maxMonsters);
                
                // #if UNITY_EDITOR
                // Debug.Log(LOG_PREFIX + $"Wave UI 업데이트: 스테이지 {stageNum + 1}, 소환된 몬스터 {enemyCount}/{maxMonsters}");
                // #endif
            }
        }

        public int GetCurrentStageIndex()
        {
            return stageNum;
        }

        public int GetSpawnedEnemyCount()
        {
            return enemyCount;
        }
        
        // 웨이포인트를 수동으로 재설정하는 공개 함수
        public void RefreshWaypoints()
        {
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[7]);
            #endif
            AutoFindWaypoints();
        }

        private bool ValidateWaypoints()
        {
            // 웨이포인트가 설정되지 않은 경우
            if (wayPointsList == null || wayPointsList.Count == 0)
            {
                #if UNITY_EDITOR
                Debug.LogWarning(LOG_PREFIX + "웨이포인트가 설정되지 않아 자동 찾기를 시도합니다.");
                #endif
                AutoFindWaypoints();
                
                if (wayPointsList == null || wayPointsList.Count == 0)
                {
                    #if UNITY_EDITOR
                    Debug.LogError(LOG_PREFIX + "웨이포인트를 찾을 수 없습니다. Game 씬에 WayPoint1, WayPoint2 오브젝트가 있는지 확인하세요.");
                    #endif
                    return false;
                }
                return true;
            }

            // 웨이포인트가 파괴되었는지 검사 (첫 번째만 체크로 성능 최적화)
            if (wayPointsList[0] == null)
            {
                #if UNITY_EDITOR
                Debug.LogWarning(LOG_PREFIX + "웨이포인트가 파괴되어 재초기화합니다.");
                #endif
                AutoFindWaypoints();
                
                // 자동 찾기 후에도 웨이포인트가 없다면 false 반환
                if (wayPointsList == null || wayPointsList.Count == 0 || wayPointsList[0] == null)
                {
                    #if UNITY_EDITOR
                    Debug.LogError(LOG_PREFIX + "웨이포인트 자동 찾기에 실패했습니다. Game 씬에 WayPoint1, WayPoint2 오브젝트가 활성화되어 있는지 확인해주세요.");
                    #endif
                    return false;
                }
            }

            return true;
        }

        // 타워 등록/해제를 위한 최적화된 메서드들
        public void RegisterTower(Tower tower)
        {
            if (tower != null && !activeTowers.Contains(tower))
            {
                activeTowers.Add(tower);
            }
        }

        public void UnregisterTower(Tower tower)
        {
            if (tower != null)
            {
                activeTowers.Remove(tower);
            }
        }

        // 필요시에만 타워를 다시 찾는 메서드
        public void RefreshTowers()
        {
            activeTowers.Clear();
            var towers = FindObjectsOfType<Tower>();
            activeTowers.AddRange(towers);
        }
    }
} 