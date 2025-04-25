using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    EnemyMove em;
    Transform wayPoint;
    NavMeshAgent agent;

    // Start is called before the first frame update
    void Start()
    {
        em = GameObject.Find("GameManager").GetComponent<EnemyMove>();
        wayPoint = em.wayPoints[1];

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
}
