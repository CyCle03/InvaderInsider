using UnityEngine;
using InvaderInsider.Core;

namespace InvaderInsider
{
    /// <summary>
    /// 플레이어 타게팅 문제를 디버깅하는 스크립트
    /// </summary>
    public class PlayerTargetingDebugger : MonoBehaviour
    {
        private const string LOG_PREFIX = "[PlayerDebug] ";
        
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebug = true;
        [SerializeField] private float debugInterval = 2f;
        
        private Player player;
        private float lastDebugTime;
        
        private void Start()
        {
            // 릴리즈 빌드에서는 자동 비활성화
            #if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            enabled = false;
            return;
            #endif
            
            player = FindObjectOfType<Player>();
            if (player == null)
            {
                Debug.LogError($"{LOG_PREFIX}플레이어를 찾을 수 없음!");
                enabled = false;
            }
        }
        
        private void Update()
        {
            if (!enableDebug || player == null) return;
            
            if (Time.time - lastDebugTime >= debugInterval)
            {
                DebugPlayerTargeting();
                lastDebugTime = Time.time;
            }
        }
        
        private void DebugPlayerTargeting()
        {
            Debug.Log($"{LOG_PREFIX}=== 플레이어 타게팅 디버그 ===");
            
            // 1. 플레이어 기본 정보
            Debug.Log($"{LOG_PREFIX}플레이어 위치: {player.transform.position}");
            Debug.Log($"{LOG_PREFIX}플레이어 공격 범위: {player.AttackRange}");
            Debug.Log($"{LOG_PREFIX}플레이어 CanAttack: {player.CanAttack()}");
            
            // 2. 레이어 마스크 확인
            int enemyLayerMask = LayerMask.GetMask(GameConstants.ENEMY_LAYER_NAME);
            Debug.Log($"{LOG_PREFIX}Enemy 레이어 마스크: {enemyLayerMask}");
            Debug.Log($"{LOG_PREFIX}Enemy 레이어 이름: '{GameConstants.ENEMY_LAYER_NAME}'");
            
            // 3. 적 감지 테스트
            Collider[] detectionBuffer = new Collider[50];
            int hitCount = Physics.OverlapSphereNonAlloc(
                player.transform.position, 
                player.AttackRange, 
                detectionBuffer, 
                enemyLayerMask
            );
            
            Debug.Log($"{LOG_PREFIX}감지된 적 수 (레이어 마스크 사용): {hitCount}");
            
            // 4. 모든 콜라이더 감지 (레이어 무시)
            int allHitCount = Physics.OverlapSphereNonAlloc(
                player.transform.position, 
                player.AttackRange, 
                detectionBuffer
            );
            
            Debug.Log($"{LOG_PREFIX}감지된 모든 콜라이더 수: {allHitCount}");
            
            // 5. 실제 적 오브젝트들 확인
            EnemyObject[] allEnemies = FindObjectsOfType<EnemyObject>();
            Debug.Log($"{LOG_PREFIX}씬의 총 적 수: {allEnemies.Length}");
            
            for (int i = 0; i < Mathf.Min(allEnemies.Length, 3); i++)
            {
                EnemyObject enemy = allEnemies[i];
                if (enemy != null)
                {
                    float distance = Vector3.Distance(player.transform.position, enemy.transform.position);
                    Debug.Log($"{LOG_PREFIX}적 {i}: {enemy.name}, 거리: {distance:F2}, 레이어: {enemy.gameObject.layer}, 태그: {enemy.gameObject.tag}");
                }
            }
            
            // 6. 범위 내 적들 직접 확인
            int enemiesInRange = 0;
            foreach (EnemyObject enemy in allEnemies)
            {
                if (enemy != null)
                {
                    float distance = Vector3.Distance(player.transform.position, enemy.transform.position);
                    if (distance <= player.AttackRange)
                    {
                        enemiesInRange++;
                        Debug.Log($"{LOG_PREFIX}범위 내 적: {enemy.name}, 거리: {distance:F2}");
                    }
                }
            }
            
            Debug.Log($"{LOG_PREFIX}범위 내 적 수 (직접 계산): {enemiesInRange}");
            Debug.Log($"{LOG_PREFIX}========================");
        }
        
        private void OnDrawGizmosSelected()
        {
            if (player != null)
            {
                // 플레이어 공격 범위 표시
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(player.transform.position, player.AttackRange);
                
                // 적들과의 연결선 표시
                EnemyObject[] enemies = FindObjectsOfType<EnemyObject>();
                foreach (EnemyObject enemy in enemies)
                {
                    if (enemy != null)
                    {
                        float distance = Vector3.Distance(player.transform.position, enemy.transform.position);
                        if (distance <= player.AttackRange)
                        {
                            Gizmos.color = Color.green;
                        }
                        else
                        {
                            Gizmos.color = Color.red;
                        }
                        Gizmos.DrawLine(player.transform.position, enemy.transform.position);
                    }
                }
            }
        }
    }
}