using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;        // 발사체 속도
    public float damage = 1;          // 발사체 피해량
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
            // 목표가 존재하면 목표를 추적
            Vector3 direction = target.position - transform.position;  // 목표 방향
            float step = speed * Time.deltaTime;                         // 이동 거리

            // 목표가 위치한 방향으로 발사체 이동
            transform.position = Vector3.MoveTowards(transform.position, target.position, step);

            // 목표와 충돌했는지 확인
            if (Vector3.Distance(transform.position, target.position) < 0.1f && !hasHit)
            {
                // 적에게 피해를 주고 발사체를 파괴
                HitTarget();
            }
        }
        else
        {
            // 목표가 없으면 발사체 삭제
            Destroy(gameObject);
        }
    }

    // 적에게 피해를 주는 함수
    private void HitTarget()
    {
        if (target != null)
        {
            EnemyObject enemy = target.GetComponent<EnemyObject>();  // 적의 스크립트 가져오기
            if (enemy != null)
            {
                enemy.TakeDamage(damage);  // 적에게 피해를 주는 함수 호출
            }
        }
        Destroy(gameObject);  // 발사체 삭제
    }
}

