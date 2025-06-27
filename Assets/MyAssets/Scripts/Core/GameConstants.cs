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
    }
} 