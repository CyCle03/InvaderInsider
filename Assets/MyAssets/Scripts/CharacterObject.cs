using UnityEngine;
using System;
// using InvaderInsider; // BaseCharacter에서 이미 포함되므로 제거 가능

namespace InvaderInsider
{
    // [RequireComponent(typeof(ParticleSystem))] // 제거
    public class CharacterObject : BaseCharacter
    {
        [Header("Character Specific")]
        [SerializeField] private bool isDestinationPoint = true;
        [SerializeField] private ParticleSystem attackEffect;
        [SerializeField] private ParticleSystem damageEffect;
        [SerializeField] private int level = 1;

        public event Action<int> OnLevelUp; // LevelUp 이벤트는 CharacterObject 고유
        // public new event Action<float> OnHealthChanged; // BaseCharacter의 이벤트를 사용하므로 제거
        
        // Start is called before the first frame-update
        protected override void Start()
        {
            base.Start();
            // InitializeEffects(); // 제거
        }

        // Update is called once per frame
        private void Update()
        {
            if (Time.time >= base.nextAttackTime)
            {
                DetectAndAttackEnemies();
            }
        }

        // InitializeEffects 메서드 제거 (파티클 시스템은 인스펙터에서 할당한다고 가정)
        /*
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
        */

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
            base.TakeDamage(damage); // BaseCharacter의 TakeDamage 호출 (OnHealthChanged 이벤트 발생)
            if (damageEffect != null)
            {
                damageEffect.Play();
            }
            // OnHealthChanged?.Invoke(base.currentHealth); // BaseCharacter에서 이미 처리하므로 제거
        }

        public void LevelUp()
        {
            level++;
            float healthIncrease = base.MaxHealth * 0.1f;
            float damageIncrease = base.AttackDamage * 0.1f;

            base.maxHealth += healthIncrease;
            base.currentHealth += healthIncrease; // 체력 증가 반영
            base.attackDamage += damageIncrease;

            OnLevelUp?.Invoke(level);
            // 레벨업으로 체력이 변경되었으므로 BaseCharacter의 OnHealthChanged 이벤트 발생
            InvokeHealthChanged(); // BaseCharacter의 InvokeHealthChanged 메서드 호출
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
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, base.AttackRange);
        }

        // Properties (BaseCharacter의 속성을 사용하므로 제거)
        // public new float CurrentHealth => base.currentHealth;
        // public new float MaxHealth => base.maxHealth;
        public bool IsDestinationPoint => isDestinationPoint;
        public int Level => level;
    }
}
