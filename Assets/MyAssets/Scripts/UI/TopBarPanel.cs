using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InvaderInsider.Managers;
using InvaderInsider.Data;
using InvaderInsider.Core;

namespace InvaderInsider.UI
{
    public class TopBarPanel : BasePanel
    {
        [Header("Top Bar References")]
        [SerializeField] private TextMeshProUGUI stageText;
        [SerializeField] private TextMeshProUGUI eDataText;
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private Button pauseButton;

        private UIManager uiManager;

        protected override void Initialize()
        {
            base.Initialize();
            uiManager = UIManager.Instance;
            pauseButton?.onClick.AddListener(HandlePauseClick);
            UpdateUI();
        }

        private void HandlePauseClick()
        {
            GameManager.Instance?.PauseGame(true);
        }

        public void UpdateStageWaveUI(int currentStage, int spawnedMonsters, int maxMonsters)
        {
            if (stageText != null)
            {
                stageText.text = $"Stage {currentStage}";
            }
            if (waveText != null)
            {
                waveText.text = $"Wave {spawnedMonsters}/{maxMonsters}";
            }
        }

        public void UpdateEData(int amount)
        {
            if (eDataText != null)
            {
                eDataText.text = $"eData: {amount}";
            }
        }

        private void UpdateUI()
        {
            // 초기 UI 업데이트 (GameManager에서 호출될 때까지 기본값 표시)
            UpdateStageWaveUI(1, 0, 0); // 예시: Stage 1, Wave 0/0
            UpdateEData(0); // 예시: eData: 0
        }
    }
}