using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using InvaderInsider.Data;


namespace InvaderInsider.UI
{
    public class MainMenuButtonHandler : MonoBehaviour
    {
        private const string LOG_PREFIX = "[MainMenuButtonHandler] ";
        
        // LOG_MESSAGES 완전히 제거
        
        [Header("Button References")]
        public Button newGameButton;
        public Button continueButton;
        public Button settingsButton;
        public Button deckButton;
        public Button exitButton;

        [Header("Button States")]
        public bool forceActivateAllButtons = true;
        public bool debugButtonStates = false;

        private MenuManager menuManager;

        // Events for external scripts - 사용하는 이벤트만 유지
        public event Action OnSettingsClicked;
        public event Action OnDeckClicked;
        public event Action OnQuitClicked;

        private void Awake()
        {
            menuManager = FindObjectOfType<MenuManager>();
            if (menuManager == null)
            {
                GameObject menuManagerGO = GameObject.Find("MenuManager");
                if (menuManagerGO != null)
                {
                    menuManager = menuManagerGO.GetComponent<MenuManager>();
                }
            }

            SetupButtonEvents();
        }

        private void Start()
        {
            // 개발 단계에서만 강제 활성화
            #if UNITY_EDITOR
            if (forceActivateAllButtons)
            {
                ForceActivateAllButtons();
            }
            #endif

            ValidateAllButtons();
        }

        private void SetupButtonEvents()
        {
            // MainMenuPanel에서 직접 버튼 이벤트를 처리하므로 
            // NewGame과 Continue 버튼은 여기서 처리하지 않음 (중복 방지)
            
            // Settings, Deck, Exit 버튼만 처리 (MainMenuPanel에 없는 기능들)
            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => {
                    OnSettingsClicked?.Invoke();
                    HandleSettingsClick();
                });
            
            if (deckButton != null)
                deckButton.onClick.AddListener(() => {
                    OnDeckClicked?.Invoke();
                    HandleDeckClick();
                });
            
            if (exitButton != null)
                exitButton.onClick.AddListener(() => {
                    OnQuitClicked?.Invoke();
                    HandleExitClick();
                });
        }

        private void ForceActivateAllButtons()
        {
            #if UNITY_EDITOR
            Button[] buttons = { newGameButton, continueButton, settingsButton, deckButton, exitButton };
            string[] buttonNames = { "NewGame", "Continue", "Settings", "Deck", "Exit" };

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    ForceButtonInteractable(buttons[i], buttonNames[i]);
                }
            }
            #endif
        }

        private void ForceButtonInteractable(Button button, string buttonName)
        {
            #if UNITY_EDITOR
            if (button == null) return;

            // 기본 상호작용 활성화
            if (!button.interactable)
            {
                button.interactable = true;
            }

            // GameObject 활성화
            if (!button.gameObject.activeSelf)
            {
                button.gameObject.SetActive(true);
            }

            // Image 컴포넌트 raycast 활성화
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                if (!buttonImage.raycastTarget)
                {
                    buttonImage.raycastTarget = true;
                }

                // 투명도 수정
                Color color = buttonImage.color;
                if (color.a < 0.5f)
                {
                    color.a = 1f;
                    buttonImage.color = color;
                }
            }

            // GraphicRaycaster 활성화
            GraphicRaycaster raycaster = button.GetComponentInParent<GraphicRaycaster>();
            if (raycaster != null)
            {
                if (!raycaster.enabled)
                {
                    raycaster.enabled = true;
                }
            }

            // Canvas 활성화
            Canvas canvas = button.GetComponentInParent<Canvas>();
            if (canvas != null && !canvas.enabled)
            {
                canvas.enabled = true;
            }
            #endif
        }

        private void ValidateAllButtons()
        {
            // 개발 중에만 유효성 검사
            #if UNITY_EDITOR
            if (debugButtonStates)
            {
                ValidateButton(newGameButton, "NewGame");
                ValidateButton(continueButton, "Continue");
                ValidateButton(settingsButton, "Settings");
                ValidateButton(deckButton, "Deck");
                ValidateButton(exitButton, "Exit");
            }
            #endif
        }

        private void ValidateButton(Button button, string buttonName)
        {
            #if UNITY_EDITOR
            if (button == null) return;

            // 기본 정보만 체크, 로그 제거
            bool isInteractable = button.interactable;
            bool isActive = button.gameObject.activeSelf;
            Image image = button.GetComponent<Image>();
            bool hasRaycast = image != null && image.raycastTarget;
            #endif
        }

        // Direct button handlers (for backward compatibility)
        private void HandleNewGameClick()
        {
            if (menuManager != null)
            {
                menuManager.HideMainMenu();
            }
            
            LoadGameSceneAsync(false).Forget();
        }

        private void HandleContinueClick()
        {
            if (SaveDataManager.Instance?.HasSaveData() == true)
            {
                if (menuManager != null)
                {
                    menuManager.HideMainMenu();
                }
                
                SaveDataManager.Instance.LoadGameData();
                LoadGameSceneAsync(true).Forget();
            }
        }
        
        private async UniTask LoadGameSceneAsync(bool isContinue)
        {
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.Cleanup();
            }
            
            await UniTask.Yield(); // 한 프레임 대기
            
            // 비동기로 Game 씬 로드
            var asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Game");
            
            // 씬 로딩 완료까지 대기
            while (!asyncLoad.isDone)
            {
                await UniTask.Yield();
            }
            
            yield return new WaitForEndOfFrame(); // 모든 오브젝트 초기화 대기
        }

        private void HandleSettingsClick()
        {
            menuManager?.ShowSettings();
        }

        private void HandleDeckClick()
        {
            menuManager?.ShowDeck();
        }

        private void HandleExitClick()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        public void SetButtonsInteractable(bool interactable)
        {
            if (newGameButton != null) newGameButton.interactable = interactable;
            if (continueButton != null && SaveDataManager.Instance?.HasSaveData() == true) 
                continueButton.interactable = interactable;
            if (settingsButton != null) settingsButton.interactable = interactable;
            if (deckButton != null) deckButton.interactable = interactable;
            if (exitButton != null) exitButton.interactable = interactable;
        }
    }
} 