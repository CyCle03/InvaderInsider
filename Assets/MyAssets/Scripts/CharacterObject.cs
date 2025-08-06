using UnityEngine;
using System;
using InvaderInsider.Core;
using InvaderInsider.Data;

namespace InvaderInsider
{
    public class CharacterObject : BaseCharacter
    {
        private static readonly string[] LOG_MESSAGES = 
        {
            "캐릭터 {0}이(가) 레벨 {1}로 레벨업했습니다. 체력: {2}, 공격력: {3}",
            "캐릭터 {0}이(가) {1}에게 {2} 데미지를 입혔습니다",
            "캐릭터 {0} 초기화 완료 - 레벨: {1}, 체력: {2}/{3}"
        };

        private const float ATTACK_CHECK_INTERVAL = 0.1f;
        private const int MAX_HIT_COLLIDERS = 20;
        private const float LEVEL_UP_HEALTH_MULTIPLIER = 0.1f;
        private const float LEVEL_UP_DAMAGE_MULTIPLIER = 0.1f;

        [Header("Character Specific")]
        [SerializeField] private bool isDestinationPoint = true;
        [SerializeField] private ParticleSystem attackEffect;
        [SerializeField] private ParticleSystem damageEffect;
        

        private readonly Collider[] hitColliders = new Collider[MAX_HIT_COLLIDERS];
        private float nextAttackCheckTime = 0f;
        private int enemyLayerMask = -1;

        public event Action<int> OnLevelUp;
        public bool IsDestinationPoint => isDestinationPoint;

        protected override void Awake()
        {
            base.Awake();
            InitializeCharacterComponents();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            InitializeCharacterComponents();
        }

        public override void Initialize(CardDBObject cardData)
        {
            base.Initialize(cardData);
        }

        private void Start()
        {
            // SetupEnemy는 StageManager에서 SpawnEnemy 시 호출되므로 여기서는 중복 호출 방지
            if (!IsInitialized)
            {
                // Initialize(); // 모든 컴포넌트가 준비된 후 초기화
            }
        }

        private void Update()
        {
            if (!IsInitialized) return;

            if (Time.time >= nextAttackCheckTime)
            {
                DetectAndAttackEnemies();
                nextAttackCheckTime = Time.time + ATTACK_CHECK_INTERVAL;
            }
        }

        private void InitializeCharacterComponents()
        {
            enemyLayerMask = LayerMask.GetMask(GameConstants.ENEMY_LAYER_NAME);
            if (attackEffect != null)
            {
                var main = attackEffect.main;
                main.loop = false;
                main.playOnAwake = false;
            }
            if (damageEffect != null)
            {
                var main = damageEffect.main;
                main.loop = false;
                main.playOnAwake = false;
            }
        }

        protected override void CleanupEventListeners()
        {
            base.CleanupEventListeners();
            OnLevelUp = null;
        }

        private void DetectAndAttackEnemies()
        {
            if (!CanAttack()) return;

            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, AttackRange, hitColliders, enemyLayerMask);
            for (int i = 0; i < hitCount; i++)
            {
                if (hitColliders[i].TryGetComponent<EnemyObject>(out var enemy))
                {
                    Attack(enemy);
                    SetNextAttackTime();
                    break;
                }
            }
        }

        public override void Attack(IDamageable target)
        {
            if (!IsInitialized || target == null) return;

            try
            {
                target.TakeDamage(AttackDamage);
                if (showDebugInfo)
                {
                    string targetName = target is MonoBehaviour mb ? mb.name : "Unknown";
                    Debug.Log($"{GameConstants.LOG_PREFIX_GAME} {string.Format(LOG_MESSAGES[1], gameObject.name, targetName, AttackDamage)}");
                }
                PlayAttackEffect();
            }
            catch (Exception e)
            {
                Debug.LogError($"캐릭터 공격 중 오류 발생: {e.Message}");
            }
        }

        public override void TakeDamage(float damage)
        {
            base.TakeDamage(damage);
            PlayDamageEffect();
        }

        private void PlayAttackEffect()
        {
            if (attackEffect != null && attackEffect.gameObject.activeInHierarchy) attackEffect.Play();
        }

        private void PlayDamageEffect()
        {
            if (damageEffect != null && damageEffect.gameObject.activeInHierarchy) damageEffect.Play();
        }

        public override void LevelUp()
        {
            if (!IsInitialized) return;

            base.LevelUp(); // BaseCharacter의 LevelUp 호출
            
            float healthIncrease = MaxHealth * LEVEL_UP_HEALTH_MULTIPLIER;
            float damageIncrease = AttackDamage * LEVEL_UP_DAMAGE_MULTIPLIER;

            Heal(healthIncrease);
            attackDamage += damageIncrease;

            OnLevelUp?.Invoke(Level); // BaseCharacter의 Level 프로퍼티 사용

            if (showDebugInfo)
            {
                Debug.Log($"{GameConstants.LOG_PREFIX_GAME} {string.Format(LOG_MESSAGES[0], gameObject.name, Level, CurrentHealth, AttackDamage)}");
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, AttackRange);
        }
    }
}