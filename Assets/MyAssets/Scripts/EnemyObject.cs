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
        // [SerializeField] private Slider healthSlider;
        // [SerializeField] private GameObject healthBarObject;

        [Header("Effects")]
        [SerializeField] private ParticleSystem hitEffect;
        [SerializeField] private ParticleSystem deathEffect;

        // Events
        public event Action<EnemyObject> OnWaypointReached;
        
        // 주인공 및 매니저 참조 캐싱
        private Player _player;
        private StageManager _stageManager;

        protected override void Start()
        {
            // BaseCharacter의 Start 호출 (체력 초기화 등)
            base.Start();
            
            // NavMeshAgent 컴포넌트 가져오기
            agent = GetComponent<NavMeshAgent>();
            
            // 적 초기화 (NavMeshAgent 속도 설정 등)
            InitializeEnemy();
            
            // StageManager 인스턴스 캐싱
            _stageManager = StageManager.Instance;
            if (_stageManager == null)
            {
                Debug.LogError("StageManager 인스턴스를 찾을 수 없습니다.");
            }

            // WayPoint 정보 가져오기 및 초기화
            InitializeWaypoints();
            
            // 씬에서 Player 스크립트 찾기 (최초 1회 캐싱)
            _player = FindObjectOfType<Player>();
            if (_player == null)
            {
                Debug.LogError("Player script not found in the scene!");
            }
            
            // 경로 업데이트 코루틴 시작
            StartCoroutine(UpdatePathRoutine());

            // 체력 UI 초기화 및 업데이트는 BaseCharacter의 OnHealthChanged 이벤트로 처리
            // UpdateHealthUI(); // 이 함수는 이제 사용하지 않습니다.
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
            if (_stageManager != null && _stageManager.wayPoints != null)
            {
                 foreach (Transform waypoint in _stageManager.wayPoints)
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
            if (_player != null)
            {
                _player.TakeDamage(enemyData.damageOnFinalWaypoint); // 주인공 체력 감소
            }
            
            // 적 오브젝트 파괴 - 최종 목적지 도달 시 eData 지급 안 함
            // Die(); // EnemyObject의 Die() 대신 BaseCharacter의 Die() 호출
            base.Die(); 
            
            // 살아있는 적 수 감소
            if (_stageManager != null)
            {
                _stageManager.DecreaseActiveEnemyCount();
            }
        }

        protected override void Die()
        {
            base.Die(); // BaseCharacter의 Die() 호출 (OnDeath 이벤트 발생 및 오브젝트 파괴)
            
            // TODO: 사망 효과 재생 또는 아이템 드랍 등 추가 로직
            if (deathEffect != null)
            {
                ParticleSystem effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
                Destroy(effect.gameObject, effect.main.duration);
            }

            // StageManager에 적이 죽었음을 알림 (보상 지급 등)
            if (_stageManager != null)
            {
                _stageManager.OnEnemyDied(enemyData.eDataAmount);
            }
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

        // 데미지를 입었을 때 처리 (BaseCharacter에서 이미 구현됨)
        public override void TakeDamage(float damage)
        {
            base.TakeDamage(damage); // BaseCharacter의 TakeDamage 호출

            if (hitEffect != null)
            {
                ParticleSystem effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
                Destroy(effect.gameObject, effect.main.duration);
            }

            // 체력 UI 업데이트는 OnHealthChanged 이벤트를 구독하는 별도의 스크립트에서 처리
            // UpdateHealthUI(); 
        }

        // private void UpdateHealthUI()
        // {
        //     if (healthSlider != null)
        //     {
        //         healthSlider.maxValue = MaxHealth;
        //         healthSlider.value = CurrentHealth;
        //         healthBarObject.SetActive(CurrentHealth < MaxHealth); // 체력이 줄어들 때만 체력바 표시
        //     }
        //     else
        //     {
        //         // Debug.LogWarning("Health Slider not assigned for " + gameObject.name);
        //     }
        // }

        private void OnDestroy()
        {
            // TODO: 필요한 경우 구독 해지
            // StageManager에서 적 리스트에서 제거하는 로직은 Die()에서 처리됨.
        }
    }
} 