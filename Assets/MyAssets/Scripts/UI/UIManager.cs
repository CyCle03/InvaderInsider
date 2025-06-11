using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using InvaderInsider.Data;
using InvaderInsider.UI;

namespace InvaderInsider.UI
{
    public class UIManager : MonoBehaviour
    {
        private const string LOG_PREFIX = "[UI] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "Awake",
            "Start",
            "Panel {0} already registered",
            "Attempting to show panel: {0}",
            "Panel {0} not found!",
            "Panel {0} shown successfully",
            "No previous panel to return to",
            "Returned to previous panel: {0}",
            "Panel {0} hidden successfully",
            "Panel {0} not found to hide",
            "UIManager destroyed: {0}",
            "Stage {0} / {1}",
            "Wave {0} / {1}"
        };

        private static UIManager _instance;
        private static readonly object _lock = new object();
        private static bool isQuitting = false;

        public static UIManager Instance
        {
            get
            {
                if (isQuitting)
                {
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<UIManager>();
                        if (_instance == null && !isQuitting)
                        {
                            GameObject go = new GameObject("UIManager");
                            _instance = go.AddComponent<UIManager>();
                            DontDestroyOnLoad(go);
                        }
                    }
                    return _instance;
                }
            }
        }

        [Header("Game UI")]
        [SerializeField] private TextMeshProUGUI stageText;
        [SerializeField] private TextMeshProUGUI waveText;

        private readonly Dictionary<string, BasePanel> panels = new Dictionary<string, BasePanel>();
        private readonly Stack<BasePanel> panelHistory = new Stack<BasePanel>();
        private BasePanel currentPanel;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning(LOG_PREFIX + LOG_MESSAGES[2]);
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[0]);
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void Start()
        {
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[1]);
        }

        public void RegisterPanel(string panelName, BasePanel panel)
        {
            if (string.IsNullOrEmpty(panelName) || panel == null) return;

            if (panels.ContainsKey(panelName))
            {
                if (panels[panelName] == panel)
                {
                    return;
                }
                
                Debug.LogWarning(string.Format(LOG_PREFIX + LOG_MESSAGES[2], panelName));
                return;
            }
            
            panels[panelName] = panel;
        }

        public void ShowPanel(string panelName)
        {
            if (string.IsNullOrEmpty(panelName)) return;

            Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[3], panelName));
            
            if (!panels.TryGetValue(panelName, out BasePanel panel))
            {
                Debug.LogError(string.Format(LOG_PREFIX + LOG_MESSAGES[4], panelName));
                return;
            }

            if (currentPanel != null)
            {
                currentPanel.Hide();
                panelHistory.Push(currentPanel);
            }

            panel.Show();
            currentPanel = panel;

            Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[5], panelName));
        }

        public void GoBack()
        {
            if (panelHistory.Count == 0)
            {
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[7]);
                return;
            }

            if (currentPanel != null)
            {
                currentPanel.Hide();
            }

            currentPanel = panelHistory.Pop();
            if (currentPanel != null)
            {
                currentPanel.Show();
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[8], currentPanel.gameObject.name));
            }
        }

        public bool IsCurrentPanel(string panelName)
        {
            if (string.IsNullOrEmpty(panelName) || currentPanel == null) return false;
            return panels.TryGetValue(panelName, out BasePanel panel) && currentPanel == panel;
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
                panel.Hide();
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[8], panelName));
            }
            else
            {
                Debug.LogWarning(string.Format(LOG_PREFIX + LOG_MESSAGES[9], panelName));
            }
        }

        public bool IsPanelRegistered(string panelName)
        {
            return !string.IsNullOrEmpty(panelName) && panels.ContainsKey(panelName);
        }

        public void UnregisterPanel(string panelName)
        {
            if (string.IsNullOrEmpty(panelName)) return;

            if (panels.TryGetValue(panelName, out BasePanel panel))
            {
                if (currentPanel == panel)
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
            Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[10], gameObject.name));
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public void UpdateStage(int currentStage, int totalStages)
        {
            if (stageText != null)
            {
                stageText.text = string.Format(LOG_PREFIX + LOG_MESSAGES[11], currentStage + 1, totalStages);
            }
        }

        public void UpdateWave(int currentWave, int totalWaves)
        {
            if (waveText != null)
            {
                waveText.text = string.Format(LOG_PREFIX + LOG_MESSAGES[12], currentWave, totalWaves);
            }
        }

        public bool IsPanelActive(string panelName)
        {
            if (string.IsNullOrEmpty(panelName)) return false;
            return panels.TryGetValue(panelName, out BasePanel panel) && panel.gameObject.activeSelf;
        }
    }
} 