using System;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using InvaderInsider.Data;
using InvaderInsider.Managers;
using InvaderInsider.ScriptableObjects;
using InvaderInsider;
using InvaderInsider.Core;
using System.Linq;
using System.Text;

namespace InvaderInsider
{
    public enum EnemyType
    {
        Normal,
        Fast,
        Tank,
        Boss
    }

    [Serializable]
    public class EnemyData
    {
        [Header("Basic Info")]
        public string enemyName = "";
        public int enemyId = -1;
        public EnemyType enemyType = EnemyType.Normal;

        [Header("Base Stats")]
        public float baseHealth = 3f;
        public float baseDamage = 1f;
        public float moveSpeed = GameConstants.DEFAULT_MOVE_SPEED;

        [Header("Rewards")]
        public int eDataAmount = GameConstants.DEFAULT_EDATA_REWARD;
        [SerializeField] public int damageOnFinalWaypoint = GameConstants.DEFAULT_DAMAGE_ON_FINAL_WAYPOINT;
    }

    public class EnemyObject : BaseCharacter
    {
        #region Constants
        
        private const string LOG_PREFIX = "[Enemy] ";
        
        #endregion
        
        #region Static Members
        
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "초기화 에러: 필수 컴포넌트를 찾을 수 없습니다.",
            "이동 중...",
            "웨이포인트 초기화 실패: 스테이지 매니저의 웨이포인트가 없습니다.",
            "공격 대상: {0}",
            "다음 웨이포인트 설정: {0}"
        };

        // 메모리 할당 최적화 - StringBuilder 재사용
        private static readonly StringBuilder stringBuilder = new StringBuilder(GameConstants.STRING_BUILDER_CAPACITY);
        
        [Header("Enemy Data")]
        [SerializeField] private EnemyData enemyData;
        
        [Header("Navigation")]
        private Transform currentWaypoint;
        private UnityEngine.AI.NavMeshAgent agent;
        private readonly Queue<Transform> waypoints = new Queue<Transform>();
        
        [Header("Effects")]
        [SerializeField] private ParticleSystem hitEffect;
        [SerializeField] private ParticleSystem deathEffect;

        public event Action<EnemyObject> OnWaypointReached;
        
        private Player player;
        private StageManager stageManager;
        private Coroutine pathUpdateCoroutine;
        private WaitForSeconds pathUpdateWait;

        private List<Transform> wayPoints;
        private int currentWaypointIndex = 0;
        private int stageNum;
        private int enemyCount;
        private bool isInitialized = false;
        private float moveSpeed;
        
        // 이동 최적화를 위한 캐싱
        private Vector3 lastPosition;
        private float destinationThreshold;

        // 설정 참조
        private GameConfigSO enemyConfig;

        protected override void Awake()
        {
            base.Awake();
            
            // ConfigManager를 먼저 로드
            LoadConfig();
            
            // pathUpdateWait 초기화 (상수 사용)
            pathUpdateWait = new WaitForSeconds(GameConstants.PATH_UPDATE_INTERVAL);
            
            // 기본 이동 속도 설정
            if (enemyData != null)
            {
                moveSpeed = enemyData.moveSpeed;
            }
            else
            {
                moveSpeed = enemyConfig?.defaultMoveSpeed ?? GameConstants.DEFAULT_MOVE_SPEED;
            }
            
            // NavMeshAgent 컴포넌트 미리 캐싱
            agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            
            // StageManager 참조 미리 캐싱
            stageManager = StageManager.Instance;
            
            DebugUtils.LogFormat(GameConstants.LOG_PREFIX_ENEMY, 
                "{0}: Awake 완료 - Config: {1}, Agent: {2}, StageManager: {3}", 
                gameObject.name, 
                enemyConfig != null ? "로드됨" : "null",
                agent != null ? "찾음" : "null",
                stageManager != null ? "찾음" : "null");
        }

        private void LoadConfig()
        {
            var configManager = ConfigManager.Instance;
            if (configManager != null && configManager.GameConfig != null)
            {
                enemyConfig = configManager.GameConfig;
                DebugUtils.Log(GameConstants.LOG_PREFIX_ENEMY, 
                    $"{gameObject.name}: 설정을 성공적으로 로드했습니다.");
            }
            else
            {
                DebugUtils.LogWarning(GameConstants.LOG_PREFIX_ENEMY, 
                    $"{gameObject.name}: ConfigManager 또는 GameConfig를 찾을 수 없습니다. 기본값을 사용합니다.");
                // 기본값으로 폴백
                enemyConfig = ScriptableObject.CreateInstance<GameConfigSO>();
                moveSpeed = GameConstants.DEFAULT_MOVE_SPEED;
                
                // ConfigManager가 나중에 초기화되면 다시 시도
                RetryLoadConfig().Forget();
            }
        }
        
        private async UniTask RetryLoadConfig()
        {
            int retryCount = 0;
            
            while ((enemyConfig == null || stageManager == null) && retryCount < GameConstants.MAX_RETRY_ATTEMPTS)
            {
                yield return new WaitForSeconds(GameConstants.RETRY_INTERVAL);
                retryCount++;
                
                var configManager = ConfigManager.Instance;
                if (configManager != null && configManager.GameConfig != null)
                {
                    enemyConfig = configManager.GameConfig;
                    moveSpeed = enemyConfig.defaultMoveSpeed;
                    DebugUtils.LogFormat(GameConstants.LOG_PREFIX_ENEMY, 
                        "{0}: 재시도 후 설정을 성공적으로 로드했습니다. (시도 횟수: {1})", 
                        gameObject.name, retryCount);
                    break;
                }
            }
            
            if (enemyConfig == null)
            {
                DebugUtils.LogErrorFormat(GameConstants.LOG_PREFIX_ENEMY, 
                    "{0}: 최대 재시도 횟수({1})를 초과했습니다. 기본값을 계속 사용합니다.", 
                    gameObject.name, GameConstants.MAX_RETRY_ATTEMPTS);
            }
        }

        protected override void Initialize()
        {
            if (isInitialized) return;

            base.Initialize();

            // 컴포넌트 초기화
            InitializeComponents();
            
            // 매니저 참조 초기화
            InitializeManagers();

            isInitialized = true;
            DebugUtils.LogInitialization($"EnemyObject ({gameObject.name})", true);
        }

        private void InitializeComponents()
        {
            // NavMeshAgent 초기화
            if (agent == null)
            {
                agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
                DebugUtils.LogNullCheck(agent, "NavMeshAgent", "EnemyObject 초기화");
            }

            // NavMeshAgent 설정
            if (agent != null && enemyConfig != null)
            {
                agent.speed = moveSpeed;
                agent.stoppingDistance = enemyConfig.defaultStoppingDistance;
                agent.autoBraking = false;
            }
        }

        private void InitializeManagers()
        {
            // Player 참조 설정
            if (player == null)
            {
                player = FindPlayer();
                DebugUtils.LogNullCheck(player, "Player", "EnemyObject 초기화");
            }

            // StageManager 참조 설정
            if (stageManager == null)
            {
                stageManager = StageManager.Instance;
                DebugUtils.LogNullCheck(stageManager, "StageManager", "EnemyObject 초기화");
            }
        }

        private Player FindPlayer()
        {
            // GameManager를 통해 먼저 시도
            var gameManagerInstance = GameManager.Instance;
            if (gameManagerInstance != null)
            {
                var playerFromManager = gameManagerInstance.GetComponent<Player>();
                if (playerFromManager != null) return playerFromManager;
            }
            
            // 태그로 찾기
            GameObject playerObj = GameObject.FindWithTag(GameConstants.TAG_PLAYER);
            return playerObj?.GetComponent<Player>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!IsInitialized)
            {
                Initialize();
            }
            
            // 초기화가 완료된 후에만 코루틴 시작
            if (IsInitialized && agent != null)
            {
                StartPathUpdateCoroutine();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            StopPathUpdateCoroutine();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            OnWaypointReached = null;
            StopPathUpdateCoroutine();
        }

        private void StopPathUpdateCoroutine()
        {
            if (pathUpdateCoroutine != null)
            {
                StopCoroutine(pathUpdateCoroutine);
                pathUpdateCoroutine = null;
            }
        }

        private void StartPathUpdateCoroutine()
        {
            StopPathUpdateCoroutine(); // 기존 코루틴이 있다면 중지
            if (agent != null && isInitialized)
            {
                pathUpdateCoroutine = UpdatePathRoutine().Forget();
            }
        }

        private async UniTask UpdatePathRoutine()
        {
            while (enabled)
            {
                UpdatePath();
                yield return pathUpdateWait;
            }
        }

        /// <summary>
        /// 최적화된 경로 업데이트 - 불필요한 계산 최소화
        /// </summary>
        private void UpdatePath()
        {
            if (!IsInitialized || currentWaypoint == null || agent == null || !agent.isActiveAndEnabled) 
            {
                return;
            }

            // 목적지가 설정되지 않았거나 경로를 찾지 못한 경우 다시 설정
            if (!agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                agent.SetDestination(currentWaypoint.position);
            }

            // 거리 계산 최적화 - SqrMagnitude 사용
            Vector3 currentPosition = transform.position;
            float sqrDistanceToWaypoint = Vector3.SqrMagnitude(currentPosition - currentWaypoint.position);
            float sqrThreshold = (agent.stoppingDistance + destinationThreshold);
            sqrThreshold *= sqrThreshold; // 제곱으로 변환

            if (sqrDistanceToWaypoint <= sqrThreshold)
            {
                if (waypoints.Count == 0)
                {
                    ReachFinalDestination();
                }
                else
                {
                    OnWaypointReached?.Invoke(this);
                    MoveToNextWaypoint();
                }
            }
            
            // 위치 변화 감지 (필요한 경우에만)
            if (Vector3.SqrMagnitude(currentPosition - lastPosition) > 0.01f)
            {
                lastPosition = currentPosition;
            }
        }

        private void InitializeWaypoints()
        {
            waypoints.Clear();
            if (stageManager == null)
            {
                Debug.LogError($"{LOG_PREFIX}{gameObject.name}: StageManager가 null입니다.");
                return;
            }

            var stageWayPoints = stageManager.WayPoints;
            if (stageWayPoints != null && stageWayPoints.Count > 0)
            {
                foreach (Transform waypoint in stageWayPoints)
                {
                    if (waypoint == null)
                    {
                        Debug.LogWarning($"{LOG_PREFIX}{gameObject.name}: null 웨이포인트가 발견되었습니다.");
                        continue;
                    }
                    waypoints.Enqueue(waypoint);
                }
                MoveToNextWaypoint();
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX}{gameObject.name}: {LOG_MESSAGES[2]}");
            }
            
            if (waypoints.Count == 0 && Application.isPlaying)
            {
                Debug.LogError($"{LOG_PREFIX}{gameObject.name}: 유효한 웨이포인트가 없습니다.");
            }
        }

        private void MoveToNextWaypoint()
        {
            if (waypoints.Count == 0)
            {
                ReachFinalDestination();
                return;
            }

            currentWaypoint = waypoints.Dequeue();
            if (currentWaypoint != null && agent != null)
            {
                agent.SetDestination(currentWaypoint.position);
                Debug.Log($"{LOG_PREFIX}{gameObject.name}: {string.Format(LOG_MESSAGES[4], currentWaypoint.name)}");
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX}{gameObject.name}: 웨이포인트 또는 NavMeshAgent가 null입니다.");
            }
        }

        private void ReachFinalDestination()
        {
            if (!IsInitialized) return;

            if (player != null)
            {
                if (enemyData != null)
                {
                    player.TakeDamage(enemyData.damageOnFinalWaypoint);
                }
                else
                {
                    Debug.LogWarning($"{LOG_PREFIX}{gameObject.name}: EnemyData가 null입니다. 기본 데미지를 사용합니다.");
                    float defaultDamage = enemyConfig?.defaultEnemyDamage ?? 1f;
                    player.TakeDamage(defaultDamage);
                }
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX}{gameObject.name}: Player 참조가 null입니다.");
            }
            
            base.Die();
            
            if (stageManager != null)
            {
                stageManager.DecreaseActiveEnemyCount();
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX}{gameObject.name}: StageManager 참조가 null입니다.");
            }
        }

        protected override void Die()
        {
            if (!IsInitialized) return;

            base.Die();
            
            if (deathEffect != null)
            {
                ParticleSystem effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
                if (effect != null)
                {
                    Destroy(effect.gameObject, effect.main.duration);
                }
                else
                {
                    Debug.LogError($"{LOG_PREFIX}{gameObject.name}: 사망 이펙트 인스턴스 생성에 실패했습니다.");
                }
            }

            if (stageManager != null)
            {
                if (enemyData != null)
                {
                    stageManager.OnEnemyDied(enemyData.eDataAmount);
                }
                else
                {
                    Debug.LogWarning($"{LOG_PREFIX}{gameObject.name}: EnemyData가 null입니다. 기본 보상을 사용합니다.");
                    int defaultReward = enemyConfig?.defaultEnemyReward ?? 1;
                    stageManager.OnEnemyDied(defaultReward);
                }
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX}{gameObject.name}: StageManager 참조가 null입니다.");
            }
        }

        public override void Attack(IDamageable target)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning($"{LOG_PREFIX}{gameObject.name}: 초기화되지 않은 상태에서 공격을 시도했습니다.");
                return;
            }

            if (target == null)
            {
                Debug.LogError($"{LOG_PREFIX}{gameObject.name}: 공격 타겟이 null입니다.");
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Application.isPlaying)
            {
                Debug.Log($"{LOG_PREFIX}{gameObject.name}: {string.Format(LOG_MESSAGES[3], target)}");
            }
#endif
        }

        public override void TakeDamage(float damage)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning($"{LOG_PREFIX}{gameObject.name}: 초기화되지 않은 상태에서 데미지를 받았습니다.");
                return;
            }

            float oldHealth = CurrentHealth;
            base.TakeDamage(damage);
        }

        /// <summary>
        /// 안전하게 적의 초기 스탯을 설정합니다.
        /// BaseCharacter의 캡슐화를 보장하면서 적절한 초기화를 수행합니다.
        /// </summary>
        /// <param name="health">초기 체력</param>
        /// <param name="damage">초기 공격력</param>
        private void SetInitialStats(float health, float damage)
        {
            // BaseCharacter의 protected 필드에 안전하게 접근
            maxHealth = health;
            attackDamage = damage;
            
            // 체력을 최대치로 설정
            if (IsInitialized)
            {
                // 이미 초기화된 경우 Heal을 사용하여 체력을 최대치로
                Heal(maxHealth);
            }
            else
            {
                // 초기화 전이므로 직접 설정
                currentHealth = maxHealth;
            }
            
            // 체력 변경 이벤트 호출
            InvokeHealthChanged();
        }

        public void SetupEnemy(int stageNum, int enemyCount)
        {
            if (stageManager == null)
            {
                DebugUtils.LogError(GameConstants.LOG_PREFIX_ENEMY, 
                    $"{gameObject.name}: StageManager가 null입니다. 적 설정을 완료할 수 없습니다.");
                return;
            }

            this.stageNum = stageNum;
            this.enemyCount = enemyCount;

            // 적의 체력을 최대체력으로 설정 - BaseCharacter 시스템 사용
            if (enemyData != null)
            {
                // BaseCharacter의 protected 필드에 안전하게 접근
                SetInitialStats(enemyData.baseHealth, enemyData.baseDamage);
                
                DebugUtils.LogFormat(GameConstants.LOG_PREFIX_ENEMY, 
                    "{0} 적 초기화 완료 - 체력: {1}, 공격력: {2}", 
                    gameObject.name, MaxHealth, AttackDamage);
            }
            else
            {
                DebugUtils.LogError(GameConstants.LOG_PREFIX_ENEMY, 
                    $"{gameObject.name}: EnemyData가 설정되지 않았습니다.");
            }
            
            // 적의 레이어와 태그 설정
            SetupEnemyLayerAndTag();

            var stageWayPoints = stageManager.WayPoints;
            if (stageWayPoints == null || stageWayPoints.Count == 0)
            {
                Debug.LogError($"{LOG_PREFIX}{gameObject.name}: {LOG_MESSAGES[2]}");
                return;
            }

            // NavMeshAgent가 있는 경우 NavMesh 기반 이동 사용
            if (agent != null)
            {
                // 에이전트 설정
                agent.speed = enemyData?.moveSpeed ?? moveSpeed;
                agent.stoppingDistance = enemyConfig?.defaultStoppingDistance ?? 0.1f;
                
                // 웨이포인트 큐 초기화
                InitializeWaypoints();
            }
            else
            {
                // NavMeshAgent가 없는 경우 직접 Transform 이동 사용
                wayPoints = new List<Transform>(stageWayPoints);
                moveSpeed = enemyData?.moveSpeed ?? moveSpeed;
                currentWaypointIndex = 0;
                
                if (wayPoints.Count > 0)
                {
                    if (wayPoints[0] != null)
                    {
                        transform.position = wayPoints[0].position;
                    }
                    else
                    {
                        Debug.LogError($"{LOG_PREFIX}{gameObject.name}: 첫 번째 웨이포인트가 null입니다.");
                    }
                }
                else
                {
                    Debug.LogError($"{LOG_PREFIX}{gameObject.name}: 웨이포인트 리스트가 비어있습니다.");
                }
            }

            // 이동 최적화 파라미터 초기화
            destinationThreshold = enemyConfig?.destinationThreshold ?? GameConstants.DEFAULT_STOPPING_DISTANCE;
            lastPosition = transform.position;
            
            isInitialized = true;
            
            // NavMeshAgent가 있고 게임 오브젝트가 활성화된 상태라면 코루틴 시작
            if (agent != null && gameObject.activeInHierarchy)
            {
                StartPathUpdateCoroutine();
            }
        }
        
        private void SetupEnemyLayerAndTag()
        {
            // config가 null인 경우 안전한 기본값 사용
            if (enemyConfig == null)
            {
                Debug.LogWarning($"{LOG_PREFIX}{gameObject.name}: config가 null입니다. 기본 태그와 레이어를 사용합니다.");
                gameObject.tag = "Enemy";
                gameObject.layer = 8; // 기본 Enemy 레이어
                return;
            }

            // 적의 태그를 설정
            if (!gameObject.CompareTag(enemyConfig.enemyTag))
            {
                try
                {
                    gameObject.tag = enemyConfig.enemyTag;
                }
                catch (UnityException ex)
                {
                    Debug.LogError($"{LOG_PREFIX}{gameObject.name}: '{enemyConfig.enemyTag}' 태그 설정 실패 - {ex.Message}");
                }
            }
            
            // 적의 레이어를 확인하고 설정
            int enemyLayer = LayerMask.NameToLayer(enemyConfig.enemyLayerName);
            if (enemyLayer != -1 && gameObject.layer != enemyLayer)
            {
                // Enemy 레이어가 존재하면 해당 레이어로 설정
                gameObject.layer = enemyLayer;
            }
            else if (enemyLayer == -1)
            {
                // Enemy 레이어가 없다면 기본값 레이어를 사용
                gameObject.layer = enemyConfig.defaultEnemyLayerIndex;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (Application.isPlaying)
                {
                    Debug.LogWarning($"{LOG_PREFIX}{gameObject.name}: '{enemyConfig.enemyLayerName}' 레이어가 존재하지 않음. 기본값 {enemyConfig.defaultEnemyLayerIndex}번 레이어로 설정");
                }
#endif
            }
        }

        private void Start()
        {
            // SetupEnemy는 StageManager에서 SpawnEnemy 시 호출되므로 여기서는 중복 호출 방지
            if (!isInitialized)
            {
                Initialize(); // 모든 컴포넌트가 준비된 후 초기화
            }
            
            if (!isInitialized && stageManager != null)
            {
                SetupEnemy(stageManager.GetCurrentStageIndex(), enemyCount);
            }
        }

        /// <summary>
        /// 최적화된 Update - NavMeshAgent가 없는 경우의 Transform 기반 이동
        /// </summary>
        private void Update()
        {
            // NavMeshAgent가 있는 경우에는 UpdatePathRoutine에서 처리하므로 여기서는 건너뜀
            if (agent != null) return;
            
            // NavMeshAgent가 없는 경우에만 직접 Transform 이동 처리
            if (!isInitialized || wayPoints == null || wayPoints.Count == 0) return;

            if (currentWaypointIndex < wayPoints.Count)
            {
                Transform targetWaypoint = wayPoints[currentWaypointIndex];
                if (targetWaypoint == null)
                {
                    DebugUtils.LogError(GameConstants.LOG_PREFIX_ENEMY, 
                        $"{gameObject.name}: 웨이포인트 {currentWaypointIndex}가 null입니다.");
                    currentWaypointIndex++;
                    return;
                }

                // 이동 계산 최적화
                Vector3 currentPosition = transform.position;
                Vector3 targetPosition = targetWaypoint.position;
                Vector3 direction = (targetPosition - currentPosition);
                
                // 거리 체크 먼저 수행 (SqrMagnitude 사용)
                float waypointReachDistance = enemyConfig?.waypointReachDistance ?? GameConstants.DEFAULT_STOPPING_DISTANCE;
                float sqrReachDistance = waypointReachDistance * waypointReachDistance;
                float sqrDistanceToWaypoint = direction.sqrMagnitude;
                
                if (sqrDistanceToWaypoint <= sqrReachDistance)
                {
                    currentWaypointIndex++;
                    if (currentWaypointIndex >= wayPoints.Count)
                    {
                        OnReachedEnd();
                    }
                    return;
                }

                // 정규화 및 이동 (도달하지 않은 경우에만)
                direction.Normalize();
                transform.position = currentPosition + direction * moveSpeed * Time.deltaTime;
            }
        }

        private void OnReachedEnd()
        {
            if (stageManager != null)
            {
                stageManager.DecrementEnemyCount();
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX}{gameObject.name}: StageManager 참조가 null입니다.");
            }
            
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Application.isPlaying)
            {
                Debug.Log($"{LOG_PREFIX}{gameObject.name}: 적이 목적지에 도달 - 스테이지: {stageNum}, 적 번호: {enemyCount}");
            }
#endif
            
            gameObject.SetActive(false);
        }
        
        #endregion
    }
} 