using UnityEngine;
using System;
using InvaderInsider.Core;

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
        [SerializeField] private int level = 1;

        private readonly Collider[] hitColliders = new Collider[MAX_HIT_COLLIDERS];
        private float nextAttackCheckTime = 0f;
        private int enemyLayerMask = -1;

        public event Action<int> OnLevelUp;
        public bool IsDestinationPoint => isDestinationPoint;
        public int Level => level;

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

        private void Start()
        {
            base.Initialize();
            if (showDebugInfo)
            {
                Debug.Log($"{GameConstants.LOG_PREFIX_GAME} {string.Format(LOG_MESSAGES[2], gameObject.name, level, CurrentHealth, MaxHealth)}");
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

            level++;
            float healthIncrease = MaxHealth * LEVEL_UP_HEALTH_MULTIPLIER;
            float damageIncrease = AttackDamage * LEVEL_UP_DAMAGE_MULTIPLIER;

            Heal(healthIncrease);
            attackDamage += damageIncrease;

            OnLevelUp?.Invoke(level);

            if (showDebugInfo)
            {
                Debug.Log($"{GameConstants.LOG_PREFIX_GAME} {string.Format(LOG_MESSAGES[0], gameObject.name, level, CurrentHealth, AttackDamage)}");
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