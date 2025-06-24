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
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "ResourceManager 인스턴스 생성됨");
                #endif
                InitializeManager();
            }
            else if (instance != this)
            {
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "중복 ResourceManager 인스턴스 파괴됨");
                #endif
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

            saveDataManager.UpdateEData(-amount);
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

            if (saveImmediately)
            {
                saveDataManager.UpdateEData(amount);
            }
            else
            {
                // 저장하지 않고 EData만 업데이트 (적 처치 시 사용)
                saveDataManager.UpdateEDataWithoutSave(amount);
            }
            
            OnEDataChanged?.Invoke(saveDataManager.GetCurrentEData());
        }

        public int GetCurrentEData()
        {
            return saveDataManager?.GetCurrentEData() ?? 0;
        }

        /// <summary>
        /// EData를 특정 값으로 설정 (SaveDataManager에 이 기능이 없으므로 직접 계산)
        /// </summary>
        public void SetEData(int amount)
        {
            if (saveDataManager == null) return;

            int currentEData = saveDataManager.GetCurrentEData();
            int difference = amount - currentEData;
            
            if (difference != 0)
            {
                saveDataManager.UpdateEData(difference);
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