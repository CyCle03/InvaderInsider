// 타워 회전 디버깅을 위한 플래그 (필요시 활성화)
#define DEBUG_TOWER_ROTATION

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
        [SerializeField] private LayerMask enemyLayer;

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
            
            isInitialized = true;
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
                    
                    #if DEBUG_TOWER_ROTATION && UNITY_EDITOR
                    if (hasTarget)
                    {
                        Debug.Log($"[Tower] 새 타겟 발견: {currentTarget.name}");
                    }
                    #endif
                }
            }
            else
            {
                if (hasTarget)
                {
                    #if DEBUG_TOWER_ROTATION && UNITY_EDITOR
                    Debug.Log("[Tower] 타겟 상실");
                    #endif
                    
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
                
                #if DEBUG_TOWER_ROTATION && UNITY_EDITOR
                Debug.Log($"[Tower] 회전 - 현재: {currentY:F1}°, 목표: {targetAngle:F1}°, 새로운: {newY:F1}°");
                #endif
                
                partToRotate.rotation = Quaternion.Euler(0, newY, 0);
            }
        }

        public override void Attack(IDamageable target)
        {
            if (target == null || firePoint == null || projectilePrefab == null) 
            {
                return;
            }

            GameObject projectileObj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            Projectile projectile = projectileObj.GetComponent<Projectile>();
            
            if (projectile != null)
            {
                projectile.SetTarget((target as MonoBehaviour).transform);
                projectile.SetDmg(attackDamage);
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
    }
}
