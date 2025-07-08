using UnityEngine;
using System;
using System.Collections.Generic;
using InvaderInsider.Cards;
using InvaderInsider.Managers;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using InvaderInsider.Core;

namespace InvaderInsider.Data
{
    [Serializable]
    public class SaveData
    {
        public ProgressData progressData = new ProgressData();
        public DeckData deckData = new DeckData();
        public SerializableSettings settings = new SerializableSettings();
        public SerializableStageProgress stageProgress = new SerializableStageProgress();

        public SaveData Clone()
        {
            return new SaveData
            {
                progressData = new ProgressData
                {
                    currentEData = this.progressData.currentEData,
                    highestStageCleared = this.progressData.highestStageCleared,
                    summonCount = this.progressData.summonCount
                },
                deckData = new DeckData
                {
                    deckCardIds = new List<int>(this.deckData.deckCardIds),
                    ownedCardIds = new List<int>(this.deckData.ownedCardIds),
                    handCardIds = new List<int>(this.deckData.handCardIds)
                },
                settings = this.settings.Clone(),
                stageProgress = this.stageProgress.Clone()
            };
        }
    }

    [Serializable]
    public class ProgressData
    {
        public int currentEData;
        public int highestStageCleared;
        public int summonCount;
    }

    [Serializable]
    public class DeckData
    {
        private HashSet<int> deckCardSet = new HashSet<int>();
        private HashSet<int> ownedCardSet = new HashSet<int>();
        private HashSet<int> handCardSet = new HashSet<int>();
        
        // 캐시된 리스트들 (메모리 할당 최소화)
        [System.NonSerialized] private List<int> cachedDeckCardIds = null;
        [System.NonSerialized] private List<int> cachedOwnedCardIds = null;
        [System.NonSerialized] private List<int> cachedHandCardIds = null;
        [System.NonSerialized] private bool deckCacheDirty = true;
        [System.NonSerialized] private bool ownedCacheDirty = true;
        [System.NonSerialized] private bool handCacheDirty = true;

        public List<int> deckCardIds
        {
            get 
            {
                if (deckCacheDirty || cachedDeckCardIds == null)
                {
                    cachedDeckCardIds = new List<int>(deckCardSet);
                    deckCacheDirty = false;
                }
                return cachedDeckCardIds;
            }
            set 
            {
                deckCardSet = new HashSet<int>(value);
                deckCacheDirty = true;
            }
        }

        public List<int> ownedCardIds
        {
            get 
            {
                if (ownedCacheDirty || cachedOwnedCardIds == null)
                {
                    cachedOwnedCardIds = new List<int>(ownedCardSet);
                    ownedCacheDirty = false;
                }
                return cachedOwnedCardIds;
            }
            set 
            {
                ownedCardSet = new HashSet<int>(value);
                ownedCacheDirty = true;
            }
        }

        public List<int> handCardIds
        {
            get 
            {
                if (handCacheDirty || cachedHandCardIds == null)
                {
                    cachedHandCardIds = new List<int>(handCardSet);
                    handCacheDirty = false;
                }
                return cachedHandCardIds;
            }
            set 
            {
                handCardSet = new HashSet<int>(value);
                handCacheDirty = true;
            }
        }

        public bool AddToDeck(int cardId) 
        { 
            bool added = deckCardSet.Add(cardId);
            if (added) deckCacheDirty = true;
            return added;
        }
        
        public bool RemoveFromDeck(int cardId) 
        { 
            bool removed = deckCardSet.Remove(cardId);
            if (removed) deckCacheDirty = true;
            return removed;
        }
        
        public bool AddToOwned(int cardId) 
        { 
            bool added = ownedCardSet.Add(cardId);
            if (added) ownedCacheDirty = true;
            return added;
        }
        
        public bool AddToHand(int cardId) 
        { 
            bool added = handCardSet.Add(cardId);
            if (added) handCacheDirty = true;
            return added;
        }
        
        public bool RemoveFromHand(int cardId) 
        { 
            bool removed = handCardSet.Remove(cardId);
            if (removed) handCacheDirty = true;
            return removed;
        }
        public bool IsInDeck(int cardId) => deckCardSet.Contains(cardId);
        public bool IsOwned(int cardId) => ownedCardSet.Contains(cardId);
        public bool IsInHand(int cardId) => handCardSet.Contains(cardId);
    }

    [Serializable]
    public class SerializableSettings
    {
        private Dictionary<string, string> settings = new Dictionary<string, string>();
        
        // 캐시된 리스트들 (메모리 할당 최소화)
        [System.NonSerialized] private List<string> cachedKeys = null;
        [System.NonSerialized] private List<string> cachedValues = null;
        [System.NonSerialized] private bool keysCacheDirty = true;
        [System.NonSerialized] private bool valuesCacheDirty = true;

        public List<string> keys
        {
            get 
            {
                if (keysCacheDirty || cachedKeys == null)
                {
                    cachedKeys = new List<string>(settings.Keys);
                    keysCacheDirty = false;
                }
                return cachedKeys;
            }
            set { } // 직렬화를 위해 필요
        }

        public List<string> values
        {
            get 
            {
                if (valuesCacheDirty || cachedValues == null)
                {
                    cachedValues = new List<string>(settings.Values);
                    valuesCacheDirty = false;
                }
                return cachedValues;
            }
            set { } // 직렬화를 위해 필요
        }

        public void Set(string key, object value)
        {
            if (string.IsNullOrEmpty(key)) return;
            settings[key] = value?.ToString() ?? string.Empty;
            keysCacheDirty = true;
            valuesCacheDirty = true;
        }

        public bool TryGetValue<T>(string key, out T value)
        {
            value = default;
            if (string.IsNullOrEmpty(key) || !settings.TryGetValue(key, out string strValue))
                return false;

            try
            {
                if (typeof(T).IsEnum)
                {
                    value = (T)Enum.Parse(typeof(T), strValue);
                }
                else
                {
                    value = (T)Convert.ChangeType(strValue, typeof(T));
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public SerializableSettings Clone()
        {
            var clone = new SerializableSettings();
            foreach (var kvp in settings)
            {
                clone.settings[kvp.Key] = kvp.Value;
            }
            return clone;
        }
    }

    [Serializable]
    public class SerializableStageProgress
    {
        private HashSet<int> clearedStages = new HashSet<int>();
        
        // 캐시된 리스트 (메모리 할당 최소화)
        [System.NonSerialized] private List<int> cachedStageNumbers = null;
        [System.NonSerialized] private bool stageNumbersCacheDirty = true;

        public List<int> stageNumbers
        {
            get 
            {
                if (stageNumbersCacheDirty || cachedStageNumbers == null)
                {
                    cachedStageNumbers = new List<int>(clearedStages);
                    stageNumbersCacheDirty = false;
                }
                return cachedStageNumbers;
            }
            set { } // 직렬화를 위해 필요
        }

        public void Set(int stageNum)
        {
            if (stageNum <= 0) return;
            clearedStages.Add(stageNum);
            stageNumbersCacheDirty = true;
        }

        public bool ContainsKey(int stageNum)
        {
            return clearedStages.Contains(stageNum);
        }

        public SerializableStageProgress Clone()
        {
            var clone = new SerializableStageProgress();
            foreach (var stage in clearedStages)
            {
                clone.clearedStages.Add(stage);
            }
            return clone;
        }
    }

    /// <summary>
    /// 게임 데이터 저장/로드를 담당하는 싱글턴 매니저
    /// DontDestroyOnLoad로 씬 전환과 무관하게 유지됨
    /// </summary>
    public class SaveDataManager : InvaderInsider.Core.SingletonManager<SaveDataManager>
    {
        // 로그 출력 제어 플래그
        
        
        private const string LOG_PREFIX = "[SaveData] ";

        private static string SAVE_KEY => Path.Combine(Application.persistentDataPath, "GameSaveData.json");
        private static string SETTINGS_SAVE_KEY => Path.Combine(Application.persistentDataPath, "GameSettings.json");
        
        // 싱글턴 인스턴스 - 단순하고 확실한 방식
        

        private SaveData currentSaveData;
        
        // 지연 저장 시스템
        // 지연 저장 관련 변수들 제거됨

        // 게임 시작 시 자동으로 SaveDataManager 인스턴스 생성
        

        // 인스턴스 존재 여부 확인 (null 체크 없이)
        

        public SaveData CurrentSaveData => currentSaveData?.Clone();

        private event Action<List<int>> onHandDataChanged;
        public event Action<List<int>> OnHandDataChanged
        {
            add
            {
                if (Instance != null && Application.isPlaying)
                {
                    onHandDataChanged -= value;
                    onHandDataChanged += value;
                }
            }
            remove
            {
                if (Instance != null && Application.isPlaying)
                {
                    onHandDataChanged -= value;
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();
            // 게임 데이터 초기화
            InitializeData();
            #if UNITY_EDITOR
            LogManager.Info(LOG_PREFIX, "SaveDataManager 초기화 완료");
            #endif
        }

        private void InitializeData()
        {
            // 설정값을 먼저 독립적으로 로드
            LoadSettingsIndependently();
            
            // 게임 데이터 로드
            LoadGameData();
            
            // 초기화 완료 후 상태 확인 (에러가 있을 때만 로그)
            if (currentSaveData == null)
            {
                LogManager.Error("초기화", "currentSaveData가 null입니다");
            }
        }

        protected override void OnDestroy()
        {
            // 이벤트 정리
            CleanupEventListeners();
            base.OnDestroy();
        }

        protected override void OnApplicationQuit()
        {
            // 게임 종료 시에는 저장하지 않음 (스테이지 클리어 시에만 저장)
            LogManager.Info(LOG_PREFIX, "게임 종료 - 저장하지 않음 (스테이지 클리어 시에만 저장)");
            base.OnApplicationQuit();
        }

        #if UNITY_EDITOR
        // 에디터에서 플레이 모드 종료 시 정리
        [UnityEditor.InitializeOnEnterPlayMode]
        static void OnEnterPlayModeInEditor(UnityEditor.EnterPlayModeOptions options)
        {
            InvaderInsider.Core.SingletonManager<SaveDataManager>.ResetInstance();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // 에디터에서 플레이 모드 종료 시 호출될 수 있음
            if (!Application.isPlaying && Instance == this)
            {
                CleanupEventListeners();
                // _instance = null; // SingletonManager에서 처리
            }
        }

        // 에디터에서 플레이 모드가 종료될 때 호출되는 정리 메서드
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            // 스크립트 리로드 시 정적 인스턴스 정리
            InvaderInsider.Core.SingletonManager<SaveDataManager>.ResetInstance();
        }
        #endif

        private void CleanupEventListeners()
        {
            onHandDataChanged = null;
        }

        public bool HasSaveData()
        {
            bool fileExists = File.Exists(SAVE_KEY);
            
            if (!fileExists)
            {
                LogManager.Info(LOG_PREFIX, "저장 파일이 존재하지 않음");
                return false;
            }

            // 파일이 존재하면 내용도 확인
            try
            {
                // 항상 최신 데이터를 로드하여 확인 (캐시 무시)
                LoadGameData();

                // Continue 가능 조건:
                // 1. 저장 데이터가 있고
                // 2. 게임을 시작한 기록이 있으면서 (EData > 기본값 또는 스테이지 클리어 기록)
                // 3. 모든 스테이지를 클리어했어도 재플레이 가능
                bool hasGameProgress = currentSaveData != null && 
                    (currentSaveData.progressData.highestStageCleared >= 0 || 
                     currentSaveData.progressData.currentEData > 100); // 기본 EData보다 많으면 플레이한 적 있음
                
                bool hasProgress = false;
                if (hasGameProgress)
                {
                    int highestCleared = currentSaveData.progressData.highestStageCleared;
                    
                    // Continue 가능 조건을 더 관대하게 변경:
                    // 1. 게임을 시작한 적이 있으면 항상 Continue 가능 (첫 스테이지 클리어 후에도)
                    // 2. 모든 스테이지를 클리어했어도 재플레이를 위해 Continue 가능
                    
                    if (highestCleared < 0)
                    {
                        // 아직 아무 스테이지도 클리어하지 않았지만 게임을 시작한 기록이 있음
                        hasProgress = true;
                        LogManager.Info(LOG_PREFIX, "게임 시작 기록 있음 - Continue 가능");
                    }
                    else
                    {
                        // StageManager에서 총 스테이지 수 확인
                        var stageManager = FindObjectOfType<InvaderInsider.Managers.StageManager>();
                        if (stageManager != null)
                        {
                            int totalStages = stageManager.GetStageCount();
                            
                            // 스테이지를 하나라도 클리어했다면 항상 Continue 가능 (재플레이 포함)
                            hasProgress = highestCleared >= 0 && totalStages > 0;
                            
                            LogManager.Info(LOG_PREFIX, $"Continue 조건 체크 - 최고 클리어: {highestCleared}, 총 스테이지: {totalStages}, Continue 가능: {hasProgress}");
                        }
                        else
                        {
                            // StageManager를 찾을 수 없으면 게임 진행 기록이 있으면 Continue 허용
                            hasProgress = true;
                            LogManager.Info(LOG_PREFIX, $"StageManager를 찾을 수 없어 기본 Continue 허용: {hasProgress}");
                        }
                    }
                }
                
                LogManager.Info(LOG_PREFIX, $"저장 데이터 확인 - 파일 존재: {fileExists}, 데이터 유효: {currentSaveData != null}, 최고 클리어 스테이지: {currentSaveData?.progressData.highestStageCleared ?? -1}, EData: {currentSaveData?.progressData.currentEData ?? 0}, 결과: {hasProgress}");
                
                return hasProgress;
            }
            catch (Exception e)
            {
                LogManager.Error("데이터 확인", e.Message);
                return false;
            }
        }

        public void ResetGameData()
        {
            currentSaveData = new SaveData();
            if (File.Exists(SAVE_KEY))
            {
                File.Delete(SAVE_KEY);
            }
        }

        // 지연 저장: 여러 변경사항을 모아서 한 번에 저장
        private void SaveGameData()
        {
            LogManager.Info("SaveData", "SaveGameData() 호출됨");
            
            // 게임이 실행 중이 아닐 때만 저장하지 않음
            if (!Application.isPlaying) 
            {
                LogManager.Info("SaveData", "Application.isPlaying이 false여서 저장 취소");
                return;
            }
            
            if (currentSaveData == null)
            {
                LogManager.Info("SaveData", "SaveGameData - currentSaveData가 null!");
                return;
            }
            
            // 지연 저장 제거하고 즉시 저장으로 변경
            SaveGameDataImmediate();
        }
        
        // 지연 저장 제거됨 - 즉시 저장으로 변경
        
        // 즉시 저장 (동기식) - 로그 최소화
        private void SaveGameDataImmediate()
        {
            LogManager.Info("SaveData", "SaveGameDataImmediate() 시작");
            
            try
            {
                string json = JsonConvert.SerializeObject(currentSaveData, Formatting.Indented);
                File.WriteAllText(SAVE_KEY, json);
                
                LogManager.Info("SaveData", "저장 완료! 파일: {0}", SAVE_KEY);
                LogManager.Info("SaveData", "저장된 최고 클리어 스테이지: {0}", currentSaveData?.progressData?.highestStageCleared);
                
                LogManager.Info(LOG_PREFIX, $"게임 데이터 저장 성공 - 파일: {SAVE_KEY}, 최고 클리어 스테이지: {currentSaveData?.progressData?.highestStageCleared}");
            }
            catch (Exception e)
            {
                LogManager.Error("SaveData", "저장 실패! 에러: {0}", e.Message);
                LogManager.Error("데이터 저장", e.Message);
            }
        }

        public void LoadGameData()
        {
            // 게임이 실행 중이 아닐 때만 로드하지 않음
            if (!Application.isPlaying) return;
            
            LogManager.Info(LOG_PREFIX, $"게임 데이터 로드 시도 - 파일 경로: {SAVE_KEY}");
            
            try
            {
                if (File.Exists(SAVE_KEY))
                {
                    string json = File.ReadAllText(SAVE_KEY);
                    currentSaveData = JsonConvert.DeserializeObject<SaveData>(json);
                    
                    if (currentSaveData == null)
                    {
                        LogManager.Error("로드", "역직렬화 실패");
                        currentSaveData = new SaveData();
                    }
                    else
                    {
                        LogManager.Info(LOG_PREFIX, $"게임 데이터 로드 성공 - 최고 클리어 스테이지: {currentSaveData.progressData.highestStageCleared}, eData: {currentSaveData.progressData.currentEData}");
                    }
                }
                else
                {
                    currentSaveData = new SaveData();
                }
            }
            catch (Exception e)
            {
                LogManager.Error("SaveDataManager", e.Message);
                currentSaveData = new SaveData();
            }
        }

        public void UpdateStageProgress(int stageNum)
        {
            UpdateStageProgress(stageNum, true);
        }

        public void UpdateStageProgress(int stageNum, bool saveImmediately)
        {
            LogManager.Info("SaveData", "UpdateStageProgress({0}, {1}) 호출됨", stageNum, saveImmediately);
            
            if (currentSaveData == null)
            {
                LogManager.Info("SaveData", "currentSaveData가 null! 초기화 시도");
                LogManager.Error("SaveDataManager", "currentSaveData가 null입니다");
                InitializeData(); // 데이터 초기화 시도
                if (currentSaveData == null)
                {
                    LogManager.Info("SaveData", "초기화 후에도 currentSaveData가 null!");
                    return;
                }
            }

            // 이전 값과 비교하여 정상적인 진행인지 확인
            int previousHighest = currentSaveData.progressData.highestStageCleared;
            LogManager.Info("SaveData", "이전 최고 클리어: {0}, 새 스테이지: {1}", previousHighest, stageNum);
            
            currentSaveData.stageProgress.Set(stageNum);
            currentSaveData.progressData.highestStageCleared = 
                Mathf.Max(currentSaveData.progressData.highestStageCleared, stageNum);

            LogManager.Info("SaveData", "업데이트 후 최고 클리어: {0}", currentSaveData.progressData.highestStageCleared);

            // 정상적인 진행 상황만 로그 (에러가 아닌 경우)
            if (stageNum > previousHighest)
            {
                LogManager.Info("SaveData", "새 스테이지 진행: {0} (이전 최고: {1})", stageNum, previousHighest);
                LogManager.Info(LOG_PREFIX, $"새 스테이지 진행: {stageNum} (이전 최고: {previousHighest})");
            }

            if (saveImmediately)
            {
                LogManager.Info("SaveData", "즉시 저장 호출 시작");
                LogManager.Info(LOG_PREFIX, "즉시 저장 호출");
                SaveGameData();
                LogManager.Info("SaveData", "즉시 저장 호출 완료");
            }
            else
            {
                LogManager.Info("SaveData", "즉시 저장하지 않음 (saveImmediately=false)");
            }
        }

        public void UpdateEData(int amount)
        {
            UpdateEData(amount, false);
        }

        public void UpdateEData(int amount, bool saveImmediately)
        {
            if (amount == 0) return;

            currentSaveData.progressData.currentEData += amount;
            // eData UI는 이제 GameManager/StageManager에서 직접 호출로 업데이트됨
        }

        // 저장하지 않고 eData만 업데이트 (적을 잡을 때 사용)
        public void UpdateEDataWithoutSave(int amount)
        {
            if (amount == 0) return;

            currentSaveData.progressData.currentEData += amount;
            // eData UI는 이제 GameManager/StageManager에서 직접 호출로 업데이트됨
        }

        // 강제 저장 (스테이지 클리어 등 중요한 이벤트 시 사용)
        public void ForceSave()
        {
            SaveGameData();
        }

        public void AddCardToDeck(int cardId)
        {
            if (cardId <= 0) return;

            if (currentSaveData.deckData.AddToDeck(cardId))
            {
                // 덱 구성 변경 시에는 저장하지 않고 메모리에만 업데이트
                // 스테이지 클리어/게임 종료 시에만 저장됨
            }
        }

        public void RemoveCardFromDeck(int cardId)
        {
            if (cardId <= 0) return;

            if (currentSaveData.deckData.RemoveFromDeck(cardId))
            {
                // 덱 구성 변경 시에는 저장하지 않고 메모리에만 업데이트
                // 스테이지 클리어/게임 종료 시에만 저장됨
            }
        }

        public void AddCardToOwned(int cardId)
        {
            if (cardId <= 0) return;

            if (currentSaveData.deckData.AddToOwned(cardId))
            {
                // 카드 획득 시에는 저장하지 않고 메모리에만 업데이트
                // 스테이지 클리어/게임 종료 시에만 저장됨
            }
        }

        public void AddCardToHandAndOwned(int cardId)
        {
            if (cardId <= 0) return;

            currentSaveData.deckData.AddToOwned(cardId);
            if (currentSaveData.deckData.AddToHand(cardId))
            {
                onHandDataChanged?.Invoke(currentSaveData.deckData.handCardIds);
                LogManager.Info("SaveDataManager", $"OnHandDataChanged 이벤트 호출됨. 핸드 카드 수: {currentSaveData.deckData.handCardIds.Count}");
            }

            // 카드 획득 시에는 저장하지 않고 메모리에만 업데이트
            // 스테이지 클리어/게임 종료 시에만 저장됨
        }

        public void RemoveCardFromHand(int cardId)
        {
            if (cardId <= 0) return;

            if (currentSaveData.deckData.RemoveFromHand(cardId))
            {
                onHandDataChanged?.Invoke(currentSaveData.deckData.handCardIds);
                // 핸드 변경 시에는 저장하지 않고 메모리에만 업데이트
                // 스테이지 클리어/게임 종료 시에만 저장됨
            }
        }

        public T GetSetting<T>(string key, T defaultValue)
        {
            if (string.IsNullOrEmpty(key)) return defaultValue;

            if (currentSaveData.settings.TryGetValue(key, out T value))
            {
                return value;
            }
            return defaultValue;
        }

        public void SaveSetting<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key)) return;

            currentSaveData.settings.Set(key, value);
            // 설정값만 별도 파일에 저장
            SaveSettingsOnly();
        }
        
        // 설정값만 별도로 저장하는 메서드
        private void SaveSettingsOnly()
        {
            // 설정은 에디터 모드에서도 저장됨 (사용자 설정이므로)
            try
            {
                string json = JsonConvert.SerializeObject(currentSaveData.settings, Formatting.Indented);
                File.WriteAllText(SETTINGS_SAVE_KEY, json);
                
                #if UNITY_EDITOR
                LogManager.Info(LOG_PREFIX, "설정 데이터만 저장 성공");
                #endif
            }
            catch (Exception e)
            {
                #if UNITY_EDITOR
                LogManager.Error(LOG_PREFIX, $"설정 저장 실패: {e.Message}");
                #endif
            }
        }
        
        // 게임 시작 시 설정값을 독립적으로 로드하는 메서드
        private void LoadSettingsIndependently()
        {
            // 에디터 모드에서도 설정은 로드
            try
            {
                if (File.Exists(SETTINGS_SAVE_KEY))
                {
                    string json = File.ReadAllText(SETTINGS_SAVE_KEY);
                    var loadedSettings = JsonConvert.DeserializeObject<SerializableSettings>(json);
                    if (loadedSettings != null)
                    {
                        // currentSaveData가 없으면 임시로 생성
                        if (currentSaveData == null)
                        {
                            currentSaveData = new SaveData();
                        }
                        currentSaveData.settings = loadedSettings;
                        #if UNITY_EDITOR
                        LogManager.Info(LOG_PREFIX, "설정 데이터 독립 로드 성공");
                        #endif
                    }
                }
                else
                {
                    // 설정 파일이 없으면 기본 설정으로 초기화
                    if (currentSaveData == null)
                    {
                        currentSaveData = new SaveData();
                    }
                    #if UNITY_EDITOR
                    LogManager.Info(LOG_PREFIX, "설정 파일 없음 - 기본 설정 사용");
                    #endif
                }
            }
            catch (Exception e)
            {
                // 설정 로드 실패 시 기본 설정 사용
                if (currentSaveData == null)
                {
                    currentSaveData = new SaveData();
                }
                #if UNITY_EDITOR
                LogManager.Warning(LOG_PREFIX, $"설정 독립 로드 실패 (기본값 사용): {e.Message}");
                #endif
            }
        }

        // 설정값만 별도로 로드하는 메서드 (기존 게임데이터 로드 중 사용)
        private void LoadSettingsOnly()
        {
            try
            {
                if (File.Exists(SETTINGS_SAVE_KEY))
                {
                    string json = File.ReadAllText(SETTINGS_SAVE_KEY);
                    var loadedSettings = JsonConvert.DeserializeObject<SerializableSettings>(json);
                    if (loadedSettings != null)
                    {
                        currentSaveData.settings = loadedSettings;
                        #if UNITY_EDITOR
                        LogManager.Info(LOG_PREFIX, "설정 데이터 로드 성공");
                        #endif
                    }
                }
            }
            catch (Exception e)
            {
                #if UNITY_EDITOR
                LogManager.Warning(LOG_PREFIX, $"설정 로드 실패 (기본값 사용): {e.Message}");
                #endif
            }
        }

        public bool IsStageUnlocked(int stageNum)
        {
            return stageNum <= 1 || currentSaveData.progressData.highestStageCleared >= stageNum - 1;
        }

        public int GetCurrentEData()
        {
            return currentSaveData.progressData.currentEData;
        }

        
    }
}