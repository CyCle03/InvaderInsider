using UnityEngine;
using UnityEngine.UI;
using System;
using InvaderInsider.Managers;
using InvaderInsider.Data;
using System.Collections.Generic;

namespace InvaderInsider.UI
{
    public class MainMenuButtonHandler : MonoBehaviour
    {
        private const string LOG_PREFIX = "[UI] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "MainMenu: Button {0} missing",
            "MainMenu: Button {0} at {1}, size {2}",
            "MainMenu: Button {0} missing Image",
            "MainMenu: Button {0} zero alpha",
            "MainMenu: Button {0} raycast disabled",
            "MainMenu: Button {0} not interactable",
            "MainMenu: New Game clicked",
            "MainMenu: Load Game clicked",
            "MainMenu: Loading stage {0}",
            "MainMenu: Deck clicked",
            "MainMenu: Shop clicked",
            "MainMenu: Settings clicked",
            "MainMenu: Achievements clicked",
            "MainMenu: Quit clicked",
            "MainMenu: Button {0} validation failed",
            "MainMenu: Button {0} validation passed"
        };

        [Header("Main Menu Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button loadGameButton;
        [SerializeField] private Button deckButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button settingsButton;

        [Header("Optional Buttons")]
        [SerializeField] private Button achievementsButton;
        [SerializeField] private Button quitButton;

        private UIManager uiManager;
        private GameManager gameManager;
        private SaveDataManager saveDataManager;
        private StageManager stageManager;
        private bool isInitialized;
        private readonly Dictionary<Button, string> buttonNames = new Dictionary<Button, string>();
        private readonly List<Button> validatedButtons = new List<Button>();

        public event Action OnNewGameClicked;
        public event Action OnLoadGameClicked;
        public event Action OnDeckClicked;
        public event Action OnShopClicked;
        public event Action OnSettingsClicked;
        public event Action OnAchievementsClicked;
        public event Action OnQuitClicked;

        private void Awake()
        {
            if (!isInitialized)
            {
                InitializeManagers();
                InitializeButtonNames();
                ValidateButtons();
                SetupButtonListeners();
                isInitialized = true;
            }
        }

        private void Start()
        {
            // Start에서 한 번 더 강제 활성화
            Debug.Log(LOG_PREFIX + "Start called - forcing button activation");
            if (isInitialized)
            {
                ValidateButtons();
                ForceActivateAllButtons();
            }
        }

        private void ForceActivateAllButtons()
        {
            Debug.Log(LOG_PREFIX + "Force activating all buttons");
            foreach (var kvp in buttonNames)
            {
                Button button = kvp.Key;
                string name = kvp.Value;
                
                if (button != null)
                {
                    // 강제 활성화
                    button.interactable = true;
                    button.gameObject.SetActive(true);
                    
                    // Image raycast 활성화
                    var image = button.GetComponent<Image>();
                    if (image != null)
                    {
                        image.raycastTarget = true;
                        if (image.color.a < 0.1f)
                        {
                            Color color = image.color;
                            color.a = 1f;
                            image.color = color;
                        }
                    }
                    
                    Debug.Log(LOG_PREFIX + "Force activated button: " + name + " - interactable: " + button.interactable + ", raycast: " + (image != null ? image.raycastTarget.ToString() : "no image"));
                }
            }
        }

        private void InitializeManagers()
        {
            uiManager = UIManager.Instance;
            gameManager = GameManager.Instance;
            saveDataManager = SaveDataManager.Instance;
            stageManager = StageManager.Instance;
        }

        private void InitializeButtonNames()
        {
            buttonNames.Clear();
            if (newGameButton != null) buttonNames[newGameButton] = "New Game";
            if (loadGameButton != null) buttonNames[loadGameButton] = "Load Game";
            if (deckButton != null) buttonNames[deckButton] = "Deck";
            if (shopButton != null) buttonNames[shopButton] = "Shop";
            if (settingsButton != null) buttonNames[settingsButton] = "Settings";
            if (achievementsButton != null) buttonNames[achievementsButton] = "Achievements";
            if (quitButton != null) buttonNames[quitButton] = "Quit";
        }

        private void ValidateButtons()
        {
            validatedButtons.Clear();
            foreach (var kvp in buttonNames)
            {
                if (ValidateButton(kvp.Key, kvp.Value))
                {
                    validatedButtons.Add(kvp.Key);
                }
            }
        }

        private bool ValidateButton(Button button, string buttonName)
        {
            if (button == null)
            {
                Debug.LogError(string.Format(LOG_PREFIX + LOG_MESSAGES[3], buttonName));
                return false;
            }

            // 강제로 버튼 활성화
            if (!button.interactable)
            {
                Debug.Log(LOG_PREFIX + "Button " + buttonName + " not interactable");
                button.interactable = true;
                Debug.Log(LOG_PREFIX + "Button " + buttonName + " forced to interactable");
            }

            // GameObject 활성화 확인
            if (!button.gameObject.activeInHierarchy)
            {
                Debug.Log(LOG_PREFIX + "Button " + buttonName + " GameObject not active");
                button.gameObject.SetActive(true);
                Debug.Log(LOG_PREFIX + "Button " + buttonName + " GameObject activated");
            }

            // Image 컴포넌트의 raycastTarget 활성화
            var buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                if (!buttonImage.raycastTarget)
                {
                    Debug.Log(LOG_PREFIX + "Button " + buttonName + " raycast disabled");
                    buttonImage.raycastTarget = true;
                    Debug.Log(LOG_PREFIX + "Button " + buttonName + " raycast enabled");
                }
                
                // 투명도가 0이면 보이도록 수정
                if (buttonImage.color.a == 0)
                {
                    Color color = buttonImage.color;
                    color.a = 1f;
                    buttonImage.color = color;
                    Debug.Log(LOG_PREFIX + "Button " + buttonName + " opacity fixed");
                }
            }
            else
            {
                Debug.LogError(LOG_PREFIX + "Button " + buttonName + " has no Image component");
                return false;
            }

            // GraphicRaycaster 활성화
            var raycaster = button.GetComponentInParent<GraphicRaycaster>();
            if (raycaster != null)
            {
                if (!raycaster.enabled)
                {
                    Debug.Log(LOG_PREFIX + "Button " + buttonName + " raycaster disabled");
                    raycaster.enabled = true;
                    Debug.Log(LOG_PREFIX + "Button " + buttonName + " raycaster enabled");
                }
            }
            else
            {
                Debug.LogWarning(LOG_PREFIX + "Button " + buttonName + " has no GraphicRaycaster in parent");
            }

            // Canvas 확인
            var canvas = button.GetComponentInParent<Canvas>();
            if (canvas != null && !canvas.enabled)
            {
                canvas.enabled = true;
                Debug.Log(LOG_PREFIX + "Button " + buttonName + " Canvas enabled");
            }

            Debug.Log(LOG_PREFIX + "Button " + buttonName + " validation completed - interactable: " + button.interactable + ", raycast: " + (buttonImage != null ? buttonImage.raycastTarget.ToString() : "no image"));
            return true;
        }

        private void SetupButtonListeners()
        {
            foreach (var button in validatedButtons)
            {
                if (button == newGameButton) button.onClick.AddListener(HandleNewGameClick);
                else if (button == loadGameButton) button.onClick.AddListener(HandleLoadGameClick);
                else if (button == deckButton) button.onClick.AddListener(HandleDeckClick);
                else if (button == shopButton) button.onClick.AddListener(HandleShopClick);
                else if (button == settingsButton) button.onClick.AddListener(HandleSettingsClick);
                else if (button == achievementsButton) button.onClick.AddListener(HandleAchievementsClick);
                else if (button == quitButton) button.onClick.AddListener(HandleQuitClick);
            }
        }

        private void HandleNewGameClick()
        {
            if (!isInitialized) return;
            if (Application.isPlaying)
            {
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[6]);
            }
            OnNewGameClicked?.Invoke();
        }

        private void HandleLoadGameClick()
        {
            if (!isInitialized) return;
            if (Application.isPlaying)
            {
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[7]);
            }
            OnLoadGameClicked?.Invoke();
        }

        private void HandleDeckClick()
        {
            if (!isInitialized) return;
            if (Application.isPlaying)
            {
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[9]);
            }
            OnDeckClicked?.Invoke();
        }

        private void HandleShopClick()
        {
            if (!isInitialized) return;
            if (Application.isPlaying)
            {
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[10]);
            }
            OnShopClicked?.Invoke();
        }

        private void HandleSettingsClick()
        {
            if (!isInitialized) return;
            if (Application.isPlaying)
            {
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[11]);
            }
            OnSettingsClicked?.Invoke();
        }

        private void HandleAchievementsClick()
        {
            if (!isInitialized) return;
            if (Application.isPlaying)
            {
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[12]);
            }
            OnAchievementsClicked?.Invoke();
        }

        private void HandleQuitClick()
        {
            if (!isInitialized) return;
            if (Application.isPlaying)
            {
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[13]);
            }
            OnQuitClicked?.Invoke();
        }

        private void OnDestroy()
        {
            if (!isInitialized) return;

            foreach (var button in validatedButtons)
            {
                if (button == newGameButton) button.onClick.RemoveListener(HandleNewGameClick);
                else if (button == loadGameButton) button.onClick.RemoveListener(HandleLoadGameClick);
                else if (button == deckButton) button.onClick.RemoveListener(HandleDeckClick);
                else if (button == shopButton) button.onClick.RemoveListener(HandleShopClick);
                else if (button == settingsButton) button.onClick.RemoveListener(HandleSettingsClick);
                else if (button == achievementsButton) button.onClick.RemoveListener(HandleAchievementsClick);
                else if (button == quitButton) button.onClick.RemoveListener(HandleQuitClick);
            }

            validatedButtons.Clear();
            buttonNames.Clear();
            isInitialized = false;
        }

        public void SetButtonsInteractable(bool interactable)
        {
            if (!isInitialized) return;

            foreach (var button in validatedButtons)
            {
                if (button != null)
                {
                    button.interactable = interactable;
                }
            }
        }
    }
} 