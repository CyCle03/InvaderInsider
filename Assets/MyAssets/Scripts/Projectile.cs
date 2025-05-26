using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InvaderInsider;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;        // 발사체 속도
    public float damage = 1;          // 발사체 데미지
    private Transform target;        // 목표 (적)
    private bool hasHit = false;     // 이미 충돌했는지 여부

    // 목표 설정 (타워에서 호출)
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetDmg(float dmg)
    {
        damage = dmg;
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

            // 목표와 충돌했는지 확인
            if (Vector3.Distance(transform.position, target.position) < 0.1f && !hasHit)
            {
                // 적에게 데미지를 주고 발사체를 파괴
                HitTarget();
            }
        }
        else
        {
            // 목표가 없으면 발사체 제거
            Destroy(gameObject);
        }
    }

    // 적에게 데미지를 주는 함수
    private void HitTarget()
    {
        if (target != null)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();  // IDamageable 인터페이스 가져오기
            if (damageable != null)
            {
                damageable.TakeDamage(damage);  // 데미지를 주는 함수 호출
            }
        }
        Destroy(gameObject);  // 발사체 제거
    }
}

