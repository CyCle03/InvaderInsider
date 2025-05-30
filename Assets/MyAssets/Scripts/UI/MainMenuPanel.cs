using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InvaderInsider.Data;
using InvaderInsider.Managers;
using InvaderInsider.Core;
using UnityEngine.EventSystems;

namespace InvaderInsider.UI
{
    public class MainMenuPanel : BasePanel
    {
        [Header("Main Menu Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button deckButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button settingsButton;

        [Header("Optional Buttons")]
        [SerializeField] private Button achievementsButton;
        [SerializeField] private Button quitButton;

        [Header("Layout Settings")]
        [SerializeField] private float buttonSpacing = 20f;
        [SerializeField] private Vector2 buttonSize = new Vector2(500f, 80f);
        [SerializeField] private Vector2 containerPadding = new Vector2(50f, 50f);

        [Header("Animation")]
        [SerializeField] private Animator menuAnimator;
        private bool hasAnimator;

        private Canvas mainCanvas;
        private GraphicRaycaster graphicRaycaster;
        private RectTransform mainMenuContainer;
        private RectTransform bottomBar;

        protected override void Awake()
        {
            base.Awake();
            panelName = "MainMenu";
            SetupCanvas();
            SetupLayout();
            ValidateComponents();
            Initialize();
        }

        protected void Start()
        {
            // MainMenuPanel specific Start logic can go here if needed
        }

        private void SetupCanvas()
        {
            // Canvas 설정
            mainCanvas = GetComponent<Canvas>();
            if (mainCanvas == null)
            {
                mainCanvas = GetComponentInParent<Canvas>();
                if (mainCanvas == null)
                {
                    Debug.LogError($"[{gameObject.name}] No Canvas found in hierarchy!");
                    return;
                }
            }

            // GraphicRaycaster 자동 추가
            graphicRaycaster = mainCanvas.GetComponent<GraphicRaycaster>();
            if (graphicRaycaster == null)
            {
                Debug.Log($"[{gameObject.name}] Adding GraphicRaycaster to Canvas");
                graphicRaycaster = mainCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            // Canvas 설정 검증
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Debug.Log($"[{gameObject.name}] Canvas Render Mode set to: {mainCanvas.renderMode}");
            Debug.Log($"[{gameObject.name}] Canvas Sort Order: {mainCanvas.sortingOrder}");
            
            var scaler = mainCanvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 1f;
                Debug.Log($"[{gameObject.name}] Canvas Scaler configured for 1920x1080");
            }
        }

        private void SetupLayout()
        {
            // 메인 메뉴 컨테이너 찾기
            mainMenuContainer = transform.Find("MainMenuContainer") as RectTransform;
            if (mainMenuContainer == null)
            {
                Debug.LogError("MainMenuContainer not found!");
                return;
            }

            // 하단 바 찾기
            bottomBar = transform.Find("BottomBar") as RectTransform;
            if (bottomBar == null)
            {
                Debug.LogError("BottomBar not found!");
                return;
            }

            // 메인 메뉴 버튼들 위치 설정
            float currentY = -containerPadding.y;
            SetButtonPosition(playButton, ref currentY);
            SetButtonPosition(deckButton, ref currentY);
            SetButtonPosition(shopButton, ref currentY);
            SetButtonPosition(settingsButton, ref currentY);

            // 하단 바 버튼들 위치 설정
            float bottomBarWidth = ((RectTransform)bottomBar).rect.width;
            float bottomButtonX = -bottomBarWidth / 4f;
            if (achievementsButton != null)
            {
                SetBottomButtonPosition(achievementsButton, bottomButtonX);
                bottomButtonX += bottomBarWidth / 2f;
            }
            if (quitButton != null)
            {
                SetBottomButtonPosition(quitButton, bottomButtonX);
            }
        }

        private void SetButtonPosition(Button button, ref float currentY)
        {
            if (button != null)
            {
                RectTransform rectTransform = button.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 1f);
                rectTransform.anchorMax = new Vector2(0.5f, 1f);
                rectTransform.sizeDelta = buttonSize;
                rectTransform.anchoredPosition = new Vector2(0f, currentY);
                currentY -= (buttonSize.y + buttonSpacing);
            }
        }

        private void SetBottomButtonPosition(Button button, float xPosition)
        {
            RectTransform rectTransform = button.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = buttonSize;
            rectTransform.anchoredPosition = new Vector2(xPosition, 0f);
        }

        private void ValidateComponents()
        {
            // EventSystem 체크
            if (EventSystem.current == null)
            {
                Debug.LogError("No EventSystem found in the scene!");
            }
            else
            {
                Debug.Log($"[{gameObject.name}] Using EventSystem: {EventSystem.current.gameObject.name}");
            }

            // 필수 버튼 컴포넌트 체크
            ValidateButton(playButton, "Play");
            ValidateButton(deckButton, "Deck");
            ValidateButton(shopButton, "Shop");
            ValidateButton(settingsButton, "Settings");

            // 선택적 버튼 컴포넌트 체크
            if (achievementsButton != null) ValidateButton(achievementsButton, "Achievements");
            if (quitButton != null) ValidateButton(quitButton, "Quit");
        }

        private void ValidateButton(Button button, string buttonName)
        {
            if (button == null)
            {
                Debug.LogError($"{buttonName} button is missing!");
                return;
            }

            // RectTransform 체크
            RectTransform rectTransform = button.GetComponent<RectTransform>();
            Debug.Log($"[{buttonName}] Position: {rectTransform.anchoredPosition}, Size: {rectTransform.sizeDelta}");
            
            // Image 컴포넌트 체크
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage == null)
            {
                Debug.LogError($"{buttonName} button is missing Image component!");
                return;
            }

            // 알파값 체크
            if (buttonImage.color.a == 0)
            {
                Debug.LogWarning($"{buttonName} button image has zero alpha!");
            }

            // Raycast Target 체크
            if (!buttonImage.raycastTarget)
            {
                Debug.LogWarning($"{buttonName} button has Raycast Target disabled!");
                buttonImage.raycastTarget = true; // 자동으로 활성화
            }

            // Interactable 체크
            if (!button.interactable)
            {
                Debug.LogWarning($"{buttonName} button is not interactable!");
                button.interactable = true; // 자동으로 활성화
            }

            // 계층구조 체크
            Debug.Log($"[{buttonName}] Hierarchy path: {GetGameObjectPath(button.gameObject)}");
        }

        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        protected override void Initialize()
        {
            Debug.Log($"[{gameObject.name}] Initializing buttons");
            SetupButtons();
            hasAnimator = menuAnimator != null;
        }

        private void SetupButtons()
        {
            // 필수 버튼들
            if (playButton != null)
            {
                playButton.onClick.AddListener(OnPlayButtonClicked);
                Debug.Log("Play button initialized");
            }
            
            if (deckButton != null)
            {
                deckButton.onClick.AddListener(OnDeckButtonClicked);
                Debug.Log("Deck button initialized");
            }
            
            if (shopButton != null)
            {
                shopButton.onClick.AddListener(OnShopButtonClicked);
                Debug.Log("Shop button initialized");
            }
            
            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsButtonClicked);
                Debug.Log("Settings button initialized");
            }

            // 선택적 버튼들
            if (achievementsButton != null)
            {
                achievementsButton.onClick.AddListener(OnAchievementsButtonClicked);
                Debug.Log("Achievements button initialized");
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitButtonClicked);
                Debug.Log("Quit button initialized");
            }
        }

        #region Button Click Handlers
        private void OnPlayButtonClicked()
        {
            Debug.Log("Play button clicked");
            Hide();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.CurrentGameState = GameState.Playing;
            }
            else
            {
                Debug.LogError("GameManager not found in scene!");
            }

            Time.timeScale = 1f;
            Debug.Log($"Time.timeScale set to: {Time.timeScale} in OnPlayButtonClicked");

            // Initialize and start the stage
            if (StageManager.Instance != null)
            {
                // Assuming InitializeStage() is now public or internal and accessible
                StageManager.Instance.InitializeStage(); // Call the initialization method
            }
            else
            {
                Debug.LogError("StageManager not found in scene!");
            }
        }

        private void OnDeckButtonClicked()
        {
            Debug.Log("Deck button clicked");
            if (UIManager.Instance == null)
            {
                Debug.LogError("UIManager instance is null!");
                return;
            }
            UIManager.Instance.ShowPanel("Deck");
        }

        private void OnShopButtonClicked()
        {
            Debug.Log("Shop button clicked");
            if (UIManager.Instance == null)
            {
                Debug.LogError("UIManager instance is null!");
                return;
            }
            UIManager.Instance.ShowPanel("Shop");
        }

        private void OnSettingsButtonClicked()
        {
            Debug.Log("Settings button clicked");
            if (UIManager.Instance == null)
            {
                Debug.LogError("UIManager instance is null!");
                return;
            }
            UIManager.Instance.ShowPanel("Settings");
        }

        private void OnAchievementsButtonClicked()
        {
            Debug.Log("Achievements button clicked");
            if (UIManager.Instance == null)
            {
                Debug.LogError("UIManager instance is null!");
                return;
            }
            UIManager.Instance.ShowPanel("Achievements");
        }

        private void OnQuitButtonClicked()
        {
            Debug.Log("Quit button clicked");
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        #endregion

        protected override void OnShow()
        {
            base.OnShow();
            GameManager.Instance.CurrentGameState = GameState.MainMenu;
            if (hasAnimator)
                menuAnimator.SetTrigger("FadeIn");

            // 버튼 상태 재검증
            ValidateComponents();
        }

        protected override void OnHide()
        {
            base.OnHide();
            if (hasAnimator)
                menuAnimator.SetTrigger("FadeOut");
        }

        private void OnDestroy()
        {
            // 버튼 리스너 제거
            if (playButton != null) playButton.onClick.RemoveAllListeners();
            if (deckButton != null) deckButton.onClick.RemoveAllListeners();
            if (shopButton != null) shopButton.onClick.RemoveAllListeners();
            if (settingsButton != null) settingsButton.onClick.RemoveAllListeners();
            if (achievementsButton != null) achievementsButton.onClick.RemoveAllListeners();
            if (quitButton != null) quitButton.onClick.RemoveAllListeners();
        }

        private void Update()
        {
            // 마우스 클릭 시 레이캐스트 디버그
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mousePosition = Input.mousePosition;
                PointerEventData eventData = new PointerEventData(EventSystem.current);
                eventData.position = mousePosition;

                var results = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);

                Debug.Log($"Mouse Position: {mousePosition}");
                Debug.Log($"Canvas RenderMode: {mainCanvas.renderMode}, Camera: {mainCanvas.worldCamera}");
                
                if (results.Count > 0)
                {
                    foreach (var result in results)
                    {
                        Debug.Log($"Raycast hit: {GetGameObjectPath(result.gameObject)} (Layer: {result.gameObject.layer})");
                    }
                }
                else
                {
                    Debug.Log("No UI elements hit");
                }
            }
        }
    }
} 