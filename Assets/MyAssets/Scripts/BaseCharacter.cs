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

        protected void InvokeHealthChanged()
        {
            OnHealthChanged?.Invoke(currentHealth / maxHealth);
        }

        // 장비 아이템 적용 메서드
        public virtual void ApplyEquipment(InvaderInsider.Data.CardDBObject equipmentCard)
        {
            if (equipmentCard.type != InvaderInsider.Cards.CardType.Equipment)
            {
                Debug.LogWarning($"Tried to apply non-equipment card {equipmentCard.cardName} to {gameObject.name}.");
                return;
            }

            attackDamage += equipmentCard.equipmentBonusAttack;
            maxHealth += equipmentCard.equipmentBonusHealth;
            currentHealth += equipmentCard.equipmentBonusHealth;

            InvokeHealthChanged();

            Debug.Log($"Equipment {equipmentCard.cardName} applied to Character {gameObject.name}. " +
                      $"Attack: {attackDamage}, MaxHealth: {maxHealth}");
        }
    }
} 