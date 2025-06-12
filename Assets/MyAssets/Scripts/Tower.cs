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

        public override float AttackRange => towerAttackRange;

        protected override void Awake()
        {
            base.Awake();
            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized) return;

            if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
            {
                FindTarget();
            }

            if (currentTarget != null)
            {
                RotateTurret();
                if (Time.time >= towerNextAttackTime)
                {
                    Attack(currentTarget as IDamageable);
                }
            }
            else
            {
                ResetRotation();
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

        private void FindTarget()
        {
            float closestDistance = towerAttackRange;
            EnemyObject closestTarget = null;

            // Physics.OverlapSphere를 사용하여 효율적으로 범위 내 적들을 찾기
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, towerAttackRange, enemyLayer);
            
            foreach (var collider in hitColliders)
            {
                // 태그로 한 번 더 검증
                if (!collider.CompareTag("Enemy")) continue;
                
                EnemyObject enemy = collider.GetComponent<EnemyObject>();
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = enemy;
                }
            }

            // 레이어 검색에서 찾지 못했다면 fallback으로 모든 EnemyObject 검색
            if (closestTarget == null)
            {
                EnemyObject[] allEnemies = FindObjectsOfType<EnemyObject>();
                
                foreach (var enemy in allEnemies)
                {
                    if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
                    if (distance <= towerAttackRange && distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestTarget = enemy;
                    }
                }
            }

            currentTarget = closestTarget;
        }

        private void RotateTurret()
        {
            if (currentTarget == null || partToRotate == null) return;

            Vector3 directionToTarget = currentTarget.transform.position - partToRotate.position;
            directionToTarget.y = 0f;

            if (directionToTarget != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                partToRotate.rotation = Quaternion.Slerp(partToRotate.rotation, targetRotation, Time.deltaTime * 10f);
            }
        }

        public void ResetRotation()
        {
            if (partToRotate != null)
            {
                partToRotate.rotation = Quaternion.identity;
            }
        }
    }
}
