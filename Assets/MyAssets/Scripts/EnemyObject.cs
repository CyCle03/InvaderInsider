using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace InvaderInsider.Core
{
    public enum EnemyType
    {
        Normal,
        Fast,
        Tank,
        Boss
    }

    [System.Serializable]
    public class EnemyData
    {
        [Header("Basic Info")]
        public string enemyName = "";
        public int enemyId = -1;
        public EnemyType enemyType = EnemyType.Normal;

        [Header("Base Stats")]
        public float baseHealth = 3f;
        public float baseDamage = 1f;
        public float moveSpeed = 3f;

        [Header("Rewards")]
        public int eDataAmount = 1;  // 기본 eData 보상량
    }

    public class EnemyObject : BaseCharacter
    {
        [Header("Enemy Data")]
        [SerializeField] private EnemyData enemyData = new EnemyData();
        
        [Header("Navigation")]
        [SerializeField] private float pathUpdateRate = 0.2f;
        private Transform currentWaypoint;
        private NavMeshAgent agent;
        private Queue<Transform> waypoints = new Queue<Transform>();
        
        [Header("UI")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private GameObject healthBarObject;

        [Header("Effects")]
        [SerializeField] private ParticleSystem hitEffect;
        [SerializeField] private ParticleSystem deathEffect;

        // Events
        public event Action<EnemyObject> OnWaypointReached;
        
        protected override void Start()
        {
            InitializeBaseStats();
            base.Start();
            
            agent = GetComponent<NavMeshAgent>();
            InitializeEnemy();
            
            if (StageManager.Instance != null)
            {
                InitializeWaypoints();
            }
            
            StartCoroutine(UpdatePathRoutine());
        }

        private void InitializeBaseStats()
        {
            // 기본 스탯 설정
            base.maxHealth = enemyData.baseHealth;
            base.attackDamage = enemyData.baseDamage;

            // 타입에 따른 스탯 수정
            switch (enemyData.enemyType)
            {
                case EnemyType.Fast:
                    enemyData.moveSpeed *= 1.5f;
                    base.maxHealth *= 0.5f;
                    enemyData.eDataAmount *= 2;  // Fast 타입은 2배의 eData
                    break;
                case EnemyType.Tank:
                    enemyData.moveSpeed *= 0.7f;
                    base.maxHealth *= 1.5f;
                    base.attackDamage *= 0.8f;
                    enemyData.eDataAmount *= 3;  // Tank 타입은 3배의 eData
                    break;
                case EnemyType.Boss:
                    enemyData.moveSpeed *= 0.5f;
                    base.maxHealth *= 3f;
                    base.attackDamage *= 2f;
                    enemyData.eDataAmount *= 5;  // 보스는 5배의 eData
                    break;
            }
        }

        private void InitializeEnemy()
        {
            if (agent != null)
            {
                agent.speed = enemyData.moveSpeed;
                agent.stoppingDistance = 0.2f;
            }

            UpdateHealthUI();
        }

        private System.Collections.IEnumerator UpdatePathRoutine()
        {
            while (enabled)
            {
                UpdatePath();
                yield return new WaitForSeconds(pathUpdateRate);
            }
        }

        private void UpdatePath()
        {
            if (currentWaypoint == null || agent == null) return;

            if (Vector3.Distance(transform.position, currentWaypoint.position) <= agent.stoppingDistance)
            {
                OnWaypointReached?.Invoke(this);
                MoveToNextWaypoint();
            }
            else if (agent.isActiveAndEnabled)
            {
                agent.SetDestination(currentWaypoint.position);
            }
        }

        private void InitializeWaypoints()
        {
            waypoints.Clear();
            foreach (Transform waypoint in StageManager.Instance.wayPoints)
            {
                waypoints.Enqueue(waypoint);
            }
            MoveToNextWaypoint();
        }

        private void MoveToNextWaypoint()
        {
            if (waypoints.Count > 0)
            {
                currentWaypoint = waypoints.Dequeue();
                if (agent != null && agent.isActiveAndEnabled)
                {
                    agent.SetDestination(currentWaypoint.position);
                }
            }
            else
            {
                ReachFinalDestination();
            }
        }

        private void ReachFinalDestination()
        {
            var destination = currentWaypoint.GetComponent<CharacterObject>();
            if (destination != null && destination.IsDestinationPoint)
            {
                Attack(destination);
            }
            Die();
        }

        public override void Attack(IDamageable target)
        {
            target.TakeDamage(base.attackDamage);
        }

        public override void TakeDamage(float damage)
        {
            base.TakeDamage(damage);
            
            if (hitEffect != null)
            {
                hitEffect.Play();
            }
            
            UpdateHealthUI();
        }

        private void UpdateHealthUI()
        {
            if (healthSlider != null)
            {
                healthSlider.value = base.currentHealth / base.maxHealth;
                healthBarObject.SetActive(base.currentHealth < base.maxHealth);
            }
        }

        protected override void Die()
        {
            if (deathEffect != null)
            {
                var effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
                effect.Play();
                Destroy(effect.gameObject, effect.main.duration);
            }

            // eData 보상 지급
            GameManager.Instance.UpdateEData(enemyData.eDataAmount);
            
            base.Die();
        }

        // Properties
        public new float CurrentHealth => base.currentHealth;
        public new float MaxHealth => base.maxHealth;
        public new float AttackDamage => base.attackDamage;
        public new float AttackRange => agent.stoppingDistance;
        public EnemyType Type => enemyData.enemyType;
    }
}