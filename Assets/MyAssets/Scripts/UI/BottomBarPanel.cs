using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InvaderInsider.Managers;
using InvaderInsider.Data;
using InvaderInsider.Cards;

namespace InvaderInsider.UI
{
    public class BottomBarPanel : BasePanel
    {
        private const float LOW_HEALTH_THRESHOLD = 0.25f; // 25% 이하 시 경고 표시
        
        private new const string LOG_PREFIX = "[BottomBar] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "Player를 찾을 수 없습니다."
        };

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI enemyRemainText;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TextMeshProUGUI lifeText;

        [Header("Card Hand UI")]
        [SerializeField] private Transform cardHandContainer;
        [SerializeField] private GameObject cardButtonPrefab;

        [Header("UI Elements")]
        [SerializeField] private Image healthFillImage;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI eDataText;
        [SerializeField] private Button pauseButton;
        
        [Header("References")]
        [SerializeField] private Player player; // FindObjectOfType 대신 직접 할당
        
        // 캐시된 참조들
        private GameManager gameManager;
        private ResourceManager resourceManager;

        private bool isInitialized = false;
        
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

            CardManager.Instance.OnHandCardsChanged += UpdateCardHandUI;

            // 게임 오버 버튼 이벤트 설정
            #if UNITY_EDITOR
            Debug.Log($"{LOG_PREFIX}Player 이벤트 리스너 설정 완료");
            #endif
            
            // Health Display 초기화
            InitializeHealthDisplay();
            
            // 추가 동기화 (안전장치)
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
            }
            if (CardManager.Instance != null)
            {
                CardManager.Instance.OnHandCardsChanged -= UpdateCardHandUI;
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
            if (player != null && healthSlider != null && player.MaxHealth > 0)
            {
                float healthRatio = player.CurrentHealth / player.MaxHealth;
                
                // Slider 초기 설정
                healthSlider.minValue = 0f;
                healthSlider.maxValue = 1f;
                healthSlider.value = healthRatio;
                
                UpdateHealthDisplay(healthRatio);
                
                #if UNITY_EDITOR
                Debug.Log($"{LOG_PREFIX}Health Display 초기화: {healthRatio:F2} ({player.CurrentHealth}/{player.MaxHealth})");
                #endif
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogWarning($"{LOG_PREFIX}Health Display 초기화 실패 - Player: {player != null}, Slider: {healthSlider != null}, MaxHealth: {player?.MaxHealth ?? 0}");
                #endif
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

        

        private void UpdateCardHandUI(System.Collections.Generic.List<int> cardIds)
        {
            foreach (Transform child in cardHandContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (int cardId in cardIds)
            {
                CardDBObject cardData = CardManager.Instance.GetCardById(cardId);
                if (cardData != null)
                {
                    GameObject cardButtonObj = Instantiate(cardButtonPrefab, cardHandContainer);
                    CardButton cardButton = cardButtonObj.GetComponent<CardButton>();
                    if (cardButton != null)
                    {
                        cardButton.Initialize(cardData);
                    }

                    cardButtonObj.GetComponent<Button>().onClick.AddListener(() => OnCardButtonClicked(cardData));
                }
            }
        }

        private void OnCardButtonClicked(CardDBObject cardData)
        {
            if (cardData.type == CardType.Tower)
            {
                GameManager.Instance.SelectedTowerPrefab = cardData.cardPrefab;
                CardManager.Instance.RemoveCardFromHand(cardData.cardId);
            }
            // TODO: 다른 카드 타입(예: 스펠)에 대한 처리 추가
        }
    }
} 