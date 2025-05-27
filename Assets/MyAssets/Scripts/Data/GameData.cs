using System;
using System.Collections.Generic;
using UnityEngine;

namespace InvaderInsider.Data
{
    [Serializable]
    public class PlayerDeckData
    {
        public List<int> deckCardIds = new List<int>();
        public List<int> ownedCardIds = new List<int>();
    }

    [Serializable]
    public class PlayerProgressData
    {
        public int maxStageCleared = 0;
        public int totalEDataEarned = 0;
        public int currentEData = 0;
        public Dictionary<int, int> stageStars = new Dictionary<int, int>();  // 스테이지별 획득 별 개수
    }

    [Serializable]
    public class GameSaveData
    {
        public PlayerDeckData deckData = new PlayerDeckData();
        public PlayerProgressData progressData = new PlayerProgressData();
        public Dictionary<string, float> settings = new Dictionary<string, float>();

        public void Initialize()
        {
            // 기본 설정값 초기화
            if (!settings.ContainsKey("BGMVolume"))
                settings["BGMVolume"] = 1f;
            if (!settings.ContainsKey("SFXVolume"))
                settings["SFXVolume"] = 1f;
        }
    }

    public class SaveDataManager
    {
        private static SaveDataManager instance;
        public static SaveDataManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SaveDataManager();
                    instance.LoadGameData();
                }
                return instance;
            }
        }

        public GameSaveData CurrentSaveData { get; private set; } = new GameSaveData();

        private const string SAVE_KEY = "InvaderInsiderSaveData";

        public void SaveGameData()
        {
            string jsonData = JsonUtility.ToJson(CurrentSaveData);
            PlayerPrefs.SetString(SAVE_KEY, jsonData);
            PlayerPrefs.Save();
            Debug.Log("Game data saved successfully");
        }

        public void LoadGameData()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string jsonData = PlayerPrefs.GetString(SAVE_KEY);
                CurrentSaveData = JsonUtility.FromJson<GameSaveData>(jsonData);
                CurrentSaveData.Initialize();
                Debug.Log("Game data loaded successfully");
            }
            else
            {
                CurrentSaveData = new GameSaveData();
                CurrentSaveData.Initialize();
                Debug.Log("No save data found, initialized new data");
            }
        }

        // 덱 관련 메서드
        public void AddCardToDeck(int cardId)
        {
            if (!CurrentSaveData.deckData.deckCardIds.Contains(cardId))
            {
                CurrentSaveData.deckData.deckCardIds.Add(cardId);
                SaveGameData();
            }
        }

        public void RemoveCardFromDeck(int cardId)
        {
            if (CurrentSaveData.deckData.deckCardIds.Remove(cardId))
            {
                SaveGameData();
            }
        }

        public void AddNewCard(int cardId)
        {
            if (!CurrentSaveData.deckData.ownedCardIds.Contains(cardId))
            {
                CurrentSaveData.deckData.ownedCardIds.Add(cardId);
                SaveGameData();
            }
        }

        // 진행 상황 관련 메서드
        public void UpdateStageProgress(int stageNum, int stars)
        {
            if (stageNum > CurrentSaveData.progressData.maxStageCleared)
            {
                CurrentSaveData.progressData.maxStageCleared = stageNum;
            }

            CurrentSaveData.progressData.stageStars[stageNum] = stars;
            SaveGameData();
        }

        public void UpdateEData(int amount)
        {
            CurrentSaveData.progressData.currentEData += amount;
            CurrentSaveData.progressData.totalEDataEarned += amount;
            SaveGameData();
        }

        public bool TrySpendEData(int amount)
        {
            if (CurrentSaveData.progressData.currentEData >= amount)
            {
                CurrentSaveData.progressData.currentEData -= amount;
                SaveGameData();
                return true;
            }
            return false;
        }

        // 설정 관련 메서드
        public void UpdateSetting(string key, float value)
        {
            CurrentSaveData.settings[key] = value;
            SaveGameData();
        }

        public float GetSetting(string key, float defaultValue = 1f)
        {
            return CurrentSaveData.settings.ContainsKey(key) ? 
                   CurrentSaveData.settings[key] : defaultValue;
        }
    }
} 