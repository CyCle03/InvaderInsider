using UnityEngine;
using InvaderInsider.Data;
using InvaderInsider.Cards;
using InvaderInsider.Managers;
using InvaderInsider.ScriptableObjects;
using InvaderInsider.Core;
using System;
using System.Collections;
using System.Collections.Generic;

namespace InvaderInsider
{
    public class Tower : BaseCharacter
    {
        #region Constants
        
        private const string LOG_PREFIX = "[Tower] ";
        
        #endregion
        
        #region Inspector Fields
        
        [Header("Tower Specific")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Transform partToRotate; // 회전할 포탑 파트
        [SerializeField] private float towerAttackRange = GameConstants.TOWER_ATTACK_RANGE; // 타워 공격 사거리
        [SerializeField] private LayerMask enemyLayer = 1 << GameConstants.DEFAULT_ENEMY_LAYER_INDEX; // Enemy 레이어 마스크
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private AudioSource fireSound;
        [SerializeField] private ParticleSystem upgradeEffect;

        // 성능 최적화용 캐시된 변수들
        private EnemyObject currentTarget;
        private float targetLostDistance;
        private int enemyLayerMask = -1;
        
        // 메모리 할당 최적화
        private Collider[] detectionBuffer;
        private readonly List<EnemyObject> validTargets = new List<EnemyObject>();
        
        // 코루틴 관리
        private Coroutine targetSearchCoroutine;
        private WaitForSeconds targetSearchWait;
        
        // 이벤트 기반 타겟팅을 위한 적 추적
        private readonly HashSet<EnemyObject> nearbyEnemies = new HashSet<EnemyObject>();
        
        // 성능 캐싱
        private Vector3 cachedPosition;
        private bool isActivelySearching = false;

        // 설정 참조
        private GameConfigSO towerConfig;

        public override float AttackRange => towerAttackRange;

        public override EquipmentTargetType EquipmentTargetability => EquipmentTargetType.Tower;
        
        /// <summary>
        /// 투사체 프리팹 (ObjectPoolManager가 접근할 수 있도록 public 프로퍼티 제공)
        /// </summary>
        public GameObject ProjectilePrefab => projectilePrefab;

        protected override void Awake()
        {
            base.Awake();
            
            // 설정 로드
            LoadConfig();
            
            // 기본값 설정
            InitializeDefaults();
            
            // 성능 최적화 초기화
            InitializeOptimization();
            
            DebugUtils.LogInitialization($"Tower ({gameObject.name})", true);
        }
        
        private void InitializeDefaults()
        {
            if (partToRotate == null)
            {
                partToRotate = transform;
                DebugUtils.LogWarning(GameConstants.LOG_PREFIX_TOWER, 
                    $"{gameObject.name}: partToRotate가 설정되지 않음. transform을 기본값으로 사용합니다.");
            }
        }
        
        private void InitializeOptimization()
        {
            // Enemy 레이어 설정
            SetupEnemyLayer();
            
            // 레이어 마스크 캐싱
            string layerName = towerConfig?.enemyLayerName ?? GameConstants.ENEMY_LAYER_NAME;
            enemyLayerMask = LayerMask.GetMask(layerName);
            if (enemyLayerMask == 0)
            {
                DebugUtils.LogErrorFormat(GameConstants.LOG_PREFIX_TOWER, 
                    "{0}: '{1}' 레이어를 찾을 수 없습니다. 레이어 설정을 확인해주세요.", 
                    gameObject.name, layerName);
                enemyLayerMask = 1 << (towerConfig?.defaultEnemyLayerIndex ?? GameConstants.DEFAULT_ENEMY_LAYER_INDEX);
            }
            
            // 성능 최적화 파라미터 설정
            float multiplier = towerConfig?.targetLostDistanceMultiplier ?? GameConstants.TARGET_LOST_DISTANCE_MULTIPLIER;
            targetLostDistance = AttackRange * multiplier;
            
            // 버퍼 및 코루틴 대기시간 초기화
            int bufferSize = towerConfig?.maxDetectionColliders ?? GameConstants.MAX_DETECTION_COLLIDERS;
            detectionBuffer = new Collider[bufferSize];
            
            float searchInterval = towerConfig?.targetSearchInterval ?? GameConstants.TARGET_SEARCH_INTERVAL;
            targetSearchWait = new WaitForSeconds(searchInterval);
            
            // 위치 캐싱
            cachedPosition = transform.position;
        }
        
        private void LoadConfig()
        {
            var configManager = ConfigManager.Instance;
            if (configManager != null && configManager.GameConfig != null)
            {
                towerConfig = configManager.GameConfig;
            }
            else
            {
                DebugUtils.LogError(GameConstants.LOG_PREFIX_TOWER, 
                    $"{gameObject.name}: ConfigManager 또는 GameConfig를 찾을 수 없습니다. 기본값을 사용합니다.");
                // 기본값으로 폴백
                towerConfig = ScriptableObject.CreateInstance<GameConfigSO>();
            }
        }
        
        private void SetupEnemyLayer()
        {
            // Enemy 레이어 인덱스 확인
            string layerName = towerConfig?.enemyLayerName ?? GameConstants.ENEMY_LAYER_NAME;
            int enemyLayerIndex = LayerMask.NameToLayer(layerName);
            
            if (enemyLayerIndex != -1)
            {
                // Enemy 레이어가 존재하면 해당 레이어를 사용
                enemyLayer = 1 << enemyLayerIndex;
            }
            else
            {
                // Enemy 레이어가 없으면 기본값 사용
                int defaultLayer = towerConfig?.defaultEnemyLayerIndex ?? GameConstants.DEFAULT_ENEMY_LAYER_INDEX;
                enemyLayer = 1 << defaultLayer;
                DebugUtils.LogWarningFormat(GameConstants.LOG_PREFIX_TOWER, 
                    "{0}: '{1}' 레이어가 존재하지 않음. 기본값 {2}번 레이어 사용", 
                    gameObject.name, layerName, defaultLayer);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            StartTargetSearching();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            StopTargetSearching();
        }

        private void StartTargetSearching()
        {
            if (!isActivelySearching)
            {
                isActivelySearching = true;
                targetSearchCoroutine = StartCoroutine(TargetSearchRoutine());
            }
        }

        private void StopTargetSearching()
        {
            if (targetSearchCoroutine != null)
            {
                StopCoroutine(targetSearchCoroutine);
                targetSearchCoroutine = null;
            }
            isActivelySearching = false;
        }

        /// <summary>
        /// 최적화된 타겟 검색 코루틴 - 매 프레임 대신 설정된 간격으로 실행
        /// </summary>
        private IEnumerator TargetSearchRoutine()
        {
            while (isActivelySearching && gameObject.activeInHierarchy)
            {
                // 타겟 유효성 검사 및 검색
                if (currentTarget == null || !IsValidTarget(currentTarget))
                {
                    FindNewTarget();
                }
                
                // 타겟이 있고 공격 가능할 때 공격
                if (currentTarget != null && Time.time >= nextAttackTime)
                {
                    Attack(currentTarget);
                    RotateTowardsTarget();
                }
                
                yield return targetSearchWait;
            }
        }

        /// <summary>
        /// 성능 최적화된 Update - 필수적인 회전만 처리
        /// </summary>
        private void Update()
        {
            // 타겟이 있을 때만 회전 처리 (부드러운 회전을 위해 매 프레임 필요)
            if (currentTarget != null)
            {
                RotateTowardsTarget();
            }
            
            // 위치 변경 감지 (타워가 이동할 수 있는 경우에만)
            if (Vector3.SqrMagnitude(transform.position - cachedPosition) > 0.01f)
            {
                cachedPosition = transform.position;
                // 위치가 변경되면 즉시 새 타겟 검색
                if (isActivelySearching)
                {
                    FindNewTarget();
                }
            }
        }

        private bool IsValidTarget(EnemyObject target)
        {
            // null 체크 최적화
            if (target == null || target.gameObject == null || !target.gameObject.activeInHierarchy)
            {
                return false;
            }
            
            // 체력 체크 (먼저 확인하여 죽은 적 빠르게 제외)
            if (target.CurrentHealth <= 0)
            {
                return false;
            }
            
            // 거리 체크 최적화 (SqrMagnitude 사용으로 sqrt 연산 제거)
            float sqrDistanceToTarget = Vector3.SqrMagnitude(target.transform.position - cachedPosition);
            float sqrTargetLostDistance = targetLostDistance * targetLostDistance;
            
            return sqrDistanceToTarget <= sqrTargetLostDistance;
        }

        /// <summary>
        /// 최적화된 타겟 검색 - 이전 검색 결과 활용 및 물리 쿼리 최소화
        /// </summary>
        private void FindNewTarget()
        {
            currentTarget = null;
            validTargets.Clear();

            // 1단계: 이전에 감지된 근처 적들 중에서 먼저 확인
            if (nearbyEnemies.Count > 0)
            {
                foreach (var enemy in nearbyEnemies)
                {
                    if (IsValidTarget(enemy))
                    {
                        validTargets.Add(enemy);
                    }
                }
                
                // 이전 적들 중에서 유효한 타겟을 찾은 경우
                if (validTargets.Count > 0)
                {
                    currentTarget = GetNearestTarget(validTargets);
                    return;
                }
            }

            // 2단계: 물리 쿼리로 새로운 적 검색 (이전 방법이 실패한 경우에만)
            PerformPhysicsBasedSearch();
        }

        /// <summary>
        /// 물리 기반 적 탐지 (메모리 할당 최적화)
        /// </summary>
        private void PerformPhysicsBasedSearch()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                cachedPosition, 
                AttackRange, 
                detectionBuffer, 
                enemyLayerMask
            );

            // 근처 적 목록 업데이트
            nearbyEnemies.Clear();
            
            // 유효한 적들을 리스트에 추가
            for (int i = 0; i < hitCount; i++)
            {
                var collider = detectionBuffer[i];
                if (collider == null) continue;

                if (collider.TryGetComponent<EnemyObject>(out var enemy))
                {
                    nearbyEnemies.Add(enemy);
                    
                    if (IsValidTarget(enemy))
                    {
                        validTargets.Add(enemy);
                    }
                }
            }

            // 가장 가까운 적을 타겟으로 선택
            if (validTargets.Count > 0)
            {
                currentTarget = GetNearestTarget(validTargets);
            }
        }

        /// <summary>
        /// 최적화된 가장 가까운 타겟 검색 - SqrMagnitude 사용 및 조기 종료
        /// </summary>
        private EnemyObject GetNearestTarget(List<EnemyObject> targets)
        {
            if (targets == null || targets.Count == 0)
            {
                return null;
            }

            // 타겟이 하나인 경우 바로 반환
            if (targets.Count == 1)
            {
                return targets[0];
            }

            EnemyObject nearestEnemy = null;
            float nearestSqrDistance = float.MaxValue;
            
            foreach (var enemy in targets)
            {
                // null 체크 최소화
                if (enemy?.transform == null) continue;

                // SqrMagnitude 사용으로 sqrt 연산 제거
                float sqrDistance = Vector3.SqrMagnitude(enemy.transform.position - cachedPosition);
                
                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearestEnemy = enemy;
                    
                    // 매우 가까운 적이 있다면 조기 종료 (성능 최적화)
                    if (sqrDistance < 1f) // 1 unit 거리의 제곱
                    {
                        break;
                    }
                }
            }

            return nearestEnemy;
        }

        /// <summary>
        /// 최적화된 타겟 방향 회전 - null 체크 최소화 및 불필요한 계산 제거
        /// </summary>
        private void RotateTowardsTarget()
        {
            // null 체크 최소화 (이미 IsValidTarget에서 확인됨)
            if (currentTarget?.transform == null || partToRotate == null) return;

            Vector3 directionToTarget = currentTarget.transform.position - partToRotate.position;
            directionToTarget.y = 0; // Y축 회전만 고려
            
            // 벡터 크기가 매우 작으면 회전하지 않음 (성능 최적화)
            if (directionToTarget.sqrMagnitude < 0.01f) return;
            
            // Unity에서 forward 방향(Z+)을 기준으로 올바른 각도 계산
            float targetAngle = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
            float currentY = partToRotate.eulerAngles.y;
            
            // 회전 속도 적용 (config가 null인 경우 기본값 사용)
            float rotationSpeed = towerConfig?.towerRotationSpeed ?? 180f; // 기본 180도/초
            float newY = Mathf.LerpAngle(currentY, targetAngle, Time.deltaTime * rotationSpeed);
            
            // 각도 차이가 작으면 즉시 스냅 (미세한 떨림 방지)
            float angleDifference = Mathf.DeltaAngle(currentY, targetAngle);
            if (Mathf.Abs(angleDifference) < 1f)
            {
                newY = targetAngle;
            }
            
            partToRotate.rotation = Quaternion.Euler(0, newY, 0);
        }

        public override void Initialize(CardDBObject cardData)
        {
            base.Initialize(cardData);
            // 타워의 공격 범위나 투사체 프리팹 등은
            // 카드 데이터가 아닌 타워 프리팹 자체에 설정되어야 합니다.
            // 따라서 여기서는 추가적인 로직이 필요 없습니다.
        }

        public override void Attack(IDamageable target)
        {
            if (target == null || projectilePrefab == null) return;

            Transform targetTransform = GetTargetTransform(target);
            if (targetTransform == null) return;

            // 오브젝트 풀을 사용한 투사체 생성
            var projectile = CreateProjectileFromPool(targetTransform);
            if (projectile != null)
            {
                // 시각/오디오 효과
                PlayAttackEffects();
                
                // 다음 공격 시간 설정
                nextAttackTime = Time.time + attackRate;
            }
        }

        /// <summary>
        /// 타겟의 Transform 가져오기 (최적화된 버전)
        /// </summary>
        private Transform GetTargetTransform(IDamageable target)
        {
            // 가장 일반적인 케이스를 먼저 확인 (성능 최적화)
            if (target is EnemyObject enemyTarget)
            {
                return enemyTarget?.transform;
            }
            
            if (target is Component component)
            {
                return component?.transform;
            }

            return null;
        }

        //[SerializeField] private string projectilePoolName = "TowerProjectiles"; // 투사체 풀 이름

        /// <summary>
        /// 오브젝트 풀을 사용한 투사체 생성 (안전성 강화)
        /// </summary>
        private Projectile CreateProjectileFromPool(Transform targetTransform)
        {
            Vector3 spawnPosition = firePoint != null ? firePoint.position : cachedPosition;
            
            if (projectilePrefab == null)
            {
                DebugUtils.LogError(GameConstants.LOG_PREFIX_TOWER, $"{gameObject.name}: projectilePrefab이 설정되지 않았습니다!");
                return null;
            }

            var poolManager = ObjectPoolManager.Instance;
            if (poolManager == null)
            {
                DebugUtils.LogError(GameConstants.LOG_PREFIX_TOWER, "ObjectPoolManager 인스턴스를 찾을 수 없습니다.");
                return null;
            }

            Projectile projectile = poolManager.GetObject<Projectile>();

            if (projectile == null)
            {
                DebugUtils.LogError(GameConstants.LOG_PREFIX_TOWER, $"오브젝트 풀에서 투사체를 가져오지 못했습니다. ObjectPoolManager 설정을 확인하세요.");
                return null;
            }

            // 투사체 설정
            try
            {
                projectile.transform.position = spawnPosition;
                projectile.transform.rotation = Quaternion.identity;

                float projectileSpeed = towerConfig?.projectileSpeed ?? GameConstants.PROJECTILE_SPEED;
                projectile.Launch(currentTarget, AttackDamage, projectileSpeed);

                var pooledObject = projectile.GetComponent<PooledObject>();
                pooledObject?.OnObjectSpawned();
            }
            catch (System.Exception ex)
            {
                DebugUtils.LogError(GameConstants.LOG_PREFIX_TOWER, $"투사체 초기화 실패: {ex.Message}");
                projectile.GetComponent<PooledObject>()?.ReturnToPool();
                return null;
            }

            return projectile;
        }

        private void PlayAttackEffects()
        {
            // 머즐 플래시 효과
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }

            // 발사 사운드
            if (fireSound != null && !fireSound.isPlaying)
            {
                fireSound.Play();
            }
        }

        
        
        // 디버깅을 위한 Gizmo 그리기
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            // 사거리 표시
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // 반투명 초록색
            Gizmos.DrawWireSphere(transform.position, AttackRange);

            // 현재 타겟 표시
            if (currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentTarget.transform.position);
            }
        }

        public override void LevelUp()
        {
            base.LevelUp(); // 이미 Level++가 포함되어 있음
            
            // 타워 레벨업 시 추가 로직
            attackDamage *= 1.1f;
            maxHealth *= 1.1f;
            currentHealth = maxHealth;
            Debug.Log($"{gameObject.name} has leveled up! New Level: {Level}, New Attack: {attackDamage:F1}, New Max Health: {maxHealth:F1}");

            if (upgradeEffect != null)
            {
                upgradeEffect.Play();
            }
        }
        
        /// <summary>
        /// 강제로 타겟을 다시 검색합니다 (디버깅/수정용)
        /// </summary>
        public void ForceRetarget()
        {
            currentTarget = null;
            validTargets.Clear();
            nearbyEnemies.Clear();
            
            // 즉시 새 타겟 검색
            FindNewTarget();
            
            Debug.Log($"{LOG_PREFIX}{gameObject.name} 강제 타겟 재검색 완료 - 새 타겟: {(currentTarget != null ? currentTarget.name : "없음")}");
        }
        
        #endregion
    }
}
