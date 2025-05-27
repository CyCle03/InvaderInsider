using UnityEngine;
using System;
using InvaderInsider.Core;

namespace InvaderInsider
{
    [RequireComponent(typeof(ParticleSystem))]
    public class CharacterObject : BaseCharacter
    {
        [Header("Character Specific")]
        [SerializeField] private bool isDestinationPoint = true;
        [SerializeField] private ParticleSystem attackEffect;
        [SerializeField] private ParticleSystem damageEffect;
        [SerializeField] private int level = 1;

        public event Action<int> OnLevelUp;
        public new event Action<float> OnHealthChanged;
        
        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();
            InitializeEffects();
        }

        // Update is called once per frame
        private void Update()
        {
            if (Time.time >= base.nextAttackTime)
            {
                DetectAndAttackEnemies();
            }
        }

        private void InitializeEffects()
        {
            if (attackEffect == null)
            {
                attackEffect = GetComponent<ParticleSystem>();
            }

            if (attackEffect != null)
            {
                var main = attackEffect.main;
                main.loop = false;
                main.playOnAwake = false;
            }
        }

        private void DetectAndAttackEnemies()
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, base.AttackRange);
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.TryGetComponent<EnemyObject>(out var enemy))
                {
                    Attack(enemy);
                    base.nextAttackTime = Time.time + base.attackRate;
                    break;
                }
            }
        }

        public override void Attack(IDamageable target)
        {
            target.TakeDamage(base.AttackDamage);
            if (attackEffect != null)
            {
                attackEffect.Play();
            }
        }

        public override void TakeDamage(float damage)
        {
            base.TakeDamage(damage);
            if (damageEffect != null)
            {
                damageEffect.Play();
            }
            OnHealthChanged?.Invoke(base.currentHealth);
        }

        public void LevelUp()
        {
            level++;
            float healthIncrease = base.MaxHealth * 0.1f;
            float damageIncrease = base.AttackDamage * 0.1f;

            base.maxHealth += healthIncrease;
            base.currentHealth += healthIncrease;
            base.attackDamage += damageIncrease;

            OnLevelUp?.Invoke(level);
        }

        protected override void Die()
        {
            OnLevelUp = null;
            OnHealthChanged = null;
            base.Die();
        }

        // Draw attack range in editor for debugging
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, base.AttackRange);
        }

        // Properties
        public new float CurrentHealth => base.currentHealth;
        public new float MaxHealth => base.maxHealth;
        public bool IsDestinationPoint => isDestinationPoint;
        public int Level => level;
    }
}
