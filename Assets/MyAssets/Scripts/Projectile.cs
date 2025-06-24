using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InvaderInsider;

namespace InvaderInsider
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Projectile : MonoBehaviour
    {
        private const string LOG_PREFIX = "[Projectile] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "Projectile {0} hit target {1} with damage {2}",
            "Projectile {0} destroyed due to missing target"
        };

        [Header("Projectile Settings")]
        [SerializeField] private float speed = 10f;        // 발사체 속도
        [SerializeField] private float damage = 1f;          // 발사체 데미지
        private Transform target;        // 목표 (적)
        private bool isInitialized = false;
        private Rigidbody rb;
        private Collider col;
        private float lifeTime = 5f; // Assuming a default lifeTime

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (isInitialized) return;

            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            isInitialized = true;
        }

        private void OnEnable()
        {
            if (!isInitialized)
            {
                Initialize();
            }
        }

        // 목표 설정 (타워에서 호출)
        public void SetTarget(Transform newTarget)
        {
            if (!isInitialized) return;
            target = newTarget;
        }

        public void SetDmg(float dmg)
        {
            if (!isInitialized) return;
            damage = dmg;
        }

        // Tower에서 호출하는 새로운 Initialize 메서드
        public void Initialize(IDamageable targetDamageable, float dmg, float projectileSpeed)
        {
            if (!isInitialized) 
            {
                Initialize(); // 기본 초기화 먼저 수행
            }

            damage = dmg;
            speed = projectileSpeed;

            // IDamageable이 Component인 경우 Transform 가져오기
            if (targetDamageable is Component component)
            {
                target = component.transform;
            }
            else if (targetDamageable is MonoBehaviour monoBehaviour)
            {
                target = monoBehaviour.transform;
            }
        }

        private void Update()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 direction = (target.position - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, target.position);

            if (distance <= 0.5f)
            {
                HitTarget();
                return;
            }

            transform.position += direction * speed * Time.deltaTime;
            transform.LookAt(target);

            lifeTime -= Time.deltaTime;
            if (lifeTime <= 0)
            {
                Destroy(gameObject);
            }
        }

        private void HitTarget()
        {
            if (target != null)
            {
                IDamageable damageable = target.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage);
                }
            }
            Destroy(gameObject);
        }

        // OnTriggerEnter를 사용하여 충돌 감지
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Enemy"))
            {
                IDamageable damageable = other.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage);
                }
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            target = null;
            rb = null;
            col = null;
        }
    }
}

