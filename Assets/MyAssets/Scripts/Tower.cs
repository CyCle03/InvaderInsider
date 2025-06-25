using UnityEngine;
using InvaderInsider.Data;
using InvaderInsider.Cards;
using InvaderInsider.Managers;
using InvaderInsider.ScriptableObjects;
using System;
using System.Collections.Generic;

namespace InvaderInsider
{
    public class Tower : BaseCharacter
    {
        private const string LOG_PREFIX = "[Tower] ";

        [Header("Tower Specific")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
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
        private Collider[] detectionBuffer;
        private readonly List<EnemyObject> validTargets = new List<EnemyObject>();

        // 설정 참조
        private GameConfigSO towerConfig;

        public override float AttackRange => towerAttackRange;

        protected override void Awake()
        {
            base.Awake();
            
            // 설정 로드
            LoadConfig();
            
            if (partToRotate == null)
            {
                partToRotate = transform;
                Debug.LogWarning($"{LOG_PREFIX}{gameObject.name}: partToRotate가 설정되지 않음. transform을 기본값으로 사용합니다.");
            }
            
            // Enemy 레이어 설정
            SetupEnemyLayer();
            
            // 레이어 마스크 캐싱
            enemyLayerMask = LayerMask.GetMask(towerConfig.enemyLayerName);
            if (enemyLayerMask == 0)
            {
                Debug.LogError($"{LOG_PREFIX}{gameObject.name}: '{towerConfig.enemyLayerName}' 레이어를 찾을 수 없습니다. 레이어 설정을 확인해주세요.");
                enemyLayerMask = 1 << towerConfig.defaultEnemyLayerIndex; // 폴백
            }
            
            targetLostDistance = AttackRange * towerConfig.targetLostDistanceMultiplier;
            
            // 버퍼 초기화
            detectionBuffer = new Collider[towerConfig.maxDetectionColliders];
        }
        
        private void LoadConfig()
        {
            var configManager = ConfigManager.Instance;
            if (configManager != null && configManager.GameConfig != null)
            {
                towerConfig = configManager.GameConfig;
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX}{gameObject.name}: ConfigManager 또는 GameConfig를 찾을 수 없습니다. 기본값을 사용합니다.");
                // 기본값으로 폴백
                towerConfig = ScriptableObject.CreateInstance<GameConfigSO>();
            }
        }
        
        private void SetupEnemyLayer()
        {
            // Enemy 레이어 인덱스 확인
            int enemyLayerIndex = LayerMask.NameToLayer(towerConfig.enemyLayerName);
            
            if (enemyLayerIndex != -1)
            {
                // Enemy 레이어가 존재하면 해당 레이어를 사용
                enemyLayer = 1 << enemyLayerIndex;
            }
            else
            {
                // Enemy 레이어가 없으면 기본값 사용
                enemyLayer = 1 << towerConfig.defaultEnemyLayerIndex;
#if UNITY_EDITOR
                Debug.LogWarning($"{LOG_PREFIX}{gameObject.name}: '{towerConfig.enemyLayerName}' 레이어가 존재하지 않음. 기본값 {towerConfig.defaultEnemyLayerIndex}번 레이어 사용");
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
                
                nextTargetSearchTime = Time.time + towerConfig.targetSearchInterval;
            }
        }

        private bool IsValidTarget(EnemyObject target)
        {
            if (target == null)
            {
                Debug.LogWarning($"{LOG_PREFIX}{gameObject.name}: 타겟이 null입니다.");
                return false;
            }
            
            if (target.gameObject == null)
            {
                Debug.LogWarning($"{LOG_PREFIX}{gameObject.name}: 타겟의 GameObject가 null입니다.");
                return false;
            }
            
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
                if (detectionBuffer[i] == null)
                {
                    Debug.LogWarning($"{LOG_PREFIX}{gameObject.name}: 감지된 콜라이더가 null입니다. (인덱스: {i})");
                    continue;
                }

                if (detectionBuffer[i].TryGetComponent<EnemyObject>(out var enemy))
                {
                    if (enemy != null && enemy.CurrentHealth > 0)
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
            if (targets == null)
            {
                Debug.LogError($"{LOG_PREFIX}{gameObject.name}: 타겟 리스트가 null입니다.");
                return null;
            }
            
            if (targets.Count == 0)
            {
                return null;
            }

            EnemyObject nearestEnemy = null;
            float nearestDistance = float.MaxValue;
            Vector3 towerPosition = transform.position;

            foreach (var enemy in targets)
            {
                if (enemy == null || enemy.transform == null)
                {
                    Debug.LogWarning($"{LOG_PREFIX}{gameObject.name}: 타겟 리스트에 null 적이 포함되어 있습니다.");
                    continue;
                }

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
            if (currentTarget == null)
            {
                Debug.LogWarning($"{LOG_PREFIX}{gameObject.name}: 회전할 타겟이 없습니다.");
                return;
            }

            if (partToRotate == null)
            {
                Debug.LogError($"{LOG_PREFIX}{gameObject.name}: 회전할 부분(partToRotate)이 설정되지 않았습니다.");
                return;
            }

            Vector3 directionToTarget = currentTarget.transform.position - partToRotate.position;
            directionToTarget.y = 0; // Y축 회전만 고려
            
            if (directionToTarget != Vector3.zero)
            {
                // Unity에서 forward 방향(Z+)을 기준으로 올바른 각도 계산
                float targetAngle = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
                float currentY = partToRotate.eulerAngles.y;
                float newY = Mathf.LerpAngle(currentY, targetAngle, Time.deltaTime * towerConfig.towerRotationSpeed);
                
                partToRotate.rotation = Quaternion.Euler(0, newY, 0);
            }
        }

        public override void Attack(IDamageable target)
        {
            if (target == null)
            {
                Debug.LogError($"{LOG_PREFIX}{gameObject.name}: 공격 타겟이 null입니다.");
                return;
            }

            if (projectilePrefab == null)
            {
                Debug.LogError($"{LOG_PREFIX}{gameObject.name}: 투사체 프리팹이 설정되지 않았습니다.");
                return;
            }

            Transform targetTransform = null;
            
            // 타겟이 EnemyObject인 경우 Transform 가져오기
            if (target is EnemyObject enemyTarget)
            {
                if (enemyTarget == null)
                {
                    Debug.LogError($"{LOG_PREFIX}{gameObject.name}: EnemyTarget이 null입니다.");
                    return;
                }
                targetTransform = enemyTarget.transform;
            }
            // 타겟이 Component인 경우
            else if (target is Component component)
            {
                if (component == null)
                {
                    Debug.LogError($"{LOG_PREFIX}{gameObject.name}: 타겟 컴포넌트가 null입니다.");
                    return;
                }
                targetTransform = component.transform;
            }

            if (targetTransform == null)
            {
                Debug.LogError($"{LOG_PREFIX}{gameObject.name}: 타겟의 Transform을 찾을 수 없습니다.");
                return;
            }

            // 발사 위치 설정
            Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
            
            // 투사체 생성 및 초기화
            GameObject projectileObj = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
            if (projectileObj == null)
            {
                Debug.LogError($"{LOG_PREFIX}{gameObject.name}: 투사체 인스턴스 생성에 실패했습니다.");
                return;
            }

            Projectile projectile = projectileObj.GetComponent<Projectile>();
            
            if (projectile != null)
            {
                projectile.Initialize(target, AttackDamage, towerConfig.projectileSpeed);
            }
            else
            {
                Debug.LogError($"{LOG_PREFIX}{gameObject.name}: 투사체에 Projectile 컴포넌트가 없습니다.");
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
            if (cardData == null)
            {
                Debug.LogError($"{LOG_PREFIX}{gameObject.name}: 장비 카드 데이터가 null입니다.");
                return;
            }

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
            }
        }

        public override void LevelUp()
        {
            base.LevelUp();
            // 타워 레벨업 시 추가 로직
        }
    }
}
