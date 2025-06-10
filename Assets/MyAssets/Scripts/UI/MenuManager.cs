using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using InvaderInsider.Data;
using InvaderInsider.Cards;
using System.Collections.Generic;
using System.Collections;

namespace InvaderInsider.UI
{
    public class MenuManager : MonoBehaviour
    {
        private static MenuManager _instance;
        public static MenuManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<MenuManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("MenuManager");
                        _instance = go.AddComponent<MenuManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Panels")]
        private MainMenuPanel mainMenuPanel;
        private SettingsPanel settingsPanel;
        private DeckPanel deckPanel;
        private PausePanel pausePanel;
        private GameplayPanel gameplayPanel;

        private bool isInitialized = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("Multiple MenuManager instances found. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("MenuManager Awake");
            // Ensure UIManager is created first
            var uiManager = UIManager.Instance;
            Debug.Log($"UIManager instance: {uiManager != null}");
        }

        private void Start()
        {
            InitializeUI();
            if (!isInitialized)
            {
                Debug.LogError("MenuManager initialization failed in Start!");
                return;
            }
            ShowMainMenu(); 
        }
        
        private IEnumerator RetryInitialization()
        {
            Debug.Log("Retrying initialization...");
            float timeout = 5f;
            float elapsed = 0f;

            while (!isInitialized && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                InitializeUI();
                if (isInitialized)
                {
                    Debug.Log("Initialization successful on retry");
                    ShowMainMenu();
                    yield break;
                }
                yield return new WaitForSeconds(0.5f);
            }

            if (!isInitialized)
            {
                Debug.LogError("Initialization retry failed!");
            }
        }

        private void InitializeUI()
        {
            Debug.Log($"InitializeUI - MainMenuPanel: {mainMenuPanel != null}");
            
            // 패널들을 씬에서 동적으로 찾아서 할당
            mainMenuPanel = FindObjectOfType<MainMenuPanel>();
            settingsPanel = FindObjectOfType<SettingsPanel>();
            deckPanel = FindObjectOfType<DeckPanel>();
            pausePanel = FindObjectOfType<PausePanel>();
            gameplayPanel = FindObjectOfType<GameplayPanel>();

            // 찾은 패널들을 숨깁니다. MainMenuPanel은 ShowMainMenu에서 표시됩니다.
            if (settingsPanel != null) settingsPanel.HideImmediate();
            if (deckPanel != null) deckPanel.HideImmediate();
            if (pausePanel != null) pausePanel.HideImmediate();
            if (gameplayPanel != null) gameplayPanel.HideImmediate();

            if (mainMenuPanel == null)
            {
                Debug.LogError("MainMenuPanel is missing!");
                return;
            }

            try
            {
                if (mainMenuPanel != null && !UIManager.Instance.IsPanelRegistered("MainMenu"))
                {
                    UIManager.Instance.RegisterPanel("MainMenu", mainMenuPanel);
                    Debug.Log("MainMenu panel registered");
                }

                if (settingsPanel != null && !UIManager.Instance.IsPanelRegistered("Settings"))
                {
                    UIManager.Instance.RegisterPanel("Settings", settingsPanel);
                    Debug.Log("Settings panel registered");
                }

                if (deckPanel != null && !UIManager.Instance.IsPanelRegistered("Deck"))
                {
                    UIManager.Instance.RegisterPanel("Deck", deckPanel);
                    Debug.Log("Deck panel registered");
                }

                if (pausePanel != null && !UIManager.Instance.IsPanelRegistered("Pause"))
                {
                    UIManager.Instance.RegisterPanel("Pause", pausePanel);
                    Debug.Log("Pause panel registered");
                }

                if (gameplayPanel != null && !UIManager.Instance.IsPanelRegistered("Gameplay"))
                {
                    UIManager.Instance.RegisterPanel("Gameplay", gameplayPanel);
                    Debug.Log("Gameplay panel registered");
                }

                isInitialized = true;
                Debug.Log("All panels initialized successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during UI initialization: {e.Message}");
                isInitialized = false;
            }
        }

        public void ShowMainMenu()
        {
            Debug.Log("ShowMainMenu called");
            if (!isInitialized)
            {
                Debug.LogError("Trying to show main menu before initialization!");
                return;
            }

            try
            {
                UIManager.Instance.ShowPanel("MainMenu");
                Debug.Log("Main menu panel show command sent");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error showing main menu: {e.Message}");
            }
        }

        public void ShowSettings()
        {
            if (!isInitialized) return;
            UIManager.Instance.ShowPanel("Settings");
        }

        public void ShowDeck()
        {
            if (!isInitialized) return;
            UIManager.Instance.ShowPanel("Deck");
        }

        public void ShowPause()
        {
            if (!isInitialized) return;
            UIManager.Instance.ShowPanel("Pause");
            Time.timeScale = 0f;
        }

        public void HidePause()
        {
            if (!isInitialized) return;
            UIManager.Instance.HideCurrentPanel();
            Time.timeScale = 1f;
        }

        public void GoBack()
        {
            if (!isInitialized) return;
            UIManager.Instance.GoBack();
            if (!UIManager.Instance.IsCurrentPanel("Pause"))
            {
                Time.timeScale = 1f;
            }
        }

        private void Update()
        {
            if (!isInitialized) return;
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (UIManager.Instance.IsCurrentPanel("Pause"))
                {
                    HidePause();
                }
                else if (SceneManager.GetActiveScene().name != "MainMenu")
                {
                    ShowPause();
                }
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
} 