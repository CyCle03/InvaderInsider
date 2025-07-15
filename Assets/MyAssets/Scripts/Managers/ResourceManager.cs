using UnityEngine;
using System;
using InvaderInsider.Data;

namespace InvaderInsider.Managers
{
    public class ResourceManager : MonoBehaviour
    {
        private const string LOG_PREFIX = "[ResourceManager] ";
        
        private static ResourceManager instance;
        private static readonly object _lock = new object();
        private static bool isQuitting = false;

        public static ResourceManager Instance
        {
            get
            {
                if (isQuitting) return null;
                
                #if UNITY_EDITOR
                if (!UnityEngine.Application.isPlaying) return null;
                #endif

                lock (_lock)
                {
                    if (instance == null)
                    {
                        instance = FindObjectOfType<ResourceManager>();
                        if (instance == null && !isQuitting)
                        {
                            GameObject go = new GameObject("ResourceManager");
                            instance = go.AddComponent<ResourceManager>();
                            DontDestroyOnLoad(go);
                        }
                    }
                    return instance;
                }
            }
        }

        public event Action<int> OnEDataChanged;
        
        private SaveDataManager saveDataManager;

        private void Awake()
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying) return;
            #endif
            
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeManager();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void InitializeManager()
        {
            saveDataManager = SaveDataManager.Instance;
        }

        public bool TrySpendEData(int amount)
        {
            if (saveDataManager == null || amount < 0) return false;

            int currentEData = saveDataManager.GetCurrentEData();
            if (currentEData < amount) return false;

            saveDataManager.UpdateEData(-amount, false); // 즉시 저장 안함
            OnEDataChanged?.Invoke(saveDataManager.GetCurrentEData());
            return true;
        }

        public void AddEData(int amount)
        {
            AddEData(amount, true); // 기본적으로 저장
        }
        
        public void AddEData(int amount, bool saveImmediately)
        {
            if (saveDataManager == null || amount <= 0) return;

            saveDataManager.UpdateEData(amount, saveImmediately);
            OnEDataChanged?.Invoke(saveDataManager.GetCurrentEData());
        }

        public int GetCurrentEData()
        {
            return saveDataManager?.GetCurrentEData() ?? 0;
        }

        public void SetEData(int amount)
        {
            if (saveDataManager == null) return;

            int currentEData = saveDataManager.GetCurrentEData();
            int difference = amount - currentEData;
            
            if (difference != 0)
            {
                saveDataManager.UpdateEData(difference, true); // 즉시 저장
                OnEDataChanged?.Invoke(saveDataManager.GetCurrentEData());
            }
        }

        private void OnDestroy()
        {
            OnEDataChanged = null;
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }
    }
}