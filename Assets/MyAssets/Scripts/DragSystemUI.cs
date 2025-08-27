using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InvaderInsider
{
    /// <summary>
    /// 드래그 시스템 상태를 보여주는 UI
    /// </summary>
    public class DragSystemUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject statusPanel;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button fixButton;
        [SerializeField] private Button testButton;
        [SerializeField] private Button toggleButton;
        
        [Header("Settings")]
        [SerializeField] private bool showOnStart = false;
        [SerializeField] private float updateInterval = 2f;
        
        private bool isVisible = false;
        
        private void Start()
        {
            SetupUI();
            
            if (showOnStart)
            {
                ShowUI();
            }
            else
            {
                HideUI();
            }
            
            // 정기적으로 상태 업데이트
            InvokeRepeating(nameof(UpdateStatus), 1f, updateInterval);
        }
        
        private void SetupUI()
        {
            // UI가 없으면 런타임에 생성
            if (statusPanel == null)
            {
                CreateRuntimeUI();
            }
            
            // 버튼 이벤트 연결
            if (fixButton != null)
                fixButton.onClick.AddListener(FixAllSystems);
            
            if (testButton != null)
                testButton.onClick.AddListener(RunSystemTest);
            
            if (toggleButton != null)
                toggleButton.onClick.AddListener(ToggleUI);
        }
        
        private void CreateRuntimeUI()
        {
            // Canvas 찾기 또는 생성
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("DragSystemCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            // 상태 패널 생성
            GameObject panelObj = new GameObject("DragSystemStatusPanel");
            panelObj.transform.SetParent(canvas.transform, false);
            
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -10);
            panelRect.sizeDelta = new Vector2(300, 200);
            
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);
            
            statusPanel = panelObj;
            
            // 상태 텍스트 생성
            GameObject textObj = new GameObject("StatusText");
            textObj.transform.SetParent(panelObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 40);
            textRect.offsetMax = new Vector2(-10, -10);
            
            statusText = textObj.AddComponent<TextMeshProUGUI>();
            statusText.text = "시스템 상태 확인 중...";
            statusText.fontSize = 12;
            statusText.color = Color.white;
            
            // 수정 버튼 생성
            CreateButton("FixButton", "시스템 수정", new Vector2(10, 10), new Vector2(80, 25), FixAllSystems);
            
            // 테스트 버튼 생성
            CreateButton("TestButton", "테스트", new Vector2(100, 10), new Vector2(60, 25), RunSystemTest);
            
            // 토글 버튼 생성 (항상 보이는 작은 버튼)
            CreateToggleButton();
        }
        
        private void CreateButton(string name, string text, Vector2 position, Vector2 size, System.Action onClick)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(statusPanel.transform, false);
            
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0, 0);
            buttonRect.anchorMax = new Vector2(0, 0);
            buttonRect.pivot = new Vector2(0, 0);
            buttonRect.anchoredPosition = position;
            buttonRect.sizeDelta = size;
            
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(() => onClick?.Invoke());
            
            // 버튼 텍스트
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = text;
            buttonText.fontSize = 10;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            
            if (name == "FixButton") fixButton = button;
            if (name == "TestButton") testButton = button;
        }
        
        private void CreateToggleButton()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            
            GameObject toggleObj = new GameObject("DragSystemToggle");
            toggleObj.transform.SetParent(canvas.transform, false);
            
            RectTransform toggleRect = toggleObj.AddComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(1, 1);
            toggleRect.anchorMax = new Vector2(1, 1);
            toggleRect.pivot = new Vector2(1, 1);
            toggleRect.anchoredPosition = new Vector2(-10, -10);
            toggleRect.sizeDelta = new Vector2(60, 25);
            
            Image toggleImage = toggleObj.AddComponent<Image>();
            toggleImage.color = new Color(0.1f, 0.3f, 0.1f, 0.8f);
            
            toggleButton = toggleObj.AddComponent<Button>();
            toggleButton.targetGraphic = toggleImage;
            toggleButton.onClick.AddListener(ToggleUI);
            
            // 토글 버튼 텍스트
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(toggleObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI toggleText = textObj.AddComponent<TextMeshProUGUI>();
            toggleText.text = "드래그";
            toggleText.fontSize = 10;
            toggleText.color = Color.white;
            toggleText.alignment = TextAlignmentOptions.Center;
        }
        
        private void UpdateStatus()
        {
            if (statusText == null) return;
            
            // 시스템 상태 확인
            bool dragSystemOK = DragAndMergeSystem.Instance != null;
            bool gameManagerOK = InvaderInsider.Managers.GameManager.Instance != null;
            
            BaseCharacter[] allUnits = FindObjectsOfType<BaseCharacter>();
            SimpleDraggableUnit[] draggables = FindObjectsOfType<SimpleDraggableUnit>();
            SimpleMergeTarget[] mergeTargets = FindObjectsOfType<SimpleMergeTarget>();
            
            int totalUnits = allUnits.Length;
            int draggableUnits = draggables.Length;
            int mergeTargetUnits = mergeTargets.Length;
            
            float completeness = 0f;
            if (totalUnits > 0)
            {
                completeness = (float)(draggableUnits + mergeTargetUnits) / (totalUnits * 2) * 100f;
            }
            
            // 상태 텍스트 업데이트
            string status = $"<b>드래그 시스템 상태</b>\n\n";
            status += $"DragAndMergeSystem: {(dragSystemOK ? "<color=green>✅</color>" : "<color=red>❌</color>")}\n";
            status += $"GameManager: {(gameManagerOK ? "<color=green>✅</color>" : "<color=red>❌</color>")}\n\n";
            status += $"총 유닛: {totalUnits}개\n";
            status += $"드래그 가능: {draggableUnits}개\n";
            status += $"머지 타겟: {mergeTargetUnits}개\n\n";
            
            if (completeness >= 95f)
            {
                status += $"<color=green>완성도: {completeness:F1}% 🎉</color>\n";
            }
            else if (completeness >= 80f)
            {
                status += $"<color=yellow>완성도: {completeness:F1}% ✅</color>\n";
            }
            else
            {
                status += $"<color=red>완성도: {completeness:F1}% ⚠️</color>\n";
            }
            
            // 현재 드래그 상태
            if (DragAndMergeSystem.Instance != null)
            {
                if (DragAndMergeSystem.Instance.IsDragging)
                {
                    status += $"\n<color=cyan>드래그 중: {DragAndMergeSystem.Instance.CurrentDragType}</color>";
                }
            }
            
            statusText.text = status;
        }
        
        private void FixAllSystems()
        {
            Debug.Log("[DragSystemUI] 시스템 수정 실행");
            
            // MasterSystemController를 통한 수정
            if (MasterSystemController.Instance != null)
            {
                MasterSystemController.Instance.ManualSystemInitialize();
            }
            
            // AllInOneFixer를 통한 수정
            AllInOneFixer fixer = FindObjectOfType<AllInOneFixer>();
            if (fixer == null)
            {
                GameObject fixerObj = new GameObject("AllInOneFixer");
                fixer = fixerObj.AddComponent<AllInOneFixer>();
            }
            fixer.FixEverythingNow();
            
            // 즉시 상태 업데이트
            UpdateStatus();
        }
        
        private void RunSystemTest()
        {
            Debug.Log("[DragSystemUI] 시스템 테스트 실행");
            
            // DragSystemTester를 통한 테스트
            DragSystemTester tester = FindObjectOfType<DragSystemTester>();
            if (tester == null)
            {
                GameObject testerObj = new GameObject("DragSystemTester");
                tester = testerObj.AddComponent<DragSystemTester>();
            }
            tester.RunManualTest();
            
            // QuickSystemTest를 통한 테스트
            QuickSystemTest quickTest = FindObjectOfType<QuickSystemTest>();
            if (quickTest == null)
            {
                GameObject quickTestObj = new GameObject("QuickSystemTest");
                quickTest = quickTestObj.AddComponent<QuickSystemTest>();
            }
            quickTest.RunQuickTestManual();
        }
        
        private void ToggleUI()
        {
            if (isVisible)
            {
                HideUI();
            }
            else
            {
                ShowUI();
            }
        }
        
        private void ShowUI()
        {
            if (statusPanel != null)
            {
                statusPanel.SetActive(true);
                isVisible = true;
                UpdateStatus();
            }
        }
        
        private void HideUI()
        {
            if (statusPanel != null)
            {
                statusPanel.SetActive(false);
                isVisible = false;
            }
        }
        
        private void Update()
        {
            // F12 키로 UI 토글
            if (Input.GetKeyDown(KeyCode.F12))
            {
                ToggleUI();
            }
        }
        
        private void OnDestroy()
        {
            if (IsInvoking(nameof(UpdateStatus)))
            {
                CancelInvoke(nameof(UpdateStatus));
            }
        }
    }
}