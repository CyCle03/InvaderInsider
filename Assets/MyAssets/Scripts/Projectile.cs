using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InvaderInsider;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    public float speed = 10f;        // 발사체 속도
    public float damage = 1;          // 발사체 데미지
    private Transform target;        // 목표 (적)
    // private bool hasHit = false;     // 이미 충돌했는지 여부 (OnTriggerEnter 사용 시 불필요)

    // 목표 설정 (타워에서 호출)
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetDmg(float dmg)
    {
        damage = dmg;
    }

    void Awake()
    {
        // Rigidbody를 kinematic으로 설정하여 물리 시뮬레이션 없이 수동으로 이동
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // Collider를 Trigger로 설정
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    void Update()
    {
        if (target != null)
        {
            // 목표가 존재하면 목표로 이동
            Vector3 direction = target.position - transform.position;  // 방향 벡터
            float step = speed * Time.deltaTime;                         // 이동 거리

            // 목표의 위치로 발사체 이동
            transform.position = Vector3.MoveTowards(transform.position, target.position, step);

            // 목표와 충돌했는지 확인 로직은 OnTriggerEnter로 이동
            // if (Vector3.Distance(transform.position, target.position) < 0.1f && !hasHit)
            // {
            //     // 적에게 데미지를 주고 발사체를 파괴
            //     HitTarget();
            // }
        }
        else
        {
            // 목표가 없으면 발사체 제거 (0.1f초 후 파괴)
            Destroy(gameObject, 0.1f); 
        }
    }

    // OnTriggerEnter를 사용하여 충돌 감지
    private void OnTriggerEnter(Collider other)
    {
        // 이미 충돌한 경우 또는 타겟이 아닌 다른 오브젝트와 충돌한 경우 무시
        if (target == null || other.transform != target) return;

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
        Destroy(gameObject); // 발사체 제거
    }

    // HitTarget 함수는 더 이상 필요 없음 (OnTriggerEnter로 대체)
    // private void HitTarget()
    // {
    //     if (target != null)
    //     {
    //         IDamageable damageable = target.GetComponent<IDamageable>();  // IDamageable 인터페이스 가져오기
    //         if (damageable != null)
    //         {
    //             damageable.TakeDamage(damage);  // 데미지를 주는 함수 호출
    //         }
    //     }
    //     Destroy(gameObject);  // 발사체 제거
    // }
}

