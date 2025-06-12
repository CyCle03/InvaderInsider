using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InvaderInsider.Managers;

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
            // 초기값 설정
            currentEData = 100;
            UpdateEData(currentEData);
            UpdateStageInfo(1, 1);
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
                eDataText.text = amount.ToString();
            }
        }

        public void AddEData(int amount)
        {
            currentEData += amount;
            if (eDataText != null)
            {
                eDataText.text = currentEData.ToString();
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
    }
} 