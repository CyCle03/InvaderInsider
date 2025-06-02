using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InvaderInsider; // StageManager 사용을 위해 추가

public class Tower : MonoBehaviour
{
    public float range = 5f;
    public float fireRate = 1f;
    public float dmg = 1f;
    private float fireCountdown = 0f;
    public GameObject projectilePrefab;

    private Transform finalWaypoint;

    void Start()
    {
        // StageManager에서 최종 WayPoint 위치 가져오기
        if (StageManager.Instance != null && StageManager.Instance.wayPoints.Count > 0)
        {
            // WayPoint 목록의 마지막 요소가 최종 목적지라고 가정
            finalWaypoint = StageManager.Instance.wayPoints[StageManager.Instance.wayPoints.Count - 1];
        }
        else
        {
            Debug.LogError("StageManager 또는 WayPoint 정보가 없습니다. 타겟팅 오류 발생 가능.");
        }
    }

    void Update()
    {
        // 타겟팅 로직 변경: 사거리 내에서 목적지에 가장 가까운 적 찾기
        GameObject targetEnemy = FindTargetEnemy();

        if (targetEnemy != null)
        {
            // 목표를 향해 포탑 회전 (선택 사항 - 포탑 모델에 따라 구현 방식 다름)
            // Vector3 direction = targetEnemy.transform.position - transform.position;
            // Quaternion lookRotation = Quaternion.LookRotation(direction);
            // Vector3 rotation = Quaternion.Lerp(transform.rotation, lookRotation, Time.deltaTime * 10).eulerAngles;
            // transform.rotation = Quaternion.Euler(0f, rotation.y, 0f); // Y축만 회전

            if (fireCountdown <= 0f)
            {
                Shoot(targetEnemy);
                fireCountdown = 1f / fireRate;
            }
            fireCountdown -= Time.deltaTime;
        }
        else
        {
             // 타겟이 없으면 발사 쿨타임 초기화 또는 유지 (선택 사항)
             // fireCountdown = 0f; 
        }
    }

    void Shoot(GameObject enemy)
    {
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        
        // 발사체 스크립트에 타겟 트랜스폼 전달
        Projectile projectile = proj.GetComponent<Projectile>();
        if (projectile != null)
        {
             // 발사체는 특정 적을 따라가도록 설정
            projectile.SetTarget(enemy.transform);
            projectile.SetDmg(dmg);
        }
        else
        {
            Debug.LogWarning("Projectile prefab is missing Projectile script!");
             // 발사체 스크립트가 없으면 그냥 앞으로 발사하거나 다른 처리
             // proj.GetComponent<Rigidbody>().AddForce(transform.forward * 10f); 
        }
    }

    // 사거리 내에서 최종 목적지에 가장 가까운 적을 찾는 함수
    GameObject FindTargetEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject target = null;
        float closestDistanceToEnd = Mathf.Infinity;
        Vector3 endPosition = Vector3.zero; // 최종 목적지 위치

        // 최종 WayPoint 위치 가져오기
        if (finalWaypoint != null)
        {
            endPosition = finalWaypoint.position;
        }
        else if (StageManager.Instance != null && StageManager.Instance.wayPoints.Count > 0)
        {
             // Start에서 finalWaypoint를 설정하지 못했거나 StageManager가 변경된 경우 재시도
             finalWaypoint = StageManager.Instance.wayPoints[StageManager.Instance.wayPoints.Count - 1];
             endPosition = finalWaypoint.position;
        }
        else
        {
            // StageManager나 WayPoint 정보가 여전히 없으면 타겟을 찾을 수 없음
            return null;
        }

        foreach (GameObject enemy in enemies)
        {
            // 적이 활성화되어 있고 EnemyObject 컴포넌트가 있는지 확인 (NavMeshAgent 사용 적만 대상으로)
            EnemyObject enemyObject = enemy.GetComponent<EnemyObject>();
            if (enemyObject != null && enemy.activeInHierarchy)
            {
                // 포탑 사거리 내에 있는지 체크
                float distanceToTower = Vector3.Distance(transform.position, enemy.transform.position);
                if (distanceToTower <= range)
                {
                    // 적의 현재 위치에서 최종 목적지까지의 거리 계산
                    float distanceToEnd = Vector3.Distance(enemy.transform.position, endPosition);

                    // 목적지에 더 가까운 적을 찾으면 타겟 갱신
                    if (distanceToEnd < closestDistanceToEnd)
                    {
                        closestDistanceToEnd = distanceToEnd;
                        target = enemy;
                    }
                }
            }
        }
        return target;
    }
}
