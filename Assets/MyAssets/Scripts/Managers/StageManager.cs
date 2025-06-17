using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InvaderInsider.Data;
using InvaderInsider.UI;
using InvaderInsider.Managers;
using InvaderInsider.Cards;
using System.Threading.Tasks;
using System;

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
            // Find operations을 한 번에 모아서 처리
            gameManager = gameManager ?? GameManager.Instance;
            bottomBarPanel = bottomBarPanel ?? FindObjectOfType<BottomBarPanel>();
            saveDataManager = saveDataManager ?? SaveDataManager.Instance;
            
            // TopBarPanel은 GameManager를 통해 접근하므로 직접 찾지 않음
            // readonly 필드이므로 이미 초기화되어 있음 - 새로운 할당 불가

            #if UNITY_EDITOR
            string gameManagerStatus = gameManager != null ? "찾음" : "없음";
            string bottomBarStatus = bottomBarPanel != null ? "찾음" : "없음";
            string saveDataStatus = saveDataManager != null ? "찾음" : "없음";
            Debug.Log($"[Stage] 컴포넌트 초기화 - GameManager: {gameManagerStatus}, BottomBarPanel: {bottomBarStatus}, SaveDataManager: {saveDataStatus}");
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
                Debug.LogError(string.Format(LOG_PREFIX + LOG_MESSAGES[0], stageIndex));
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

            // 게임 상태를 Loading으로 변경
            if (gameManager != null)
            {
                gameManager.SetGameState(GameState.Loading);
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
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + string.Format(LOG_MESSAGES[1], stageNum + 1, stageWave));
            #endif
            
            // TopBar UI 업데이트 (현재/최대 형식) - GameManager를 통해
            if (gameManager != null)
            {
                int spawnedMonsters = enemyCount; // 현재 소환된 몬스터 수
                int maxMonsters = stageWave;      // 현재 스테이지의 최대 몬스터 수
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

            // 스테이지가 준비되었으므로 게임 상태를 Playing으로 변경
            if (gameManager != null)
            {
                gameManager.SetGameState(GameState.Playing);
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
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + string.Format(LOG_MESSAGES[5], stageNum, enemyCount, stageWave));
                    #endif
                    clearedStageIndex = stageNum;
                    currentState = StageState.End;
                    break; // 상태가 변경되었으므로 루프 종료
                }

                yield return null;
            }
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
                // 모든 스테이지 완료 시 Over 상태로 변경하고 스테이지 진행 중단
                currentState = StageState.Over;
                
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "모든 스테이지 완료 - 스테이지 진행 종료");
                #endif
                
                yield break; // 코루틴 종료로 더 이상 진행하지 않음
            }
            else
            {
                currentState = StageState.Wait;
            }

            yield return new WaitForSeconds(STAGE_END_DELAY);
        }

        private IEnumerator HandleWaitState()
        {
            // 다음 스테이지 시작 전 UI 업데이트 (GameManager를 통해)
            if (gameManager != null)
            {
                int maxMonsters = GetStageWaveCount(stageNum);
                gameManager.UpdateStageWaveUI(stageNum + 1, 0, maxMonsters); // 새 스테이지는 0부터 시작
            }
            
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
            if (!ValidateWaypoints())
            {
                #if UNITY_EDITOR
                Debug.LogWarning(LOG_PREFIX + "웨이포인트 검증에 실패했지만 기본 위치에서 적을 소환합니다.");
                #endif
                
                // 웨이포인트가 없어도 기본 위치에서 적 소환 시도
                // 완전히 스테이지를 중단하지 않고 몇 마리라도 소환해서 플레이어가 게임을 진행할 수 있도록 함
            }

            // StageData에서 적 프리팹 가져오기
            GameObject enemyPrefab = null;
            int currentEnemyIndex = enemyCount;
            
            if (stageData != null)
            {
                int waveCount = stageData.GetStageWaveCount(stageNum);
                if (waveCount > 0)
                {
                    int enemyIndex = currentEnemyIndex % waveCount;
                    enemyPrefab = stageData.GetStageObject(stageNum, enemyIndex);
                }
            }
            
            if (enemyPrefab == null)
            {
                enemyPrefab = defaultEnemyPrefab;
            }
            
            if (enemyPrefab == null)
            {
                Debug.LogError(LOG_PREFIX + string.Format(LOG_MESSAGES[6], stageNum));
                return;
            }

            // 웨이포인트가 있으면 첫 번째 웨이포인트 위치, 없으면 기본 위치 사용
            Vector3 spawnPosition = Vector3.zero;
            if (wayPointsList != null && wayPointsList.Count > 0 && wayPointsList[0] != null)
            {
                spawnPosition = wayPointsList[0].position;
            }
            else
            {
                // 기본 스폰 위치 (원점 또는 적절한 기본 위치)
                spawnPosition = new Vector3(-10f, 0f, 0f);
                #if UNITY_EDITOR
                Debug.LogWarning(LOG_PREFIX + $"웨이포인트가 없어 기본 위치 {spawnPosition}에서 적을 소환합니다.");
                #endif
            }
            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            
            enemyCount++;
            
            EnemyObject enemyObject = enemy.GetComponent<EnemyObject>();
            if (enemyObject != null)
            {
                enemyObject.SetupEnemy(stageNum, currentEnemyIndex);
                activeEnemies.Add(enemyObject);
                IncrementEnemyCount();
            }
            else
            {
                IncrementEnemyCount();
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
        }

        public void OnEnemyDied(int eDataAmount)
        {
            if (!isInitialized) return;

            DecreaseActiveEnemyCount();
            
            // Active Enemy 카운트 UI 업데이트
            if (bottomBarPanel != null)
            {
                bottomBarPanel.UpdateMonsterCountDisplay(activeEnemyCountValue);
            }
            
            // SaveDataManager 참조 확인 및 재참조 (Continue Game 후 안전장치)
            if (saveDataManager == null)
            {
                saveDataManager = SaveDataManager.Instance;
                if (saveDataManager == null)
                {
                    saveDataManager = FindObjectOfType<SaveDataManager>();
                }
            }
            
            // eData 업데이트는 GameManager를 통해 처리
            if (gameManager != null)
            {
                gameManager.AddEData(eDataAmount);
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
            
            // Active Enemy 카운트 UI 업데이트
            if (bottomBarPanel != null)
            {
                bottomBarPanel.UpdateMonsterCountDisplay(activeEnemyCountValue);
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
            
            // Active Enemy 카운트 UI 업데이트
            if (bottomBarPanel != null)
            {
                bottomBarPanel.UpdateMonsterCountDisplay(activeEnemyCountValue);
            }
        }

        public void DecrementEnemyCount()
        {
            activeEnemyCountValue = Mathf.Max(0, activeEnemyCountValue - 1);
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