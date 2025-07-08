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

        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "TopBarPanel: UI 요소가 할당되지 않았습니다."
        };

        private int currentEData;
        private UIManager uiManager;

        private void Start()
        {
                    if (stageText == null || waveText == null || eDataText == null || pauseButton == null)
        {
            LogManager.Error(GameConstants.LOG_PREFIX_UI, LOG_MESSAGES[0]);
            return;
        }

            InitializeUI();
        }

        private void InitializeUI()
        {
            // 기본 텍스트 설정 (실제 값은 GameManager에서 업데이트됨)
            if (stageText != null)
            {
                stageText.text = "Stage 1/1"; // GameManager에서 실제 스테이지 수로 업데이트됨
            }
            
            if (waveText != null)
            {
                waveText.text = "Wave 0/0"; // 소환된 몬스터/최대 몬스터 (GameManager에서 업데이트됨)
            }
            
            // eData는 GameManager에서 직접 호출로 업데이트됨 (Stage/Wave UI와 동일한 방식)
            if (eDataText != null)
            {
                eDataText.text = "eData: 0"; // 기본값, GameManager에서 실제 값으로 업데이트
            }
            currentEData = 0;
        }

        protected override void Initialize()
        {
            if (!ValidateReferences())
            {
                return;
            }

            uiManager = UIManager.Instance;
            pauseButton.onClick.AddListener(HandlePauseClick);
            
            // Canvas Sorting Order 설정 (다른 UI보다 낮게)
            SetupCanvasSortingOrder();
            
            // eData는 이제 GameManager에서 직접 호출로 업데이트됨 (이벤트 구독 제거)
            
            // UpdateUI(); // 초기 UI 업데이트는 GameManager에서 직접 처리하므로 여기서는 제거
        }

        private bool ValidateReferences()
        {
            if (stageText == null || eDataText == null || 
                waveText == null || pauseButton == null)
            {
                LogManager.Error(GameConstants.LOG_PREFIX_UI, LOG_MESSAGES[0]);
                return false;
            }
            return true;
        }

        private void UpdateUI()
        {
            // GameManager에서 현재 게임 상태를 가져와서 UI 업데이트
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                // 현재 스테이지 정보 가져오기
                var stageManager = StageManager.Instance;
                if (stageManager != null)
                {
                    int currentStage = stageManager.GetCurrentStageIndex() + 1; // 0-based to 1-based
                    int totalStages = stageManager.GetStageCount();
                    int spawnedMonsters = stageManager.GetSpawnedEnemyCount();
                    int maxMonsters = stageManager.GetStageWaveCount(stageManager.GetCurrentStageIndex());
                    
                    UpdateStageInfo(currentStage, totalStages, spawnedMonsters, maxMonsters);
                }
                
                // 현재 EData 가져오기
                var resourceManager = ResourceManager.Instance;
                if (resourceManager != null)
                {
                    int currentEData = resourceManager.GetCurrentEData();
                    UpdateEData(currentEData);
                }
            }
            else
            {
                // GameManager가 없는 경우 기본값 설정
                if (stageText != null)
                {
                    stageText.text = "Stage 1/1";
                }
                
                if (waveText != null)
                {
                    waveText.text = "Wave 0/0";
                }
                
                if (eDataText != null)
                {
                    eDataText.text = "eData: 0";
                }
            }
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
                LogManager.Info(GameConstants.LOG_PREFIX_UI, $"[TopBar] EData 업데이트: {amount}");
            }
            else
            {
                LogManager.Error(GameConstants.LOG_PREFIX_UI, "UpdateEData 호출됨 - eDataText가 null!");
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

        public void UpdateStageInfo(int currentStage, int totalStages, int spawnedMonsters, int maxMonsters)
        {
            if (stageText != null)
            {
                stageText.text = $"Stage {currentStage}/{totalStages}";
                LogManager.Info(GameConstants.LOG_PREFIX_UI, $"[TopBar] Stage 업데이트: {currentStage}/{totalStages}");
            }
            else
            {
                LogManager.Error(GameConstants.LOG_PREFIX_UI, "UpdateStageInfo 호출됨 - stageText가 null!");
            }
            
            if (waveText != null)
            {
                waveText.text = $"Wave {spawnedMonsters}/{maxMonsters}";
                LogManager.Info(GameConstants.LOG_PREFIX_UI, $"[TopBar] Wave 업데이트: {spawnedMonsters}/{maxMonsters}");
            }
            else
            {
                LogManager.Error(GameConstants.LOG_PREFIX_UI, "UpdateStageInfo 호출됨 - waveText가 null!");
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

        private void OnDestroy()
        {
            // eData 이벤트 구독 해제 불필요 (직접 호출 방식으로 전환)
        }

        protected override void OnShow()
        {
            base.OnShow();
            
            // 패널이 표시될 때 UI 업데이트는 GameManager에서 직접 처리하므로 여기서는 제거
        }

        protected override void OnHide()
        {
            base.OnHide();
        }

        private void SetupCanvasSortingOrder()
        {
            // TopBar의 Canvas를 확인하고 없으면 추가
            Canvas topBarCanvas = GetComponent<Canvas>();
            if (topBarCanvas == null)
            {
                topBarCanvas = gameObject.AddComponent<Canvas>();
                
                // GraphicRaycaster도 필요
                if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                {
                    gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                }
            }
            
            // Sorting Order를 낮게 설정하여 다른 UI 아래에 표시
            topBarCanvas.overrideSorting = true;
            topBarCanvas.sortingOrder = 10; // 기본보다 낮은 값 (SummonChoice는 100)
            
            #if UNITY_EDITOR
            LogManager.Info(GameConstants.LOG_PREFIX_UI, $"[TopBar] TopBar Canvas Sorting Order 설정 완료: {topBarCanvas.sortingOrder}");
            #endif
        }
    }
} 