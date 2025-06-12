using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using InvaderInsider.Data;
using InvaderInsider.UI;
using System.Linq;

namespace InvaderInsider.UI
{
    public class UIManager : MonoBehaviour
    {
        private const string LOG_PREFIX = "[UI] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "Panel not found: {0}", // 0
            "Panel {0} not found for showing", // 1
            "UIManager destroyed: {0}" // 2
        };

        private static UIManager instance;
        private static readonly object _lock = new object();

        public static UIManager Instance
        {
            get
            {
                lock (_lock)
                {
                    if (instance == null)
                    {
                        instance = FindObjectOfType<UIManager>();
                        if (instance == null)
                        {
                            GameObject go = new GameObject("UIManager");
                            instance = go.AddComponent<UIManager>();
                            DontDestroyOnLoad(go);
                        }
                    }
                    return instance;
                }
            }
        }

        [Header("Game UI")]
        [SerializeField] private TextMeshProUGUI stageText;
        [SerializeField] private TextMeshProUGUI waveText;

        private readonly Dictionary<string, BasePanel> panels = new Dictionary<string, BasePanel>();
        private readonly Stack<BasePanel> panelHistory = new Stack<BasePanel>();
        private BasePanel currentPanel;

        // 이벤트 선언 추가
        public event Action<string> OnPanelShown;
        public event Action<string> OnPanelHidden;

        private readonly string[] menuScenes = { "Main" }; // 메인 메뉴 씬
        private readonly string[] gameScenes = { "Game" }; // 게임 씬

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
                UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // 시작 시 기본 설정
        }

        public void RegisterPanel(string panelName, BasePanel panel)
        {
            if (string.IsNullOrEmpty(panelName) || panel == null)
            {
                return;
            }

            if (panels.ContainsKey(panelName))
            {
                Debug.LogWarning(string.Format(LOG_PREFIX + LOG_MESSAGES[0], panelName));
                panels[panelName] = panel;
            }
            else
            {
                panels.Add(panelName, panel);
            }
        }

        public void ShowPanel(string panelName)
        {
            if (string.IsNullOrEmpty(panelName)) return;

            if (panels.TryGetValue(panelName, out BasePanel panel))
            {
                if (currentPanel != null && currentPanel != panel)
                {
                    currentPanel.Hide();
                    OnPanelHidden?.Invoke(currentPanel.name);
                }

                panel.Show();
                currentPanel = panel;
                OnPanelShown?.Invoke(panelName);
            }
            else
            {
                Debug.LogError(string.Format(LOG_PREFIX + LOG_MESSAGES[1], panelName));
            }
        }

        public void HideCurrentPanel()
        {
            if (currentPanel != null)
            {
                currentPanel.Hide();
                currentPanel = null;
            }
        }

        public void HidePanel(string panelName)
        {
            if (panels.TryGetValue(panelName, out BasePanel panel))
            {
                panel.Hide();
                OnPanelHidden?.Invoke(panelName);
                if (currentPanel == panel)
                {
                    currentPanel = null;
                }
            }
        }

        public bool IsPanelActive(string panelName)
        {
            return panels.TryGetValue(panelName, out BasePanel panel) && panel.gameObject.activeInHierarchy;
        }

        public bool IsCurrentPanel(string panelName)
        {
            return currentPanel != null && panels.TryGetValue(panelName, out BasePanel panel) && currentPanel == panel;
        }

        public void GoBack()
        {
            HideCurrentPanel();
        }

        public void UpdateStage(int currentStage, int totalStages)
        {
            if (stageText != null)
            {
                stageText.text = string.Format(LOG_PREFIX + LOG_MESSAGES[3], currentStage + 1, totalStages);
            }
        }

        public void UpdateWave(int currentWave, int totalWaves)
        {
            if (waveText != null)
            {
                waveText.text = string.Format(LOG_PREFIX + LOG_MESSAGES[4], currentWave, totalWaves);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[2], gameObject.name));
                UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
                UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;
                instance = null;
            }
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            string sceneName = scene.name;
            Debug.Log($"{LOG_PREFIX}Scene loaded: {sceneName}");
        }

        private void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
        {
            string sceneName = scene.name;
            Debug.Log($"{LOG_PREFIX}Scene unloaded: {sceneName}");

            if (menuScenes.Any(s => s == sceneName))
            {
                // 메뉴 씬이 언로드될 때 패널 목록 초기화
                panels.Clear();
            }
        }

        // 디버그용 - 개발 중에만 사용
        public void DebugPrintRegisteredPanels()
        {
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + "=== 등록된 패널 목록 ===");
            foreach (var kvp in panels)
            {
                Debug.Log(string.Format(LOG_PREFIX + "Panel: {0}, Active: {1}", kvp.Key, kvp.Value.gameObject.activeSelf));
            }
            Debug.Log(LOG_PREFIX + string.Format("총 {0}개 패널 등록됨", panels.Count));
            #endif
        }

        public void Cleanup()
        {
            // 모든 패널 숨기기
            foreach (var panel in panels.Values)
            {
                if (panel != null)
                {
                    panel.ForceHide();
                }
            }
            
            // 패널 목록 초기화
            panels.Clear();
            
            // 이벤트 정리
            OnPanelShown = null;
            OnPanelHidden = null;
        }
    }
} 