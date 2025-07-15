using UnityEngine;
using System;
using InvaderInsider.Core;

namespace InvaderInsider
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Projectile : MonoBehaviour
    {
        private static readonly string[] LOG_MESSAGES = 
        {
            "투사체 {0}이(가) 타겟 {1}에게 {2} 데미지를 입혔습니다",
            "투사체 {0}이(가) 타겟 손실로 인해 제거됩니다",
            "투사체 {0} 초기화 완료 - 타겟: {1}, 데미지: {2}, 속도: {3}",
            "투사체 {0}이(가) 생명주기 만료로 제거됩니다"
        };

        public enum ProjectileState { Inactive, Tracking, Hit, Expired }

        [Header("Projectile Settings")]
        [SerializeField] private float defaultSpeed = GameConstants.PROJECTILE_SPEED;
        [SerializeField] private float defaultLifeTime = GameConstants.OBJECT_AUTO_RETURN_TIME;
        [SerializeField] private float hitDistance = 0.5f;

        [Header("Visual Effects")]
        [SerializeField] private GameObject hitEffect;
        [SerializeField] private TrailRenderer trail;

        // Removed: [SerializeField] private bool showDebugInfo = false;

        private ProjectileState currentState = ProjectileState.Inactive;
        private IDamageable targetDamageable;
        private Transform targetTransform;
        private float speed;
        private float damage;
        private float lifeTime;
        private bool isInitialized = false;
        private Rigidbody rb;
        private PooledObject pooledObject;

        public event Action<Projectile, IDamageable, float> OnTargetHit;
        public event Action<Projectile> OnProjectileExpired;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Update()
        {
            if (currentState != ProjectileState.Tracking) return;
            lifeTime -= Time.deltaTime;
            if (lifeTime <= 0f) ExpireProjectile();
            else UpdateMovement();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (currentState != ProjectileState.Tracking) return;
            if (other.transform == targetTransform) HitTarget();
        }

        private void InitializeComponents()
        {
            if (isInitialized) return;
            rb = GetComponent<Rigidbody>();
            pooledObject = GetComponent<PooledObject>();
            rb.isKinematic = true;
            rb.useGravity = false;
            GetComponent<Collider>().isTrigger = true;
            isInitialized = true;
        }

        public void Launch(IDamageable target, float projectileDamage, float projectileSpeed = 0f)
        {
            if (target == null) { ReturnToPool(); return; }

            targetDamageable = target;
            targetTransform = (target as MonoBehaviour)?.transform;
            if (targetTransform == null) { ReturnToPool(); return; }

            damage = projectileDamage;
            speed = projectileSpeed > 0f ? projectileSpeed : defaultSpeed;
            lifeTime = defaultLifeTime;
            currentState = ProjectileState.Tracking;

            if (trail != null) trail.Clear();
        }

        private void UpdateMovement()
        {
            if (targetTransform == null || !targetTransform.gameObject.activeInHierarchy)
            {
                LoseTarget();
                return;
            }

            Vector3 direction = (targetTransform.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(direction);

            if (Vector3.Distance(transform.position, targetTransform.position) <= hitDistance) HitTarget();
        }

        private void HitTarget()
        {
            if (currentState != ProjectileState.Tracking) return;

            currentState = ProjectileState.Hit;
            ApplyDamage(targetDamageable);
            OnTargetHit?.Invoke(this, targetDamageable, damage);

            if (hitEffect != null) Instantiate(hitEffect, transform.position, Quaternion.identity);
            ReturnToPool();
        }

        private void ApplyDamage(IDamageable target)
        {
            try
            {
                target?.TakeDamage(damage);
            }
            catch (Exception e)
            {
                Debug.LogError($"투사체 데미지 적용 중 오류 발생: {e.Message}");
            }
        }

        private void LoseTarget()
        {
            currentState = ProjectileState.Expired;
            ReturnToPool();
        }

        private void ExpireProjectile()
        {
            currentState = ProjectileState.Expired;
            OnProjectileExpired?.Invoke(this);
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            if (pooledObject != null) pooledObject.ReturnToPool();
            else Destroy(gameObject);
        }

        private void OnDisable()
        {
            currentState = ProjectileState.Inactive;
            targetDamageable = null;
            targetTransform = null;
            OnTargetHit = null;
            OnProjectileExpired = null;
        }
    }
}
