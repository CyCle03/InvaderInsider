using UnityEngine;

namespace InvaderInsider.ScriptableObjects
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "InvaderInsider/GameConfig")]
    public class GameConfigSO : ScriptableObject
    {
        [Header("Tower Settings")]
        [Tooltip("타워가 적을 탐지하는 주기 (초)")]
        public float targetSearchInterval = 0.15f;
        
        [Tooltip("투사체 기본 속도")]
        public float projectileSpeed = 15f;
        
        [Tooltip("한 번에 감지할 수 있는 최대 적 수")]
        [Range(10, 100)]
        public int maxDetectionColliders = 30;
        
        [Tooltip("타겟 놓치는 거리 배율 (사거리의 %)")]
        [Range(1.0f, 2.0f)]
        public float targetLostDistanceMultiplier = 1.2f;
        
        [Tooltip("타워 회전 속도")]
        public float towerRotationSpeed = 5f;
        
        [Header("Enemy Settings")]
        [Tooltip("적 기본 이동 속도")]
        public float defaultMoveSpeed = 5f;
        
        [Tooltip("NavMeshAgent 정지 거리")]
        public float defaultStoppingDistance = 0.1f;
        
        [Tooltip("웨이포인트 도달 거리")]
        public float waypointReachDistance = 0.1f;
        
        [Tooltip("경로 업데이트 주기 (초)")]
        public float pathUpdateInterval = 0.1f;
        
        [Tooltip("목적지 도달 임계값")]
        public float destinationThreshold = 0.5f;
        
        [Tooltip("웨이포인트 체크 주기 (초)")]
        public float waypointCheckInterval = 0.1f;
        
        [Tooltip("기본 적 데미지 (목적지 도달 시)")]
        public int defaultEnemyDamage = 10;
        
        [Tooltip("기본 적 보상 (EData)")]
        public int defaultEnemyReward = 1;
        
        [Header("Character Settings")]
        [Tooltip("캐릭터 기본 최대 체력")]
        public float defaultMaxHealth = 100f;
        
        [Tooltip("캐릭터 기본 공격력")]
        public float defaultAttackDamage = 10f;
        
        [Tooltip("캐릭터 기본 공격 사거리")]
        public float defaultAttackRange = 5f;
        
        [Tooltip("캐릭터 기본 공격 속도")]
        public float defaultAttackRate = 1f;
        
        [Tooltip("최소 체력값")]
        public float minHealthValue = 0f;
        
        [Tooltip("최소 최대 체력값")]
        public float minMaxHealthValue = 1f;
        
        [Header("Game Performance")]
        [Tooltip("게임 상태 체크 주기 (초)")]
        public float stateCheckInterval = 0.2f;
        
        [Tooltip("기본 시간 배율")]
        public float defaultTimeScale = 1f;
        
        [Tooltip("일시정지 시간 배율")]
        public float pausedTimeScale = 0f;
        
        [Header("Layer & Tag Settings")]
        [Tooltip("적 레이어 이름")]
        public string enemyLayerName = "Enemy";
        
        [Tooltip("적 태그 이름")]
        public string enemyTag = "Enemy";
        
        [Tooltip("기본 적 레이어 인덱스")]
        [Range(0, 31)]
        public int defaultEnemyLayerIndex = 6;
        
        [Header("Scene Names")]
        [Tooltip("게임 씬 이름")]
        public string gameSceneName = "Game";
        
        [Tooltip("메인 씬 이름")]
        public string mainSceneName = "Main";
        
        [Header("Validation")]
        [Tooltip("유효하지 않은 스테이지 인덱스")]
        public int invalidStageIndex = -1;
        
        [Header("Game Manager Settings")]
        [Tooltip("스테이지 클리어 중복 처리 방지 플래그")]
        public bool enableStageClearDuplicatePrevention = true;
        
        [Tooltip("게임 상태 변경 중복 호출 방지")]
        public bool enableStateChangeDuplicatePrevention = true;
        
        [Tooltip("EData 유효성 검사 활성화")]
        public bool enableEDataValidation = true;
        
        [Tooltip("최소 EData 값")]
        public int minEDataValue = 0;
        
        [Tooltip("최대 EData 값")]
        public int maxEDataValue = 999999;
    }
} 