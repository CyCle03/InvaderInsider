using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using InvaderInsider.Data;
using InvaderInsider.UI;
using System.Linq;
using UnityEngine.SceneManagement;

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
                // 에디터에서 플레이 모드가 아닐 때는 인스턴스 생성하지 않음
                #if UNITY_EDITOR
                if (!UnityEngine.Application.isPlaying) return null;
                #endif
                
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

        private readonly string[] menuScenes = { "Main" }; // 메인 메뉴 씬
        private readonly string[] gameScenes = { "Game" }; // 게임 씬

        // 메모리 할당 최적화를 위한 정적 리스트들
        private static readonly List<string> tempKeysToRemove = new List<string>();
        private static readonly List<BasePanel> tempPanelList = new List<BasePanel>();

        private void Awake()
        {
            // 에디터 모드에서는 초기화하지 않음
            #if UNITY_EDITOR
            if (!Application.isPlaying) return;
            #endif
            
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                SceneManager.sceneLoaded += OnSceneLoaded;
                SceneManager.sceneUnloaded += OnSceneUnloaded;
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
            if (string.IsNullOrEmpty(panelName) || panel == null) return;

            if (panels.ContainsKey(panelName))
            {
                #if UNITY_EDITOR
                Debug.LogWarning(string.Format(LOG_PREFIX + LOG_MESSAGES[0], panelName));
                #endif
                return;
            }

            panels[panelName] = panel;
            
            if (!panel.gameObject.activeSelf)
            {
                panel.gameObject.SetActive(true);
            }
        }

        public void ShowPanel(string panelName)
        {
            if (string.IsNullOrEmpty(panelName)) return;

            if (panels.TryGetValue(panelName, out BasePanel panel))
            {
                // 패널이 파괴되었는지 확인
                if (panel == null)
                {
                    panels.Remove(panelName);
                    return;
                }
                
                if (currentPanel != null && currentPanel != panel)
                {
                    currentPanel.Hide();
                }

                if (!panel.gameObject.activeSelf)
                {
                    panel.gameObject.SetActive(true);
                }

                panel.Show();
                currentPanel = panel;
            }
            else
            {
                #if UNITY_EDITOR
                string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                string registeredPanels = string.Join(", ", panels.Keys);
                Debug.LogError($"{LOG_PREFIX}Panel '{panelName}' not found for showing. 현재 씬: {currentSceneName}, 등록된 패널: [{registeredPanels}]");
                #endif
            }
        }

        // 기존 패널을 숨기지 않고 새 패널을 표시하는 메서드
        public void ShowPanelConcurrent(string panelName)
        {
            if (string.IsNullOrEmpty(panelName)) return;

            if (panels.TryGetValue(panelName, out BasePanel panel))
            {
                panel.Show();
                OnPanelShown?.Invoke(panelName);
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogError(string.Format(LOG_PREFIX + LOG_MESSAGES[1], panelName));
                #endif
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
            if (string.IsNullOrEmpty(panelName)) return;

            if (panels.TryGetValue(panelName, out BasePanel panel))
            {
                // 패널이 파괴되었는지 확인
                if (panel == null)
                {
                    panels.Remove(panelName);
                    if (currentPanel == panel)
                    {
                        currentPanel = null;
                    }
                    return;
                }
                
                panel.Hide();
                
                if (currentPanel == panel)
                {
                    currentPanel = null;
                }
            }
            else
            {
                #if UNITY_EDITOR
                string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                string registeredPanels = string.Join(", ", panels.Keys);
                Debug.LogError($"{LOG_PREFIX}Panel '{panelName}' not found for hiding. 현재 씬: {currentSceneName}, 등록된 패널: [{registeredPanels}]");
                #endif
            }
        }

        public bool IsPanelActive(string panelName)
        {
            return panels.TryGetValue(panelName, out BasePanel panel) && panel.gameObject.activeInHierarchy;
        }

        public bool IsPanelRegistered(string panelName)
        {
            return panels.ContainsKey(panelName);
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
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            #if UNITY_EDITOR
            Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[2], gameObject.name));
            #endif
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            string sceneName = scene.name;
            #if UNITY_EDITOR
            Debug.Log($"{LOG_PREFIX}Scene loaded: {sceneName}");
            #endif
        }

        private void OnSceneUnloaded(Scene scene)
        {
            string sceneName = scene.name;
            #if UNITY_EDITOR
            Debug.Log($"{LOG_PREFIX}Scene unloaded: {sceneName}");
            #endif

            // 파괴된 패널 참조 정리 (재사용 가능한 리스트 사용)
            tempKeysToRemove.Clear();
            foreach (var kvp in panels)
            {
                if (kvp.Value == null)
                {
                    tempKeysToRemove.Add(kvp.Key);
                }
            }
            
            // 파괴된 패널 참조 제거
            for (int i = 0; i < tempKeysToRemove.Count; i++)
            {
                panels.Remove(tempKeysToRemove[i]);
            }

            // 씬별로 적절한 패널 정리 (LINQ 사용 최소화)
            bool isMenuScene = false;
            bool isGameScene = false;
            
            for (int i = 0; i < menuScenes.Length; i++)
            {
                if (menuScenes[i] == sceneName)
                {
                    isMenuScene = true;
                    break;
                }
            }
            
            if (!isMenuScene)
            {
                for (int i = 0; i < gameScenes.Length; i++)
                {
                    if (gameScenes[i] == sceneName)
                    {
                        isGameScene = true;
                        break;
                    }
                }
            }
            
            if (isMenuScene)
            {
                // 메뉴 씬이 언로드될 때 메뉴 관련 패널만 정리
                #if UNITY_EDITOR
                Debug.Log($"{LOG_PREFIX}메뉴 씬 언로드 - 메뉴 관련 패널 정리");
                #endif
                
                panels.Remove("MainMenu");
                panels.Remove("Settings");
            }
            else if (isGameScene)
            {
                // 게임 씬이 언로드될 때 게임 관련 패널만 정리
                #if UNITY_EDITOR
                Debug.Log($"{LOG_PREFIX}게임 씬 언로드 - 게임 관련 패널 정리");
                #endif
                
                panels.Remove("InGame");
                panels.Remove("Pause");
                panels.Remove("Deck");
                panels.Remove("SummonChoice");
            }
            
            #if UNITY_EDITOR
            if (tempKeysToRemove.Count > 0)
            {
                Debug.Log($"{LOG_PREFIX}{tempKeysToRemove.Count}개의 파괴된 패널 참조가 정리되었습니다.");
            }
            #endif
        }

        // 디버그용 - 개발 중에만 사용
        public void DebugPrintRegisteredPanels()
        {
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + "=== 등록된 패널 목록 ===");
            
            // 파괴된 패널 참조 정리
            var keysToRemove = new List<string>();
            foreach (var kvp in panels)
            {
                if (kvp.Value == null)
                {
                    keysToRemove.Add(kvp.Key);
                }
                else
                {
                    Debug.Log(string.Format(LOG_PREFIX + "Panel: {0}, Active: {1}", kvp.Key, kvp.Value.gameObject.activeSelf));
                }
            }
            
            // 파괴된 패널 참조 제거
            foreach (var key in keysToRemove)
            {
                panels.Remove(key);
            }
            
            Debug.Log(LOG_PREFIX + string.Format("총 {0}개 패널 등록됨", panels.Count));
            #endif
        }

        public void Cleanup()
        {
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + "UI 정리 시작...");
            #endif
            
            // 등록된 패널들만 정리 (FindObjectsOfType 사용 최소화)
            tempKeysToRemove.Clear();
            
            foreach (var kvp in panels)
            {
                if (kvp.Value == null)
                {
                    tempKeysToRemove.Add(kvp.Key);
                }
                else
                {
                    // 유효한 패널은 숨기기
                    try
                    {
                        kvp.Value.gameObject.SetActive(false);
                        kvp.Value.ForceHide();
                    }
                    catch (System.Exception ex)
                    {
                        #if UNITY_EDITOR
                        Debug.LogWarning(LOG_PREFIX + $"등록된 패널 {kvp.Key} 정리 중 오류: {ex.Message}");
                        #endif
                        tempKeysToRemove.Add(kvp.Key); // 오류가 난 패널은 제거
                    }
                }
            }
            
            // 파괴된 패널 참조 제거
            for (int i = 0; i < tempKeysToRemove.Count; i++)
            {
                panels.Remove(tempKeysToRemove[i]);
            }
            
            // 현재 패널 참조 정리
            currentPanel = null;
            
            // 패널 히스토리 정리
            panelHistory.Clear();
            
            // 이벤트 정리
            OnPanelShown = null;
            
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + $"UI 정리 완료 - {tempKeysToRemove.Count}개의 참조 제거됨");
            #endif
        }
    }
} 