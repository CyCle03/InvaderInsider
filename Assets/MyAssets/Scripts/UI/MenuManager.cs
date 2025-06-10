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
        [Header("Panels")]
        [SerializeField] private MainMenuPanel mainMenuPanel;
        [SerializeField] private SettingsPanel settingsPanel;
        [SerializeField] private DeckPanel deckPanel;
        [SerializeField] private PausePanel pausePanel;

        private bool isInitialized = false;

        private void Awake()
        {
            Debug.Log("MenuManager Awake");
            // Ensure UIManager is created first
            var uiManager = UIManager.Instance;
            Debug.Log($"UIManager instance: {uiManager != null}");
            InitializeUI();
        }

        private void Start()
        {
            if (!isInitialized)
            {
                Debug.LogError("MenuManager not initialized on Start!");
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
            
            if (mainMenuPanel == null)
            {
                Debug.LogError("MainMenuPanel is missing!");
                return;
            }

            try
            {
                if (mainMenuPanel != null)
                {
                    UIManager.Instance.RegisterPanel("MainMenu", mainMenuPanel);
                    Debug.Log("MainMenu panel registered");
                }

                if (settingsPanel != null)
                {
                    UIManager.Instance.RegisterPanel("Settings", settingsPanel);
                    Debug.Log("Settings panel registered");
                }

                if (deckPanel != null)
                {
                    UIManager.Instance.RegisterPanel("Deck", deckPanel);
                    Debug.Log("Deck panel registered");
                }

                if (pausePanel != null)
                {
                    UIManager.Instance.RegisterPanel("Pause", pausePanel);
                    Debug.Log("Pause panel registered");
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
    }
} 