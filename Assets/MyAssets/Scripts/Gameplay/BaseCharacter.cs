using UnityEngine;

namespace InvaderInsider.Gameplay
{
    public abstract class BaseCharacter : MonoBehaviour
    {
        [Header("Character Stats")]
        public float maxHealth = 100f;
        public float currentHealth;
        public float moveSpeed = 5f;
        public float attackDamage = 10f;
        public float attackSpeed = 1f;
        public float attackRange = 2f;

        protected virtual void Start()
        {
            currentHealth = maxHealth;
        }

        public virtual void TakeDamage(float damage)
        {
            currentHealth -= damage;
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            // 기본 사망 처리
            Destroy(gameObject);
        }

        public virtual void Heal(float amount)
        {
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        }

        public virtual bool IsAlive()
        {
            return currentHealth > 0;
        }

        public virtual float GetHealthPercentage()
        {
            return currentHealth / maxHealth;
        }
    }
} 