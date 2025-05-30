using UnityEngine;
using InvaderInsider.Managers;

namespace InvaderInsider.UI
{
    public class MenuInputHandler : MonoBehaviour
    {
        [System.Serializable]
        public struct MenuKeyBinding
        {
            public KeyCode key;
            public string panelName;
            public GameState allowedState; // 이 키가 동작할 수 있는 게임 상태
        }

        [SerializeField] private MenuKeyBinding[] menuBindings;
        [SerializeField] private KeyCode escapeKey = KeyCode.Escape;

        private void Update()
        {
            // Use GetKeyUp for Escape key to avoid double triggering
            if (Input.GetKeyUp(escapeKey))
            {
                HandleEscapeKey();
            }

            HandleMenuBindings();
        }

        private void HandleEscapeKey()
        {
            Debug.Log($"Escape key released. Current Game State: {GameManager.Instance.CurrentGameState}");

            GameState currentState = GameManager.Instance.CurrentGameState;
            switch (currentState)
            {
                case GameState.Playing:
                    // 게임 플레이 중: PausePanel 표시
                    UIManager.Instance.ShowPanel("Pause");
                    break;

                case GameState.Paused:
                    // 일시정지 상태: 게임 재개 (PausePanel의 Resume 기능과 동일)
                    Time.timeScale = 1f;
                    GameManager.Instance.CurrentGameState = GameState.Playing;
                    UIManager.Instance.HideCurrentPanel();
                    break;

                case GameState.MainMenu:
                case GameState.Settings:
                    // 메인메뉴나 설정 화면: 이전 화면으로 돌아가기
                    UIManager.Instance.GoBack();
                    break;
            }
        }

        private void HandleMenuBindings()
        {
            GameState currentState = GameManager.Instance.CurrentGameState;

            foreach (var binding in menuBindings)
            {
                // 현재 게임 상태가 허용된 상태일 때만 키 입력 처리
                if (binding.allowedState == currentState && Input.GetKeyDown(binding.key))
                {
                    TogglePanel(binding.panelName);
                    break;
                }
            }
        }

        private void TogglePanel(string panelName)
        {
            if (UIManager.Instance.IsCurrentPanel(panelName))
            {
                UIManager.Instance.HideCurrentPanel();
            }
            else
            {
                UIManager.Instance.ShowPanel(panelName);
            }
        }
    }
} 