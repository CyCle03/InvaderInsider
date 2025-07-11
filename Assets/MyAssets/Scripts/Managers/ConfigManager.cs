using UnityEngine;
using InvaderInsider.ScriptableObjects;
using InvaderInsider.Managers;
using InvaderInsider.Core;

namespace InvaderInsider.Managers
{
    public class ConfigManager : SingletonManager<ConfigManager>
    {
        [Header("Configuration")]
        [SerializeField] private GameConfigSO gameConfig;
        private IStageData stageData;
        
        public GameConfigSO GameConfig
        {
            get
            {
                if (gameConfig == null)
                {
                    LogManager.Error(GameConstants.LOG_PREFIX_CONFIG, "GameConfig가 설정되지 않았습니다. Inspector에서 설정해주세요.");
                }
                return gameConfig;
            }
        }

        public IStageData StageData
        {
            get
            {
                if (stageData == null)
                {
                    LogManager.Error(GameConstants.LOG_PREFIX_CONFIG, "StageData가 설정되지 않았습니다. ConfigManager에서 로드해주세요.");
                }
                return stageData;
            }
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            // GameConfig가 할당되지 않았으면 자동으로 로드 시도
            if (gameConfig == null)
            {
                LoadGameConfigFromResources();
            }
            
            // StageData가 할당되지 않았으면 자동으로 로드 시도
            if (stageData == null)
            {
                LoadStageDataFromResources();
            }
            
            ValidateConfig();
            LogManager.LogInitialization("ConfigManager", true, "게임 설정 로드 완료");
        }
        
        /// <summary>
        /// Resources 폴더에서 GameConfig를 자동으로 로드
        /// </summary>
        private void LoadGameConfigFromResources()
        {
            // 여러 가능한 경로에서 GameConfig 검색
            string[] possiblePaths = {
                "GameConfig",
                "Config/GameConfig",
                "ScriptableObjects/Config/GameConfig"
            };
            
            foreach (string path in possiblePaths)
            {
                gameConfig = Resources.Load<GameConfigSO>(path);
                if (gameConfig != null)
                {
                    LogManager.Info(GameConstants.LOG_PREFIX_CONFIG, $"GameConfig를 Resources에서 로드했습니다: {path}");
                    return;
                }
            }
            
            // Resources에서 찾지 못했으면 Assets 폴더에서 직접 검색 (에디터 전용)
            #if UNITY_EDITOR
            LoadGameConfigFromAssets();
            #endif
        }

        /// <summary>
        /// Resources 폴더에서 StageData를 자동으로 로드
        /// </summary>
        private void LoadStageDataFromResources()
        {
            IStageData loadedStageData = null;

            // Prioritize loading StageList for multi-stage games
            string[] stageListPaths = {
                "StageList1",
                "ScriptableObjects/StageSystem/StageList1",
                "StageSystem/StageList1"
            };

            foreach (string path in stageListPaths)
            {
                var list = Resources.Load<StageList>(path);
                if (list != null)
                {
                    loadedStageData = list;
                    LogManager.Info(GameConstants.LOG_PREFIX_CONFIG, $"Resources에서 StageList를 로드했습니다: {list.name}, StageCount: {loadedStageData.StageCount}");
                    break;
                }
            }

            if (loadedStageData == null)
            {
                // Fallback to StageDBObject if StageList not found
                string[] stageDBObjectPaths = {
                    "Stage1 Database",
                    "StageSystem/Stage1 Database",
                    "ScriptableObjects/StageSystem/Stage1 Database"
                };

                foreach (string path in stageDBObjectPaths)
                {
                    var dbObject = Resources.Load<StageDBObject>(path);
                    if (dbObject != null)
                    {
                        loadedStageData = dbObject;
                        LogManager.Info(GameConstants.LOG_PREFIX_CONFIG, $"Resources에서 StageDBObject를 로드했습니다: {dbObject.name}, StageCount: {loadedStageData.StageCount}");
                        break;
                    }
                }
            }

            if (loadedStageData != null)
            {
                stageData = loadedStageData;
            }
            else
            {
                LogManager.Error(GameConstants.LOG_PREFIX_CONFIG, "StageData를 찾을 수 없습니다. 다음 경로들을 확인하세요:");
            }
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// Assets 폴더에서 GameConfig를 검색 (에디터 전용)
        /// </summary>
        private void LoadGameConfigFromAssets()
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:GameConfigSO");
            
            if (guids.Length > 0)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                gameConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<GameConfigSO>(assetPath);
                
                if (gameConfig != null)
                {
                    LogManager.Info(GameConstants.LOG_PREFIX_CONFIG, $"GameConfig를 Assets에서 로드했습니다: {assetPath}");
                }
            }
            
            if (gameConfig == null)
            {
                LogManager.Error(GameConstants.LOG_PREFIX_CONFIG, "GameConfig를 찾을 수 없습니다. Assets/MyAssets/Scripts/ScriptableObjects/Config/GameConfig.asset을 확인해주세요.");
            }
        }
        #endif
        
        private void ValidateConfig()
        {
            if (gameConfig == null)
            {
                LogManager.Error(GameConstants.LOG_PREFIX_CONFIG, "GameConfig가 설정되지 않았습니다!");
                return;
            }
            
            // 설정값 유효성 검사
            if (gameConfig.targetSearchInterval <= 0)
            {
                LogManager.Warning(GameConstants.LOG_PREFIX_CONFIG, 
                    $"targetSearchInterval이 0 이하입니다: {gameConfig.targetSearchInterval}");
            }
            
            if (gameConfig.projectileSpeed <= 0)
            {
                LogManager.Warning(GameConstants.LOG_PREFIX_CONFIG, 
                    $"projectileSpeed가 0 이하입니다: {gameConfig.projectileSpeed}");
            }
            
            if (gameConfig.maxDetectionColliders <= 0)
            {
                LogManager.Warning(GameConstants.LOG_PREFIX_CONFIG, 
                    $"maxDetectionColliders가 0 이하입니다: {gameConfig.maxDetectionColliders}");
            }
            
            if (string.IsNullOrEmpty(gameConfig.enemyLayerName))
            {
                LogManager.Warning(GameConstants.LOG_PREFIX_CONFIG, "enemyLayerName이 비어있습니다.");
            }
            
            LogManager.Info(GameConstants.LOG_PREFIX_CONFIG, "설정 검증 완료");
        }
        
        /// <summary>
        /// 런타임에 설정값을 동적으로 변경
        /// </summary>
        public void UpdateConfigValue<T>(System.Func<GameConfigSO, T> getter, System.Action<GameConfigSO, T> setter, T newValue)
        {
            if (gameConfig == null)
            {
                LogManager.Error(GameConstants.LOG_PREFIX_CONFIG, "GameConfig가 null입니다.");
                return;
            }
            
            T oldValue = getter(gameConfig);
            setter(gameConfig, newValue);
            
            LogManager.Info(GameConstants.LOG_PREFIX_CONFIG, $"설정값 변경: {oldValue} → {newValue}");
        }
        
        /// <summary>
        /// 설정값을 기본값으로 리셋
        /// </summary>
        public void ResetToDefaults()
        {
            if (gameConfig == null)
            {
                LogManager.Error(GameConstants.LOG_PREFIX_CONFIG, "GameConfig가 null입니다.");
                return;
            }
            
            // 기본값으로 리셋하는 로직
            LogManager.Info(GameConstants.LOG_PREFIX_CONFIG, "설정을 기본값으로 리셋합니다.");
        }
        
        /// <summary>
        /// 현재 설정을 JSON으로 내보내기
        /// </summary>
        public string ExportConfigToJson()
        {
            if (gameConfig == null)
            {
                LogManager.Error(GameConstants.LOG_PREFIX_CONFIG, "GameConfig가 null입니다.");
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
                LogManager.Error(GameConstants.LOG_PREFIX_CONFIG, "JSON 데이터가 비어있습니다.");
                return;
            }
            
            try
            {
                JsonUtility.FromJsonOverwrite(jsonData, gameConfig);
                LogManager.Info(GameConstants.LOG_PREFIX_CONFIG, "설정을 JSON에서 불러왔습니다.");
            }
            catch (System.Exception ex)
            {
                LogManager.Error(GameConstants.LOG_PREFIX_CONFIG, $"JSON 파싱 오류: {ex.Message}");
            }
        }
    }
} 