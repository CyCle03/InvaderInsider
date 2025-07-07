using UnityEngine;
using UnityEngine.UI;
using InvaderInsider.Managers;
using InvaderInsider.UI;
using InvaderInsider.Cards;
using InvaderInsider.Data;
using TMPro;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System;
using InvaderInsider.Managers;


namespace InvaderInsider.UI
{
    public class InGamePanel : BasePanel
    {
        private new const string LOG_PREFIX = "[InGame] ";
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

        [Header("Hand Display Controls")]
        [SerializeField] private Button showHandButton;

        [SerializeField] private TextMeshProUGUI handCardCountText; // 핸드 카드 수 표시

        private UIManager uiManager;
        private CardManager cardManager;
        private bool lastSummonPanelState = false; // 이전 프레임의 SummonChoice 패널 상태
        private float lastSummonClickTime = 0f;
        private const float SUMMON_CLICK_COOLDOWN = 1.0f; // 1초 쿨다운
        private bool isSummonButtonProcessing = false; // 소환 버튼 처리 중 플래그
        private bool isInitialized = false; // 초기화 중복 방지 플래그
        private bool buttonsSetup = false; // 버튼 이벤트 등록 완료 플래그

        protected override void Awake()
        {
            base.Awake();
            
            uiManager = UIManager.Instance;
            
            // BasePanel.Awake()에서 이미 Initialize()가 호출되므로 중복 호출 제거
        }

        protected override void Initialize()
        {
            // 중복 초기화 방지
            if (isInitialized)
            {
                LogManager.Info("InGamePanel", "InGame 패널이 이미 초기화되었습니다. 중복 초기화를 방지합니다.");
                return;
            }

            uiManager = UIManager.Instance;
            cardManager = CardManager.Instance;

            // 버튼 이벤트를 한 번만 등록
            if (!buttonsSetup)
            {
                SetupButtons();
                buttonsSetup = true;
            }

            // SaveDataManager 이벤트 중복 구독 방지
            if (SaveDataManager.Instance != null)
            {
                SaveDataManager.Instance.OnHandDataChanged -= UpdateHandCardCount; // 기존 구독 해제
                SaveDataManager.Instance.OnHandDataChanged += UpdateHandCardCount; // 새로 구독
            }

            UpdateHandCardCount();
            
            isInitialized = true;

            LogManager.Info("InGamePanel", "InGame 패널 초기화 완료");
        }

        private void Update()
        {
            if (!isInitialized) return;
            
            // SummonChoice 패널 상태 변화 감지 (UIManager를 통해)
            if (uiManager != null && uiManager.IsPanelRegistered("SummonChoice"))
            {
                bool currentSummonPanelState = uiManager.IsPanelActive("SummonChoice");
                if (currentSummonPanelState != lastSummonPanelState)
                {
                    UpdateSummonPanelButtons();
                    lastSummonPanelState = currentSummonPanelState;
                }
            }
        }

        private void UpdateSummonPanelButtons()
        {
            if (uiManager != null && uiManager.IsPanelRegistered("SummonChoice"))
            {
                bool isSummonPanelActive = uiManager.IsPanelActive("SummonChoice");
                
                if (isSummonPanelActive)
                {
                    // SummonChoice 패널이 활성화된 경우
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
            else
            {
                // SummonChoice 패널이 등록되지 않은 경우 버튼들 숨김
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

        private void OnPauseButtonClicked()
        {
            LogManager.Info(LOG_PREFIX, LOG_MESSAGES[2]);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PauseGame();
            }
            else
            {
                #if UNITY_EDITOR
                LogManager.Error(LOG_PREFIX, "GameManager를 찾을 수 없습니다");
                #endif
            }
        }

        private void OnResumeButtonClicked()
        {
            LogManager.Info("[InGame] Resume 버튼이 클릭되었습니다.");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResumeGame();
            }
            else
            {
                #if UNITY_EDITOR
                LogManager.Error(LOG_PREFIX, "GameManager를 찾을 수 없습니다");
                #endif
            }
        }

        private void OnSummonButtonClicked()
        {
            // 이미 처리 중인 경우 즉시 반환
            if (isSummonButtonProcessing)
            {
                LogManager.Info($"{LOG_PREFIX}소환 버튼이 이미 처리 중입니다. 무시됩니다.");
                return;
            }
            
            // 쿨다운 체크
            float currentTime = Time.unscaledTime;
            if (currentTime - lastSummonClickTime < SUMMON_CLICK_COOLDOWN)
            {
                LogManager.Info($"{LOG_PREFIX}소환 버튼 쿨다운 중입니다. 남은 시간: {SUMMON_CLICK_COOLDOWN - (currentTime - lastSummonClickTime):F1}초");
                return;
            }
            
            // 처리 시작
            isSummonButtonProcessing = true;
            lastSummonClickTime = currentTime;
            
            // 버튼 일시적 비활성화 (UI 레벨 보호)
            if (summonButton != null)
            {
                summonButton.interactable = false;
            }
            
            LogManager.Info(LOG_PREFIX, LOG_MESSAGES[3]);
            
            try
            {
                if (CardManager.Instance != null)
                {
                    CardManager.Instance.Summon();
                }
                else
                {
                    #if UNITY_EDITOR
                    LogManager.Error(LOG_PREFIX, LOG_MESSAGES[4]);
                    #endif
                }
            }
            finally
            {
                // 처리 완료 (일정 시간 후 버튼 다시 활성화)
                ResetSummonButtonAfterDelay().Forget();
            }
        }
        
        private async UniTask ResetSummonButtonAfterDelay()
        {
            // 0.5초 후 버튼 다시 활성화 (쿨다운보다 짧게)
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), ignoreTimeScale: true);
            
            isSummonButtonProcessing = false;
            
            if (summonButton != null)
            {
                summonButton.interactable = true;
            }
            
            LogManager.Info($"{LOG_PREFIX}소환 버튼이 다시 활성화되었습니다.");
        }

        private void OnHideSummonPanelButtonClicked()
        {
            LogManager.Info(LOG_PREFIX, LOG_MESSAGES[7]);
            
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
                    else
                {
                    LogManager.Warning(LOG_PREFIX, LOG_MESSAGES[9]);
                    #endif
                }
            }
        }

        private void OnShowSummonPanelButtonClicked()
        {
            LogManager.Info(LOG_PREFIX, LOG_MESSAGES[8]);
            
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
                    else
                {
                    LogManager.Warning(LOG_PREFIX, LOG_MESSAGES[10]);
                    #endif
                }
            }
        }

        private void OnShowHandButtonClicked()
        {
            LogManager.Info("[InGame] 핸드 보기 버튼이 클릭되었습니다.");

            if (uiManager != null && uiManager.IsPanelRegistered("HandDisplay"))
            {
                uiManager.ShowPanel("HandDisplay");
            }
            else
            {
                LogManager.Warning("[InGame] HandDisplay 패널이 UIManager에 등록되지 않았습니다.");
            }

            // 현재 핸드 카드 목록 로깅
            if (cardManager != null)
            {
                var handCards = cardManager.GetHandCards();
                #if UNITY_EDITOR
                LogManager.Info("[InGame] 현재 핸드에 있는 카드 수: {handCards.Count}");
                foreach (var card in handCards)
                {
                    LogManager.Info("[InGame] 핸드 카드: {card.cardName} (ID: {card.cardId})");
                }
                #endif
            }
        }

        private void OnClearHandButtonClicked()
        {
            LogManager.Info("[InGame] 핸드 초기화 버튼이 클릭되었습니다.");

            if (cardManager != null && SaveDataManager.HasInstance && SaveDataManager.Instance != null)
            {
                var handCardIds = cardManager.GetHandCardIds();
                foreach (int cardId in handCardIds)
                {
                    SaveDataManager.Instance.RemoveCardFromHand(cardId);
                }
                
                LogManager.Info("[InGame] {handCardIds.Count}개의 카드가 핸드에서 제거되었습니다.");
            }
        }

        private void UpdateHandCardCount(List<int> handCardIds = null)
        {
            if (handCardCountText != null && cardManager != null)
            {
                int handCount = cardManager.GetHandCardCount();
                handCardCountText.text = $"핸드: {handCount}";
                
                LogManager.Info("[InGame] 핸드 카드 수 업데이트: {handCount}");
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

        private void SetupButtons()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(OnPauseButtonClicked);
            }
            else
            {
                LogManager.Warning(LOG_PREFIX, LOG_MESSAGES[0]);
            }

            if (summonButton != null)
            {
                summonButton.onClick.AddListener(OnSummonButtonClicked);
            }
            else
            {
                #if UNITY_EDITOR
                else
            {
                LogManager.Warning(LOG_PREFIX, LOG_MESSAGES[1]);
            }
                #endif
            }

            if (hideSummonPanelButton != null)
            {
                hideSummonPanelButton.onClick.AddListener(OnHideSummonPanelButtonClicked);
            }

            if (showSummonPanelButton != null)
            {
                showSummonPanelButton.onClick.AddListener(OnShowSummonPanelButtonClicked);
            }

            if (showHandButton != null)
            {
                showHandButton.onClick.AddListener(OnShowHandButtonClicked);
            }

            // clearHandButton은 선언되지 않았으므로 주석 처리
            // if (clearHandButton != null)
            // {
            //     clearHandButton.onClick.AddListener(OnClearHandButtonClicked);
            // }
        }
        
        private void CleanupButtonEvents()
        {
            if (buttonsSetup)
            {
                if (pauseButton != null)
                    pauseButton.onClick.RemoveListener(OnPauseButtonClicked);
                if (summonButton != null)
                    summonButton.onClick.RemoveListener(OnSummonButtonClicked);
                if (hideSummonPanelButton != null)
                    hideSummonPanelButton.onClick.RemoveListener(OnHideSummonPanelButtonClicked);
                if (showSummonPanelButton != null)
                    showSummonPanelButton.onClick.RemoveListener(OnShowSummonPanelButtonClicked);
                if (showHandButton != null)
                    showHandButton.onClick.RemoveListener(OnShowHandButtonClicked);
                // if (clearHandButton != null)
                //     clearHandButton.onClick.RemoveListener(OnClearHandButtonClicked);
                
                buttonsSetup = false;
            }
        }

        private void OnDestroy()
        {
            // 버튼 이벤트 정리
            CleanupButtonEvents();
            
            // SaveDataManager 이벤트 구독 해제 (인스턴스가 존재할 때만)
            if (SaveDataManager.HasInstance && SaveDataManager.Instance != null)
            {
                SaveDataManager.Instance.OnHandDataChanged -= UpdateHandCardCount;
            }
        }
    }
} 