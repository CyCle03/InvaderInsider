using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InvaderInsider;
using InvaderInsider.Managers; // StageManager 사용을 위해 추가

public class Tower : MonoBehaviour
{
    public float range = 5f;
    public float fireRate = 1f;
    public float dmg = 1f;
    private float fireCountdown = 0f;
    public GameObject projectilePrefab;

    // 회전할 포탑 부분을 여기에 할당하세요.
    public Transform partToRotate;
    public float turnSpeed = 10f; // 회전 속도

    // 발사체가 나갈 지점
    public Transform firePoint;

    private Transform finalWaypoint;
    private StageManager _stageManager; // StageManager 인스턴스 캐싱

    private Quaternion _initialPartToRotateLocalRotation; // partToRotate의 초기 로컬 회전 저장

    void Start()
    {
        _stageManager = StageManager.Instance; // StageManager 인스턴스 캐싱

        // StageManager에서 최종 WayPoint 위치 가져오기
        if (_stageManager != null && _stageManager.wayPoints.Count > 0)
        {
            // WayPoint 목록의 마지막 요소가 최종 목적지라고 가정
            finalWaypoint = _stageManager.wayPoints[_stageManager.wayPoints.Count - 1];
        }
        else
        {
            Debug.LogError("StageManager 또는 WayPoint 정보가 없습니다. 타겟팅 오류 발생 가능.");
        }

        // partToRotate의 초기 로컬 회전 저장
        if (partToRotate != null)
        {
            _initialPartToRotateLocalRotation = partToRotate.localRotation;
        }
    }

    void Update()
    {
        // 타겟팅 로직 변경: 사거리 내에서 목적지에 가장 가까운 적 찾기
        GameObject targetEnemy = FindTargetEnemy();

        if (targetEnemy != null && partToRotate != null)
        {
            // 타겟까지의 월드 방향 벡터를 계산하되, Y축은 무시하여 수평 회전만 고려합니다.
            Vector3 directionToTarget = targetEnemy.transform.position - partToRotate.position;
            directionToTarget.y = 0f; 

            // 방향 벡터가 유효한 경우 (0 벡터가 아닌 경우)
            if (directionToTarget != Vector3.zero)
            {
                // 1. 타겟을 바라보는 월드 회전(Yaw)을 계산합니다. (Z축이 전방, Y축이 상방)
                Quaternion targetWorldYaw = Quaternion.LookRotation(directionToTarget, Vector3.up);

                // 2. 이 월드 Y축 회전을 partToRotate의 부모 공간에서의 로컬 회전으로 변환합니다.
                //    이것이 빈 게임 오브젝트인 partToRotate가 가져야 할 로컬 Y축 회전입니다.
                Quaternion desiredLocalRotation = Quaternion.Inverse(partToRotate.parent.rotation) * targetWorldYaw;

                // 3. desiredLocalRotation에서 Y축 각도만 추출하여 새로운 Quaternion을 만듭니다.
                //    이는 partToRotate의 로컬 X, Z축 회전을 0으로 고정합니다.
                Quaternion finalTargetLocalRotation = Quaternion.Euler(0f, desiredLocalRotation.eulerAngles.y, 0f);

                // 4. 현재 로컬 회전을 목표 로컬 회전으로 부드럽게 보간합니다.
                partToRotate.localRotation = Quaternion.Slerp(
                    partToRotate.localRotation,
                    finalTargetLocalRotation,
                    Time.deltaTime * turnSpeed
                );
            }

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
        // firePoint가 설정되어 있으면 해당 위치와 회전에서 발사체를 생성합니다.
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        
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
        else if (_stageManager != null && _stageManager.wayPoints.Count > 0)
        {
             // Start에서 finalWaypoint를 설정하지 못했거나 StageManager가 변경된 경우 재시도
             finalWaypoint = _stageManager.wayPoints[_stageManager.wayPoints.Count - 1];
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

    // partToRotate를 초기 상태로 되돌리는 메서드
    public void ResetTowerRotation()
    {
        if (partToRotate != null)
        {
            partToRotate.localRotation = _initialPartToRotateLocalRotation;
            Debug.Log($"{gameObject.name}의 포탑 회전이 초기 상태로 재설정되었습니다.");
        }
    }
}
