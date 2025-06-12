using UnityEngine;
using System;
using System.Collections.Generic;
using InvaderInsider.Cards;
using System.Threading.Tasks;
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

        public List<int> deckCardIds
        {
            get => new List<int>(deckCardSet);
            set => deckCardSet = new HashSet<int>(value);
        }

        public List<int> ownedCardIds
        {
            get => new List<int>(ownedCardSet);
            set => ownedCardSet = new HashSet<int>(value);
        }

        public List<int> handCardIds
        {
            get => new List<int>(handCardSet);
            set => handCardSet = new HashSet<int>(value);
        }

        public bool AddToDeck(int cardId) => deckCardSet.Add(cardId);
        public bool RemoveFromDeck(int cardId) => deckCardSet.Remove(cardId);
        public bool AddToOwned(int cardId) => ownedCardSet.Add(cardId);
        public bool AddToHand(int cardId) => handCardSet.Add(cardId);
        public bool RemoveFromHand(int cardId) => handCardSet.Remove(cardId);
        public bool IsInDeck(int cardId) => deckCardSet.Contains(cardId);
        public bool IsOwned(int cardId) => ownedCardSet.Contains(cardId);
        public bool IsInHand(int cardId) => handCardSet.Contains(cardId);
    }

    [Serializable]
    public class SerializableSettings
    {
        private Dictionary<string, string> settings = new Dictionary<string, string>();

        public List<string> keys
        {
            get => new List<string>(settings.Keys);
            set { } // 직렬화를 위해 필요
        }

        public List<string> values
        {
            get => new List<string>(settings.Values);
            set { } // 직렬화를 위해 필요
        }

        public void Set(string key, object value)
        {
            if (string.IsNullOrEmpty(key)) return;
            settings[key] = value?.ToString() ?? string.Empty;
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
        private Dictionary<int, int> stageStars = new Dictionary<int, int>();

        public List<int> stageNumbers
        {
            get => new List<int>(stageStars.Keys);
            set { } // 직렬화를 위해 필요
        }

        public List<int> stars
        {
            get => new List<int>(stageStars.Values);
            set { } // 직렬화를 위해 필요
        }

        public void Set(int stageNum, int starCount)
        {
            if (stageNum <= 0) return;
            stageStars[stageNum] = Mathf.Clamp(starCount, 0, 3);
        }

        public bool TryGetValue(int stageNum, out int starCount)
        {
            return stageStars.TryGetValue(stageNum, out starCount);
        }

        public bool ContainsKey(int stageNum)
        {
            return stageStars.ContainsKey(stageNum);
        }

        public SerializableStageProgress Clone()
        {
            var clone = new SerializableStageProgress();
            foreach (var kvp in stageStars)
            {
                clone.stageStars[kvp.Key] = kvp.Value;
            }
            return clone;
        }
    }

    public class SaveDataManager : MonoBehaviour
    {
        private const string LOG_PREFIX = "[SaveData] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "Error saving game data: {0}", // 0
            "Error loading game data: {0}" // 1
        };

        private const string SAVE_KEY = "GameSaveData";
        private static SaveDataManager instance;
        private static readonly object _lock = new object();
        private static bool isQuitting = false;

        private SaveData currentSaveData;

        public static SaveDataManager Instance
        {
            get
            {
                if (isQuitting) return null;

                lock (_lock)
                {
                    if (instance == null)
                    {
                        instance = FindObjectOfType<SaveDataManager>();
                        if (instance == null && !isQuitting)
                        {
                            GameObject go = new GameObject("SaveDataManager");
                            instance = go.AddComponent<SaveDataManager>();
                            DontDestroyOnLoad(go);
                        }
                    }
                    return instance;
                }
            }
        }

        public SaveData CurrentSaveData => currentSaveData?.Clone();

        private event Action<int> onEDataChanged;
        public event Action<int> OnEDataChanged
        {
            add
            {
                if (!isQuitting)
                {
                    onEDataChanged -= value;
                    onEDataChanged += value;
                }
            }
            remove
            {
                if (!isQuitting)
                {
                    onEDataChanged -= value;
                }
            }
        }

        private event Action<List<int>> onHandDataChanged;
        public event Action<List<int>> OnHandDataChanged
        {
            add
            {
                if (!isQuitting)
                {
                    onHandDataChanged -= value;
                    onHandDataChanged += value;
                }
            }
            remove
            {
                if (!isQuitting)
                {
                    onHandDataChanged -= value;
                }
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                
                LoadGameData();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                SaveGameData();
                instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            SaveGameData();
        }

        private void CleanupEventListeners()
        {
            onEDataChanged = null;
            onHandDataChanged = null;
        }

        public bool HasSaveData()
        {
            return PlayerPrefs.HasKey(SAVE_KEY);
        }

        public void ResetGameData()
        {
            currentSaveData = new SaveData();
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
        }

        public async void SaveGameData()
        {
            try
            {
                string json = JsonConvert.SerializeObject(currentSaveData, Formatting.Indented);
                await File.WriteAllTextAsync(SAVE_KEY, json);
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format(LOG_PREFIX + LOG_MESSAGES[0], e.Message));
            }
        }

        public async void LoadGameData()
        {
            try
            {
                if (File.Exists(SAVE_KEY))
                {
                    string json = await File.ReadAllTextAsync(SAVE_KEY);
                    currentSaveData = JsonConvert.DeserializeObject<SaveData>(json);
                }
                else
                {
                    currentSaveData = new SaveData();
                    SaveGameData();
                }
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format(LOG_PREFIX + LOG_MESSAGES[1], e.Message));
                currentSaveData = new SaveData();
            }
        }

        public void UpdateStageProgress(int stageNum, int stars)
        {
            if (stageNum <= 0 || stars < 0 || stars > 3) return;

            currentSaveData.stageProgress.Set(stageNum, stars);
            if (stars > 0)
            {
                currentSaveData.progressData.highestStageCleared = 
                    Mathf.Max(currentSaveData.progressData.highestStageCleared, stageNum);
            }

            SaveGameData();
        }

        public void UpdateEData(int amount)
        {
            if (amount == 0) return;

            currentSaveData.progressData.currentEData += amount;
            onEDataChanged?.Invoke(currentSaveData.progressData.currentEData);
            SaveGameData();
        }

        public void AddCardToDeck(int cardId)
        {
            if (cardId <= 0) return;

            if (currentSaveData.deckData.AddToDeck(cardId))
            {
                SaveGameData();
            }
        }

        public void RemoveCardFromDeck(int cardId)
        {
            if (cardId <= 0) return;

            if (currentSaveData.deckData.RemoveFromDeck(cardId))
            {
                SaveGameData();
            }
        }

        public void AddCardToOwned(int cardId)
        {
            if (cardId <= 0) return;

            if (currentSaveData.deckData.AddToOwned(cardId))
            {
                SaveGameData();
            }
        }

        public void AddCardToHandAndOwned(int cardId)
        {
            if (cardId <= 0) return;

            bool added = false;
            if (currentSaveData.deckData.AddToOwned(cardId))
            {
                added = true;
            }
            if (currentSaveData.deckData.AddToHand(cardId))
            {
                added = true;
                onHandDataChanged?.Invoke(currentSaveData.deckData.handCardIds);
            }

            if (added)
            {
                SaveGameData();
            }
        }

        public void RemoveCardFromHand(int cardId)
        {
            if (cardId <= 0) return;

            if (currentSaveData.deckData.RemoveFromHand(cardId))
            {
                onHandDataChanged?.Invoke(currentSaveData.deckData.handCardIds);
                SaveGameData();
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
            SaveGameData();
        }

        public int GetStageStars(int stageNum)
        {
            return currentSaveData.stageProgress.TryGetValue(stageNum, out int stars) ? stars : 0;
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