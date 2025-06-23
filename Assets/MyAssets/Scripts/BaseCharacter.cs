using UnityEngine;
using System;
using InvaderInsider.Data;
using InvaderInsider.Cards;

namespace InvaderInsider
{
    public abstract class BaseCharacter : MonoBehaviour, IDamageable, IAttacker
    {
        private const string LOG_PREFIX = "[Character] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "Equipment {0} applied to Character {1}. Attack: {2}, MaxHealth: {3}",
            "Tried to apply non-equipment card {0} to {1}"
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

        protected virtual void Awake()
        {
            Initialize();
        }

        protected virtual void Initialize()
        {
            if (_isInitialized) return;

            currentHealth = maxHealth;
            _isInitialized = true;
        }

        protected virtual void OnEnable()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
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
            if (!_isInitialized) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);
            OnHealthChanged?.Invoke(currentHealth / maxHealth);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            if (!_isInitialized) return;

            OnDeath?.Invoke();
            Destroy(gameObject);
        }

        public abstract void Attack(IDamageable target);

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float AttackDamage => attackDamage;
        public virtual float AttackRange => baseAttackRange;

        protected void InvokeHealthChanged()
        {
            if (!_isInitialized) return;
            OnHealthChanged?.Invoke(currentHealth / maxHealth);
        }

        public virtual void LevelUp()
        {
            // 기본 레벨업 로직 - 상속 클래스에서 오버라이드 가능
        }

        public virtual void ApplyEquipment(CardDBObject equipmentCard)
        {
            if (!_isInitialized || equipmentCard == null) return;

            if (equipmentCard.type != CardType.Equipment)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (Application.isPlaying)
                {
                    Debug.LogWarning(string.Format(LOG_PREFIX + LOG_MESSAGES[1], equipmentCard.cardName, gameObject.name));
                }
#endif
                return;
            }

            attackDamage += equipmentCard.equipmentBonusAttack;
            maxHealth += equipmentCard.equipmentBonusHealth;
            currentHealth += equipmentCard.equipmentBonusHealth;

            InvokeHealthChanged();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Application.isPlaying)
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[0], 
                    equipmentCard.cardName, gameObject.name, attackDamage, maxHealth));
            }
#endif
        }
    }
} 