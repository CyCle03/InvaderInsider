using UnityEngine;
using InvaderInsider.Core;

namespace InvaderInsider
{
    /// <summary>
    /// 적이 무적인 문제를 해결하는 스크립트
    /// </summary>
    public class EnemyDamageFixer : MonoBehaviour
    {
        private const string LOG_PREFIX = "EnemyDamageFixer";
        
        [Header("Fix Settings")]
        [SerializeField] private bool autoFixOnStart = true;
        [SerializeField] private bool forceEnemyInitialization = true;
        [SerializeField] private bool fixProjectileCollision = true;
        [SerializeField] private bool enableDebugLogs = false;
        
        private void Start()
        {
            if (autoFixOnStart)
            {
                FixEnemyDamageIssues();
            }
        }
        
        /// <summary>
        /// 적 데미지 문제 전체 수정
        /// </summary>
        [ContextMenu("Fix Enemy Damage Issues")]
        public void FixEnemyDamageIssues()
        {
            DebugUtils.LogInfo(LOG_PREFIX, "적 데미지 문제 수정 시작");
            
            // 1. 모든 적 강제 초기화
            if (forceEnemyInitialization)
            {
                ForceInitializeAllEnemies();
            }
            
            // 2. 투사체 충돌 문제 수정
            if (fixProjectileCollision)
            {
                FixProjectileCollisionIssues();
            }
            
            // 3. 적 레이어 및 콜라이더 수정
            FixEnemyLayersAndColliders();
            
            // 4. 실시간 데미지 테스트
            StartCoroutine(TestEnemyDamageRoutine());
            
            DebugUtils.LogInfo(LOG_PREFIX, "적 데미지 문제 수정 완료");
        }
        
        /// <summary>
        /// 모든 적 강제 초기화
        /// </summary>
        private void ForceInitializeAllEnemies()
        {
            EnemyObject[] allEnemies = FindObjectsOfType<EnemyObject>();
            int fixedCount = 0;
            
            foreach (EnemyObject enemy in allEnemies)
            {
                if (enemy == null) continue;
                
                // 강제 초기화
                if (!enemy.IsInitialized)
                {
                    enemy.Initialize();
                    fixedCount++;
                    DebugUtils.LogVerbose(LOG_PREFIX, $"적 강제 초기화: {enemy.name}");
                }
                
                // 체력 확인 및 수정
                if (enemy.CurrentHealth <= 0 && enemy.MaxHealth > 0)
                {
                    // 체력을 최대치로 복구
                    enemy.Heal(enemy.MaxHealth);
                    fixedCount++;
                    DebugUtils.LogVerbose(LOG_PREFIX, $"적 체력 복구: {enemy.name} -> {enemy.MaxHealth}");
                }
                
                // IDamageable 인터페이스 확인
                IDamageable damageable = enemy.GetComponent<IDamageable>();
                if (damageable == null)
                {
                    DebugUtils.LogError(LOG_PREFIX, $"적 {enemy.name}에 IDamageable 인터페이스가 없음!");
                }
            }
            
            DebugUtils.LogInfo(LOG_PREFIX, $"{fixedCount}개 적 초기화/체력 수정됨 (총 {allEnemies.Length}개)");
        }
        
        /// <summary>
        /// 투사체 충돌 문제 수정
        /// </summary>
        private void FixProjectileCollisionIssues()
        {
            // 모든 투사체에 향상된 충돌 감지 추가
            Projectile[] allProjectiles = FindObjectsOfType<Projectile>();
            
            foreach (Projectile projectile in allProjectiles)
            {
                if (projectile == null) continue;
                
                // 향상된 충돌 감지 컴포넌트 추가
                if (projectile.GetComponent<EnhancedProjectileCollision>() == null)
                {
                    projectile.gameObject.AddComponent<EnhancedProjectileCollision>();
                }
            }
            
            DebugUtils.LogVerbose(LOG_PREFIX, $"{allProjectiles.Length}개 투사체에 향상된 충돌 감지 추가");
        }
        
        /// <summary>
        /// 적 레이어 및 콜라이더 수정
        /// </summary>
        private void FixEnemyLayersAndColliders()
        {
            EnemyObject[] allEnemies = FindObjectsOfType<EnemyObject>();
            int fixedCount = 0;
            
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer == -1) enemyLayer = 6; // 기본값
            
            foreach (EnemyObject enemy in allEnemies)
            {
                if (enemy == null) continue;
                
                bool wasFixed = false;
                
                // 레이어 수정
                if (enemy.gameObject.layer != enemyLayer)
                {
                    enemy.gameObject.layer = enemyLayer;
                    wasFixed = true;
                }
                
                // 콜라이더 확인 및 수정
                Collider[] colliders = enemy.GetComponents<Collider>();
                bool hasValidCollider = false;
                
                foreach (Collider col in colliders)
                {
                    if (col != null && col.enabled)
                    {
                        hasValidCollider = true;
                        // 트리거가 아닌 콜라이더가 있어야 투사체와 충돌 가능
                        if (col.isTrigger)
                        {
                            // 추가 콜라이더 생성 (트리거가 아닌)
                            BoxCollider solidCollider = enemy.gameObject.AddComponent<BoxCollider>();
                            solidCollider.isTrigger = false;
                            solidCollider.size = col.bounds.size;
                            solidCollider.center = enemy.transform.InverseTransformPoint(col.bounds.center);
                            wasFixed = true;
                            break;
                        }
                    }
                }
                
                // 콜라이더가 없으면 새로 추가
                if (!hasValidCollider)
                {
                    BoxCollider newCollider = enemy.gameObject.AddComponent<BoxCollider>();
                    newCollider.isTrigger = false;
                    newCollider.size = new Vector3(1f, 2f, 1f);
                    newCollider.center = new Vector3(0, 1f, 0);
                    wasFixed = true;
                }
                
                if (wasFixed)
                {
                    fixedCount++;
                    DebugUtils.LogVerbose(LOG_PREFIX, $"적 레이어/콜라이더 수정: {enemy.name}");
                }
            }
            
            DebugUtils.LogInfo(LOG_PREFIX, $"{fixedCount}개 적의 레이어/콜라이더 수정됨");
        }
        
        /// <summary>
        /// 실시간 데미지 테스트
        /// </summary>
        private System.Collections.IEnumerator TestEnemyDamageRoutine()
        {
            yield return new WaitForSeconds(1f);
            
            while (enabled)
            {
                yield return new WaitForSeconds(3f);
                
                if (enableDebugLogs)
                {
                    TestEnemyDamage();
                }
            }
        }
        
        /// <summary>
        /// 적 데미지 테스트
        /// </summary>
        private void TestEnemyDamage()
        {
            EnemyObject[] allEnemies = FindObjectsOfType<EnemyObject>();
            
            if (allEnemies.Length == 0)
            {
                DebugUtils.LogVerbose(LOG_PREFIX, "테스트할 적이 없음");
                return;
            }
            
            EnemyObject testEnemy = allEnemies[0];
            if (testEnemy != null && testEnemy.IsAlive)
            {
                float oldHealth = testEnemy.CurrentHealth;
                
                // 1 데미지 테스트
                testEnemy.TakeDamage(1f);
                
                float newHealth = testEnemy.CurrentHealth;
                
                if (oldHealth == newHealth)
                {
                    DebugUtils.LogError(LOG_PREFIX, $"적 {testEnemy.name}이 데미지를 받지 않음! (체력: {oldHealth})");
                    
                    // 강제 데미지 적용
                    ForceApplyDamage(testEnemy, 1f);
                }
                else
                {
                    DebugUtils.LogVerbose(LOG_PREFIX, $"적 {testEnemy.name} 데미지 정상 작동 ({oldHealth} -> {newHealth})");
                }
            }
        }
        
        /// <summary>
        /// 강제 데미지 적용
        /// </summary>
        private void ForceApplyDamage(EnemyObject enemy, float damage)
        {
            if (enemy == null) return;
            
            // Reflection을 사용해서 직접 체력 조작
            var baseType = enemy.GetType().BaseType; // BaseCharacter
            var currentHealthField = baseType.GetField("currentHealth", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (currentHealthField != null)
            {
                float currentHealth = (float)currentHealthField.GetValue(enemy);
                float newHealth = Mathf.Max(0, currentHealth - damage);
                currentHealthField.SetValue(enemy, newHealth);
                
                DebugUtils.LogInfo(LOG_PREFIX, $"강제 데미지 적용: {enemy.name} ({currentHealth} -> {newHealth})");
                
                // 체력이 0 이하면 사망 처리
                if (newHealth <= 0)
                {
                    var dieMethod = baseType.GetMethod("Die", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    dieMethod?.Invoke(enemy, null);
                }
            }
        }
        
        /// <summary>
        /// 수동 적 데미지 테스트 (F8 키)
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                ManualDamageTest();
            }
        }
        
        /// <summary>
        /// 수동 데미지 테스트
        /// </summary>
        private void ManualDamageTest()
        {
            EnemyObject[] allEnemies = FindObjectsOfType<EnemyObject>();
            
            if (allEnemies.Length == 0)
            {
                DebugUtils.LogInfo(LOG_PREFIX, "테스트할 적이 없습니다");
                return;
            }
            
            EnemyObject testEnemy = allEnemies[0];
            if (testEnemy != null)
            {
                DebugUtils.LogInfo(LOG_PREFIX, $"수동 데미지 테스트: {testEnemy.name}");
                DebugUtils.LogInfo(LOG_PREFIX, $"테스트 전 체력: {testEnemy.CurrentHealth}/{testEnemy.MaxHealth}");
                DebugUtils.LogInfo(LOG_PREFIX, $"초기화 상태: {testEnemy.IsInitialized}");
                DebugUtils.LogInfo(LOG_PREFIX, $"생존 상태: {testEnemy.IsAlive}");
                
                // 5 데미지 적용
                testEnemy.TakeDamage(5f);
                
                DebugUtils.LogInfo(LOG_PREFIX, $"테스트 후 체력: {testEnemy.CurrentHealth}/{testEnemy.MaxHealth}");
            }
        }
    }
    
    /// <summary>
    /// 향상된 투사체 충돌 감지 컴포넌트
    /// </summary>
    public class EnhancedProjectileCollision : MonoBehaviour
    {
        private const string LOG_PREFIX = "EnhancedProjectileCollision";
        
        private Projectile projectile;
        
        private void Start()
        {
            projectile = GetComponent<Projectile>();
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (projectile == null) return;
            
            // 더 관대한 적 감지
            IDamageable target = null;
            
            // 1. 직접 컴포넌트 확인
            target = other.GetComponent<IDamageable>();
            
            // 2. 부모에서 찾기
            if (target == null)
            {
                target = other.GetComponentInParent<IDamageable>();
            }
            
            // 3. 자식에서 찾기
            if (target == null)
            {
                target = other.GetComponentInChildren<IDamageable>();
            }
            
            // 4. EnemyObject 직접 찾기
            if (target == null)
            {
                EnemyObject enemy = other.GetComponent<EnemyObject>();
                if (enemy == null) enemy = other.GetComponentInParent<EnemyObject>();
                if (enemy == null) enemy = other.GetComponentInChildren<EnemyObject>();
                target = enemy;
            }
            
            if (target != null)
            {
                // 레이어 체크를 더 관대하게
                bool isEnemy = other.gameObject.layer == LayerMask.NameToLayer("Enemy") ||
                              other.CompareTag("Enemy") ||
                              other.GetComponent<EnemyObject>() != null;
                
                if (isEnemy)
                {
                    DebugUtils.LogVerbose(LOG_PREFIX, $"향상된 충돌 감지: {other.name}");
                    
                    // Projectile의 HitTarget 메서드를 직접 호출하려고 시도
                    var hitTargetMethod = projectile.GetType().GetMethod("HitTarget", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (hitTargetMethod != null)
                    {
                        hitTargetMethod.Invoke(projectile, new object[] { target });
                    }
                    else
                    {
                        // 직접 데미지 적용
                        target.TakeDamage(1f); // 기본 데미지
                        DebugUtils.LogInfo(LOG_PREFIX, $"직접 데미지 적용: {other.name}");
                    }
                }
            }
        }
    }
}