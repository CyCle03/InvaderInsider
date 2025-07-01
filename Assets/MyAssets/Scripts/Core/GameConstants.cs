namespace InvaderInsider.Core
{
    /// <summary>
    /// 게임 전반에서 사용되는 상수들을 정의합니다.
    /// 매직 넘버를 방지하고 코드 가독성을 향상시킵니다.
    /// </summary>
    public static class GameConstants
    {
        // === 체력 관련 ===
        public const float DEFAULT_MAX_HEALTH = 100f;
        public const float DEFAULT_ATTACK_DAMAGE = 10f;
        public const float DEFAULT_ATTACK_RANGE = 5f;
        public const float DEFAULT_ATTACK_RATE = 1f;
        public const float MIN_HEALTH_VALUE = 0f;
        public const float MIN_MAX_HEALTH_VALUE = 1f;

        // === 이동 및 네비게이션 ===
        public const float DEFAULT_MOVE_SPEED = 5f;
        public const float DEFAULT_STOPPING_DISTANCE = 0.1f;
        public const float PATH_UPDATE_INTERVAL = 0.1f;
        public const float TARGET_SEARCH_INTERVAL = 0.5f;
        public const float TARGET_LOST_DISTANCE_MULTIPLIER = 1.2f;

        // === 타워 관련 ===
        public const float TOWER_ATTACK_RANGE = 5f;
        public const float PROJECTILE_SPEED = 10f;
        public const int MAX_DETECTION_COLLIDERS = 20;

        // === UI 관련 ===
        public const float UI_UPDATE_INTERVAL = 0.1f;
        public const float HEALTH_BAR_UPDATE_SMOOTHNESS = 5f;

        // === 오디오 관련 ===
        public const float DEFAULT_SFX_VOLUME = 0.7f;
        public const float DEFAULT_BGM_VOLUME = 0.5f;

        // === 레이어 이름 ===
        public const string ENEMY_LAYER_NAME = "Enemy";
        public const string PLAYER_LAYER_NAME = "Player";
        public const string TOWER_LAYER_NAME = "Tower";
        public const string UI_LAYER_NAME = "UI";

        // === 기본 레이어 인덱스 (폴백용) ===
        public const int DEFAULT_ENEMY_LAYER_INDEX = 6;
        public const int DEFAULT_PLAYER_LAYER_INDEX = 7;
        public const int DEFAULT_TOWER_LAYER_INDEX = 8;

        // === 성능 최적화 관련 ===
        public const int MAX_RETRY_ATTEMPTS = 10;
        public const float RETRY_INTERVAL = 0.5f;
        public const int STRING_BUILDER_CAPACITY = 256;

        // === 스테이지 관련 ===
        public const int DEFAULT_EDATA_REWARD = 1;
        public const int DEFAULT_DAMAGE_ON_FINAL_WAYPOINT = 10;
        public const float STAGE_CLEAR_DELAY = 2f;

        // === 애니메이션 관련 ===
        public const float FADE_DURATION = 0.3f;
        public const float PANEL_TRANSITION_DURATION = 0.2f;

        // === 디버그 관련 ===
        public const string LOG_PREFIX_GAME = "[Game] ";
        public const string LOG_PREFIX_PLAYER = "[Player] ";
        public const string LOG_PREFIX_ENEMY = "[Enemy] ";
        public const string LOG_PREFIX_TOWER = "[Tower] ";
        public const string LOG_PREFIX_UI = "[UI] ";
        public const string LOG_PREFIX_CONFIG = "[Config] ";
        public const string LOG_PREFIX_SAVE = "[Save] ";
        public const string LOG_PREFIX_ERROR = "[Error] ";
        public const string LOG_PREFIX_POOL = "[Pool] ";

        // === 에러 처리 관련 ===
        public const int DEFAULT_MAX_ERROR_HISTORY = 100;
        public const float DEFAULT_ERROR_RECOVERY_DELAY = 1f;
        public const int MAX_RECOVERY_ATTEMPTS = 3;
        public const float CRITICAL_ERROR_TIMEOUT = 10f;

        // === 오브젝트 풀링 관련 ===
        public const int DEFAULT_POOL_SIZE = 10;
        public const int DEFAULT_MAX_POOL_SIZE = 100;
        public const int MAX_POOL_SIZE = 200;
        public const float POOL_CLEANUP_INTERVAL = 30f;
        public const float OBJECT_AUTO_RETURN_TIME = 5f;

        // === 태그 이름 ===
        public const string TAG_PLAYER = "Player";
        public const string TAG_ENEMY = "Enemy";
        public const string TAG_TOWER = "Tower";
        public const string TAG_WAYPOINT = "Waypoint";

        // === 씬 이름 ===
        public const string SCENE_MAIN_MENU = "Main";
        public const string SCENE_GAME = "Game";
        public const string DEFAULT_GAME_SCENE = "Game";

        // === 게임 기본값 ===
        public const int DEFAULT_STARTING_LIVES = 3;
        public const int DEFAULT_STARTING_EDATA = 100;

        // === 애니메이터 파라미터 ===
        public const string ANIM_PARAM_SPEED = "Speed";
        public const string ANIM_PARAM_ATTACK = "Attack";
        public const string ANIM_PARAM_DIE = "Die";
        public const string ANIM_PARAM_IDLE = "Idle";
        
        // === 공통 로그 메시지 ===
        public static class LogMessages
        {
            // 초기화 관련
            public const string INITIALIZATION_SUCCESS = "{0} 초기화 완료";
            public const string INITIALIZATION_FAILED = "{0} 초기화 실패: {1}";
            public const string COMPONENT_NOT_FOUND = "{0} 컴포넌트를 찾을 수 없습니다";
            public const string MANAGER_NOT_FOUND = "{0} 매니저를 찾을 수 없습니다";
            
            // 상태 변경 관련
            public const string STATE_CHANGED = "상태가 {0}에서 {1}로 변경됨";
            public const string HEALTH_CHANGED = "체력 변경: {0}/{1} ({2:F1}%)";
            public const string DAMAGE_RECEIVED = "{0}이(가) {1} 데미지를 받음. 현재 체력: {2}/{3}";
            
            // UI 관련
            public const string PANEL_REGISTERED = "패널 등록: {0}";
            public const string PANEL_NOT_FOUND = "패널 '{0}'을(를) 찾을 수 없습니다";
            public const string UI_UPDATE_SUCCESS = "UI 업데이트 완료: {0}";
            
            // 게임플레이 관련
            public const string STAGE_CLEARED = "스테이지 {0} 클리어";
            public const string ENEMY_SPAWNED = "적 생성: {0} (총 {1}/{2})";
            public const string PROJECTILE_HIT = "투사체가 {0}에게 {1} 데미지";
            
            // 에러/경고 관련
            public const string NULL_REFERENCE_ERROR = "{0}이(가) null입니다";
            public const string INVALID_PARAMETER = "잘못된 매개변수: {0}";
            public const string OPERATION_FAILED = "{0} 작업 실패: {1}";
            
            // 성능 관련
            public const string PERFORMANCE_WARNING = "성능 경고: {0}";
            public const string MEMORY_ALLOCATION = "메모리 할당: {0}";
            
            // 저장/로드 관련
            public const string SAVE_SUCCESS = "저장 완료: {0}";
            public const string LOAD_SUCCESS = "로드 완료: {0}";
            public const string SAVE_FAILED = "저장 실패: {0}";
            public const string LOAD_FAILED = "로드 실패: {0}";
        }
    }
} 