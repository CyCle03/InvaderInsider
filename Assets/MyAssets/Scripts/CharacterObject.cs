using UnityEngine;
using System;
// using InvaderInsider; // BaseCharacter에서 이미 포함되므로 제거 가능

namespace InvaderInsider
{
    // [RequireComponent(typeof(ParticleSystem))] // 제거
    public class CharacterObject : BaseCharacter
    {
        private const string LOG_PREFIX = "[Character] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "Character {0} leveled up to {1}. Health: {2}, Damage: {3}"
        };

        // 성능 최적화 상수들
        private const float ATTACK_CHECK_INTERVAL = 0.1f; // 0.1초마다 공격 체크
        private const int MAX_HIT_COLLIDERS = 20;
        
        // 레벨업 관련 상수들 
        private const float LEVEL_UP_HEALTH_MULTIPLIER = 0.1f; // 10% 체력 증가
        private const float LEVEL_UP_DAMAGE_MULTIPLIER = 0.1f; // 10% 데미지 증가
        
        // 기즈모 관련 상수
        private const float GIZMO_ALPHA = 0.3f;

        [Header("Character Specific")]
        [SerializeField] private bool isDestinationPoint = true;
        [SerializeField] private ParticleSystem attackEffect;
        [SerializeField] private ParticleSystem damageEffect;
        [SerializeField] private int level = 1;

        public event Action<int> OnLevelUp; // LevelUp 이벤트는 CharacterObject 고유
        // public new event Action<float> OnHealthChanged; // BaseCharacter의 이벤트를 사용하므로 제거
        
        private bool isInitialized = false;
        private readonly Collider[] hitColliders = new Collider[MAX_HIT_COLLIDERS];
        private float nextAttackCheckTime = 0f; // 공격 체크 최적화용
        private int enemyLayerMask = -1; // 캐싱된 레이어 마스크

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        protected override void Initialize()
        {
            if (isInitialized) return;

            base.Initialize();

            // 레이어 마스크 캐싱
            enemyLayerMask = LayerMask.GetMask("Enemy");

            if (attackEffect != null)
            {
                var main = attackEffect.main;
                main.loop = false;
                main.playOnAwake = false;
            }

            isInitialized = true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!isInitialized)
            {
                Initialize();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            CleanupEventListeners();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            CleanupEventListeners();
        }

        protected override void CleanupEventListeners()
        {
            base.CleanupEventListeners();
            OnLevelUp = null;
        }

        // Update is called once per frame
        private void Update()
        {
            if (!isInitialized) return;

            // 공격 체크 주기 최적화 (매 프레임이 아닌 0.1초마다)
            if (Time.time >= nextAttackCheckTime)
            {
                DetectAndAttackEnemies();
                nextAttackCheckTime = Time.time + ATTACK_CHECK_INTERVAL;
            }
        }

        private void DetectAndAttackEnemies()
        {
            // 공격 준비가 되었을 때만 적 감지
            if (Time.time < nextAttackTime) return;

            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, AttackRange, hitColliders, enemyLayerMask);
            
            for (int i = 0; i < hitCount; i++)
            {
                if (hitColliders[i].TryGetComponent<EnemyObject>(out var enemy))
                {
                    Attack(enemy);
                    nextAttackTime = Time.time + attackRate;
                    break;
                }
            }
        }

        public override void Attack(IDamageable target)
        {
            if (!isInitialized || target == null) return;

            target.TakeDamage(AttackDamage);
            if (attackEffect != null)
            {
                attackEffect.Play();
            }
        }

        public override void TakeDamage(float damage)
        {
            if (!isInitialized) return;

            base.TakeDamage(damage); // BaseCharacter의 TakeDamage 호출 (OnHealthChanged 이벤트 발생)
            if (damageEffect != null)
            {
                damageEffect.Play();
            }
            // OnHealthChanged?.Invoke(base.currentHealth); // BaseCharacter에서 이미 처리하므로 제거
        }

        public override void LevelUp()
        {
            if (!isInitialized) return;

            level++;
            
            // 성능 최적화: 한 번에 계산
            float healthIncrease = MaxHealth * LEVEL_UP_HEALTH_MULTIPLIER;
            float damageIncrease = AttackDamage * LEVEL_UP_DAMAGE_MULTIPLIER;

            maxHealth += healthIncrease;
            currentHealth += healthIncrease; // 체력 증가 반영
            attackDamage += damageIncrease;

            OnLevelUp?.Invoke(level);
            // 레벨업으로 체력이 변경되었으므로 BaseCharacter의 OnHealthChanged 이벤트 발생
            InvokeHealthChanged(); // BaseCharacter의 InvokeHealthChanged 메서드 호출

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Application.isPlaying)
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[0], 
                    gameObject.name, level, currentHealth, attackDamage));
            }
#endif
        }

        protected override void Die()
        {
            // OnLevelUp = null; // OnDestroy에서 처리
            // OnHealthChanged = null; // BaseCharacter에서 처리
            base.Die();
        }

        // Draw attack range in editor for debugging
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            
            Gizmos.color = new Color(1f, 0f, 0f, GIZMO_ALPHA); // 반투명 빨간색
            Gizmos.DrawWireSphere(transform.position, AttackRange);
        }

        // Properties (BaseCharacter의 속성을 사용하므로 제거)
        // public new float CurrentHealth => base.currentHealth;
        // public new float MaxHealth => base.maxHealth;
        public bool IsDestinationPoint => isDestinationPoint;
        public int Level => level;
    }
}
