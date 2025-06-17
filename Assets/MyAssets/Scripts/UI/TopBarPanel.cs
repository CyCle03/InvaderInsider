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
                #if UNITY_EDITOR
                Debug.LogError(LOG_MESSAGES[0]);
                #endif
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
            
            // eData는 이제 GameManager에서 직접 호출로 업데이트됨 (이벤트 구독 제거)
            
            UpdateUI();
        }

        private bool ValidateReferences()
        {
            if (stageText == null || eDataText == null || 
                waveText == null || pauseButton == null)
            {
                Debug.LogError(LOG_MESSAGES[0]);
                return false;
            }
            return true;
        }

        private void UpdateUI()
        {
            // eData는 GameManager에서 직접 호출로 업데이트됨
            
            // 초기 스테이지 정보 표시 (기본값)
            if (stageText != null)
            {
                stageText.text = "Stage 1/1";
            }
            
            if (waveText != null)
            {
                waveText.text = "Wave 0/0";  // 몬스터 소환 수/최대 몬스터 수
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
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogError("[TopBarPanel] UpdateEData 호출됨 - eDataText가 null!");
                #endif
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
            
            // eData는 GameManager에서 직접 호출로 업데이트됨 (이벤트 재구독 제거)
        }

        protected override void OnHide()
        {
            base.OnHide();
        }
    }
} 