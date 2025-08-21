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
        [SerializeField] private float hitDistance = 0.5f; // Reverted to original for robustness

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
        public event Action<Projectile> OnProjectileExpired;

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

            Debug.Log($"[Projectile] OnTriggerEnter called with {other.name} (Layer: {LayerMask.LayerToName(other.gameObject.layer)}). CurrentState: {currentState}");
            if (currentState != ProjectileState.Tracking) return;

            var hitDamageable = other.GetComponent<IDamageable>();
            if (hitDamageable == null) hitDamageable = other.GetComponentInParent<IDamageable>();
            if (hitDamageable == null) hitDamageable = other.GetComponentInChildren<IDamageable>();

            if (hitDamageable != null)
            {
                if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                {
                    Debug.Log($"[Projectile] Collided with an enemy: {other.name}. Calling HitTarget().");
                    HitTarget(hitDamageable);
                }
                else
                {
                    Debug.Log($"[Projectile] Collided with IDamageable on a non-enemy layer: {other.name}. Ignoring.");
                }
            }
            else
            {
                Debug.Log($"[Projectile] No IDamageable found on {other.name} or its parents/children.");
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
                Debug.Log($"[Projectile] Target lost or inactive. Calling LoseTarget().");
                LoseTarget();
                return;
            }

            Vector3 direction = (targetTransform.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(direction);

            // if (Vector3.Distance(transform.position, targetTransform.position) <= hitDistance)
            // {
            //     Debug.Log($"[Projectile] Distance to target ({Vector3.Distance(transform.position, targetTransform.position):F2}) <= hitDistance ({hitDistance}). Calling HitTarget().");
            //     HitTarget();
            // }
        }

        private void HitTarget(IDamageable hitTarget)
        {
            Debug.Log($"[Projectile] HitTarget() called. CurrentState: {currentState}");
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
                Debug.Log($"[Projectile] Finally block in HitTarget(). Calling ReturnToPool().");
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
            Debug.Log($"[Projectile] LoseTarget() called. CurrentState: {currentState}");
            currentState = ProjectileState.Expired;
            ReturnToPool();
        }

        

        private void ReturnToPool()
        {
            Debug.Log($"[Projectile] ReturnToPool() called on {gameObject.name}. PooledObject component: {(GetComponent<PooledObject>() != null ? "Found" : "Missing")}");
            // Ensure we have the component right before we use it.
            var pooledObject = GetComponent<PooledObject>();
            if (pooledObject != null)
            {
                Debug.Log($"[Projectile] About to call pooledObject.ReturnToPool() on {gameObject.name}.");
                pooledObject.ReturnToPool();
            }
            else
            {
                // This case should ideally not happen if prefabs are set up correctly.
                Destroy(gameObject);
            }
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
