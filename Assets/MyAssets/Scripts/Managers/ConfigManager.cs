using UnityEngine;
using InvaderInsider.ScriptableObjects;

namespace InvaderInsider.Managers
{
    public class ConfigManager : MonoBehaviour
    {
        private const string LOG_PREFIX = "[ConfigManager] ";
        
        [Header("Configuration")]
        [SerializeField] private GameConfigSO gameConfig;
        
        private static ConfigManager instance;
        private static readonly object _lock = new object();
        
        public static ConfigManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (_lock)
                    {
                        if (instance == null)
                        {
                            instance = FindObjectOfType<ConfigManager>();
                            if (instance == null)
                            {
                                GameObject go = new GameObject("ConfigManager");
                                instance = go.AddComponent<ConfigManager>();
                                DontDestroyOnLoad(go);
                            }
                        }
                    }
                }
                return instance;
            }
        }
        
        public GameConfigSO GameConfig
        {
            get
            {
                if (gameConfig == null)
                {
                    Debug.LogError($"{LOG_PREFIX}GameConfig가 설정되지 않았습니다. Inspector에서 설정해주세요.");
                }
                return gameConfig;
            }
        }
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                ValidateConfig();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void ValidateConfig()
        {
            if (gameConfig == null)
            {
                Debug.LogError($"{LOG_PREFIX}GameConfig가 설정되지 않았습니다!");
                return;
            }
            
            // 설정값 유효성 검사
            if (gameConfig.targetSearchInterval <= 0)
            {
                Debug.LogWarning($"{LOG_PREFIX}targetSearchInterval이 0 이하입니다: {gameConfig.targetSearchInterval}");
            }
            
            if (gameConfig.projectileSpeed <= 0)
            {
                Debug.LogWarning($"{LOG_PREFIX}projectileSpeed가 0 이하입니다: {gameConfig.projectileSpeed}");
            }
            
            if (gameConfig.maxDetectionColliders <= 0)
            {
                Debug.LogWarning($"{LOG_PREFIX}maxDetectionColliders가 0 이하입니다: {gameConfig.maxDetectionColliders}");
            }
            
            if (string.IsNullOrEmpty(gameConfig.enemyLayerName))
            {
                Debug.LogWarning($"{LOG_PREFIX}enemyLayerName이 비어있습니다.");
            }
            
            Debug.Log($"{LOG_PREFIX}설정 검증 완료");
        }
        
        /// <summary>
        /// 런타임에 설정값을 동적으로 변경
        /// </summary>
        public void UpdateConfigValue<T>(System.Func<GameConfigSO, T> getter, System.Action<GameConfigSO, T> setter, T newValue)
        {
            if (gameConfig == null)
            {
                Debug.LogError($"{LOG_PREFIX}GameConfig가 null입니다.");
                return;
            }
            
            T oldValue = getter(gameConfig);
            setter(gameConfig, newValue);
            
            Debug.Log($"{LOG_PREFIX}설정값 변경: {oldValue} → {newValue}");
        }
        
        /// <summary>
        /// 설정값을 기본값으로 리셋
        /// </summary>
        public void ResetToDefaults()
        {
            if (gameConfig == null)
            {
                Debug.LogError($"{LOG_PREFIX}GameConfig가 null입니다.");
                return;
            }
            
            // 기본값으로 리셋하는 로직
            Debug.Log($"{LOG_PREFIX}설정을 기본값으로 리셋합니다.");
        }
        
        /// <summary>
        /// 현재 설정을 JSON으로 내보내기
        /// </summary>
        public string ExportConfigToJson()
        {
            if (gameConfig == null)
            {
                Debug.LogError($"{LOG_PREFIX}GameConfig가 null입니다.");
                return "";
            }
            
            return JsonUtility.ToJson(gameConfig, true);
        }
        
        /// <summary>
        /// JSON에서 설정 불러오기
        /// </summary>
        public void ImportConfigFromJson(string jsonData)
        {
            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.LogError($"{LOG_PREFIX}JSON 데이터가 비어있습니다.");
                return;
            }
            
            try
            {
                JsonUtility.FromJsonOverwrite(jsonData, gameConfig);
                Debug.Log($"{LOG_PREFIX}설정을 JSON에서 불러왔습니다.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX}JSON 파싱 오류: {ex.Message}");
            }
        }
    }
} 