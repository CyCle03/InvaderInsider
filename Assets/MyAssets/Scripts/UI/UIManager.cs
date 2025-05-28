using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using InvaderInsider.Data;

namespace InvaderInsider.UI
{
    public class UIManager : MonoBehaviour
    {
        private static UIManager instance;
        public static UIManager Instance
        {
            get
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

        [Header("Game UI")]
        [SerializeField] private TextMeshProUGUI stageText;
        [SerializeField] private TextMeshProUGUI waveText;

        private Stack<BasePanel> panelStack = new Stack<BasePanel>();
        private Dictionary<string, BasePanel> panels = new Dictionary<string, BasePanel>();

        private void Start()
        {
        }

        private void OnDestroy()
        {
        }

        public void RegisterPanel(string panelName, BasePanel panel)
        {
            if (!panels.ContainsKey(panelName))
            {
                panels.Add(panelName, panel);
            }
        }

        public void ShowPanel(string panelName)
        {
            if (panels.TryGetValue(panelName, out BasePanel panel))
            {
                if (panelStack.Count > 0)
                {
                    panelStack.Peek().Hide();
                }
                panel.Show();
                panelStack.Push(panel);
            }
            else
            {
                Debug.LogWarning($"Panel {panelName} not found!");
            }
        }

        public void GoBack()
        {
            if (panelStack.Count > 0)
            {
                panelStack.Pop().Hide();
                if (panelStack.Count > 0)
                {
                    panelStack.Peek().Show();
                }
            }
        }

        public void ClearAllPanels()
        {
            while (panelStack.Count > 0)
            {
                panelStack.Pop().Hide();
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


    }
} 