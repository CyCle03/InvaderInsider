using UnityEngine;
using System;
using System.Collections.Generic;
using InvaderInsider.Cards;

namespace InvaderInsider.Data
{
    [Serializable]
    public class SaveData
    {
        public ProgressData progressData = new ProgressData();
        public DeckData deckData = new DeckData();
        public SerializableSettings settings = new SerializableSettings();
        public SerializableStageProgress stageProgress = new SerializableStageProgress();
    }

    [Serializable]
    public class ProgressData
    {
        public int currentEData = 0;
        public int highestStageCleared = 0;
        public int summonCount = 0;
    }

    [Serializable]
    public class DeckData
    {
        public List<int> deckCardIds = new List<int>();
        public List<int> ownedCardIds = new List<int>();
        public List<int> handCardIds = new List<int>();
    }

    [Serializable]
    public class SerializableSettings
    {
        public List<string> keys = new List<string>();
        public List<string> values = new List<string>();

        public void Set(string key, object value)
        {
            int index = keys.IndexOf(key);
            if (index != -1)
            {
                values[index] = value.ToString();
            }
            else
            {
                keys.Add(key);
                values.Add(value.ToString());
            }
        }

        public bool TryGetValue<T>(string key, out T value)
        {
            int index = keys.IndexOf(key);
            if (index != -1)
            {
                try
                {
                    if (typeof(T).IsEnum)
                    {
                        value = (T)Enum.Parse(typeof(T), values[index]);
                    }
                    else
                    {
                        value = (T)Convert.ChangeType(values[index], typeof(T));
                    }
                    return true;
                }
                catch
                {
                    value = default(T);
                    return false;
                }
            }
            value = default(T);
            return false;
        }
    }

    [Serializable]
    public class SerializableStageProgress
    {
        public List<int> stageNumbers = new List<int>();
        public List<int> stars = new List<int>();

        public void Set(int stageNum, int starCount)
        {
            int index = stageNumbers.IndexOf(stageNum);
            if (index != -1)
            {
                stars[index] = starCount;
            }
            else
            {
                stageNumbers.Add(stageNum);
                stars.Add(starCount);
            }
        }

        public bool TryGetValue(int stageNum, out int starCount)
        {
            int index = stageNumbers.IndexOf(stageNum);
            if (index != -1)
            {
                starCount = stars[index];
                return true;
            }
            starCount = 0;
            return false;
        }

        public bool ContainsKey(int stageNum)
        {
            return stageNumbers.Contains(stageNum);
        }
    }

    public class SaveDataManager : MonoBehaviour
    {
        private static SaveDataManager instance;
        private static readonly object _lock = new object();
        private static bool isQuitting = false;

        public static SaveDataManager Instance
        {
            get
            {
                if (isQuitting)
                {
                    return null;
                }

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

        private SaveData currentSaveData = new SaveData();
        public SaveData CurrentSaveData => currentSaveData;

        // eData 변경 이벤트 추가
        public event Action<int> OnEDataChanged;

        // 핸드 데이터 변경 이벤트 추가
        public event Action<List<int>> OnHandDataChanged; // 핸드에 있는 카드 ID 목록을 전달

        // 저장된 게임 데이터 로드가 완료되었음을 알리는 이벤트
        public event Action OnGameDataLoaded;

        private void Awake()
        {
            if (instance == null)
            {
                Debug.Log("[SaveDataManager] Initializing instance");
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Debug.Log("[SaveDataManager] Instance already exists, destroying duplicate");
                Destroy(gameObject);
            }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        // 저장된 게임 데이터를 모두 초기화하는 함수
        public void ResetGameData()
        {
            currentSaveData = new SaveData(); // 새로운 SaveData 인스턴스로 교체
            PlayerPrefs.DeleteKey("SaveData"); // PlayerPrefs에 저장된 데이터 삭제
            PlayerPrefs.Save(); // 변경사항 저장
            Debug.Log("[SaveDataManager] Game data has been reset.");
        }

        public void SaveGameData()
        {
            string jsonData = JsonUtility.ToJson(currentSaveData);
            PlayerPrefs.SetString("SaveData", jsonData);
            PlayerPrefs.Save();
            Debug.Log($"[SaveDataManager] Saved game data: {jsonData}");
        }

        public bool LoadGameData()
        {
            if (PlayerPrefs.HasKey("SaveData"))
            {
                string jsonData = PlayerPrefs.GetString("SaveData");
                currentSaveData = JsonUtility.FromJson<SaveData>(jsonData);
                Debug.Log($"[SaveDataManager] Loaded game data: {jsonData}");

                // 로드된 eData 값을 UI에 반영하기 위해 이벤트 발생
                OnEDataChanged?.Invoke(currentSaveData.progressData.currentEData);

                // 저장된 게임 데이터 로드가 완료되었음을 알리는 이벤트
                OnGameDataLoaded?.Invoke();
                return true;
            }
            else
            {
                Debug.Log("[SaveDataManager] No saved data found, using default values");
                currentSaveData = new SaveData();
                return false;
            }
        }

        public void UpdateStageProgress(int stageNum, int stars)
        {
            if (!currentSaveData.stageProgress.ContainsKey(stageNum) || 
                !currentSaveData.stageProgress.TryGetValue(stageNum, out int currentStars) || 
                currentStars < stars)
            {
                currentSaveData.stageProgress.Set(stageNum, stars);
                
                if (stageNum >= currentSaveData.progressData.highestStageCleared)
                {
                    // 클리어한 스테이지 번호가 현재 최고 기록과 같거나 높으면 다음 스테이지로 업데이트
                    currentSaveData.progressData.highestStageCleared = stageNum + 1;
                }
                
                // SaveGameData(); // 스테이지 클리어 시점에 저장
            }
        }

        public T GetSetting<T>(string key, T defaultValue)
        {
            if (currentSaveData.settings.TryGetValue<T>(key, out T value))
            {
                return value;
            }
            return defaultValue;
        }

        public void SaveSetting<T>(string key, T value)
        {
            currentSaveData.settings.Set(key, value);
            // SaveGameData();
        }

        public void AddCardToDeck(int cardId)
        {
            if (!currentSaveData.deckData.deckCardIds.Contains(cardId))
            {
                currentSaveData.deckData.deckCardIds.Add(cardId);
                SaveGameData();
            }
        }

        public void RemoveCardFromDeck(int cardId)
        {
            if (currentSaveData.deckData.deckCardIds.Contains(cardId))
            {
                currentSaveData.deckData.deckCardIds.Remove(cardId);
                SaveGameData();
            }
        }

        public void AddCardToOwned(int cardId)
        {
            if (!currentSaveData.deckData.ownedCardIds.Contains(cardId))
            {
                currentSaveData.deckData.ownedCardIds.Add(cardId);
                SaveGameData();
            }
        }

        public int GetStageStars(int stageNum)
        {
            return currentSaveData.stageProgress.TryGetValue(stageNum, out int stars) ? stars : 0;
        }

        public bool IsStageUnlocked(int stageNum)
        {
            return stageNum <= currentSaveData.progressData.highestStageCleared + 1;
        }

        public void UpdateEData(int amount)
        {
            currentSaveData.progressData.currentEData += amount;
            // eData가 0 미만이 되지 않도록 방지 (선택 사항)
            if (currentSaveData.progressData.currentEData < 0)
            {
                currentSaveData.progressData.currentEData = 0;
            }
            OnEDataChanged?.Invoke(currentSaveData.progressData.currentEData);
        }

        public int GetCurrentEData()
        {
            return currentSaveData.progressData.currentEData;
        }

        // 새로운 카드를 핸드 및 소유 목록에 추가하는 함수
        public void AddCardToHandAndOwned(int cardId)
        {
            // 핸드에 추가
            if (currentSaveData.deckData.handCardIds.Count < 10) // 핸드 최대 크기 제한 (예시: 10)
            {
                currentSaveData.deckData.handCardIds.Add(cardId);
                Debug.Log($"[SaveDataManager] Card {cardId} added to hand. Hand size: {currentSaveData.deckData.handCardIds.Count}");

                // 소유 목록에 없으면 추가
                if (!currentSaveData.deckData.ownedCardIds.Contains(cardId))
                {
                    currentSaveData.deckData.ownedCardIds.Add(cardId);
                    Debug.Log($"[SaveDataManager] Card {cardId} added to owned list.");
                }

                SaveGameData(); // 데이터 저장
                // 핸드 UI 업데이트 이벤트 발생
                OnHandDataChanged?.Invoke(currentSaveData.deckData.handCardIds);
            }
            else
            {
                Debug.LogWarning($"[SaveDataManager] Hand is full. Cannot add card {cardId}.");
                // TODO: 핸드가 가득 찼음을 알리는 UI 메시지 표시
            }
        }

        // 핸드에서 카드를 제거하는 함수 (사용 또는 장착 시)
        public void RemoveCardFromHand(int cardId)
        {
            if (currentSaveData.deckData.handCardIds.Contains(cardId))
            {
                currentSaveData.deckData.handCardIds.Remove(cardId);
                SaveGameData(); // 데이터 저장
                Debug.Log($"[SaveDataManager] Card {cardId} removed from hand. Hand size: {currentSaveData.deckData.handCardIds.Count}");
                // 핸드 UI 업데이트 이벤트 발생
                OnHandDataChanged?.Invoke(currentSaveData.deckData.handCardIds);
            }
        }
    }
} 