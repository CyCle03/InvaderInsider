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
        private Dictionary<int, int> stageStars = new Dictionary<int, int>();
        
        // 캐시된 리스트들 (메모리 할당 최소화)
        [System.NonSerialized] private List<int> cachedStageNumbers = null;
        [System.NonSerialized] private List<int> cachedStars = null;
        [System.NonSerialized] private bool stageNumbersCacheDirty = true;
        [System.NonSerialized] private bool starsCacheDirty = true;

        public List<int> stageNumbers
        {
            get 
            {
                if (stageNumbersCacheDirty || cachedStageNumbers == null)
                {
                    cachedStageNumbers = new List<int>(stageStars.Keys);
                    stageNumbersCacheDirty = false;
                }
                return cachedStageNumbers;
            }
            set { } // 직렬화를 위해 필요
        }

        public List<int> stars
        {
            get 
            {
                if (starsCacheDirty || cachedStars == null)
                {
                    cachedStars = new List<int>(stageStars.Values);
                    starsCacheDirty = false;
                }
                return cachedStars;
            }
            set { } // 직렬화를 위해 필요
        }

        public void Set(int stageNum, int starCount)
        {
            if (stageNum <= 0) return;
            stageStars[stageNum] = Mathf.Clamp(starCount, 0, 3);
            stageNumbersCacheDirty = true;
            starsCacheDirty = true;
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

    /// <summary>
    /// 게임 데이터 저장/로드를 담당하는 싱글턴 매니저
    /// DontDestroyOnLoad로 씬 전환과 무관하게 유지됨
    /// </summary>
    public class SaveDataManager : MonoBehaviour
    {
        private const string LOG_PREFIX = "[SaveData] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "게임 데이터 저장 실패: {0}",
            "게임 데이터 로드 실패: {0}"
        };

        private const string SAVE_KEY = "GameSaveData.json";
        
        // 싱글턴 인스턴스 - 단순하고 확실한 방식
        private static SaveDataManager _instance;
        public static SaveDataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    CreateInstance();
                }
                return _instance;
            }
        }

        private SaveData currentSaveData;
        
        // 지연 저장 시스템
        private bool pendingSave = false;
        private float saveDelay = 1f; // 1초 대기 후 저장
        private Coroutine saveCoroutine = null;

        // 게임 시작 시 자동으로 SaveDataManager 인스턴스 생성
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

            // 기존 인스턴스가 있는지 확인
            _instance = FindObjectOfType<SaveDataManager>();
            
            if (_instance == null)
            {
                // 새 인스턴스 생성
                GameObject go = new GameObject("[SaveDataManager]");
                _instance = go.AddComponent<SaveDataManager>();
                DontDestroyOnLoad(go);
                
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "SaveDataManager 싱글턴 인스턴스 생성됨");
                #endif
            }
            else
            {
                // 기존 인스턴스 발견
                DontDestroyOnLoad(_instance.gameObject);
                
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "기존 SaveDataManager 인스턴스 발견 및 설정됨");
                #endif
            }
        }

        // 인스턴스 존재 여부 확인 (null 체크 없이)
        public static bool HasInstance => _instance != null;

        public SaveData CurrentSaveData => currentSaveData?.Clone();

        private event Action<List<int>> onHandDataChanged;
        public event Action<List<int>> OnHandDataChanged
        {
            add
            {
                if (_instance != null && Application.isPlaying)
                {
                    onHandDataChanged -= value;
                    onHandDataChanged += value;
                }
            }
            remove
            {
                if (_instance != null && Application.isPlaying)
                {
                    onHandDataChanged -= value;
                }
            }
        }

        private void Awake()
        {
            // 싱글턴 패턴 - 이미 인스턴스가 있으면 자신을 파괴
            if (_instance != null && _instance != this)
            {
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "중복 SaveDataManager 감지 - 파괴됨");
                #endif
                Destroy(gameObject);
                return;
            }

            // 첫 번째 인스턴스라면 설정
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                
                // 게임 데이터 초기화
                InitializeData();
                
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "SaveDataManager 초기화 완료");
                #endif
            }
        }

        private void InitializeData()
        {
            LoadGameData();
        }

        private void OnDestroy()
        {
            // 코루틴 정리
            if (saveCoroutine != null)
            {
                StopCoroutine(saveCoroutine);
                saveCoroutine = null;
            }
            
            // 이벤트 정리
            CleanupEventListeners();
            
            // 주 인스턴스가 파괴되는 경우에만 null로 설정
            if (_instance == this)
            {
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "SaveDataManager 인스턴스 파괴됨");
                #endif
                _instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            // 애플리케이션 종료 시 즉시 저장
            if (pendingSave && _instance == this)
            {
                SaveGameDataImmediate();
            }
        }

        private void CleanupEventListeners()
        {
            onHandDataChanged = null;
        }

        public bool HasSaveData()
        {
            return File.Exists(SAVE_KEY);
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
        public void SaveGameData()
        {
            // 에디터 모드에서는 저장하지 않음
            #if UNITY_EDITOR
            if (!Application.isPlaying) return;
            #endif
            
            if (!pendingSave)
            {
                pendingSave = true;
                if (saveCoroutine != null)
                {
                    StopCoroutine(saveCoroutine);
                }
                saveCoroutine = StartCoroutine(SaveGameDataDelayed());
            }
        }
        
        // 코루틴을 통한 지연 저장
        private System.Collections.IEnumerator SaveGameDataDelayed()
        {
            yield return new WaitForSecondsRealtime(saveDelay);
            SaveGameDataImmediate();
        }
        
        // 즉시 저장 (동기식)
        private void SaveGameDataImmediate()
        {
            try
            {
                string json = JsonConvert.SerializeObject(currentSaveData, Formatting.Indented);
                File.WriteAllText(SAVE_KEY, json);
                pendingSave = false;
                saveCoroutine = null;
                
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "게임 데이터 저장 성공");
                #endif
            }
            catch (Exception e)
            {
                pendingSave = false;
                saveCoroutine = null;
                
                #if UNITY_EDITOR
                Debug.LogError(string.Format(LOG_PREFIX + LOG_MESSAGES[0], e.Message));
                #endif
            }
        }

        public void LoadGameData()
        {
            // 에디터 모드에서는 로드하지 않음
            #if UNITY_EDITOR
            if (!Application.isPlaying) return;
            #endif
            
            try
            {
                if (File.Exists(SAVE_KEY))
                {
                    string json = File.ReadAllText(SAVE_KEY);
                    currentSaveData = JsonConvert.DeserializeObject<SaveData>(json);
                    
                    // eData UI는 이제 GameManager/StageManager에서 직접 호출로 업데이트됨
                    
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + $"게임 데이터 로드 성공 - 최고 클리어 스테이지: {currentSaveData.progressData.highestStageCleared}, eData: {currentSaveData.progressData.currentEData}");
                    #endif
                }
                else
                {
                    currentSaveData = new SaveData();
                    SaveGameData();
                    
                    // eData UI는 이제 GameManager/StageManager에서 직접 호출로 업데이트됨
                }
            }
            catch (Exception e)
            {
                #if UNITY_EDITOR
                Debug.LogError(string.Format(LOG_PREFIX + LOG_MESSAGES[1], e.Message));
                #endif
                currentSaveData = new SaveData();
                
                // eData UI는 이제 GameManager/StageManager에서 직접 호출로 업데이트됨
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

            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + $"스테이지 진행 업데이트: 스테이지 {stageNum}, 별 {stars}개, 최고 클리어 스테이지: {currentSaveData.progressData.highestStageCleared}");
            #endif

            SaveGameData();
        }

        public void UpdateEData(int amount)
        {
            if (amount == 0) return;

            currentSaveData.progressData.currentEData += amount;
            // eData UI는 이제 GameManager/StageManager에서 직접 호출로 업데이트됨
            SaveGameData();
        }

        // 저장하지 않고 eData만 업데이트 (적을 잡을 때 사용)
        public void UpdateEDataWithoutSave(int amount)
        {
            if (amount == 0) return;

            currentSaveData.progressData.currentEData += amount;
            // eData UI는 이제 GameManager/StageManager에서 직접 호출로 업데이트됨
        }

        // 강제 저장 (스테이지 클리어, 게임 종료 등 중요한 이벤트 시 사용)
        public void ForceSave()
        {
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