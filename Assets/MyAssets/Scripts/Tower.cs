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
            if (!isInitialized || StageManager.Instance == null) return;
            
            var wayPoints = StageManager.Instance.WayPoints;
            if (wayPoints == null || wayPoints.Count == 0) return;

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
            if (target == null || firePoint == null || projectilePrefab == null) return;

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
                
                if (Application.isPlaying)
                {
                    Debug.Log($"[Tower] 타워 장비 적용: {cardData.cardName}, 공격력 증가: {cardData.equipmentBonusAttack}");
                }
            }
        }

        private void FindTarget()
        {
            if (StageManager.Instance == null || StageManager.Instance.WayPoints == null) return;

            float closestDistance = towerAttackRange;
            IDamageable closestTarget = null;

            foreach (var waypoint in StageManager.Instance.WayPoints)
            {
                if (waypoint == null) continue;

                float distance = Vector3.Distance(transform.position, waypoint.position);
                if (distance <= towerAttackRange)
                {
                    var enemies = Physics.OverlapSphere(waypoint.position, 0.5f, enemyLayer);
                    foreach (var enemy in enemies)
                    {
                        var enemyObject = enemy.GetComponent<EnemyObject>();
                        if (enemyObject != null && enemyObject.gameObject.activeInHierarchy)
                        {
                            float enemyDistance = Vector3.Distance(transform.position, enemy.transform.position);
                            if (enemyDistance < closestDistance)
                            {
                                closestDistance = enemyDistance;
                                closestTarget = enemyObject;
                            }
                        }
                    }
                }
            }

            currentTarget = closestTarget as EnemyObject;
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
