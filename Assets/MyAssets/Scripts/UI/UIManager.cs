using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using InvaderInsider.Data;
using InvaderInsider.UI;
using UnityEngine.SceneManagement;

namespace InvaderInsider.UI
{
    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UIManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("UIManager");
                        _instance = go.AddComponent<UIManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Game UI")]
        [SerializeField] private TextMeshProUGUI stageText;
        [SerializeField] private TextMeshProUGUI waveText;

        [Header("Control Buttons")]
        [SerializeField] private UnityEngine.UI.Button summonButton;
        [SerializeField] private UnityEngine.UI.Button mainMenuButton;
        [SerializeField] private UnityEngine.UI.Button pauseMenuButton;

        private List<UnityEngine.UI.Button> controlButtons; // 제어 버튼들을 관리할 리스트

        private Dictionary<string, BasePanel> panels = new Dictionary<string, BasePanel>();
        private Stack<BasePanel> panelHistory = new Stack<BasePanel>();
        private BasePanel currentPanel;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("Multiple UIManager instances found. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Control Buttons 리스트 초기화
            controlButtons = new List<UnityEngine.UI.Button>
            {
                summonButton,
                mainMenuButton,
                pauseMenuButton
            };

            // 버튼 이벤트 리스너 추가
            if (summonButton != null)
            {
                summonButton.onClick.AddListener(OnSummonButtonClicked);
            }
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
            }
            if (pauseMenuButton != null)
            {
                pauseMenuButton.onClick.AddListener(OnPauseMenuButtonClicked);
            }

            // 씬 로드 이벤트 구독
            SceneManager.sceneLoaded += OnSceneLoaded;

            Debug.Log("UIManager Awake");
        }

        private void Start()
        {
            Debug.Log("UIManager Start");
        }

        public void RegisterPanel(string panelName, BasePanel panel)
        {
            if (panels.ContainsKey(panelName))
            {
                Debug.LogWarning($"Panel {panelName} already registered");
                return; // 이미 등록된 패널은 무시
            }
            panels[panelName] = panel;
        }

        public void ShowPanel(string panelName)
        {
            Debug.Log($"Attempting to show panel: {panelName}");
            
            if (!panels.ContainsKey(panelName))
            {
                Debug.LogError($"Panel {panelName} not found!");
                return;
            }

            if (currentPanel != null)
            {
                currentPanel.Hide();
                panelHistory.Push(currentPanel);
            }

            BasePanel panel = panels[panelName];
            panel.Show();
            currentPanel = panel;

            // 특정 패널이 나타날 때 제어 버튼 숨김
            if (panelName == "MainMenu" || panelName == "Pause")
            {
                SetControlButtonsActive(false);
            }

            Debug.Log($"Panel {panelName} shown successfully");
        }

        public void GoBack()
        {
            if (panelHistory.Count > 0)
            {
                if (currentPanel != null)
                {
                    currentPanel.Hide();
                }

                currentPanel = panelHistory.Pop();
                if (currentPanel != null)
                {
                    currentPanel.Show();
                    Debug.Log($"Returned to previous panel: {currentPanel.gameObject.name}");

                    // 돌아온 패널에 따라 제어 버튼 가시성 설정
                    // 패널 이름이 GameObject의 이름과 같다고 가정
                    if (currentPanel.gameObject.name == "MainMenu" || currentPanel.gameObject.name == "Pause")
                    {
                        SetControlButtonsActive(false);
                    }
                }
                else
                {
                    Debug.Log("No previous panel to return to. Resuming game.");
                    if (currentPanel != null)
                    {
                        currentPanel.Hide(); // 현재 패널 (MainMenuPanel) 숨기기
                    }
                    Time.timeScale = 1f; // 게임 재개
                    SetControlButtonsActive(true); // 제어 버튼 활성화
                }
            }
            else // panelHistory.Count == 0
            {
                Debug.Log("No previous panel to return to. Resuming game.");
                if (currentPanel != null) // MainMenuPanel이 현재 패널인 경우
                {
                    currentPanel.Hide(); // MainMenuPanel 숨기기
                }
                Time.timeScale = 1f; // 게임 재개
                SetControlButtonsActive(true); // 제어 버튼 활성화
            }
        }

        public bool HasPreviousPanel()
        {
            return panelHistory.Count > 0;
        }

        public bool IsCurrentPanel(string panelName)
        {
            if (currentPanel == null) return false;
            if (!panels.ContainsKey(panelName)) return false;
            return currentPanel == panels[panelName];
        }

        public void HideCurrentPanel()
        {
            if (currentPanel != null)
            {
                currentPanel.Hide();
                currentPanel = null;
            }
        }

        public bool IsPanelRegistered(string panelName)
        {
            return panels.ContainsKey(panelName);
        }

        public void UnregisterPanel(string panelName)
        {
            if (panels.ContainsKey(panelName))
            {
                if (currentPanel == panels[panelName])
                {
                    currentPanel = null;
                }
                panels.Remove(panelName);
            }
        }

        public void ClearPanelHistory()
        {
            panelHistory.Clear();
            if (currentPanel != null)
            {
                currentPanel.Hide();
                currentPanel = null;
            }
        }

        private void OnDestroy()
        {
            Debug.Log($"UIManager 오브젝트가 파괴되었습니다: {gameObject.name}");
            if (_instance == this)
            {
                _instance = null;
            }

            // 씬 로드 이벤트 구독 해제
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void UpdateStage(int currentStage, int totalStages)
        {
            if (stageText != null)
                stageText.text = $"Stage {currentStage + 1} / {totalStages}";
        }

        public void UpdateWave(int currentWave, int totalWaves)
        {
            if (waveText != null)
                waveText.text = $"Wave {currentWave} / {totalWaves}";
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"OnSceneLoaded 호출됨: 씬 이름 = {scene.name}, 모드 = {mode}");
            if (scene.name == "Main") // 실제 메인 게임 씬 이름으로 변경하세요.
            {
                SetControlButtonsActive(true);
                Debug.Log("Main 로드됨. 제어 버튼 활성화.");
            }
            // 다른 씬에 대한 처리가 필요하면 여기에 추가합니다.
        }

        // 모든 제어 버튼의 활성 상태를 설정하는 헬퍼 메서드
        public void SetControlButtonsActive(bool isActive)
        {
            Debug.Log($"SetControlButtonsActive 호출됨: isActive = {isActive}");
            foreach (var button in controlButtons)
            {
                if (button != null)
                {
                    button.gameObject.SetActive(isActive);
                }
            }
        }

        private void OnSummonButtonClicked()
        {
            if (InvaderInsider.Managers.SummonManager.Instance != null)
            {
                InvaderInsider.Managers.SummonManager.Instance.Summon();
                Debug.Log("소환 버튼이 클릭되었습니다.");
            }
            else
            {
                Debug.LogError("SummonManager 인스턴스를 찾을 수 없습니다.");
            }
        }

        private void OnMainMenuButtonClicked()
        {
            ShowPanel("MainMenu");
            Debug.Log("메인 메뉴 버튼이 클릭되었습니다.");
        }

        private void OnPauseMenuButtonClicked()
        {
            ShowPanel("Pause");
            Debug.Log("일시 정지 메뉴 버튼이 클릭되었습니다.");
        }

        private void CloseMainMenu()
        {
            // 메인 메뉴를 닫는 로직, 예: 메뉴 게임 오브젝트 비활성화
            gameObject.SetActive(false);
        }
    }
} 