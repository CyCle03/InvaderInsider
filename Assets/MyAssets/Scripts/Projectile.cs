using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;        // �߻�ü �ӵ�
    public float damage = 1;          // �߻�ü ���ط�
    private Transform target;        // ��ǥ (��)
    private bool hasHit = false;     // �̹� �浹�ߴ��� ����

    // ��ǥ ���� (Ÿ������ ȣ��)
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
            // ��ǥ�� �����ϸ� ��ǥ�� ����
            Vector3 direction = target.position - transform.position;  // ��ǥ ����
            float step = speed * Time.deltaTime;                         // �̵� �Ÿ�

            // ��ǥ�� ��ġ�� �������� �߻�ü �̵�
            transform.position = Vector3.MoveTowards(transform.position, target.position, step);

            // ��ǥ�� �浹�ߴ��� Ȯ��
            if (Vector3.Distance(transform.position, target.position) < 0.1f && !hasHit)
            {
                // ������ ���ظ� �ְ� �߻�ü�� �ı�
                HitTarget();
            }
        }
        else
        {
            // ��ǥ�� ������ �߻�ü ����
            Destroy(gameObject);
        }
    }

    // ������ ���ظ� �ִ� �Լ�
    private void HitTarget()
    {
        if (target != null)
        {
            EnemyObject enemy = target.GetComponent<EnemyObject>();  // ���� ��ũ��Ʈ ��������
            if (enemy != null)
            {
                enemy.TakeDamage(damage);  // ������ ���ظ� �ִ� �Լ� ȣ��
            }
        }
        Destroy(gameObject);  // �߻�ü ����
    }
}

