using UnityEngine;
using System;
using InvaderInsider.Data;
using InvaderInsider.Cards;
using InvaderInsider.ScriptableObjects;
using InvaderInsider.Managers;

namespace InvaderInsider
{
    public abstract class BaseCharacter : MonoBehaviour, IDamageable, IAttacker
    {
        private const string LOG_PREFIX = "[Character] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "Equipment {0} applied to Character {1}. Attack: {2}, MaxHealth: {3}",
            "Tried to apply non-equipment card {0} to {1}",
            "장비 카드가 null입니다.",
            "초기화되지 않은 상태에서 장비를 적용하려고 했습니다.",
            "이미 초기화된 캐릭터입니다."
        };

        [Header("Base Stats")]
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected float currentHealth;
        [SerializeField] protected float attackDamage = 10f;
        [SerializeField] protected float baseAttackRange = 5f;
        [SerializeField] protected float attackRate = 1f;
        protected float nextAttackTime;

        public event Action<float> OnHealthChanged;
        public event Action OnDeath;

        private bool _isInitialized = false;
        protected bool IsInitialized => _isInitialized;

        // 설정 참조
        private GameConfigSO baseConfig;

        protected virtual void Awake()
        {
            LoadConfig();
            // Initialize() 호출 제거 - 자식 클래스에서 적절한 타이밍에 호출하도록 함
        }

        private void LoadConfig()
        {
            var configManager = ConfigManager.Instance;
            if (configManager != null && configManager.GameConfig != null)
            {
                baseConfig = configManager.GameConfig;
                
                // 기본값을 설정에서 가져오기
                if (maxHealth == 100f) maxHealth = baseConfig.defaultMaxHealth;
                if (attackDamage == 10f) attackDamage = baseConfig.defaultAttackDamage;
                if (baseAttackRange == 5f) baseAttackRange = baseConfig.defaultAttackRange;
                if (attackRate == 1f) attackRate = baseConfig.defaultAttackRate;
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX}{gameObject.name}: ConfigManager 또는 GameConfig를 찾을 수 없습니다. 기본값을 사용합니다.");
                // 기본값으로 폴백
                baseConfig = ScriptableObject.CreateInstance<GameConfigSO>();
            }
        }

        protected virtual void Initialize()
        {
            if (_isInitialized)
            {
                Debug.LogWarning($"{LOG_PREFIX}{gameObject.name}: {LOG_MESSAGES[4]}");
                return;
            }

            currentHealth = maxHealth;
            _isInitialized = true;
        }

        protected virtual void OnEnable()
        {
            // Initialize() 호출 제거 - 자식 클래스에서 적절한 타이밍에 호출하도록 함
        }

        protected virtual void OnDisable()
        {
            CleanupEventListeners();
        }

        protected virtual void OnDestroy()
        {
            CleanupEventListeners();
        }

        protected virtual void CleanupEventListeners()
        {
            OnHealthChanged = null;
            OnDeath = null;
        }

        public virtual void TakeDamage(float damage)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"{LOG_PREFIX}{gameObject.name}: 초기화되지 않은 상태에서 데미지를 받았습니다.");
                return;
            }

            if (damage < 0)
            {
                Debug.LogWarning($"{LOG_PREFIX}{gameObject.name}: 음수 데미지를 받았습니다. 데미지: {damage}");
                return;
            }

            currentHealth = Mathf.Max(baseConfig.minHealthValue, currentHealth - damage);
            OnHealthChanged?.Invoke(currentHealth / maxHealth);

            if (currentHealth <= baseConfig.minHealthValue)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"{LOG_PREFIX}{gameObject.name}: 초기화되지 않은 상태에서 사망 처리를 시도했습니다.");
                return;
            }

            OnDeath?.Invoke();
            
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX}GameObject가 null입니다. 사망 처리를 완료할 수 없습니다.");
            }
        }

        public abstract void Attack(IDamageable target);

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float AttackDamage => attackDamage;
        public virtual float AttackRange => baseAttackRange;

        protected void InvokeHealthChanged()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"{LOG_PREFIX}{gameObject.name}: 초기화되지 않은 상태에서 체력 변경 이벤트를 호출했습니다.");
                return;
            }

            OnHealthChanged?.Invoke(currentHealth / maxHealth);
        }

        public virtual void LevelUp()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"{LOG_PREFIX}{gameObject.name}: 초기화되지 않은 상태에서 레벨업을 시도했습니다.");
                return;
            }
            
            // 기본 레벨업 로직 - 상속 클래스에서 오버라이드 가능
        }

        public virtual void ApplyEquipment(CardDBObject equipmentCard)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"{LOG_PREFIX}{gameObject.name}: {LOG_MESSAGES[3]}");
                return;
            }

            if (equipmentCard == null)
            {
                Debug.LogError($"{LOG_PREFIX}{gameObject.name}: {LOG_MESSAGES[2]}");
                return;
            }

            if (equipmentCard.type != CardType.Equipment)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (Application.isPlaying)
                {
                    Debug.LogWarning($"{LOG_PREFIX}{string.Format(LOG_MESSAGES[1], equipmentCard.cardName, gameObject.name)}");
                }
#endif
                return;
            }

            // 장비 효과 적용 전 값 체크
            float oldAttackDamage = attackDamage;
            float oldMaxHealth = maxHealth;

            attackDamage += equipmentCard.equipmentBonusAttack;
            maxHealth += equipmentCard.equipmentBonusHealth;
            currentHealth += equipmentCard.equipmentBonusHealth;

            // 음수 값 방지
            attackDamage = Mathf.Max(0f, attackDamage);
            maxHealth = Mathf.Max(baseConfig.minMaxHealthValue, maxHealth); // 최소 체력은 설정값
            currentHealth = Mathf.Max(baseConfig.minHealthValue, currentHealth);

            InvokeHealthChanged();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Application.isPlaying)
            {
                Debug.Log($"{LOG_PREFIX}{string.Format(LOG_MESSAGES[0], equipmentCard.cardName, gameObject.name, attackDamage, maxHealth)}");
                
                // 변경사항 로그
                if (equipmentCard.equipmentBonusAttack != 0)
                {
                    Debug.Log($"{LOG_PREFIX}{gameObject.name}: 공격력 {oldAttackDamage} → {attackDamage}");
                }
                
                if (equipmentCard.equipmentBonusHealth != 0)
                {
                    Debug.Log($"{LOG_PREFIX}{gameObject.name}: 최대 체력 {oldMaxHealth} → {maxHealth}");
                }
            }
#endif
        }
    }
} 