using UnityEngine;
using InvaderInsider.Managers;
using InvaderInsider.Core;

namespace InvaderInsider
{
    /// <summary>
    /// 투사체 클래스 - 타워에서 발사되어 적을 추적하고 데미지를 입힙니다.
    /// 오브젝트 풀링 시스템과 호환되도록 설계되었습니다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Projectile : MonoBehaviour
    {
        #region Constants & Enums
        
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "투사체 {0}이(가) 타겟 {1}에게 {2} 데미지를 입혔습니다",
            "투사체 {0}이(가) 타겟 손실로 인해 제거됩니다",
            "투사체 {0} 초기화 완료 - 타겟: {1}, 데미지: {2}, 속도: {3}",
            "투사체 {0}이(가) 생명주기 만료로 제거됩니다"
        };

        public enum ProjectileState
        {
            Inactive,
            Tracking,
            Hit,
            Expired
        }
        
        #endregion

        #region Inspector Fields
        
        [Header("Projectile Settings")]
        [SerializeField] private float defaultSpeed = GameConstants.PROJECTILE_SPEED;
        [SerializeField] private float defaultLifeTime = GameConstants.OBJECT_AUTO_RETURN_TIME;
        [SerializeField] private float hitDistance = 0.5f;
        [SerializeField] private bool usePhysicsMovement = false;
        
        [Header("Visual Effects")]
        [SerializeField] private GameObject hitEffect;
        [SerializeField] private TrailRenderer trail;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        #endregion

        #region Runtime State
        
        private ProjectileState currentState = ProjectileState.Inactive;
        private IDamageable targetDamageable;
        private Transform targetTransform;
        private float speed;
        private float damage;
        private float lifeTime;
        private bool isInitialized = false;
        
        // 성능 최적화를 위한 캐싱
        private Rigidbody rb;
        private Collider col;
        private PooledObject pooledObject;
        
        // 이동 최적화
        private Vector3 lastTargetPosition;
        private float targetCheckInterval = 0.1f;
        private float nextTargetCheckTime = 0f;
        
        #endregion

        #region Events
        
        /// <summary>타겟에 명중했을 때 발생하는 이벤트</summary>
        public System.Action<Projectile, IDamageable, float> OnTargetHit;
        
        /// <summary>투사체가 만료되어 제거될 때 발생하는 이벤트</summary>
        public System.Action<Projectile> OnProjectileExpired;
        
        #endregion

        #region Properties
        
        /// <summary>현재 투사체 상태</summary>
        public ProjectileState State => currentState;
        
        /// <summary>현재 타겟</summary>
        public IDamageable Target => targetDamageable;
        
        /// <summary>현재 데미지</summary>
        public float Damage => damage;
        
        /// <summary>현재 속도</summary>
        public float Speed => speed;
        
        /// <summary>남은 생명주기</summary>
        public float RemainingLifeTime => lifeTime;
        
        #endregion

        #region Unity Lifecycle
        
        /// <summary>
        /// Unity Awake - 컴포넌트 초기화
        /// </summary>
        private void Awake()
        {
            InitializeComponents();
        }

        /// <summary>
        /// Unity OnEnable - 오브젝트 풀에서 활성화될 때 호출
        /// </summary>
        private void OnEnable()
        {
            if (!isInitialized)
            {
                InitializeComponents();
            }
        }

        /// <summary>
        /// Unity Update - 투사체 이동 및 상태 업데이트
        /// </summary>
        private void Update()
        {
            if (currentState != ProjectileState.Tracking)
            {
                return;
            }

            UpdateLifeTime();
            UpdateTargetTracking();
            UpdateMovement();
        }

        /// <summary>
        /// Unity OnTriggerEnter - 충돌 감지
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (currentState != ProjectileState.Tracking)
            {
                return;
            }

            HandleCollision(other);
        }

        /// <summary>
        /// Unity OnDisable - 비활성화 시 정리
        /// </summary>
        private void OnDisable()
        {
            CleanupProjectile();
        }
        
        #endregion

        #region Initialization
        
        /// <summary>
        /// 컴포넌트들을 초기화합니다.
        /// </summary>
        private void InitializeComponents()
        {
            if (isInitialized) 
            {
                return;
            }

            // 컴포넌트 캐싱
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
            pooledObject = GetComponent<PooledObject>();

            // Rigidbody 설정
            if (rb != null)
            {
                rb.isKinematic = !usePhysicsMovement;
                rb.useGravity = false;
            }

            // Collider 설정
            if (col != null)
            {
                col.isTrigger = true;
            }

            isInitialized = true;
            
            if (showDebugInfo)
            {
                LogManager.Info(GameConstants.LOG_PREFIX_GAME, 
                    $"투사체 컴포넌트 초기화 완료: {gameObject.name}");
            }
        }

        /// <summary>
        /// 투사체를 초기화하고 발사합니다. (오브젝트 풀링 호환)
        /// </summary>
        /// <param name="target">공격할 대상</param>
        /// <param name="projectileDamage">투사체 데미지</param>
        /// <param name="projectileSpeed">투사체 속도 (0이면 기본값 사용)</param>
        public void Launch(IDamageable target, float projectileDamage, float projectileSpeed = 0f)
        {
            if (!isInitialized)
            {
                InitializeComponents();
            }

            // 파라미터 검증
            if (target == null)
            {
                LogManager.Error(GameConstants.LOG_PREFIX_GAME, 
                    "투사체 발사 실패: 타겟이 null입니다");
                ReturnToPool();
                return;
            }

            // 투사체 설정
            SetupProjectile(target, projectileDamage, projectileSpeed);
            
            // 상태 변경
            currentState = ProjectileState.Tracking;
            
            // 초기 방향 설정
            SetInitialDirection();
            
            if (showDebugInfo)
            {
                string targetName = GetTargetName(target);
                LogManager.Info(GameConstants.LOG_PREFIX_GAME, 
                    $"투사체 {gameObject.name} 초기화 완료 - 타겟: {targetName}, 데미지: {damage}, 속도: {speed}");
            }
        }

        /// <summary>
        /// 투사체 설정을 적용합니다.
        /// </summary>
        private void SetupProjectile(IDamageable target, float projectileDamage, float projectileSpeed)
        {
            targetDamageable = target;
            damage = projectileDamage;
            speed = projectileSpeed > 0f ? projectileSpeed : defaultSpeed;
            lifeTime = defaultLifeTime;
            
            // Transform 가져오기
            targetTransform = GetTargetTransform(target);
            if (targetTransform != null)
            {
                lastTargetPosition = targetTransform.position;
            }
            
            // Trail 초기화
            if (trail != null)
            {
                trail.Clear();
                trail.enabled = true;
            }
        }

        /// <summary>
        /// 타겟으로부터 Transform을 가져옵니다.
        /// </summary>
        private Transform GetTargetTransform(IDamageable target)
        {
            return target switch
            {
                MonoBehaviour monoBehaviour => monoBehaviour.transform,
                Component component => component.transform,
                _ => null
            };
        }

        /// <summary>
        /// 타겟의 이름을 가져옵니다.
        /// </summary>
        private string GetTargetName(IDamageable target)
        {
            var transform = GetTargetTransform(target);
            return transform != null ? transform.name : "Unknown";
        }

        /// <summary>
        /// 초기 방향을 설정합니다.
        /// </summary>
        private void SetInitialDirection()
        {
            if (targetTransform != null)
            {
                Vector3 direction = (targetTransform.position - transform.position).normalized;
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
        
        #endregion

        #region Movement & Tracking

        /// <summary>
        /// 생명주기를 업데이트합니다.
        /// </summary>
        private void UpdateLifeTime()
        {
            lifeTime -= Time.deltaTime;
            if (lifeTime <= 0f)
            {
                ExpireProjectile();
            }
        }

        /// <summary>
        /// 타겟 추적을 업데이트합니다.
        /// </summary>
        private void UpdateTargetTracking()
        {
            // 주기적으로 타겟 위치 확인 (성능 최적화)
            if (Time.time >= nextTargetCheckTime)
            {
                nextTargetCheckTime = Time.time + targetCheckInterval;
                
                if (!IsTargetValid())
                {
                    LoseTarget();
                    return;
                }
                
                lastTargetPosition = targetTransform.position;
            }
        }

        /// <summary>
        /// 투사체 이동을 업데이트합니다.
        /// </summary>
        private void UpdateMovement()
        {
            if (targetTransform == null)
            {
                return;
            }

            Vector3 direction = (lastTargetPosition - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, lastTargetPosition);

            // 타겟에 근접했는지 확인
            if (distance <= hitDistance)
            {
                HitTarget();
                return;
            }

            // 이동 방식 선택
            if (usePhysicsMovement && rb != null && !rb.isKinematic)
            {
                MoveWithPhysics(direction);
            }
            else
            {
                MoveWithTransform(direction);
            }

            // 회전 업데이트
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        /// <summary>
        /// Transform을 사용한 이동
        /// </summary>
        private void MoveWithTransform(Vector3 direction)
        {
            transform.position += direction * speed * Time.deltaTime;
        }

        /// <summary>
        /// Physics를 사용한 이동
        /// </summary>
        private void MoveWithPhysics(Vector3 direction)
        {
            rb.velocity = direction * speed;
        }

        /// <summary>
        /// 타겟이 유효한지 확인합니다.
        /// </summary>
        private bool IsTargetValid()
        {
            return targetDamageable != null && 
                   targetTransform != null && 
                   targetTransform.gameObject.activeInHierarchy;
        }
        
        #endregion

        #region Hit & Collision System

        /// <summary>
        /// 충돌을 처리합니다.
        /// </summary>
        private void HandleCollision(Collider other)
        {
            // 타겟과의 충돌인지 확인
            if (IsTargetCollision(other))
            {
                HitTarget();
                return;
            }

            // 적 태그를 가진 오브젝트와의 충돌 처리
            if (other.CompareTag(GameConstants.TAG_ENEMY))
            {
                IDamageable damageable = other.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    HitAlternativeTarget(damageable);
                }
            }
        }

        /// <summary>
        /// 타겟과의 충돌인지 확인합니다.
        /// </summary>
        private bool IsTargetCollision(Collider other)
        {
            if (targetTransform == null)
            {
                return false;
            }

            return other.transform == targetTransform || 
                   other.transform.IsChildOf(targetTransform) ||
                   targetTransform.IsChildOf(other.transform);
        }

        /// <summary>
        /// 타겟에 명중했을 때의 처리
        /// </summary>
        private void HitTarget()
        {
            if (currentState != ProjectileState.Tracking)
            {
                return;
            }

            if (targetDamageable != null)
            {
                ApplyDamage(targetDamageable);
                OnTargetHit?.Invoke(this, targetDamageable, damage);
                
                if (showDebugInfo)
                {
                    string targetName = GetTargetName(targetDamageable);
                    LogManager.Info(GameConstants.LOG_PREFIX_GAME, 
                        $"투사체 {gameObject.name}이(가) 타겟 {targetName}에게 {damage} 데미지를 입혔습니다");
                }
            }

            currentState = ProjectileState.Hit;
            CreateHitEffect();
            ReturnToPool();
        }

        /// <summary>
        /// 대체 타겟에 명중했을 때의 처리
        /// </summary>
        private void HitAlternativeTarget(IDamageable alternativeTarget)
        {
            if (currentState != ProjectileState.Tracking)
            {
                return;
            }

            ApplyDamage(alternativeTarget);
            OnTargetHit?.Invoke(this, alternativeTarget, damage);

            if (showDebugInfo)
            {
                string targetName = GetTargetName(alternativeTarget);
                LogManager.Info(GameConstants.LOG_PREFIX_GAME, 
                    $"투사체 {gameObject.name}이(가) 타겟 {targetName}에게 {damage} 데미지를 입혔습니다");
            }

            currentState = ProjectileState.Hit;
            CreateHitEffect();
            ReturnToPool();
        }

        /// <summary>
        /// 데미지를 적용합니다.
        /// </summary>
        private void ApplyDamage(IDamageable target)
        {
            ExceptionHandler.SafeExecute(() => target.TakeDamage(damage), 
                "투사체 데미지 적용 중 오류 발생");
        }

        /// <summary>
        /// 명중 효과를 생성합니다.
        /// </summary>
        private void CreateHitEffect()
        {
            if (hitEffect != null)
            {
                var effect = Instantiate(hitEffect, transform.position, transform.rotation);
                Destroy(effect, 2f); // 2초 후 자동 삭제
            }
        }
        
        #endregion

        #region Lifecycle Management

        /// <summary>
        /// 타겟을 잃었을 때의 처리
        /// </summary>
        private void LoseTarget()
        {
            if (showDebugInfo)
            {
                LogManager.Info(GameConstants.LOG_PREFIX_GAME, 
                    $"투사체 {gameObject.name}이(가) 타겟 손실로 인해 제거됩니다");
            }

            currentState = ProjectileState.Expired;
            ReturnToPool();
        }

        /// <summary>
        /// 투사체가 만료되었을 때의 처리
        /// </summary>
        private void ExpireProjectile()
        {
            if (showDebugInfo)
            {
                LogManager.Info(GameConstants.LOG_PREFIX_GAME, 
                    $"투사체 {gameObject.name}이(가) 생명주기 만료로 제거됩니다");
            }

            currentState = ProjectileState.Expired;
            OnProjectileExpired?.Invoke(this);
            ReturnToPool();
        }

        /// <summary>
        /// 투사체를 정리하고 오브젝트 풀로 반환합니다.
        /// </summary>
        private void ReturnToPool()
        {
            CleanupProjectile();
            
            if (pooledObject != null)
            {
                pooledObject.ReturnToPool();
            }
            else
            {
                // 풀링이 안되는 경우 일반 파괴
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 투사체 정리를 수행합니다.
        /// </summary>
        private void CleanupProjectile()
        {
            currentState = ProjectileState.Inactive;
            targetDamageable = null;
            targetTransform = null;
            
            // Trail 정리
            if (trail != null)
            {
                trail.enabled = false;
            }
            
            // Physics 정리
            if (rb != null && !rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            // 이벤트 정리
            OnTargetHit = null;
            OnProjectileExpired = null;
        }
        
        #endregion
    }
}

