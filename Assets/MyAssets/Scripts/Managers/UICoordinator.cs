using UnityEngine;
using InvaderInsider.UI;
using InvaderInsider.Managers;
using System.Linq;
using UnityEngine.SceneManagement;
using InvaderInsider.Core;
using System.Collections.Generic;

    /// <summary>
    /// UI 시스템 조정자 - GameManager에서 UI 관련 기능을 분리
    /// UI 패널 간의 조정과 상태 관리를 담당합니다.
    /// </summary>
    public class UICoordinator : SingletonManager<UICoordinator>
    {
        #region Constants
        
        private const string LOG_PREFIX = "[UICoordinator] ";
        
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "UI 패널 캐싱 시작",
            "UI 패널 캐싱 완료 - {0}개 패널 등록",
            "EData UI 업데이트: {0}",
            "스테이지 UI 업데이트: Stage {0}, Enemies {1}/{2}",
            "게임플레이 패널 설정 완료",
            "비게임플레이 패널 숨김 완료"
        };
        
        #endregion
        
        #region Inspector Fields
        
        [Header("UI Panel References")]
        [SerializeField] private BottomBarPanel bottomBarPanel;
        [SerializeField] private TopBarPanel topBarPanel;
        [SerializeField] private InGamePanel inGamePanel;
        [SerializeField] private PausePanel pausePanel;
        
        #endregion
        
        #region Runtime State
        
        private UIManager uiManager;
        private readonly Dictionary<System.Type, BasePanel> panelsByType = new Dictionary<System.Type, BasePanel>();
        private bool isInitialized = false;
        
        #endregion
        
        #region Properties
        
        public BottomBarPanel BottomBarPanel => bottomBarPanel;
        public TopBarPanel TopBarPanel => topBarPanel;
        public InGamePanel InGamePanel => inGamePanel;
        public PausePanel PausePanel => pausePanel;
        
        #endregion
        
        #region Initialization
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            uiManager = UIManager.Instance;
            if (uiManager == null)
            {
                LogManager.Error(LOG_PREFIX, "UIManager를 찾을 수 없습니다.");
                return;
            }
            
            CacheAndRegisterAllPanels();
            isInitialized = true;
            
            LogManager.LogInitialization("UICoordinator", true, "UI 조정자 초기화 완료");
        }
        
        #endregion
        
        #region UI Panel Management
        
        /// <summary>
        /// 모든 UI 패널을 캐시하고 등록합니다.
        /// </summary>
        private void CacheAndRegisterAllPanels()
        {
            if (!isInitialized)
            {
                LogManager.Info(LOG_PREFIX, LOG_MESSAGES[0]);
            }
            
            // 주요 패널 참조 자동 설정
            EnsurePanelReferences();
            
            int registeredCount = 0;
            
            // 각 패널 타입별로 등록
            registeredCount += RegisterPanelByType<BottomBarPanel>("BottomBar");
            registeredCount += RegisterPanelByType<TopBarPanel>("TopBar");
            registeredCount += RegisterPanelByType<InGamePanel>("InGame");
            registeredCount += RegisterPanelByType<PausePanel>("Pause");
            registeredCount += RegisterPanelByType<MainMenuPanel>("MainMenu");
            registeredCount += RegisterPanelByType<SettingsPanel>("Settings");
            registeredCount += RegisterPanelByType<SummonChoicePanel>("SummonChoice");
            registeredCount += RegisterPanelByType<HandDisplayPanel>("HandDisplay");
            
            LogManager.Info(LOG_PREFIX, $"UI 패널 캐싱 완료 - {registeredCount}개 패널 등록");
        }
        
        /// <summary>
        /// 주요 패널 참조를 자동으로 설정합니다.
        /// </summary>
        private void EnsurePanelReferences()
        {
            // TopBarPanel 참조 설정
            if (topBarPanel == null)
            {
                topBarPanel = FindObjectOfType<TopBarPanel>(true);
                if (topBarPanel != null)
                {
                    #if UNITY_EDITOR
                    LogManager.Info(LOG_PREFIX, "TopBarPanel 참조를 자동으로 찾았습니다.");
                    #endif
                }
            }
            
            // BottomBarPanel 참조 설정
            if (bottomBarPanel == null)
            {
                bottomBarPanel = FindObjectOfType<BottomBarPanel>(true);
                if (bottomBarPanel != null)
                {
                    #if UNITY_EDITOR
                    LogManager.Info(LOG_PREFIX, "BottomBarPanel 참조를 자동으로 찾았습니다.");
                    #endif
                }
            }
            
            // InGamePanel 참조 설정
            if (inGamePanel == null)
            {
                inGamePanel = FindObjectOfType<InGamePanel>(true);
                if (inGamePanel != null)
                {
                    #if UNITY_EDITOR
                    LogManager.Info(LOG_PREFIX, "InGamePanel 참조를 자동으로 찾았습니다.");
                    #endif
                }
            }
        }
        
        /// <summary>
        /// 특정 타입의 패널을 등록합니다.
        /// </summary>
        private int RegisterPanelByType<T>(string panelName) where T : BasePanel
        {
            T panel = FindCachedOrNewPanel<T>(panelName);
            if (panel != null)
            {
                panelsByType[typeof(T)] = panel;
                if (uiManager != null)
                {
                    uiManager.RegisterPanel(panelName, panel);
                }
                return 1;
            }
            return 0;
        }
        
        /// <summary>
        /// 캐시된 패널을 찾거나 UIManager에서 찾습니다.
        /// </summary>
        private T FindCachedOrNewPanel<T>(params string[] possibleNames) where T : BasePanel
        {
            // 1. 직접 할당된 참조 확인
            if (typeof(T) == typeof(BottomBarPanel) && bottomBarPanel != null) return bottomBarPanel as T;
            if (typeof(T) == typeof(TopBarPanel) && topBarPanel != null) return topBarPanel as T;
            if (typeof(T) == typeof(InGamePanel) && inGamePanel != null) return inGamePanel as T;
            if (typeof(T) == typeof(PausePanel) && pausePanel != null) return pausePanel as T;
            
            // 2. UIManager에서 이미 등록된 패널 확인
            if (uiManager != null && possibleNames.Length > 0)
            {
                T existingPanel = uiManager.GetPanel(possibleNames[0]) as T;
                if (existingPanel != null)
                {
                    return existingPanel;
                }
            }
            
            // 3. FindObjectOfType으로 찾기 (최후의 수단)
            T panel = FindObjectOfType<T>(true);
            if (panel != null)
            {
                ActivatePanelHierarchy(panel.transform);
                return panel;
            }
            
            LogManager.Warning(LOG_PREFIX, string.Format(GameConstants.LogMessages.COMPONENT_NOT_FOUND, typeof(T).Name));
            return null;
        }
        
        /// <summary>
        /// 패널 계층구조를 활성화합니다.
        /// </summary>
        private void ActivatePanelHierarchy(Transform panel)
        {
            Transform current = panel;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    current.gameObject.SetActive(true);
                }
                current = current.parent;
            }
        }
        
        #endregion
        
        #region UI Updates
        
        /// <summary>
        /// EData UI를 업데이트합니다.
        /// </summary>
        public void UpdateEDataUI(int currentEData)
        {
            // TopBarPanel 참조 재확인
            if (topBarPanel == null)
            {
                topBarPanel = FindObjectOfType<TopBarPanel>(true);
                if (topBarPanel == null)
                {
                    #if UNITY_EDITOR
                    LogManager.Warning(LOG_PREFIX, "TopBarPanel을 찾을 수 없습니다. EData UI 업데이트를 건너뜁니다.");
                    #endif
                    return;
                }
            }
            
            topBarPanel.UpdateEData(currentEData);
        }
        
        /// <summary>
        /// 스테이지 관련 UI를 업데이트합니다.
        /// </summary>
        public void UpdateStageWaveUI(int currentStage, int spawnedMonsters, int maxMonsters, int totalStages)
        {
            if (topBarPanel != null)
            {
                topBarPanel.UpdateStageInfo(currentStage, spawnedMonsters, maxMonsters, totalStages);
            }
        }

        public void UpdateActiveEnemyCountUI(int count)
        {
            if (bottomBarPanel != null)
            {
                bottomBarPanel.UpdateMonsterCountDisplay(count);
            }
        }
        
        /// <summary>
        /// 게임플레이 패널들을 설정합니다.
        /// </summary>
        public void SetupGameplayPanels()
        {
            HideNonGameplayPanels();
            
            // 게임플레이 관련 패널들 표시
            if (bottomBarPanel != null) bottomBarPanel.gameObject.SetActive(true);
            if (topBarPanel != null) topBarPanel.gameObject.SetActive(true);
            if (inGamePanel != null) inGamePanel.gameObject.SetActive(true);
            
            LogManager.Info(LOG_PREFIX, LOG_MESSAGES[4]);
        }
        
        /// <summary>
        /// 비게임플레이 패널들을 숨깁니다.
        /// </summary>
        private void HideNonGameplayPanels()
        {
            var nonGameplayPanels = new System.Type[]
            {
                typeof(MainMenuPanel),
                typeof(SettingsPanel),
                typeof(PausePanel)
            };
            
            foreach (var panelType in nonGameplayPanels)
            {
                if (panelsByType.TryGetValue(panelType, out BasePanel panel) && panel != null)
                {
                    panel.gameObject.SetActive(false);
                }
            }
            
            LogManager.Info(LOG_PREFIX, LOG_MESSAGES[5]);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 특정 타입의 패널을 가져옵니다.
        /// </summary>
        public T GetPanel<T>() where T : BasePanel
        {
            if (panelsByType.TryGetValue(typeof(T), out BasePanel panel))
            {
                return panel as T;
            }
            return null;
        }
        
        /// <summary>
        /// UI 시스템 상태를 리셋합니다.
        /// </summary>
        public void ResetUIState()
        {
            HideNonGameplayPanels();
            
            if (uiManager != null)
            {
                uiManager.HideCurrentPanel();
            }
        }
        
        #endregion
        
        #region Cleanup
        
        protected override void OnCleanup()
        {
            panelsByType.Clear();
            base.OnCleanup();
        }
        
        #endregion
    } 