// 타워 회전 디버깅을 위한 플래그 (필요시 활성화)
#define DEBUG_TOWER_ROTATION
#define DEBUG_TOWER_ATTACK // 공격 디버깅 추가

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
        [SerializeField] private Transform partToRotate;
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float fireRate = 1f;
        [SerializeField] private float towerAttackRange = 5f;
        [SerializeField] private LayerMask enemyLayer = 1 << 6; // 기본값: 6번 레이어 (Enemy)

        private EnemyObject currentTarget;
        private float towerNextAttackTime = 0f;
        private bool isInitialized = false;
        private bool hasTarget = false; // 타겟 상태 추적

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
            
            /*#if DEBUG_TOWER_ATTACK && UNITY_EDITOR
            Debug.Log($"[Tower] 타워 초기화 완료 - 공격범위: {towerAttackRange}, 데미지: {attackDamage}, 연사: {fireRate}/s");
            if (projectilePrefab == null || firePoint == null)
            {
                Debug.LogWarning($"[Tower] 누락된 설정 - ProjectilePrefab: {projectilePrefab != null}, FirePoint: {firePoint != null}");
            }
            #endif*/
            
            isInitialized = true;
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
                #if DEBUG_TOWER_ATTACK && UNITY_EDITOR
                Debug.LogWarning($"[Tower] 'Enemy' 레이어가 존재하지 않음. 기본값 6번 레이어 사용");
                #endif
            }
        }

        private void Update()
        {
            if (!isInitialized) return;

            FindTarget();
            
            if (currentTarget != null && hasTarget)
            {
                RotateTowardsTarget();
                
                // 공격 쿨다운 확인 후 공격
                if (Time.time >= towerNextAttackTime)
                {
                    Attack(currentTarget as IDamageable);
                }
            }
        }

        private void FindTarget()
        {
            Collider[] enemies = Physics.OverlapSphere(transform.position, towerAttackRange, enemyLayer);
            
            if (enemies.Length > 0)
            {
                float closestDistance = Mathf.Infinity;
                EnemyObject closestEnemy = null;
                
                foreach (Collider enemy in enemies)
                {
                    EnemyObject enemyObject = enemy.GetComponent<EnemyObject>();
                    if (enemyObject != null && enemyObject.gameObject.activeInHierarchy)
                    {
                        float distance = Vector3.Distance(transform.position, enemy.transform.position);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestEnemy = enemyObject;
                        }
                    }
                }
                
                if (closestEnemy != currentTarget)
                {
                    currentTarget = closestEnemy;
                    hasTarget = currentTarget != null;
                    
                    /*#if DEBUG_TOWER_ROTATION && UNITY_EDITOR
                    if (hasTarget)
                    {
                        Debug.Log($"[Tower] 새 타겟 발견: {currentTarget.name}");
                    }
                    #endif*/
                }
            }
            else
            {
                if (hasTarget)
                {
                    /*#if DEBUG_TOWER_ROTATION && UNITY_EDITOR
                    Debug.Log("[Tower] 타겟 상실");
                    #endif*/
                    
                    currentTarget = null;
                    hasTarget = false;
                }
            }
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
            if (target == null || firePoint == null || projectilePrefab == null) 
            {
                #if DEBUG_TOWER_ATTACK && UNITY_EDITOR
                Debug.LogWarning($"[Tower] 공격 실패 - Target: {target != null}, FirePoint: {firePoint != null}, ProjectilePrefab: {projectilePrefab != null}");
                #endif
                return;
            }

            GameObject projectileObj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            Projectile projectile = projectileObj.GetComponent<Projectile>();
            
            if (projectile != null)
            {
                projectile.SetTarget((target as MonoBehaviour).transform);
                projectile.SetDmg(attackDamage);
                
                #if DEBUG_TOWER_ATTACK && UNITY_EDITOR
                Debug.Log($"[Tower] 발사체 발사! 대상: {(target as MonoBehaviour).name}, 데미지: {attackDamage}");
                #endif
            }
            else
            {
                #if DEBUG_TOWER_ATTACK && UNITY_EDITOR
                Debug.LogError($"[Tower] Projectile 컴포넌트가 없습니다!");
                #endif
            }

            towerNextAttackTime = Time.time + 1f / fireRate;
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
            // 공격 범위 표시
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, towerAttackRange);
            
            // 현재 타겟에 대한 선 그리기
            if (hasTarget && currentTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, currentTarget.transform.position);
            }
        }
    }
}
