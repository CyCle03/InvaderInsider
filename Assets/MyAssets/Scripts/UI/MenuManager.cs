using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using InvaderInsider.Data;
using InvaderInsider.Cards;
using System.Collections.Generic;

using InvaderInsider.UI;

namespace InvaderInsider.UI
{
    public class MenuManager : MonoBehaviour
    {
        [Header("Panel References")]
        public MainMenuPanel mainMenuPanel;
        public SettingsPanel settingsPanel;
        public DeckPanel deckPanel;
        public PausePanel pausePanel;

        private UIManager uiManager;
        private bool isInitialized = false;

        private void Awake()
        {
            // UIManager 인스턴스 확보
            uiManager = UIManager.Instance;
        }

        private void Start()
        {
            if (!isInitialized)
            {
                InitializeUI();
            }

            if (!isInitialized)
            {
                // 1프레임 대기 후 재시도
                RetryInitialization().Forget();
            }
        }

        private async UniTask RetryInitialization()
        {
            await UniTask.Yield(PlayerLoopTiming.EndOfFrame);
            
            InitializeUI();
            
            if (!isInitialized)
            {
                await UniTask.Yield(PlayerLoopTiming.EndOfFrame);
                InitializeUI();
            }
        }

        private void InitializeUI()
        {
            try
            {
                if (mainMenuPanel == null)
                {
                    return;
                }

                if (uiManager == null)
                {
                    uiManager = UIManager.Instance;
                }

                // 메인 메뉴 패널 등록
                if (mainMenuPanel != null)
                {
                    uiManager.RegisterPanel("MainMenu", mainMenuPanel);
                }

                // 설정 패널 등록
                if (settingsPanel != null)
                {
                    uiManager.RegisterPanel("Settings", settingsPanel);
                    settingsPanel.gameObject.SetActive(false);
                }

                // 덱 패널 등록
                if (deckPanel != null)
                {
                    uiManager.RegisterPanel("Deck", deckPanel);
                    deckPanel.gameObject.SetActive(false);
                }

                // 일시정지 패널 등록
                if (pausePanel != null)
                {
                    uiManager.RegisterPanel("Pause", pausePanel);
                    pausePanel.gameObject.SetActive(false);
                }

                isInitialized = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during UI initialization: {e.Message}");
            }
        }

        private void HideAllPanelsExceptMain()
        {
            // 메인 메뉴를 제외한 모든 패널 숨기기
            GameObject[] allPanels = GameObject.FindGameObjectsWithTag("UIPanel");
            
            foreach (GameObject panel in allPanels)
            {
                if (panel != null && panel != mainMenuPanel?.gameObject)
                {
                    panel.SetActive(false);
                }
            }

            // 메인 메뉴는 활성화 상태 유지
            if (mainMenuPanel != null)
            {
                mainMenuPanel.gameObject.SetActive(true);
            }
        }

        public void ShowMainMenu()
        {
            if (!isInitialized)
            {
                return;
            }

            try
            {
                uiManager.ShowPanel("MainMenu");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error showing main menu: {e.Message}");
            }
        }

        public void ShowSettings()
        {
            if (isInitialized)
            {
                uiManager.ShowPanel("Settings");
            }
        }

        public void ShowDeck()
        {
            if (isInitialized)
            {
                uiManager.ShowPanel("Deck");
            }
        }

        public void ShowPause()
        {
            if (isInitialized)
            {
                uiManager.ShowPanel("Pause");
            }
        }

        public void HideMainMenu()
        {
            if (isInitialized && mainMenuPanel != null)
            {
                uiManager.HidePanel("MainMenu");
                mainMenuPanel.gameObject.SetActive(false);
            }
        }

        public void HidePause()
        {
            if (isInitialized)
            {
                uiManager.HidePanel("Pause");
            }
        }

        public void GoBack()
        {
            if (isInitialized)
            {
                uiManager.GoBack();
            }
        }

        private void Update()
        {
            if (!isInitialized) return;
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (uiManager.IsCurrentPanel("Pause"))
                {
                    HidePause();
                }
                else if (SceneManager.GetActiveScene().name != "MainMenu")
                {
                    ShowPause();
                }
            }
        }
    }
} 