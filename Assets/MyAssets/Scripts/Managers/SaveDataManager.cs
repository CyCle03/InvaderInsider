using UnityEngine;
using System;
using System.Collections.Generic;
using InvaderInsider.Managers;
using System.IO;
using Newtonsoft.Json;

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
                    cardIds = new List<int>(this.deckData.cardIds)
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
        private HashSet<int> cardIdSet = new HashSet<int>();
        
        [System.NonSerialized] private List<int> cachedCardIds = null;
        [System.NonSerialized] private bool cardCacheDirty = true;

        public List<int> cardIds
        {
            get 
            {
                if (cardCacheDirty || cachedCardIds == null)
                {
                    cachedCardIds = new List<int>(cardIdSet);
                    cardCacheDirty = false;
                }
                return cachedCardIds;
            }
            set 
            {
                cardIdSet = new HashSet<int>(value);
                cardCacheDirty = true;
            }
        }

        public bool AddCard(int cardId) 
        { 
            bool added = cardIdSet.Add(cardId);
            if (added) cardCacheDirty = true;
            return added;
        }
        
        public bool RemoveCard(int cardId) 
        { 
            bool removed = cardIdSet.Remove(cardId);
            if (removed) cardCacheDirty = true;
            return removed;
        }
        
        public bool HasCard(int cardId) => cardIdSet.Contains(cardId);

        public void Clear()
        {
            cardIdSet.Clear();
            cardCacheDirty = true;
        }
    }

    [Serializable]
    public class SerializableSettings
    {
        private Dictionary<string, string> settings = new Dictionary<string, string>();
        
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
            set { }
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
            set { }
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
            set { }
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

    public class SaveDataManager : MonoBehaviour
    {
        private const bool ENABLE_LOGS = false;
        private const string LOG_PREFIX = "[SaveData] ";
        
        private static void LogOnly(string message)
        {
#if UNITY_EDITOR && !DISABLE_LOGS
            if (ENABLE_LOGS) Debug.Log(LOG_PREFIX + message);
#endif
        }
        
        private static string SAVE_KEY => Path.Combine(Application.persistentDataPath, "GameSaveData.json");
        private static string SETTINGS_SAVE_KEY => Path.Combine(Application.persistentDataPath, "GameSettings.json");
        
        private static SaveDataManager _instance;
        private static readonly object _lock = new object();
        public static SaveDataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            CreateInstance();
                        }
                    }
                }
                
                if (_instance != null && _instance.currentSaveData == null)
                {
                    _instance.InitializeData();
                }
                
                return _instance;
            }
        }

        private SaveData currentSaveData;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (Application.isPlaying)
            {
                CreateInstance();
            }
        }

        private static void CreateInstance()
        {
            if (_instance != null) return;

            _instance = FindObjectOfType<SaveDataManager>();
            
            if (_instance == null)
            {
                GameObject go = new GameObject("[SaveDataManager]");
                _instance = go.AddComponent<SaveDataManager>();
                DontDestroyOnLoad(go);
            }
            else
            {
                DontDestroyOnLoad(_instance.gameObject);
            }
        }

        public static bool HasInstance => _instance != null;

        public SaveData CurrentSaveData => currentSaveData?.Clone();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeData();
            }
        }

        private void InitializeData()
        {
            LoadSettingsIndependently();
            LoadGameData();
            
            if (currentSaveData == null)
            {
                LogManager.LogSave("초기화", "currentSaveData가 null입니다", true);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            if (currentSaveData != null && Application.isPlaying)
            {
                try 
                {
                    SaveGameDataImmediate();
                }
                catch (System.Exception e)
                {
                    Debug.LogError(LOG_PREFIX + "게임 종료 시 저장 실패: " + e.Message);
                }
            }
            
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public bool HasSaveData()
        {
            bool fileExists = File.Exists(SAVE_KEY);
            if (fileExists)
            {
                try
                {
                    LoadGameData();
                    bool hasGameProgress = currentSaveData != null && 
                        (currentSaveData.progressData.highestStageCleared >= 0 || 
                         currentSaveData.progressData.currentEData > 100);
                    
                    return hasGameProgress;
                }
                catch (Exception e)
                {
                    LogManager.LogSave("데이터 확인", e.Message, true);
                    return false;
                }
            }
            return false;
        }

        public void ResetGameData()
        {
            currentSaveData = new SaveData();
            if (File.Exists(SAVE_KEY))
            {
                File.Delete(SAVE_KEY);
            }
        }

        public void SaveGameData()
        {
            if (!Application.isPlaying || currentSaveData == null) return;
            SaveGameDataImmediate();
        }
        
        private void SaveGameDataImmediate()
        {
            try
            {
                string json = JsonConvert.SerializeObject(currentSaveData, Formatting.Indented);
                File.WriteAllText(SAVE_KEY, json);
            }
            catch (Exception e)
            {
                LogManager.LogSave("데이터 저장", e.Message, true);
            }
        }

        public void LoadGameData()
        {
            if (!Application.isPlaying) return;
            
            try
            {
                if (File.Exists(SAVE_KEY))
                {
                    string json = File.ReadAllText(SAVE_KEY);
                    currentSaveData = JsonConvert.DeserializeObject<SaveData>(json);
                    
                    if (currentSaveData == null)
                    {
                        LogManager.LogSave("로드", "역직렬화 실패", true);
                        currentSaveData = new SaveData();
                    }
                }
                else
                {
                    currentSaveData = new SaveData();
                }
            }
            catch (Exception e)
            {
                LogManager.LogSave("데이터 로드", e.Message, true);
                currentSaveData = new SaveData();
            }
        }
        
        public void SetOwnedCards(List<int> cardIds)
        {
            if (currentSaveData == null) InitializeData();
            currentSaveData.deckData.cardIds = cardIds;
        }

        public void UpdateStageProgress(int stageNum, bool saveImmediately)
        {
            if (currentSaveData == null)
            {
                InitializeData();
                if (currentSaveData == null) return;
            }

            currentSaveData.stageProgress.Set(stageNum);
            currentSaveData.progressData.highestStageCleared = 
                Mathf.Max(currentSaveData.progressData.highestStageCleared, stageNum);

            if (saveImmediately)
            {
                SaveGameData();
            }
        }

        public void UpdateEData(int amount, bool saveImmediately)
        {
            if (amount == 0) return;
            currentSaveData.progressData.currentEData += amount;

            // GameManager를 통해 UI 업데이트 요청
            GameManager.Instance?.UpdateEDataUI(currentSaveData.progressData.currentEData);

            if (saveImmediately)
            {
                SaveGameData();
            }
        }

        public void UpdateEDataWithoutSave(int amount)
        {
            if (amount == 0) return;
            currentSaveData.progressData.currentEData += amount;
        }

        public void ForceSave()
        {
            SaveGameData();
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
            SaveSettingsOnly();
        }
        
        private void SaveSettingsOnly()
        {
            try
            {
                string json = JsonConvert.SerializeObject(currentSaveData.settings, Formatting.Indented);
                File.WriteAllText(SETTINGS_SAVE_KEY, json);
            }
            catch (Exception e)
            {
                Debug.LogError(LOG_PREFIX + $"설정 저장 실패: {e.Message}");
            }
        }
        
        private void LoadSettingsIndependently()
        {
            try
            {
                if (File.Exists(SETTINGS_SAVE_KEY))
                {
                    string json = File.ReadAllText(SETTINGS_SAVE_KEY);
                    var loadedSettings = JsonConvert.DeserializeObject<SerializableSettings>(json);
                    if (loadedSettings != null)
                    {
                        if (currentSaveData == null)
                        {
                            currentSaveData = new SaveData();
                        }
                        currentSaveData.settings = loadedSettings;
                    }
                }
                else
                {
                    if (currentSaveData == null)
                    {
                        currentSaveData = new SaveData();
                    }
                }
            }
            catch (Exception e)
            {
                if (currentSaveData == null)
                {
                    currentSaveData = new SaveData();
                }
                Debug.LogWarning(LOG_PREFIX + $"설정 독립 로드 실패 (기본값 사용): {e.Message}");
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
