using UnityEngine;
using System.Collections;
using InvaderInsider.Core;

namespace InvaderInsider
{
    /// <summary>
    /// Player의 타게팅 시스템을 최적화하는 컴포넌트
    /// 기존 Player.FindAndAttackEnemies()를 대체합니다
    /// </summary>
    public class OptimizedPlayerTargeting : MonoBehaviour
    {
        private const string LOG_PREFIX = "OptimizedTargeting";
        
        [Header("Optimization Settings")]
        [SerializeField] private float targetingInterval = 0.1f; // 10Hz로 타게팅 업데이트
        [SerializeField] private float maxTargetingRange = 10f;
        [SerializeField] private int maxTargetsToCheck = 20;
        
        // 캐시된 참조
        private Player player;
        private int enemyLayerMask;
        
        // 성능 최적화용 버퍼
        private Collider[] detectionBuffer;
        private IDamageable[] targetBuffer;
        private float[] distanceBuffer;
        
        // 타게팅 상태
        private IDamageable currentTarget;
        private Coroutine targetingCoroutine;
        
        // 성능 통계
        private int framesSinceLastTargeting;
        private float averageTargetingTime;
        
        private void Start()
        {
            InitializeOptimizedTargeting();
        }
        
        private void InitializeOptimizedTargeting()
        {
            // Player 참조 캐싱
            player = GetComponent<Player>();
            if (player == null)
            {
                DebugUtils.LogError(LOG_PREFIX, "Player 컴포넌트를 찾을 수 없습니다!");
                enabled = false;
                return;
            }
            
            // 레이어 마스크 캐싱
            enemyLayerMask = LayerMask.GetMask(GameConstants.ENEMY_LAYER_NAME);
            if (enemyLayerMask == 0)
            {
                enemyLayerMask = 1 << 6; // 기본값
                DebugUtils.LogError(LOG_PREFIX, "Enemy 레이어를 찾을 수 없어 기본값 사용");
            }
            
            // 버퍼 초기화 (메모리 할당 최소화)
            detectionBuffer = new Collider[maxTargetsToCheck];
            targetBuffer = new IDamageable[maxTargetsToCheck];
            distanceBuffer = new float[maxTargetsToCheck];
            
            // 최적화된 타게팅 코루틴 시작
            targetingCoroutine = StartCoroutine(OptimizedTargetingRoutine());
            
            // 기존 타게팅 시스템 비활성화 시도
            DisableOriginalTargeting();
            
            DebugUtils.LogInfo(LOG_PREFIX, "최적화된 플레이어 타게팅 시스템 초기화 완료");
            DebugUtils.LogVerbose(LOG_PREFIX, $"타게팅 간격: {targetingInterval}초, 최대 범위: {maxTargetingRange}, 최대 타겟: {maxTargetsToCheck}");
        }
        
        /// <summary>
        /// 최적화된 타게팅 코루틴 - 매 프레임 대신 설정된 간격으로 실행
        /// </summary>
        private IEnumerator OptimizedTargetingRoutine()
        {
            var waitTime = new WaitForSeconds(targetingInterval);
            
            while (enabled && player != null)
            {
                float startTime = Time.realtimeSinceStartup;
                
                // 타겟 업데이트
                UpdateTarget();
                
                // 타겟이 있고 공격 가능할 때 공격
                if (currentTarget != null && player.CanAttack())
                {
                    player.Attack(currentTarget);
                }
                
                // 성능 통계 업데이트
                float targetingTime = Time.realtimeSinceStartup - startTime;
                UpdatePerformanceStats(targetingTime);
                
                yield return waitTime;
            }
        }
        
        /// <summary>
        /// 타겟을 업데이트합니다.
        /// </summary>
        private void UpdateTarget()
        {
            if (!IsValidTarget(currentTarget))
            {
                currentTarget = FindNearestEnemyOptimized();
            }
        }
        
        /// <summary>
        /// 타겟 유효성 검사 (최적화됨)
        /// </summary>
        private bool IsValidTarget(IDamageable target)
        {
            if (target == null) return false;
            
            // MonoBehaviour 캐스팅 최소화
            if (target is Component component)
            {
                if (component == null || !component.gameObject.activeInHierarchy)
                    return false;
                
                // 거리 체크 (SqrMagnitude 사용으로 sqrt 연산 제거)
                float sqrDistance = Vector3.SqrMagnitude(
                    component.transform.position - player.transform.position
                );
                
                float maxRange = Mathf.Max(player.AttackRange, maxTargetingRange);
                return sqrDistance <= maxRange * maxRange;
            }
            
            return false;
        }
        
        /// <summary>
        /// 최적화된 가장 가까운 적 검색
        /// </summary>
        private IDamageable FindNearestEnemyOptimized()
        {
            // Physics.OverlapSphereNonAlloc 사용 (메모리 할당 없음)
            int hitCount = Physics.OverlapSphereNonAlloc(
                player.transform.position,
                maxTargetingRange,
                detectionBuffer,
                enemyLayerMask
            );
            
            if (hitCount == 0) return null;
            
            // 유효한 타겟들을 버퍼에 수집
            int validTargetCount = 0;
            Vector3 playerPos = player.transform.position;
            
            for (int i = 0; i < hitCount && validTargetCount < maxTargetsToCheck; i++)
            {
                var collider = detectionBuffer[i];
                if (collider == null) continue;
                
                // IDamageable 컴포넌트 확인
                if (collider.TryGetComponent<IDamageable>(out var target))
                {
                    // 거리 계산 (SqrMagnitude 사용)
                    float sqrDistance = Vector3.SqrMagnitude(
                        collider.transform.position - playerPos
                    );
                    
                    // 공격 범위 내에 있는지 확인
                    float attackRangeSqr = player.AttackRange * player.AttackRange;
                    if (sqrDistance <= attackRangeSqr)
                    {
                        targetBuffer[validTargetCount] = target;
                        distanceBuffer[validTargetCount] = sqrDistance;
                        validTargetCount++;
                    }
                }
            }
            
            // 가장 가까운 타겟 선택 (정렬 없이 최소값 찾기)
            if (validTargetCount == 0) return null;
            
            int nearestIndex = 0;
            float nearestDistance = distanceBuffer[0];
            
            for (int i = 1; i < validTargetCount; i++)
            {
                if (distanceBuffer[i] < nearestDistance)
                {
                    nearestDistance = distanceBuffer[i];
                    nearestIndex = i;
                }
            }
            
            return targetBuffer[nearestIndex];
        }
        
        /// <summary>
        /// 성능 통계 업데이트
        /// </summary>
        private void UpdatePerformanceStats(float targetingTime)
        {
            framesSinceLastTargeting++;
            
            // 이동 평균으로 평균 타게팅 시간 계산
            if (averageTargetingTime == 0)
            {
                averageTargetingTime = targetingTime;
            }
            else
            {
                averageTargetingTime = Mathf.Lerp(averageTargetingTime, targetingTime, 0.1f);
            }
            
            // 성능 경고 (1ms 이상 걸리면 경고)
            if (targetingTime > 0.001f)
            {
                DebugUtils.LogError(LOG_PREFIX, $"타게팅 시간 과다: {targetingTime * 1000f:F2}ms");
            }
        }
        
        /// <summary>
        /// 기존 Player의 FindAndAttackEnemies 비활성화
        /// </summary>
        public void DisableOriginalTargeting()
        {
            // Player에 최적화 플래그 설정 (Player 스크립트에서 확인)
            if (player != null)
            {
                // Player 스크립트에 isOptimizedTargetingEnabled 플래그가 있다면 설정
                var playerType = player.GetType();
                var field = playerType.GetField("isOptimizedTargetingEnabled", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (field != null)
                {
                    field.SetValue(player, true);
                    DebugUtils.LogVerbose(LOG_PREFIX, "원본 타게팅 시스템 비활성화됨");
                }
                else
                {
                    DebugUtils.LogVerbose(LOG_PREFIX, "Player 스크립트에 최적화 플래그 추가 권장");
                }
            }
        }
        
        /// <summary>
        /// 성능 정보 출력
        /// </summary>
        [ContextMenu("Show Performance Info")]
        public void ShowPerformanceInfo()
        {
            DebugUtils.LogInfo(LOG_PREFIX, "=== 성능 정보 ===");
            DebugUtils.LogInfo(LOG_PREFIX, $"타게팅 간격: {targetingInterval}초");
            DebugUtils.LogInfo(LOG_PREFIX, $"평균 타게팅 시간: {averageTargetingTime * 1000f:F2}ms");
            DebugUtils.LogInfo(LOG_PREFIX, $"현재 타겟: {(currentTarget != null ? "있음" : "없음")}");
            DebugUtils.LogInfo(LOG_PREFIX, $"프레임 절약: {framesSinceLastTargeting}프레임마다 1회 실행");
        }
        
        private void OnDisable()
        {
            if (targetingCoroutine != null)
            {
                StopCoroutine(targetingCoroutine);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (player == null) return;
            
            // 타게팅 범위 표시
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(player.transform.position, maxTargetingRange);
            
            // 공격 범위 표시
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawWireSphere(player.transform.position, player.AttackRange);
            
            // 현재 타겟 표시
            if (currentTarget != null && currentTarget is Component targetComponent)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(player.transform.position, targetComponent.transform.position);
                Gizmos.DrawWireCube(targetComponent.transform.position, Vector3.one * 0.5f);
            }
        }
    }
}