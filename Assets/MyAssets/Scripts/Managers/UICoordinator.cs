using UnityEngine;
using InvaderInsider.UI;
using InvaderInsider.Core;
using System.Collections.Generic;

namespace InvaderInsider.Managers
{
    public class UICoordinator : SingletonManager<UICoordinator>
    {
        private const string LOG_PREFIX = "[UICoordinator] ";

        [Header("UI Panel References")]
        [SerializeField] private BottomBarPanel bottomBarPanel;
        [SerializeField] private TopBarPanel topBarPanel;
        [SerializeField] private InGamePanel inGamePanel;
        [SerializeField] private PausePanel pausePanel;

        private UIManager uiManager;
        private readonly Dictionary<System.Type, BasePanel> panelsByType = new Dictionary<System.Type, BasePanel>();

        protected override void OnInitialize()
        {
            base.OnInitialize();
            uiManager = UIManager.Instance;
            if (uiManager == null)
            {
                Debug.LogError(LOG_PREFIX + "UIManager를 찾을 수 없습니다.");
                return;
            }
            CacheAndRegisterAllPanels();
        }

        private void CacheAndRegisterAllPanels()
        {
            EnsurePanelReferences();

            RegisterPanelByType<BottomBarPanel>("BottomBar");
            RegisterPanelByType<TopBarPanel>("TopBar");
            RegisterPanelByType<InGamePanel>("InGame");
            RegisterPanelByType<PausePanel>("Pause");
            RegisterPanelByType<MainMenuPanel>("MainMenu");
            RegisterPanelByType<SettingsPanel>("Settings");
            RegisterPanelByType<SummonChoicePanel>("SummonChoice");
            RegisterPanelByType<HandDisplayPanel>("HandDisplay");
        }

        private void EnsurePanelReferences()
        {
            if (topBarPanel == null) topBarPanel = FindObjectOfType<TopBarPanel>(true);
            if (bottomBarPanel == null) bottomBarPanel = FindObjectOfType<BottomBarPanel>(true);
            if (inGamePanel == null) inGamePanel = FindObjectOfType<InGamePanel>(true);
        }

        private int RegisterPanelByType<T>(string panelName) where T : BasePanel
        {
            T panel = FindCachedOrNewPanel<T>(panelName);
            if (panel != null)
            {
                panelsByType[typeof(T)] = panel;
                uiManager.RegisterPanel(panelName, panel);
                return 1;
            }
            return 0;
        }

        private T FindCachedOrNewPanel<T>(params string[] possibleNames) where T : BasePanel
        {
            if (typeof(T) == typeof(BottomBarPanel) && bottomBarPanel != null) return bottomBarPanel as T;
            if (typeof(T) == typeof(TopBarPanel) && topBarPanel != null) return topBarPanel as T;
            if (typeof(T) == typeof(InGamePanel) && inGamePanel != null) return inGamePanel as T;
            if (typeof(T) == typeof(PausePanel) && pausePanel != null) return pausePanel as T;

            if (uiManager != null && possibleNames.Length > 0)
            {
                T existingPanel = uiManager.GetPanel(possibleNames[0]) as T;
                if (existingPanel != null) return existingPanel;
            }

            T panel = FindObjectOfType<T>(true);
            if (panel != null)
            {
                ActivatePanelHierarchy(panel.transform);
                return panel;
            }
            return null;
        }

        private void ActivatePanelHierarchy(Transform panel)
        {
            Transform current = panel;
            while (current != null)
            {
                if (!current.gameObject.activeSelf) current.gameObject.SetActive(true);
                current = current.parent;
            }
        }

        public void UpdateEDataUI(int currentEData)
        {
            if (topBarPanel == null) topBarPanel = FindObjectOfType<TopBarPanel>(true);
            topBarPanel?.UpdateEData(currentEData);
        }

        public void UpdateStageWaveUI(int currentStage, int spawnedMonsters, int maxMonsters, int totalStages)
        {
            if (topBarPanel == null) topBarPanel = FindObjectOfType<TopBarPanel>(true);
            topBarPanel?.UpdateStageWaveUI(currentStage, spawnedMonsters, maxMonsters);
        }

        protected override void OnCleanup()
        {
            panelsByType.Clear();
            base.OnCleanup();
        }
    }
}