using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using InvaderInsider.Data;
using InvaderInsider.Managers;
using InvaderInsider;

namespace InvaderInsider
{
    public enum EnemyType
    {
        Normal,
        Fast,
        Tank,
        Boss
    }

    [System.Serializable]
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
        public int eDataAmount = 1;  // 기본 eData 보상량
        [SerializeField] public int damageOnFinalWaypoint = 10; // 최종 WayPoint 도달 시 데미지 양
    }

    public class EnemyObject : BaseCharacter
    {
        [Header("Enemy Data")]
        [SerializeField] private EnemyData enemyData = new EnemyData();
        
        [Header("Navigation")]
        [SerializeField] private float pathUpdateRate = 0.2f;
        private Transform currentWaypoint;
        private NavMeshAgent agent;
        private Queue<Transform> waypoints = new Queue<Transform>();
        
        [Header("UI")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private GameObject healthBarObject;

        [Header("Effects")]
        [SerializeField] private ParticleSystem hitEffect;
        [SerializeField] private ParticleSystem deathEffect;

        // Events
        public event Action<EnemyObject> OnWaypointReached;
        
        // 주인공 참조
        private Player player;

        protected override void Start()
        {
            // BaseCharacter의 Start 호출 (체력 초기화 등)
            base.Start();
            
            // NavMeshAgent 컴포넌트 가져오기
            agent = GetComponent<NavMeshAgent>();
            
            // 적 초기화 (NavMeshAgent 속도 설정 등)
            InitializeEnemy();
            
            // StageManager에서 WayPoint 정보 가져오기 및 초기화
            if (StageManager.Instance != null)
            {
                InitializeWaypoints();
            }

            // 씬에서 Player 스크립트 찾기 (최초 1회)
            // 주: Player 스크립트가 InvaderInsider 네임스페이스에 없다면 using 처리가 필요할 수 있습니다.
            player = FindObjectOfType<Player>();
            if (player == null)
            {
                Debug.LogError("Player script not found in the scene!");
            }
            
            // 경로 업데이트 코루틴 시작
            StartCoroutine(UpdatePathRoutine());

            // 체력 UI 초기화 및 업데이트
            UpdateHealthUI();
        }

        private void InitializeEnemy()
        {
            if (agent != null)
            {
                agent.speed = enemyData.moveSpeed;
                agent.stoppingDistance = 0.2f;
            }
            // 체력 UI 업데이트 (BaseCharacter에 UpdateHealthUI가 있다면 호출)
            // UpdateHealthUI(); 
        }

        private System.Collections.IEnumerator UpdatePathRoutine()
        {
            // 스크립트가 활성화되어 있는 동안 반복
            while (enabled)
            {
                UpdatePath();
                yield return new WaitForSeconds(pathUpdateRate); // 설정된 시간 간격으로 업데이트
            }
        }

        private void UpdatePath()
        {
            // WayPoint나 NavMeshAgent가 유효하지 않으면 리턴
            if (currentWaypoint == null || agent == null || !agent.isActiveAndEnabled) return;

            // 현재 Waypoint에 거의 도달했는지 체크
            if (Vector3.Distance(transform.position, currentWaypoint.position) <= agent.stoppingDistance)
            {
                // 마지막 WayPoint인지 확인
                if (waypoints.Count == 0) // 더 이상 남은 WayPoint가 없으면 최종 목적지
                {
                    ReachFinalDestination();
                }
                else
                {
                    // 중간 WayPoint 도달 처리 (필요시 이벤트 발생 등)
                    OnWaypointReached?.Invoke(this);
                    MoveToNextWaypoint(); // 다음 WayPoint로 이동
                }
            }
            else // Waypoint에 아직 도달하지 않았으면 경로 업데이트
            {
                agent.SetDestination(currentWaypoint.position);
            }
        }

        private void InitializeWaypoints()
        {
            // Waypoints 큐 초기화
            waypoints.Clear();
            // StageManager에서 WayPoint 목록 가져와 큐에 추가
            if (StageManager.Instance != null && StageManager.Instance.wayPoints != null)
            {
                 foreach (Transform waypoint in StageManager.Instance.wayPoints)
                {
                    waypoints.Enqueue(waypoint);
                }
                // 첫 번째 WayPoint로 이동 시작
                MoveToNextWaypoint();
            }
            else
            {
                Debug.LogError("StageManager or Waypoints not found!");
            }
        }

        private void MoveToNextWaypoint()
        {
            // Waypoints 큐에 다음 WayPoint가 있는지 확인
            if (waypoints.Count > 0)
            {
                currentWaypoint = waypoints.Dequeue(); // 다음 WayPoint 가져오기
                 if (agent != null && agent.isActiveAndEnabled)
                {
                    agent.SetDestination(currentWaypoint.position); // NavMeshAgent 목적지 설정
                }
            }
            else
            {
                // 더 이상 WayPoint가 없으면 최종 목적지 도착 처리 (ReachFinalDestination에서 별도 처리)
                // 이 경우는 MoveToNextWaypoint가 Waypoints가 비어있을 때 호출된 상황
            }
        }

        private void ReachFinalDestination()
        {
            // 최종 목적지 (WayPoint2) 도달 시 주인공에게 데미지
            if (player != null)
            {
                player.TakeDamage(enemyData.damageOnFinalWaypoint); // 주인공 체력 감소
            }
            
            // 적 오브젝트 파괴 - 최종 목적지 도달 시 eData 지급 안 함
            // Die(); // EnemyObject의 Die() 대신 BaseCharacter의 Die() 호출
            base.Die(); 
            
            // 살아있는 적 수 감소
            if (StageManager.Instance != null)
            {
                StageManager.Instance.DecreaseActiveEnemyCount();
            }
        }

        protected override void Die()
        {
            if (deathEffect != null)
            {
                var effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
                effect.Play();
                Destroy(effect.gameObject, effect.main.duration);
            }

            // eData 보상 지급 (적 처치 시에만 호출됨)
            Debug.Log($"[EnemyObject] Enemy died: Type({enemyData.enemyType}), eData reward({enemyData.eDataAmount})");
            SaveDataManager.Instance.UpdateEData(enemyData.eDataAmount); // eData 값만 업데이트
            
            // 살아있는 적 수 감소
            if (StageManager.Instance != null)
            {
                StageManager.Instance.DecreaseActiveEnemyCount();
            }

            // Attack coroutine이 있다면 여기서 중지 필요

            base.Die();
        }

        // BaseCharacter의 Attack 추상 메서드 구현
        public override void Attack(IDamageable target)
        {
            // 여기에 적의 일반 공격 로직을 구현합니다.
            // 예를 들어, 일정 범위 내의 타겟에게 데미지를 주는 등의 코드가 들어갈 수 있습니다.
            // 현재는 최종 목적지 도달 시 데미지를 주는 로직으로 대체되었으므로,
            // 이 함수가 호출될 필요가 없다면 비워두거나 디버그 로그를 남길 수 있습니다.
            Debug.Log($"[EnemyObject] Attack method called on {target.ToString()}");
            // 예: target.TakeDamage(base.attackDamage);
        }

        public override void TakeDamage(float damage)
        {
            base.TakeDamage(damage);
            
            if (hitEffect != null)
            {
                hitEffect.Play();
            }
            
            // 체력 변경 후 UI 업데이트
            UpdateHealthUI();
        }

        // 체력 UI를 업데이트하는 함수
        private void UpdateHealthUI()
        {
            // Slider UI 업데이트
            if (healthSlider != null)
            {
                // BaseCharacter의 currentHealth와 maxHealth를 사용
                healthSlider.value = currentHealth / maxHealth; 
            }
            
            // 체력 바 오브젝트 활성화/비활성화 (체력이 최대 체력보다 작을 때 활성화)
            if (healthBarObject != null)
            {
                healthBarObject.SetActive(currentHealth < maxHealth);
            }
            
            // 필요하다면 TextMeshProUGUI 업데이트 로직도 추가 가능
            // if (healthText != null)
            // {
            //     healthText.text = currentHealth.ToString();
            // }
        }

        // 오브젝트 파괴 시 이벤트 구독 해지
        private void OnDestroy()
        {
            if (StageManager.Instance != null)
            {
                // WaypointReached 이벤트 구독 해지 (만약 MainMenuPanel 등에서 구독했다면)
                // StageManager.Instance.OnEnemyWaypointReached -= HandleEnemyWaypointReached; 
            }
        }
    }
} 