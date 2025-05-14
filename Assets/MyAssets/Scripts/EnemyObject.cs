using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class EnemyObject : MonoBehaviour
{

    Transform wayPoint;
    NavMeshAgent agent;

    public Slider hpSlider;
    public Enemy data = new Enemy();

    public Enemy CreateEnemy()
    {
        Enemy newEnemy = new Enemy(this);
        return newEnemy;
    }

    // Start is called before the first frame update
    void Start()
    {
        wayPoint = EnemyManager.Instance.wayPoints[1];

        agent = GetComponent<NavMeshAgent>();
        UpdateHP();
    }

    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(transform.position, wayPoint.position) > 0.2f)
        {
            agent.isStopped = true;
            agent.ResetPath();

            agent.stoppingDistance = 0.2f;
            agent.destination = wayPoint.position;
        }
        else
        {
            agent.isStopped = true;
            agent.ResetPath();

            transform.position = wayPoint.position;
            transform.rotation = Quaternion.identity;

            Destroy(gameObject);
        }
    }

    public void TakeDamage(float damage)
    {
        data.curruntHP -= damage;
        UpdateHP();
        if (data.curruntHP <= 0)
        {
            Die();
        }
    }

    public void UpdateHP()
    {
        hpSlider.value = data.curruntHP / data.maxHP;
    }

    private void Die()
    {
        // 적이 죽었을 때의 처리 (예: 적을 제거하고 보상 지급)
        Destroy(gameObject);
    }
}

[System.Serializable]
public class Enemy
{
    public string eName;
    public int eID = -1;
    public float maxHP;
    public float curruntHP;
    public float moveSpeed;
    public float damage;
    public int eDataDrop;

    public Enemy()
    {
        eName = "";
        eID = -1;
    }

    public Enemy(EnemyObject enemy)
    {
        eName = enemy.name;
        eID = enemy.data.eID;
        maxHP = enemy.data.maxHP;
        curruntHP = maxHP;
        moveSpeed = enemy.data.moveSpeed;
        damage = enemy.data.damage;
        eDataDrop = enemy.data.eDataDrop;
    }

}