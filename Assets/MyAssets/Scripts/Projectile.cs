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

        void Update()
        {
            if (!isInitialized) return;

            if (target != null)
            {
                // 목표가 존재하면 목표로 이동
                Vector3 direction = target.position - transform.position;  // 방향 벡터
                float step = speed * Time.deltaTime;                         // 이동 거리

                // 목표의 위치로 발사체 이동
                transform.position = Vector3.MoveTowards(transform.position, target.position, step);
            }
            else
            {
                if (Application.isPlaying)
                {
                    Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[1], gameObject.name));
                }
                // 목표가 없으면 발사체 제거 (0.1f초 후 파괴)
                Destroy(gameObject, 0.1f); 
            }
        }

        // OnTriggerEnter를 사용하여 충돌 감지
        private void OnTriggerEnter(Collider other)
        {
            if (!isInitialized || target == null || other.transform != target) return;

            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                if (Application.isPlaying)
                {
                    Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[0], gameObject.name, target.name, damage));
                }
            }
            Destroy(gameObject); // 발사체 제거
        }

        private void OnDestroy()
        {
            target = null;
            rb = null;
            col = null;
        }
    }
}

