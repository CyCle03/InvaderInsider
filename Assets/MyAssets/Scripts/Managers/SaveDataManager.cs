using UnityEngine;
using System;
using System.Collections.Generic;
using InvaderInsider.Cards;
using System.Threading.Tasks;

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
            "Initializing instance",
            "Instance already exists, destroying duplicate",
            "Game data has been reset.",
            "Saved game data: {0}",
            "Loaded game data: {0}",
            "No saved data found, using default values",
            "Stage {0} progress updated to {1} stars",
            "Stage {0} is now unlocked",
            "EData updated: {0}",
            "Card {0} added to deck",
            "Card {0} removed from deck",
            "Card {0} added to owned cards",
            "Card {0} added to hand and owned cards",
            "Card {0} removed from hand",
            "SaveData key not found in PlayerPrefs",
            "Failed to parse saved data: {0}",
            "Failed to save game data: {0}",
            "Save operation started",
            "Save operation completed",
            "Load operation started",
            "Load operation completed"
        };

        private const string SAVE_KEY = "GameSaveData";
        private static SaveDataManager instance;
        private static readonly object _lock = new object();
        private static bool isQuitting = false;
        private static bool isSaving = false;
        private static bool isLoading = false;

        private SaveData currentSaveData;
        private readonly Queue<Action> saveQueue = new Queue<Action>();
        private readonly Queue<Action> loadQueue = new Queue<Action>();

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

        private event Action onGameDataLoaded;
        public event Action OnGameDataLoaded
        {
            add
            {
                if (!isQuitting)
                {
                    onGameDataLoaded -= value;
                    onGameDataLoaded += value;
                }
            }
            remove
            {
                if (!isQuitting)
                {
                    onGameDataLoaded -= value;
                }
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                if (Application.isPlaying)
                {
                    Debug.Log(LOG_PREFIX + LOG_MESSAGES[0]);
                }
                instance = this;
                DontDestroyOnLoad(gameObject);
                currentSaveData = new SaveData();
                LoadGameData();
            }
            else if (instance != this)
            {
                if (Application.isPlaying)
                {
                    Debug.Log(LOG_PREFIX + LOG_MESSAGES[1]);
                }
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                SaveGameData();
                CleanupEventListeners();
            }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
            SaveGameData();
        }

        private void CleanupEventListeners()
        {
            onEDataChanged = null;
            onHandDataChanged = null;
            onGameDataLoaded = null;
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
            if (Application.isPlaying)
            {
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[2]);
            }
        }

        public async void SaveGameData()
        {
            if (isSaving)
            {
                saveQueue.Enqueue(() => SaveGameData());
                return;
            }

            isSaving = true;
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[17]);

            try
            {
                string json = JsonUtility.ToJson(currentSaveData);
                PlayerPrefs.SetString(SAVE_KEY, json);
                PlayerPrefs.Save();
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[3], json));
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format(LOG_PREFIX + LOG_MESSAGES[16], e.Message));
            }

            isSaving = false;
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[18]);

            if (saveQueue.Count > 0)
            {
                var nextSave = saveQueue.Dequeue();
                nextSave?.Invoke();
            }
        }

        public async void LoadGameData()
        {
            if (isLoading)
            {
                loadQueue.Enqueue(() => LoadGameData());
                return;
            }

            isLoading = true;
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[19]);

            try
            {
                if (PlayerPrefs.HasKey(SAVE_KEY))
                {
                    string json = PlayerPrefs.GetString(SAVE_KEY);
                    currentSaveData = JsonUtility.FromJson<SaveData>(json);
                    Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[4], json));
                }
                else
                {
                    currentSaveData = new SaveData();
                    Debug.Log(LOG_PREFIX + LOG_MESSAGES[5]);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format(LOG_PREFIX + LOG_MESSAGES[15], e.Message));
                currentSaveData = new SaveData();
            }

            isLoading = false;
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[20]);
            onGameDataLoaded?.Invoke();

            if (loadQueue.Count > 0)
            {
                var nextLoad = loadQueue.Dequeue();
                nextLoad?.Invoke();
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

            Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[6], stageNum, stars));
            SaveGameData();
        }

        public void UpdateEData(int amount)
        {
            if (amount == 0) return;

            currentSaveData.progressData.currentEData += amount;
            Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[8], currentSaveData.progressData.currentEData));
            onEDataChanged?.Invoke(currentSaveData.progressData.currentEData);
            SaveGameData();
        }

        public void AddCardToDeck(int cardId)
        {
            if (cardId <= 0) return;

            if (currentSaveData.deckData.AddToDeck(cardId))
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[9], cardId));
                SaveGameData();
            }
        }

        public void RemoveCardFromDeck(int cardId)
        {
            if (cardId <= 0) return;

            if (currentSaveData.deckData.RemoveFromDeck(cardId))
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[10], cardId));
                SaveGameData();
            }
        }

        public void AddCardToOwned(int cardId)
        {
            if (cardId <= 0) return;

            if (currentSaveData.deckData.AddToOwned(cardId))
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[11], cardId));
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
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[12], cardId));
                SaveGameData();
            }
        }

        public void RemoveCardFromHand(int cardId)
        {
            if (cardId <= 0) return;

            if (currentSaveData.deckData.RemoveFromHand(cardId))
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[13], cardId));
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