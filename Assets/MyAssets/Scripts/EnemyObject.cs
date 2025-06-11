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
        private const string LOG_PREFIX = "[Enemy] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "StageManager not found in the scene!",
            "Player not found in the scene!",
            "No waypoints available for enemy path!",
            "Enemy {0} attacked",
            "Enemy {0} died",
            "Enemy {0} reached end"
        };

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

        protected override void Awake()
        {
            base.Awake();
            stageManager = StageManager.Instance;
            player = FindObjectOfType<Player>();
            pathUpdateWait = new WaitForSeconds(0.1f);
        }

        protected override void Initialize()
        {
            base.Initialize();
            if (stageManager == null)
            {
                stageManager = StageManager.Instance;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!IsInitialized)
            {
                Initialize();
            }
            pathUpdateCoroutine = StartCoroutine(UpdatePathRoutine());
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
            if (!IsInitialized || currentWaypoint == null || agent == null || !agent.isActiveAndEnabled) return;

            if (Vector3.Distance(transform.position, currentWaypoint.position) <= agent.stoppingDistance)
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
            else
            {
                agent.SetDestination(currentWaypoint.position);
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
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[2]);
            }
        }

        private void MoveToNextWaypoint()
        {
            if (!IsInitialized || waypoints.Count == 0) return;

            currentWaypoint = waypoints.Dequeue();
            if (agent != null && agent.isActiveAndEnabled)
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

            if (Application.isPlaying)
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[3], target));
            }
        }

        public override void TakeDamage(float damage)
        {
            if (!IsInitialized) return;

            base.TakeDamage(damage);

            if (hitEffect != null)
            {
                ParticleSystem effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
                Destroy(effect.gameObject, effect.main.duration);
            }
        }

        public void SetupEnemy(int stageNum, int enemyCount)
        {
            if (stageManager == null) return;

            var stageWayPoints = stageManager.WayPoints;
            if (stageWayPoints != null)
            {
                wayPoints = new List<Transform>(stageWayPoints);
            }
            else
            {
                wayPoints = new List<Transform>();
            }

            this.stageNum = stageNum;
            this.enemyCount = enemyCount;
            isInitialized = true;

            if (wayPoints.Count > 0)
            {
                transform.position = wayPoints[0].position;
            }
        }

        private void Start()
        {
            if (stageManager == null) return;
            SetupEnemy(stageManager.GetCurrentStageIndex(), enemyCount);
        }

        private void Update()
        {
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
            
            if (Application.isPlaying)
            {
                Debug.Log($"[Enemy] 적이 목적지에 도달 - 스테이지: {stageNum}, 적 번호: {enemyCount}");
            }
            
            gameObject.SetActive(false);
        }
    }
} 