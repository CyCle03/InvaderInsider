using UnityEngine;
using System;

namespace InvaderInsider
{
    public abstract class BaseCharacter : MonoBehaviour, IDamageable, IAttacker
    {
        [Header("Base Stats")]
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected float currentHealth;
        [SerializeField] protected float attackDamage = 10f;
        [SerializeField] protected float attackRange = 5f;
        [SerializeField] protected float attackRate = 1f;
        protected float nextAttackTime;

        public event Action<float> OnHealthChanged;
        public event Action OnDeath;

        protected virtual void Start()
        {
            currentHealth = maxHealth;
        }

        public virtual void TakeDamage(float damage)
        {
            currentHealth = Mathf.Max(0, currentHealth - damage);
            OnHealthChanged?.Invoke(currentHealth / maxHealth);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            OnDeath?.Invoke();
            Destroy(gameObject);
        }

        public abstract void Attack(IDamageable target);

        // Interface implementations
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float AttackDamage => attackDamage;
        public float AttackRange => attackRange;
    }
} 