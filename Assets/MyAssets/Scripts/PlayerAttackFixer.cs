using UnityEngine;
using InvaderInsider.Core;

namespace InvaderInsider
{
    /// <summary>
    /// Player의 CanAttack 문제를 즉시 해결하는 스크립트
    /// </summary>
    public class PlayerAttackFixer : MonoBehaviour
    {
        private const string LOG_PREFIX = "PlayerAttackFixer";
        
        [Header("Fix Settings")]
        [SerializeField] private bool autoFixOnStart = true;
        [SerializeField] private bool forceEnableAttack = true;
        [SerializeField] private float fixedAttackRate = 0.5f;
        [SerializeField] private bool enableVerboseLogs = false;
        
        private Player player;
        
        private void Start()
        {
            if (autoFixOnStart)
            {
                FixPlayerAttack();
            }
        }
        
        /// <summary>
        /// Player 공격 문제 즉시 수정
        /// </summary>
        [ContextMenu("Fix Player Attack")]
        public void FixPlayerAttack()
        {
            player = GetComponent<Player>();
            if (player == null)
            {
                player = FindObjectOfType<Player>();
            }
            
            if (player == null)
            {
                DebugUtils.LogError(LOG_PREFIX, "Player를 찾을 수 없습니다!");
                return;
            }
            
            DebugUtils.LogInfo(LOG_PREFIX, "Player 공격 문제 수정 시작");
            
            // 1. 모든 적들을 Enemy 레이어로 강제 설정
            FixEnemyLayers();
            
            // 2. Player의 공격 속도 문제 해결
            FixAttackRate();
            
            // 3. 강제 공격 활성화
            if (forceEnableAttack)
            {
                StartCoroutine(ForceAttackRoutine());
            }
            
            DebugUtils.LogInfo(LOG_PREFIX, "Player 공격 문제 수정 완료");
        }
        
        /// <summary>
        /// 모든 적을 Enemy 레이어로 강제 설정
        /// </summary>
        private void FixEnemyLayers()
        {
            EnemyObject[] allEnemies = FindObjectsOfType<EnemyObject>();
            int fixedCount = 0;
            
            int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
            if (enemyLayerIndex == -1) enemyLayerIndex = 6;
            
            foreach (EnemyObject enemy in allEnemies)
            {
                if (enemy != null && enemy.gameObject.layer != enemyLayerIndex)
                {
                    enemy.gameObject.layer = enemyLayerIndex;
                    fixedCount++;
                    DebugUtils.LogVerbose(LOG_PREFIX, $"적 레이어 수정: {enemy.name} -> Enemy 레이어");
                }
            }
            
            if (fixedCount > 0)
            {
                DebugUtils.LogInfo(LOG_PREFIX, $"{fixedCount}개 적의 레이어 수정됨");
            }
        }
        
        /// <summary>
        /// Player의 공격 속도 문제 해결
        /// </summary>
        private void FixAttackRate()
        {
            if (player == null) return;
            
            // Reflection을 사용해서 nextAttackTime을 강제로 0으로 설정
            var playerType = player.GetType().BaseType; // BaseCharacter
            var nextAttackTimeField = playerType.GetField("nextAttackTime", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (nextAttackTimeField != null)
            {
                nextAttackTimeField.SetValue(player, 0f);
                DebugUtils.LogVerbose(LOG_PREFIX, "Player nextAttackTime 초기화됨");
            }
            
            // attackRate도 설정
            var attackRateField = playerType.GetField("attackRate", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (attackRateField != null)
            {
                attackRateField.SetValue(player, fixedAttackRate);
                DebugUtils.LogVerbose(LOG_PREFIX, $"Player attackRate 설정됨: {fixedAttackRate}");
            }
        }
        
        /// <summary>
        /// 강제 공격 루틴 - CanAttack이 False여도 공격하도록 함
        /// </summary>
        private System.Collections.IEnumerator ForceAttackRoutine()
        {
            DebugUtils.LogInfo(LOG_PREFIX, "강제 공격 루틴 시작");
            
            int attackCount = 0;
            while (enabled && player != null)
            {
                yield return new WaitForSeconds(0.2f); // 5Hz로 감소
                
                // 범위 내 적 찾기
                EnemyObject[] allEnemies = FindObjectsOfType<EnemyObject>();
                EnemyObject nearestEnemy = null;
                float nearestDistance = float.MaxValue;
                
                foreach (EnemyObject enemy in allEnemies)
                {
                    if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
                    
                    float distance = Vector3.Distance(player.transform.position, enemy.transform.position);
                    if (distance <= player.AttackRange && distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = enemy;
                    }
                }
                
                // 적이 있으면 강제 공격
                if (nearestEnemy != null)
                {
                    attackCount++;
                    
                    // 5번마다 한 번씩만 로그 출력
                    if (enableVerboseLogs && attackCount % 5 == 1)
                    {
                        DebugUtils.LogVerbose(LOG_PREFIX, $"강제 공격 실행: {nearestEnemy.name} (거리: {nearestDistance:F1})");
                    }
                    
                    // 직접 공격 실행 (CanAttack 무시)
                    player.Attack(nearestEnemy);
                    
                    // 공격 후 잠시 대기
                    yield return new WaitForSeconds(fixedAttackRate);
                }
            }
        }
        
        /// <summary>
        /// 실시간 디버그 정보
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
            {
                ShowDebugInfo();
            }
        }
        
        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        private void ShowDebugInfo()
        {
            if (player == null) return;
            
            Debug.Log($"{LOG_PREFIX}=== 실시간 디버그 정보 ===");
            Debug.Log($"{LOG_PREFIX}Player CanAttack: {player.CanAttack()}");
            Debug.Log($"{LOG_PREFIX}Player 위치: {player.transform.position}");
            Debug.Log($"{LOG_PREFIX}Player 공격 범위: {player.AttackRange}");
            
            EnemyObject[] enemies = FindObjectsOfType<EnemyObject>();
            Debug.Log($"{LOG_PREFIX}총 적 수: {enemies.Length}");
            
            int inRangeCount = 0;
            foreach (EnemyObject enemy in enemies)
            {
                if (enemy != null)
                {
                    float distance = Vector3.Distance(player.transform.position, enemy.transform.position);
                    if (distance <= player.AttackRange)
                    {
                        inRangeCount++;
                        Debug.Log($"{LOG_PREFIX}범위 내 적: {enemy.name}, 거리: {distance:F2}, 레이어: {enemy.gameObject.layer}");
                    }
                }
            }
            
            Debug.Log($"{LOG_PREFIX}범위 내 적 수: {inRangeCount}");
        }
    }
}