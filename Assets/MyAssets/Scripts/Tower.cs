using UnityEngine;
using InvaderInsider.Data;
using InvaderInsider.Cards;
using InvaderInsider.Managers;
using System;
using System.Collections.Generic;

namespace InvaderInsider
{
    public class Tower : BaseCharacter
    {
        // 성능 최적화 상수들
        private const float TARGET_SEARCH_INTERVAL = 0.15f; // 적 탐지 주기
        private const float PROJECTILE_SPEED = 15f;
        private const int MAX_DETECTION_COLLIDERS = 30; // 감지 가능한 최대 적 수
        private const float TARGET_LOST_DISTANCE_MULTIPLIER = 1.2f; // 타겟 놓치는 거리 (사거리의 120%)

        [Header("Tower Specific")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float projectileSpeed = PROJECTILE_SPEED;
        [SerializeField] private Transform partToRotate; // 회전할 포탑 파트
        [SerializeField] private float towerAttackRange = 5f; // 타워 공격 사거리
        [SerializeField] private LayerMask enemyLayer = 1 << 6; // Enemy 레이어 마스크
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private AudioSource fireSound;

        // 성능 최적화용 캐시된 변수들
        private EnemyObject currentTarget;
        private float nextTargetSearchTime = 0f;
        private float targetLostDistance;
        private int enemyLayerMask = -1;
        
        // 메모리 할당 최적화
        private readonly Collider[] detectionBuffer = new Collider[MAX_DETECTION_COLLIDERS];
        private readonly List<EnemyObject> validTargets = new List<EnemyObject>(MAX_DETECTION_COLLIDERS);

        public override float AttackRange => towerAttackRange;

        protected override void Awake()
        {
            base.Awake();
            
            if (partToRotate == null)
            {
                partToRotate = transform;
            }
            
            // Enemy 레이어가 6번인지 확인하고 설정
            SetupEnemyLayer();
            
            // 레이어 마스크 캐싱
            enemyLayerMask = LayerMask.GetMask("Enemy");
            targetLostDistance = AttackRange * TARGET_LOST_DISTANCE_MULTIPLIER;
        }
        
        private void SetupEnemyLayer()
        {
            // Enemy 레이어가 6번으로 설정되어 있는지 확인
            int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
            
            if (enemyLayerIndex != -1)
            {
                // Enemy 레이어가 존재하면 해당 레이어를 사용
                enemyLayer = 1 << enemyLayerIndex;
            }
            else
            {
                // Enemy 레이어가 없으면 6번 레이어를 기본값으로 사용
                enemyLayer = 1 << 6;
#if UNITY_EDITOR
                Debug.LogWarning($"[Tower] 'Enemy' 레이어가 존재하지 않음. 기본값 6번 레이어 사용");
#endif
            }
        }

        private void Update()
        {
            if (Time.time >= nextTargetSearchTime)
            {
                if (currentTarget == null || !IsValidTarget(currentTarget))
                {
                    FindNewTarget();
                }
                else if (Time.time >= nextAttackTime)
                {
                    Attack(currentTarget);
                }
                
                nextTargetSearchTime = Time.time + TARGET_SEARCH_INTERVAL;
            }
        }

        private bool IsValidTarget(EnemyObject target)
        {
            if (target == null || target.gameObject == null) return false;
            
            // 거리 체크 (사거리보다 조금 더 여유롭게)
            float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
            return distanceToTarget <= targetLostDistance && target.CurrentHealth > 0;
        }

        private void FindNewTarget()
        {
            currentTarget = null;
            validTargets.Clear();

            // 물리 기반 적 탐지 (메모리 할당 최적화)
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position, 
                AttackRange, 
                detectionBuffer, 
                enemyLayerMask
            );

            // 유효한 적들을 리스트에 추가
            for (int i = 0; i < hitCount; i++)
            {
                if (detectionBuffer[i].TryGetComponent<EnemyObject>(out var enemy))
                {
                    if (enemy.CurrentHealth > 0)
                    {
                        validTargets.Add(enemy);
                    }
                }
            }

            // 가장 가까운 적을 타겟으로 선택
            if (validTargets.Count > 0)
            {
                currentTarget = GetNearestTarget(validTargets);
            }
        }

        private EnemyObject GetNearestTarget(List<EnemyObject> targets)
        {
            if (targets.Count == 0) return null;

            EnemyObject nearestEnemy = null;
            float nearestDistance = float.MaxValue;
            Vector3 towerPosition = transform.position;

            foreach (var enemy in targets)
            {
                float distance = Vector3.SqrMagnitude(enemy.transform.position - towerPosition);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = enemy;
                }
            }

            return nearestEnemy;
        }

        private void RotateTowardsTarget()
        {
            if (currentTarget == null) return;

            Vector3 directionToTarget = currentTarget.transform.position - partToRotate.position;
            directionToTarget.y = 0; // Y축 회전만 고려
            
            if (directionToTarget != Vector3.zero)
            {
                // Unity에서 forward 방향(Z+)을 기준으로 올바른 각도 계산
                float targetAngle = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
                float currentY = partToRotate.eulerAngles.y;
                float newY = Mathf.LerpAngle(currentY, targetAngle, Time.deltaTime * 5f);
                
                partToRotate.rotation = Quaternion.Euler(0, newY, 0);
            }
        }

        public override void Attack(IDamageable target)
        {
            if (target == null || projectilePrefab == null) return;

            Transform targetTransform = null;
            
            // 타겟이 EnemyObject인 경우 Transform 가져오기
            if (target is EnemyObject enemyTarget)
            {
                targetTransform = enemyTarget.transform;
            }
            // 타겟이 Component인 경우
            else if (target is Component component)
            {
                targetTransform = component.transform;
            }

            if (targetTransform == null) return;

            // 발사 위치 설정
            Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
            
            // 투사체 생성 및 초기화
            GameObject projectileObj = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
            Projectile projectile = projectileObj.GetComponent<Projectile>();
            
            if (projectile != null)
            {
                projectile.Initialize(target, AttackDamage, projectileSpeed);
            }

            // 시각/오디오 효과
            PlayAttackEffects();

            // 다음 공격 시간 설정
            nextAttackTime = Time.time + attackRate;
        }

        private void PlayAttackEffects()
        {
            // 머즐 플래시 효과
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }

            // 발사 사운드
            if (fireSound != null && !fireSound.isPlaying)
            {
                fireSound.Play();
            }
        }

        public override void ApplyEquipment(CardDBObject cardData)
        {
            if (cardData == null) return;

            base.ApplyEquipment(cardData);
            
            if (cardData.type == CardType.Equipment && cardData.equipmentTarget == EquipmentTargetType.Tower)
            {
                attackDamage += cardData.equipmentBonusAttack;
            }
        }
        
        // 디버깅을 위한 Gizmo 그리기
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            // 사거리 표시
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // 반투명 초록색
            Gizmos.DrawWireSphere(transform.position, AttackRange);

            // 현재 타겟 표시
            if (currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentTarget.transform.position);
                Gizmos.DrawWireSphere(currentTarget.transform.position, 1f);
            }
        }

        // 타워 업그레이드 시 사거리 재계산
        public override void LevelUp()
        {
            base.LevelUp(); // 기본 레벨업 로직 호출
            // Tower만의 레벨업 로직
            targetLostDistance = AttackRange * TARGET_LOST_DISTANCE_MULTIPLIER;
        }
    }
}
