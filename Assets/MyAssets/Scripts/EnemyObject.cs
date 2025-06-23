using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using InvaderInsider.Data;
using InvaderInsider.Managers;
using InvaderInsider;
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
        public float moveSpeed = 3f;

        [Header("Rewards")]
        public int eDataAmount = 1;
        [SerializeField] public int damageOnFinalWaypoint = 10;
    }

    public class EnemyObject : BaseCharacter
    {
        // 성능 최적화 상수들
        private const float WAYPOINT_CHECK_INTERVAL = 0.1f; // 웨이포인트 체크 주기
        private const float DESTINATION_THRESHOLD = 0.5f; // 목적지 도달 임계값
        
        private const string LOG_PREFIX = "[Enemy] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "초기화 에러: GameObject가 null입니다.",
            "이동 중...",
            "웨이포인트 초기화 실패",
            "이동 대상: {0}",
            "다음 웨이포인트 설정: {0}"
        };

        // 메모리 할당 최적화 - StringBuilder 재사용
        private static readonly StringBuilder stringBuilder = new StringBuilder(256);
        
        [Header("Enemy Data")]
        [SerializeField] private EnemyData enemyData;
        
        [Header("Navigation")]
        private Transform currentWaypoint;
        private NavMeshAgent agent;
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
        private float moveSpeed = 5f;
        // private float nextWaypointCheckTime = 0f; // 사용되지 않으므로 주석 처리 // 웨이포인트 체크 최적화용

        protected override void Awake()
        {
            base.Awake();
            stageManager = StageManager.Instance;
            // Player 참조는 GameManager를 통해 가져오도록 변경
            agent = GetComponent<NavMeshAgent>();
            pathUpdateWait = new WaitForSeconds(0.1f);
        }

        protected override void Initialize()
        {
            if (isInitialized) return;

            base.Initialize();

            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
#if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[0]);
#endif
                return;
            }

            // 성능 최적화: 한 번만 설정
            agent.speed = moveSpeed;
            agent.stoppingDistance = 0.1f;
            agent.autoBraking = false;

            stageManager = StageManager.Instance;
            if (stageManager != null)
            {
                player = GameManager.Instance.GetComponent<Player>() ?? GameObject.FindWithTag("Player")?.GetComponent<Player>();
            }

            isInitialized = true;
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
                pathUpdateCoroutine = StartCoroutine(UpdatePathRoutine());
            }
        }

        private IEnumerator UpdatePathRoutine()
        {
            while (enabled)
            {
                UpdatePath();
                yield return pathUpdateWait;
            }
        }

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

            if (Vector3.Distance(transform.position, currentWaypoint.position) <= agent.stoppingDistance + 0.5f)
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
        }

        private void InitializeWaypoints()
        {
            waypoints.Clear();
            if (stageManager != null)
            {
                var stageWayPoints = stageManager.WayPoints;
                if (stageWayPoints != null)
                {
                    foreach (Transform waypoint in stageWayPoints)
                    {
                        waypoints.Enqueue(waypoint);
                    }
                    MoveToNextWaypoint();
                }
            }
            
            if (waypoints.Count == 0 && Application.isPlaying)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[2]);
#endif
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
            }
        }

        private void ReachFinalDestination()
        {
            if (!IsInitialized) return;

            if (player != null)
            {
                player.TakeDamage(enemyData.damageOnFinalWaypoint);
            }
            
            base.Die();
            
            if (stageManager != null)
            {
                stageManager.DecreaseActiveEnemyCount();
            }
        }

        protected override void Die()
        {
            if (!IsInitialized) return;

            base.Die();
            
            if (deathEffect != null)
            {
                ParticleSystem effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
                Destroy(effect.gameObject, effect.main.duration);
            }

            if (stageManager != null)
            {
                stageManager.OnEnemyDied(enemyData.eDataAmount);
            }
        }

        public override void Attack(IDamageable target)
        {
            if (!IsInitialized || target == null) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Application.isPlaying)
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[3], target));
            }
#endif
        }

        public override void TakeDamage(float damage)
        {
            if (!IsInitialized) return;

            float oldHealth = CurrentHealth;
            base.TakeDamage(damage);
        }

        public void SetupEnemy(int stageNum, int enemyCount)
        {
            if (stageManager == null) return;

            this.stageNum = stageNum;
            this.enemyCount = enemyCount;

            // 적의 체력을 최대체력으로 설정
            if (enemyData != null)
            {
                maxHealth = enemyData.baseHealth;
                currentHealth = maxHealth;
                attackDamage = enemyData.baseDamage;
                
                // 체력 변경 이벤트 호출하여 UI 업데이트
                InvokeHealthChanged();
            }
            
            // 적의 레이어와 태그 설정
            SetupEnemyLayerAndTag();

            var stageWayPoints = stageManager.WayPoints;
            if (stageWayPoints == null || stageWayPoints.Count == 0)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[2]);
#endif
                return;
            }

            // NavMeshAgent가 있는 경우 NavMesh 기반 이동 사용
            if (agent != null)
            {
                // 에이전트 설정
                agent.speed = enemyData?.moveSpeed ?? moveSpeed;
                agent.stoppingDistance = 0.1f;
                
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
                    transform.position = wayPoints[0].position;
                }
            }

            isInitialized = true;
            
            // NavMeshAgent가 있고 게임 오브젝트가 활성화된 상태라면 코루틴 시작
            if (agent != null && gameObject.activeInHierarchy)
            {
                StartPathUpdateCoroutine();
            }
        }
        
        private void SetupEnemyLayerAndTag()
        {
            // 적의 태그를 "Enemy"로 설정
            if (!gameObject.CompareTag("Enemy"))
            {
                gameObject.tag = "Enemy";
            }
            
            // 적의 레이어를 확인하고 설정
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1 && gameObject.layer != enemyLayer)
            {
                // Enemy 레이어가 존재하면 해당 레이어로 설정
                gameObject.layer = enemyLayer;
            }
            else if (enemyLayer == -1)
            {
                // Enemy 레이어가 없다면 6번 레이어를 기본값으로 사용
                gameObject.layer = 6;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (Application.isPlaying)
                {
                    Debug.LogWarning($"[Enemy] 'Enemy' 레이어가 존재하지 않음. 기본값 6번 레이어로 설정");
                }
#endif
            }
        }

        private void Start()
        {
            // SetupEnemy는 StageManager에서 SpawnEnemy 시 호출되므로 여기서는 중복 호출 방지
            if (!isInitialized && stageManager != null)
            {
                SetupEnemy(stageManager.GetCurrentStageIndex(), enemyCount);
            }
        }

        private void Update()
        {
            // NavMeshAgent가 있는 경우에는 UpdatePathRoutine에서 처리하므로 여기서는 건너뜀
            if (agent != null) return;
            
            // NavMeshAgent가 없는 경우에만 직접 Transform 이동 처리
            if (!isInitialized || wayPoints == null || wayPoints.Count == 0) return;

            if (currentWaypointIndex < wayPoints.Count)
            {
                Transform targetWaypoint = wayPoints[currentWaypointIndex];
                Vector3 direction = (targetWaypoint.position - transform.position).normalized;
                transform.position += direction * moveSpeed * Time.deltaTime;

                if (Vector3.Distance(transform.position, targetWaypoint.position) < 0.1f)
                {
                    currentWaypointIndex++;
                    if (currentWaypointIndex >= wayPoints.Count)
                    {
                        OnReachedEnd();
                    }
                }
            }
        }

        private void OnReachedEnd()
        {
            if (stageManager != null)
            {
                stageManager.DecrementEnemyCount();
            }
            
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Application.isPlaying)
            {
                Debug.Log($"[Enemy] 적이 목적지에 도달 - 스테이지: {stageNum}, 적 번호: {enemyCount}");
            }
#endif
            
            gameObject.SetActive(false);
        }
    }
} 