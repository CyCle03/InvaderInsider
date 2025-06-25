using UnityEngine;
using TMPro;
using UnityEngine.UI;
using InvaderInsider.Managers;

namespace InvaderInsider.UI
{
    public class BottomBarPanel : BasePanel
    {
        private const float LOW_HEALTH_THRESHOLD = 0.25f; // 25% 이하 시 경고 표시
        
        private const string LOG_PREFIX = "[BottomBar] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "Player를 찾을 수 없습니다."
        };

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI enemyRemainText;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TextMeshProUGUI lifeText;
        
        [Header("Game Over UI")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private TextMeshProUGUI gameOverMessageText;

        private Player player;
        private bool isInitialized = false;
        private bool isGameOverUIVisible = false;
        
        // 런타임에 찾을 UI 요소들
        private Image healthFillImage;
        
        // 색상 캐싱
        private static readonly Color normalHealthColor = Color.green;
        private static readonly Color lowHealthColor = Color.red;
        private static readonly Color criticalHealthColor = new Color(1f, 0.2f, 0.2f, 1f);

        protected override void Initialize()
        {
            #if UNITY_EDITOR
            Debug.Log($"{LOG_PREFIX}BottomBarPanel Initialize 시작");
            #endif
            
            if (isInitialized) return;

            if (!ValidateReferences())
            {
                #if UNITY_EDITOR
                Debug.LogError($"{LOG_PREFIX}UI 요소 검증 실패로 인해 초기화가 중단되었습니다.");
                #endif
                return;
            }

            // Player 찾기 - 여러 방법으로 시도
            player = FindObjectOfType<Player>();
            if (player == null)
            {
                // Player 태그로도 찾아보기
                GameObject playerObject = GameObject.FindWithTag("Player");
                if (playerObject != null)
                {
                    player = playerObject.GetComponent<Player>();
                }
            }

            if (player == null)
            {
                #if UNITY_EDITOR
                Debug.LogError($"{LOG_PREFIX}Player 컴포넌트를 찾을 수 없습니다. Player 오브젝트가 씬에 있는지 확인해주세요.");
                Debug.LogError($"{LOG_PREFIX}Player 오브젝트에 Player 컴포넌트가 있는지, 또는 'Player' 태그가 설정되어 있는지 확인해주세요.");
                #endif
                return;
            }

            #if UNITY_EDITOR
            Debug.Log($"{LOG_PREFIX}Player를 찾았습니다: {player.gameObject.name}");
            #endif

            // healthSlider에서 Fill Image 자동 찾기
            if (healthSlider != null && healthSlider.fillRect != null)
            {
                healthFillImage = healthSlider.fillRect.GetComponent<Image>();
                
                #if UNITY_EDITOR
                if (healthFillImage != null)
                {
                    Debug.Log($"{LOG_PREFIX}Health Fill Image를 자동으로 찾았습니다: {healthFillImage.name}");
                }
                else
                {
                    Debug.LogWarning($"{LOG_PREFIX}Health Slider의 Fill Rect에서 Image 컴포넌트를 찾을 수 없습니다.");
                }
                #endif
            }

            // Canvas Sorting Order 설정 (다른 UI보다 낮게)
            SetupCanvasSortingOrder();

            SetupEventListeners();
            InitializeHealthDisplay();

            isInitialized = true;
            
            #if UNITY_EDITOR
            Debug.Log($"{LOG_PREFIX}BottomBarPanel 초기화 완료");
            #endif
        }

        private bool ValidateReferences()
        {
            bool isValid = true;
            string missingElements = "";

            // 필수 UI 요소들 체크
            if (enemyRemainText == null)
            {
                missingElements += "enemyRemainText, ";
                isValid = false;
            }

            if (healthSlider == null)
            {
                missingElements += "healthSlider, ";
                isValid = false;
            }

            if (lifeText == null)
            {
                missingElements += "lifeText, ";
                isValid = false;
            }

            if (!isValid)
            {
                missingElements = missingElements.TrimEnd(' ', ',');
                #if UNITY_EDITOR
                Debug.LogError($"{LOG_PREFIX}다음 필수 UI 요소가 할당되지 않았습니다: {missingElements}");
                Debug.LogError($"{LOG_PREFIX}BottomBarPanel의 Inspector에서 UI 요소들을 할당해주세요.");
                #endif
            }

            // 선택적 UI 요소들에 대한 경고
            #if UNITY_EDITOR
            if (gameOverPanel == null)
            {
                Debug.LogWarning($"{LOG_PREFIX}gameOverPanel이 할당되지 않았습니다. 게임 오버 UI가 표시되지 않습니다.");
            }
            #endif

            return isValid;
        }

        private void OnEnable()
        {
            if (!isInitialized)
            {
                Initialize();
            }
        }

        private void OnDisable()
        {
            CleanupEventListeners();
        }

        private void OnDestroy()
        {
            CleanupEventListeners();
        }

        private void SetupEventListeners()
        {
            if (player == null) return;

            player.OnHealthChanged += UpdateHealthDisplay;
            player.OnDeath += HandlePlayerDeath;

            // 게임 오버 버튼 이벤트 설정
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartButtonClicked);
            
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
                
            #if UNITY_EDITOR
            Debug.Log($"{LOG_PREFIX}Player 이벤트 리스너 설정 완료");
            #endif
            
            // 이벤트 구독 후 현재 Player 상태로 즉시 동기화
            if (player.MaxHealth > 0)
            {
                float currentHealthRatio = player.CurrentHealth / player.MaxHealth;
                UpdateHealthDisplay(currentHealthRatio);
                
                #if UNITY_EDITOR
                Debug.Log($"{LOG_PREFIX}Player 체력 강제 동기화: {player.CurrentHealth}/{player.MaxHealth} ({currentHealthRatio:F2})");
                #endif
            }
        }

        private void CleanupEventListeners()
        {
            if (player != null)
            {
                player.OnHealthChanged -= UpdateHealthDisplay;
                player.OnDeath -= HandlePlayerDeath;
            }
        }

        public void UpdateMonsterCountDisplay(int activeCount, int totalCount)
        {
            if (enemyRemainText == null) return;

            enemyRemainText.text = $"Active Enemy: {activeCount}";
        }
        
        // 기존 메서드와의 호환성을 위한 오버로드
        public void UpdateMonsterCountDisplay(int count)
        {
            if (enemyRemainText == null) return;

            enemyRemainText.text = $"Active Enemy: {count}";
        }

        private void InitializeHealthDisplay()
        {
            if (player != null && healthSlider != null)
            {
                UpdateHealthDisplay(player.CurrentHealth / player.MaxHealth);
            }
        }

        public void UpdateHealthDisplay(float healthPercentage)
        {
            // 기본 요소들만 체크 (isInitialized 체크 제거)
            if (player == null) return;

            #if UNITY_EDITOR
            Debug.Log($"{LOG_PREFIX}체력 업데이트: {healthPercentage:F2} ({Mathf.RoundToInt(player?.CurrentHealth ?? 0)}/{Mathf.RoundToInt(player?.MaxHealth ?? 0)})");
            #endif

            // 슬라이더 업데이트
            if (healthSlider != null)
            {
                healthSlider.value = healthPercentage;
            }

            // 텍스트 업데이트
            if (lifeText != null && player != null)
            {
                lifeText.text = $"{Mathf.RoundToInt(player.CurrentHealth)}/{Mathf.RoundToInt(player.MaxHealth)}";
            }

            // 체력에 따른 색상 변경
            UpdateHealthColor(healthPercentage);
        }

        private void UpdateHealthColor(float healthPercentage)
        {
            if (healthFillImage == null) return;

            Color targetColor;
            if (healthPercentage > 0.5f)
                targetColor = normalHealthColor;
            else if (healthPercentage > LOW_HEALTH_THRESHOLD)
                targetColor = Color.yellow;
            else
                targetColor = healthPercentage > 0.1f ? lowHealthColor : criticalHealthColor;

            healthFillImage.color = targetColor;
        }

        private void HandlePlayerDeath()
        {
            if (!isInitialized || isGameOverUIVisible) return;

            isGameOverUIVisible = true;
            
            // 게임 일시 정지
            Time.timeScale = 0f;
            
            // 게임 오버 UI 표시 (GameState를 통해 간접적으로)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameState(GameState.GameOver);
            }
        }

        private void ShowGameOverUI()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                
                // 게임 오버 메시지 설정
                if (gameOverMessageText != null)
                {
                    gameOverMessageText.text = "게임 오버\n다시 시도하시겠습니까?";
                }
            }
        }

        private void HideGameOverUI()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
            isGameOverUIVisible = false;
        }

        private void OnRestartButtonClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartNewGame();
            }
        }

        private void OnMainMenuButtonClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadMainMenuScene();
            }
        }

        private void SetupCanvasSortingOrder()
        {
            // BottomBar의 Canvas를 확인하고 없으면 추가
            Canvas bottomBarCanvas = GetComponent<Canvas>();
            if (bottomBarCanvas == null)
            {
                bottomBarCanvas = gameObject.AddComponent<Canvas>();
                
                // GraphicRaycaster도 필요
                if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                {
                    gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                }
            }
            
            // Sorting Order를 낮게 설정하여 다른 UI 아래에 표시
            bottomBarCanvas.overrideSorting = true;
            bottomBarCanvas.sortingOrder = 10; // 기본보다 낮은 값 (SummonChoice는 100)
            
            // GraphicRaycaster의 우선순위를 낮춰서 다른 UI가 우선되도록 설정
            UnityEngine.UI.GraphicRaycaster graphicRaycaster = GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (graphicRaycaster != null)
            {
                // Raycast Priority를 낮게 설정 (기본값 0보다 낮음)
                // 이렇게 하면 다른 UI 요소들이 우선적으로 클릭 이벤트를 받습니다
                graphicRaycaster.blockingObjects = UnityEngine.UI.GraphicRaycaster.BlockingObjects.None;
            }
            
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + "BottomBar Canvas Sorting Order 설정 완료: " + bottomBarCanvas.sortingOrder);
            #endif
        }
    }
} 