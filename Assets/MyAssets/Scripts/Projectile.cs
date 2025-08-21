using UnityEngine;
using System;
using InvaderInsider.Core;

namespace InvaderInsider
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Projectile : MonoBehaviour
    {
        public enum ProjectileState { Inactive, Tracking, Hit, Expired }

        [Header("Projectile Settings")]
        [SerializeField] private float defaultSpeed = 10f;
        //[SerializeField] private float hitDistance = 0.5f; // Reverted to original for robustness

        [Header("Visual Effects")]
        [SerializeField] private GameObject hitEffect;
        [SerializeField] private TrailRenderer trail;

        private ProjectileState currentState = ProjectileState.Inactive;
        private IDamageable targetDamageable;
        private Transform targetTransform;
        private float speed;
        private float damage;
        private bool isInitialized = false;
        private Rigidbody rb;

        public event Action<Projectile, IDamageable, float> OnTargetHit;
        //public event Action<Projectile> OnProjectileExpired;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Update()
        {
            if (currentState != ProjectileState.Tracking) return;
            UpdateMovement();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<Projectile>() != null) return;
            if (currentState != ProjectileState.Tracking) return;

            var hitDamageable = other.GetComponent<IDamageable>();
            if (hitDamageable == null) hitDamageable = other.GetComponentInParent<IDamageable>();
            if (hitDamageable == null) hitDamageable = other.GetComponentInChildren<IDamageable>();

            if (hitDamageable != null)
            {
                if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                {
                    HitTarget(hitDamageable);
                }
            }
        }

        private void InitializeComponents()
        {
            if (isInitialized) return;
            rb = GetComponent<Rigidbody>();
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
        }

        private void HitTarget(IDamageable hitTarget)
        {
            if (currentState != ProjectileState.Tracking) return;

            try
            {
                currentState = ProjectileState.Hit;
                ApplyDamage(hitTarget);
                OnTargetHit?.Invoke(this, hitTarget, damage);

                if (hitEffect != null)
                {
                    var effect = ObjectPoolManager.Instance.GetObject<PooledObject>(hitEffect.name);
                    if (effect != null)
                    {
                        effect.transform.position = transform.position;
                        effect.transform.rotation = Quaternion.identity;
                    }
                }
            }
            finally
            {
                ReturnToPool();
            }
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

        private void ReturnToPool()
        {
            if (!gameObject.activeInHierarchy) return;

            var pooledObject = GetComponent<PooledObject>();
            if (pooledObject != null)
            {
                pooledObject.ReturnToPool();
            }
            else
            {
                Destroy(gameObject);
            }

            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            currentState = ProjectileState.Inactive;
            targetDamageable = null;
            targetTransform = null;
            OnTargetHit = null;
            //OnProjectileExpired = null;
        }
    }
}