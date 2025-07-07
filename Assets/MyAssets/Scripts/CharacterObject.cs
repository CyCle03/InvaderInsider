using UnityEngine;
using System;
using InvaderInsider.Managers;

namespace InvaderInsider
{
    /// <summary>
    /// 캐릭터 오브젝트 클래스 - BaseCharacter를 상속받아 레벨링과 전투 기능을 구현합니다.
    /// </summary>
    public class CharacterObject : BaseCharacter
    {
        #region Constants & Log Messages
        
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "캐릭터 {0}이(가) 레벨 {1}로 레벨업했습니다. 체력: {2}, 공격력: {3}",
            "캐릭터 {0}이(가) {1}에게 {2} 데미지를 입혔습니다",
            "캐릭터 {0} 초기화 완료 - 레벨: {1}, 체력: {2}/{3}"
        };

        // 성능 최적화 상수들
        private const float ATTACK_CHECK_INTERVAL = 0.1f; // 0.1초마다 공격 체크
        private const int MAX_HIT_COLLIDERS = 20;
        
        // 레벨업 관련 상수들 
        private const float LEVEL_UP_HEALTH_MULTIPLIER = 0.1f; // 10% 체력 증가
        private const float LEVEL_UP_DAMAGE_MULTIPLIER = 0.1f; // 10% 데미지 증가
        
        // 기즈모 관련 상수
        private const float GIZMO_ALPHA = 0.3f;
        
        #endregion

        #region Inspector Fields
        
        [Header("Character Specific")]
        [SerializeField] private bool isDestinationPoint = true;
        [SerializeField] private ParticleSystem attackEffect;
        [SerializeField] private ParticleSystem damageEffect;
        [SerializeField] private int level = 1;
        
        #endregion

        #region Runtime State
        
        private readonly Collider[] hitColliders = new Collider[MAX_HIT_COLLIDERS];
        private float nextAttackCheckTime = 0f; // 공격 체크 최적화용
        private int enemyLayerMask = -1; // 캐싱된 레이어 마스크
        
        #endregion

        #region Events
        
        /// <summary>레벨업 시 발생하는 이벤트 (새 레벨)</summary>
        public event Action<int> OnLevelUp;
        
        #endregion

        #region Properties
        
        /// <summary>목적지 지점 여부</summary>
        public bool IsDestinationPoint => isDestinationPoint;
        
        /// <summary>현재 레벨</summary>
        public int Level => level;
        
        #endregion

        #region Unity Lifecycle
        
        /// <summary>
        /// Unity Awake - BaseCharacter 초기화 후 캐릭터 특화 초기화
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            InitializeCharacterComponents();
        }

        /// <summary>
        /// Unity OnEnable - 활성화 시 초기화
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            InitializeCharacterComponents();
        }

        /// <summary>
        /// Unity Start - 게임 시작 시 최종 초기화
        /// </summary>
        private void Start()
        {
            // BaseCharacter 초기화
            base.Initialize();
            
            if (showDebugInfo)
            {
                LogManager.Info(GameConstants.LOG_PREFIX_GAME, 
                    $"캐릭터 {gameObject.name} 초기화 완료 - 레벨: {level}, 체력: {CurrentHealth}/{MaxHealth}");
            }
        }

        /// <summary>
        /// Unity Update - 적 감지 및 공격 처리
        /// </summary>
        private void Update()
        {
            if (!IsInitialized) 
            {
                return;
            }

            // 공격 체크 주기 최적화 (매 프레임이 아닌 0.1초마다)
            if (Time.time >= nextAttackCheckTime)
            {
                DetectAndAttackEnemies();
                nextAttackCheckTime = Time.time + ATTACK_CHECK_INTERVAL;
            }
        }
        
        #endregion

        #region Initialization
        
        /// <summary>
        /// 캐릭터 전용 컴포넌트들을 초기화합니다.
        /// </summary>
        private void InitializeCharacterComponents()
        {
            // 레이어 마스크 캐싱
            enemyLayerMask = LayerMask.GetMask(GameConstants.ENEMY_LAYER_NAME);

            // 공격 이펙트 설정
            if (attackEffect != null)
            {
                var main = attackEffect.main;
                main.loop = false;
                main.playOnAwake = false;
            }

            // 데미지 이펙트 설정
            if (damageEffect != null)
            {
                var main = damageEffect.main;
                main.loop = false;
                main.playOnAwake = false;
            }
        }

        /// <summary>
        /// 이벤트 리스너들을 정리합니다.
        /// </summary>
        protected override void CleanupEventListeners()
        {
            base.CleanupEventListeners();
            OnLevelUp = null;
        }
        
        #endregion

        #region Combat System
        
        /// <summary>
        /// 적을 감지하고 공격합니다.
        /// </summary>
        private void DetectAndAttackEnemies()
        {
            // 공격 준비가 되었을 때만 적 감지
            if (!CanAttack()) 
            {
                return;
            }

            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, AttackRange, hitColliders, enemyLayerMask);
            
            for (int i = 0; i < hitCount; i++)
            {
                if (hitColliders[i].TryGetComponent<EnemyObject>(out var enemy))
                {
                    Attack(enemy);
                    SetNextAttackTime(); // BaseCharacter의 메서드 사용
                    break;
                }
            }
        }

        /// <summary>
        /// 대상을 공격합니다.
        /// </summary>
        /// <param name="target">공격할 대상</param>
        public override void Attack(IDamageable target)
        {
            if (!IsInitialized || target == null) 
            {
                return;
            }

            ExceptionHandler.SafeExecute(() => 
            {
                target.TakeDamage(AttackDamage);
                
                if (showDebugInfo)
                {
                    string targetName = target is MonoBehaviour mb ? mb.name : "Unknown";
                    LogManager.Info(GameConstants.LOG_PREFIX_GAME, 
                        $"캐릭터 {gameObject.name}이(가) {targetName}에게 {AttackDamage} 데미지를 입혔습니다");
                }
                
                PlayAttackEffect();
            }, "캐릭터 공격 중 오류 발생");
        }

        /// <summary>
        /// 데미지를 받습니다.
        /// </summary>
        /// <param name="damage">받을 데미지</param>
        public override void TakeDamage(float damage)
        {
            base.TakeDamage(damage); // BaseCharacter의 TakeDamage 호출
            PlayDamageEffect();
        }

        /// <summary>
        /// 공격 이펙트를 재생합니다.
        /// </summary>
        private void PlayAttackEffect()
        {
            if (attackEffect != null && attackEffect.gameObject.activeInHierarchy)
            {
                attackEffect.Play();
            }
        }

        /// <summary>
        /// 데미지 이펙트를 재생합니다.
        /// </summary>
        private void PlayDamageEffect()
        {
            if (damageEffect != null && damageEffect.gameObject.activeInHierarchy)
            {
                damageEffect.Play();
            }
        }
        
        #endregion

        #region Level System
        
        /// <summary>
        /// 레벨업을 수행합니다.
        /// </summary>
        public override void LevelUp()
        {
            if (!IsInitialized) 
            {
                return;
            }

            level++;
            
            // 스탯 증가량 계산
            float healthIncrease = MaxHealth * LEVEL_UP_HEALTH_MULTIPLIER;
            float damageIncrease = AttackDamage * LEVEL_UP_DAMAGE_MULTIPLIER;

            // 직접 필드 접근 대신 BaseCharacter의 시스템 활용
            // 체력 회복으로 최대 체력 증가 효과
            Heal(healthIncrease);

            // 공격력은 직접 조정 (BaseCharacter에 공격력 증가 메서드가 없으므로)
            attackDamage += damageIncrease;

            // 이벤트 발생
            OnLevelUp?.Invoke(level);

            if (showDebugInfo)
            {
                LogManager.Info(GameConstants.LOG_PREFIX_GAME, 
                    $"캐릭터 {gameObject.name}이(가) 레벨 {level}로 레벨업했습니다. 체력: {CurrentHealth}, 공격력: {AttackDamage}");
            }
        }
        
        #endregion

        #region Debug & Gizmos
        
        /// <summary>
        /// 에디터에서 공격 범위를 표시합니다.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) 
            {
                return;
            }
            
            Gizmos.color = new Color(1f, 0f, 0f, GIZMO_ALPHA); // 반투명 빨간색
            Gizmos.DrawWireSphere(transform.position, AttackRange);
        }
        
        #endregion
    }
}
