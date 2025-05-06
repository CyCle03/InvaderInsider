using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMove : MonoBehaviour
{

    Transform wayPoint;
    NavMeshAgent agent;

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

            gameObject.SetActive(false);
        }
    }

    public void TakeDamage(int damage)
    {
        data.curruntHP -= damage;
        if (data.curruntHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // 적이 죽었을 때의 처리 (예: 적을 제거하고 보상 지급)
        gameObject.SetActive(false);
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

    public Enemy(EnemyMove enemy)
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