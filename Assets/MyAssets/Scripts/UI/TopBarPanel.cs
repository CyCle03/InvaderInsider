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

        protected override void OnShow()
        {
            base.OnShow();
            UpdateUI();
        }

        private void UpdateUI()
        {
            // SaveDataManager에서 EData 가져오기
            int currentEData = SaveDataManager.Instance?.CurrentSaveData?.progressData.currentEData ?? 0;
            UpdateEData(currentEData);

            // StageManager에서 스테이지 및 웨이브 정보 가져오기
            // StageManager가 게임 씬에만 존재하므로 null 체크 필요
            StageManager stageManager = StageManager.Instance;
            if (stageManager != null)
            {
                UpdateStageWaveUI(stageManager.GetCurrentStageIndex() + 1, stageManager.GetSpawnedEnemyCount(), stageManager.GetStageWaveCount(stageManager.GetCurrentStageIndex()));
            }
            else
            {
                // StageManager가 없을 경우 (예: 메인 메뉴 씬) 기본값 표시
                UpdateStageWaveUI(1, 0, 0);
            }
        }
    }
}