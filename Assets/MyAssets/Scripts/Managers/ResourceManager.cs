using UnityEngine;
using System;
using InvaderInsider.Data;
using InvaderInsider.UI;
using InvaderInsider.Core;

namespace InvaderInsider.Managers
{
    public class ResourceManager : InvaderInsider.Managers.SingletonManager<ResourceManager>
    {
        private const string LOG_PREFIX = "[ResourceManager] ";
        
        

        
        
        public event Action<int> OnEDataChanged;
        private SaveDataManager saveDataManager;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
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

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
        }
    }
} 