using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    public float range = 5f;
    public float fireRate = 1f;
    public float dmg = 1f;
    private float fireCountdown = 0f;
    public GameObject projectilePrefab;

    void Update()
    {
        GameObject enemy = FindClosestEnemy();
        if (enemy != null && Vector3.Distance(transform.position, enemy.transform.position) <= range)
        {
            if (fireCountdown <= 0f)
            {
                Shoot(enemy);
                fireCountdown = 1f / fireRate;
            }
            fireCountdown -= Time.deltaTime;
        }
    }

    void Shoot(GameObject enemy)
    {
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        proj.GetComponent<Projectile>().SetTarget(enemy.transform);
        proj.GetComponent<Projectile>().SetDmg(dmg);
    }

    GameObject FindClosestEnemy()
    {
        // 적 찾기 로직 (예: 태그 기반 탐색)
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject closest = null;
        float shortestDistance = Mathf.Infinity;

        foreach (GameObject e in enemies)
        {
            float dist = Vector3.Distance(transform.position, e.transform.position);
            if (dist < shortestDistance)
            {
                shortestDistance = dist;
                closest = e;
            }
        }
        return closest;
    }
}
