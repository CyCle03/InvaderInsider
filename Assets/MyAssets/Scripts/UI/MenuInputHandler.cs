using UnityEngine;
using InvaderInsider.Managers;

namespace InvaderInsider.UI
{
    public class MenuInputHandler : MonoBehaviour
    {
        private const string LOG_PREFIX = "[MenuInput] ";
        
        // LOG_MESSAGES 제거 - 로그 출력하지 않음

        [Header("Input Settings")]
        public bool enableEscapeInput = true;
        public bool enableBackgroundClick = true;

        private UIManager uiManager;
        private MenuManager menuManager;
        private UIState currentState = UIState.MainMenu;

        private void Start()
        {
            uiManager = UIManager.Instance;
            menuManager = FindObjectOfType<MenuManager>();
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            // ESC 키 입력 처리
            if (enableEscapeInput && Input.GetKeyDown(KeyCode.Escape))
            {
                HandleEscapeInput();
            }

            // 마우스 우클릭으로 뒤로가기
            if (enableBackgroundClick && Input.GetMouseButtonDown(1))
            {
                HandleBackInput();
            }
        }

        private void HandleEscapeInput()
        {
            switch (currentState)
            {
                case UIState.MainMenu:
                    // 메인 메뉴에서는 게임 종료 확인
                    break;
                
                case UIState.Settings:
                case UIState.Deck:
                case UIState.Achievements:
                    // 서브 패널에서는 메인 메뉴로 돌아가기
                    GoBackToMainMenu();
                    break;
                
                case UIState.Pause:
                    // 일시정지에서는 게임 재개
                    ResumeGame();
                    break;
            }
        }

        private void HandleBackInput()
        {
            if (currentState != UIState.MainMenu)
            {
                GoBackToMainMenu();
            }
        }

        private void GoBackToMainMenu()
        {
            menuManager?.ShowMainMenu();
            currentState = UIState.MainMenu;
        }

        private void ResumeGame()
        {
            uiManager?.HideCurrentPanel();
            Time.timeScale = 1f;
        }

        public void SetUIState(UIState newState)
        {
            currentState = newState;
        }

        public UIState GetCurrentState()
        {
            return currentState;
        }

        public void SetPanelActive(string panelName)
        {
            switch (panelName.ToLower())
            {
                case "mainmenu":
                    currentState = UIState.MainMenu;
                    break;
                case "settings":
                    currentState = UIState.Settings;
                    break;
                case "deck":
                    currentState = UIState.Deck;
                    break;
                case "achievements":
                    currentState = UIState.Achievements;
                    break;
                case "pause":
                    currentState = UIState.Pause;
                    break;
            }
        }
    }

    public enum UIState
    {
        MainMenu,
        Settings,
        Deck,
        Achievements,
        Pause,
        InGame
    }
} 