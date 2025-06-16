using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InvaderInsider.Managers;
using InvaderInsider.Data;

namespace InvaderInsider.UI
{
    public class TopBarPanel : BasePanel
    {
        [Header("Top Bar References")]
        [SerializeField] private TextMeshProUGUI stageText;
        [SerializeField] private TextMeshProUGUI eDataText;
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI lifeText;
        [SerializeField] private Button pauseButton;

        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "TopBarPanel: UI 요소가 할당되지 않았습니다."
        };

        private int currentEData;
        private UIManager uiManager;

        private void Start()
        {
            #if UNITY_EDITOR
            Debug.Log($"[TopBarPanel] Start - stageText: {(stageText != null ? "할당됨" : "없음")}, " +
                     $"waveText: {(waveText != null ? "할당됨" : "없음")}, " +
                     $"lifeText: {(lifeText != null ? "할당됨" : "없음")}, " +
                     $"eDataText: {(eDataText != null ? "할당됨" : "없음")}, " +
                     $"pauseButton: {(pauseButton != null ? "할당됨" : "없음")}");
            #endif
            
            if (stageText == null || waveText == null || lifeText == null || eDataText == null || pauseButton == null)
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_MESSAGES[0]);
                #endif
                return;
            }

            InitializeUI();
        }

        private void InitializeUI()
        {
            // 기본 텍스트 설정
            if (stageText != null)
            {
                stageText.text = "Stage 1/10";
            }
            
            if (waveText != null)
            {
                waveText.text = "Wave 0/20"; // 소환된 몬스터/최대 몬스터
            }
            
            if (lifeText != null)
            {
                lifeText.text = "HP 100/100";
            }
            
            if (eDataText != null)
            {
                eDataText.text = "0";
            }
        }

        protected override void Initialize()
        {
            if (!ValidateReferences())
            {
                return;
            }

            uiManager = UIManager.Instance;
            pauseButton.onClick.AddListener(HandlePauseClick);
            UpdateUI();
        }

        private bool ValidateReferences()
        {
            if (stageText == null || eDataText == null || 
                waveText == null || lifeText == null || pauseButton == null)
            {
                Debug.LogError(LOG_MESSAGES[0]);
                return false;
            }
            return true;
        }

        private void UpdateUI()
        {
            // SaveDataManager에서 실제 eData 값 가져오기
            var saveDataManager = SaveDataManager.Instance;
            currentEData = saveDataManager != null ? saveDataManager.GetCurrentEData() : 0;
            UpdateEData(currentEData);
            
            // 초기 스테이지 정보 표시 (기본값)
            if (stageText != null)
            {
                stageText.text = "Stage 1/1";
            }
            
            if (waveText != null)
            {
                waveText.text = "Wave 0/0";  // 몬스터 소환 수/최대 몬스터 수
            }
            
            Debug.Log("[TopBarPanel] UI 초기화 완료 - Stage와 Wave 기본값 설정");
        }

        private void HandlePauseClick()
        {
            if (uiManager != null)
            {
                uiManager.ShowPanel("Pause");
            }
        }

        public void UpdateEData(int amount)
        {
            currentEData = amount;
            if (eDataText != null)
            {
                eDataText.text = $"eData: {amount}";
            }
        }

        public void AddEData(int amount)
        {
            currentEData += amount;
            if (eDataText != null)
            {
                eDataText.text = $"eData: {currentEData}";
            }
        }

        public void UpdateStageInfo(int stage, int wave)
        {
            if (stageText != null)
            {
                stageText.text = $"Stage {stage}";
            }
            
            if (waveText != null)
            {
                waveText.text = $"Wave {wave}";
            }
        }

        public void UpdateStageInfo(int currentStage, int totalStages, int spawnedMonsters, int maxMonsters)
        {
            if (stageText != null)
            {
                stageText.text = $"Stage {currentStage}/{totalStages}";
            }
            
            if (waveText != null)
            {
                waveText.text = $"Wave {spawnedMonsters}/{maxMonsters}";
            }
        }

        public void UpdateHealth(float currentHealth, float maxHealth)
        {
            if (lifeText != null)
            {
                lifeText.text = $"HP: {currentHealth:F0}/{maxHealth:F0}";
            }
        }

        public int GetCurrentEData()
        {
            return currentEData;
        }

        public bool CanAfford(int cost)
        {
            return currentEData >= cost;
        }

        public bool SpendEData(int cost)
        {
            if (CanAfford(cost))
            {
                UpdateEData(currentEData - cost);
                return true;
            }
            return false;
        }

        protected override void OnShow()
        {
            base.OnShow();
            
            var saveDataManager = SaveDataManager.Instance;
            if (saveDataManager != null)
            {
                UpdateEData(saveDataManager.GetCurrentEData());
            }
        }

        protected override void OnHide()
        {
            base.OnHide();
        }
    }
} 