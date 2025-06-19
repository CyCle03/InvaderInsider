using UnityEngine;
using UnityEngine.UI;
using InvaderInsider.Managers;
using InvaderInsider.UI;
using InvaderInsider.Cards;

namespace InvaderInsider.UI
{
    public class InGamePanel : BasePanel
    {
        private const string LOG_PREFIX = "[UI] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "InGame: Pause button missing",
            "InGame: Summon button missing",
            "InGame: Pause clicked",
            "InGame: Summon clicked",
            "InGame: CardManager not found",
            "InGame: Panel shown",
            "InGame: Panel hidden",
            "InGame: Hide Summon Panel clicked",
            "InGame: Show Summon Panel clicked",
            "InGame: No active SummonChoice panel to hide",
            "InGame: No active SummonChoice panel to show"
        };

        [Header("Buttons")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button summonButton;
        [SerializeField] private Button hideSummonPanelButton;
        [SerializeField] private Button showSummonPanelButton;

        private UIManager uiManager;

        protected override void Awake()
        {
            base.Awake();
            
            uiManager = UIManager.Instance;
            
            Initialize();
        }

        protected override void Initialize()
        {
            if (pauseButton == null)
            {
                #if UNITY_EDITOR
                Debug.LogWarning(LOG_PREFIX + LOG_MESSAGES[0]);
                #endif
            }
            else
            {
                pauseButton.onClick.AddListener(OnPauseButtonClicked);
            }

            if (summonButton != null)
            {
                summonButton.onClick.AddListener(OnSummonButtonClicked);
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogWarning(LOG_PREFIX + LOG_MESSAGES[1]);
                #endif
            }

            // Hide/Show 버튼 초기화
            if (hideSummonPanelButton != null)
            {
                hideSummonPanelButton.onClick.AddListener(OnHideSummonPanelClicked);
                hideSummonPanelButton.gameObject.SetActive(false); // 처음에는 숨김
            }

            if (showSummonPanelButton != null)
            {
                showSummonPanelButton.onClick.AddListener(OnShowSummonPanelClicked);
                showSummonPanelButton.gameObject.SetActive(false); // 처음에는 숨김
            }

            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + "InGame 패널 초기화 완료");
            #endif
        }

        private void Update()
        {
            // SummonChoice 패널 상태에 따라 Hide/Show 버튼 활성화 상태 업데이트
            UpdateSummonPanelButtons();
        }

        private void UpdateSummonPanelButtons()
        {
            if (CardManager.Instance != null)
            {
                bool isSummonPanelActive = CardManager.Instance.IsSummonChoicePanelActive();
                
                if (isSummonPanelActive)
                {
                    // SummonChoice 패널이 활성화된 경우
                    if (uiManager != null && uiManager.IsPanelRegistered("SummonChoice"))
                    {
                        var summonChoicePanel = uiManager.GetPanel("SummonChoice") as SummonChoicePanel;
                        if (summonChoicePanel != null)
                        {
                            bool isTemporarilyHidden = summonChoicePanel.IsPanelTemporarilyHidden();
                            
                            if (hideSummonPanelButton != null)
                            {
                                hideSummonPanelButton.gameObject.SetActive(!isTemporarilyHidden);
                            }
                            
                            if (showSummonPanelButton != null)
                            {
                                showSummonPanelButton.gameObject.SetActive(isTemporarilyHidden);
                            }
                        }
                    }
                }
                else
                {
                    // SummonChoice 패널이 비활성화된 경우 버튼들 숨김
                    if (hideSummonPanelButton != null)
                    {
                        hideSummonPanelButton.gameObject.SetActive(false);
                    }
                    
                    if (showSummonPanelButton != null)
                    {
                        showSummonPanelButton.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void OnPauseButtonClicked()
        {
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[2]);
            #endif
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PauseGame();
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + "GameManager를 찾을 수 없습니다");
                #endif
            }
        }

        private void OnSummonButtonClicked()
        {
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[3]);
            #endif
            if (CardManager.Instance != null)
            {
                CardManager.Instance.Summon();
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[4]);
                #endif
            }
        }

        private void OnHideSummonPanelClicked()
        {
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[7]);
            #endif
            
            if (uiManager != null && uiManager.IsPanelRegistered("SummonChoice"))
            {
                var summonChoicePanel = uiManager.GetPanel("SummonChoice") as SummonChoicePanel;
                if (summonChoicePanel != null && !summonChoicePanel.IsPanelTemporarilyHidden())
                {
                    summonChoicePanel.TemporarilyHidePanel();
                }
                else
                {
                    #if UNITY_EDITOR
                    Debug.LogWarning(LOG_PREFIX + LOG_MESSAGES[9]);
                    #endif
                }
            }
        }

        private void OnShowSummonPanelClicked()
        {
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[8]);
            #endif
            
            if (uiManager != null && uiManager.IsPanelRegistered("SummonChoice"))
            {
                var summonChoicePanel = uiManager.GetPanel("SummonChoice") as SummonChoicePanel;
                if (summonChoicePanel != null && summonChoicePanel.IsPanelTemporarilyHidden())
                {
                    summonChoicePanel.ShowPanelAgain();
                }
                else
                {
                    #if UNITY_EDITOR
                    Debug.LogWarning(LOG_PREFIX + LOG_MESSAGES[10]);
                    #endif
                }
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
        }

        protected override void OnHide()
        {
            base.OnHide();
        }

        private void OnDestroy()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveAllListeners();
            }
            
            if (summonButton != null)
            {
                summonButton.onClick.RemoveAllListeners();
            }
            
            if (hideSummonPanelButton != null)
            {
                hideSummonPanelButton.onClick.RemoveAllListeners();
            }
            
            if (showSummonPanelButton != null)
            {
                showSummonPanelButton.onClick.RemoveAllListeners();
            }
        }
    }
} 