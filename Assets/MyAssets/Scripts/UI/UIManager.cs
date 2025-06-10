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
                }
            }
            else
            {
                Debug.Log("No previous panel to return to");
            }
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

        private void CloseMainMenu()
        {
            // 메인 메뉴를 닫는 로직, 예: 메뉴 게임 오브젝트 비활성화
            gameObject.SetActive(false);
        }
    }
} 