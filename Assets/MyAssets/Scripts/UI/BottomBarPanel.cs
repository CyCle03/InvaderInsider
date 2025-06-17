using UnityEngine;
using TMPro;
using UnityEngine.UI;
using InvaderInsider;

namespace InvaderInsider.UI
{
    public class BottomBarPanel : BasePanel
    {
        private const string LOG_PREFIX = "[BottomBar] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "BottomBar: Player instance not found",
            "BottomBar: Player death handled",
            "BottomBar: Health updated - {0}/{1}",
            "BottomBar: Enemy count updated - {0}"
        };

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI enemyRemainText;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TextMeshProUGUI lifeText;

        private Player player;
        private bool isInitialized = false;

        protected override void Initialize()
        {
            if (isInitialized) return;

            if (!ValidateReferences())
            {
                return;
            }

            player = FindObjectOfType<Player>();
            if (player == null)
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[0]);
                return;
            }

            SetupEventListeners();
            UpdateHealthDisplayFromPlayer(player.CurrentHealth / player.MaxHealth);
            UpdateHealth(player.CurrentHealth, player.MaxHealth);

            isInitialized = true;
        }

        private bool ValidateReferences()
        {
            if (enemyRemainText == null || healthSlider == null)
            {
                Debug.LogError(LOG_PREFIX + "필수 UI 요소가 할당되지 않았습니다.");
                return false;
            }
            return true;
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
            if (!isInitialized || player == null) return;

            player.OnHealthChanged += OnPlayerHealthChanged;
            player.OnDeath += OnPlayerDeath;
        }

        private void CleanupEventListeners()
        {
            if (player != null)
            {
                player.OnHealthChanged -= OnPlayerHealthChanged;
                player.OnDeath -= OnPlayerDeath;
            }
        }

        public void UpdateMonsterCountDisplay(int activeCount, int totalCount)
        {
            if (!isInitialized || enemyRemainText == null) return;

            enemyRemainText.text = $"Enemy: {activeCount}/{totalCount}";
        }
        
        // 기존 메서드와의 호환성을 위한 오버로드
        public void UpdateMonsterCountDisplay(int count)
        {
            if (!isInitialized || enemyRemainText == null) return;

            enemyRemainText.text = $"Enemy: {count}";
        }

        private void OnPlayerHealthChanged(float healthRatio)
        {
            if (!isInitialized || player == null) return;

            UpdateHealthDisplayFromPlayer(healthRatio);
            UpdateHealth(player.CurrentHealth, player.MaxHealth);
        }

        public void UpdateHealthDisplay(float healthRatio)
        {
            if (healthSlider != null)
            {
                healthSlider.maxValue = 1.0f; // 0-1 사이의 비율로 설정
                healthSlider.value = healthRatio;
            }
        }

        public void UpdateHealth(float currentHealth, float maxHealth)
        {
            if (lifeText != null)
            {
                lifeText.text = $"HP: {currentHealth:F0}/{maxHealth:F0}";
            }
            
            if (healthSlider != null)
            {
                healthSlider.maxValue = maxHealth;
                healthSlider.value = currentHealth;
            }
        }

        private void UpdateHealthDisplayFromPlayer(float healthRatio)
        {
            if (!isInitialized || player == null) return;
            
            if (healthSlider != null)
            {
                healthSlider.maxValue = player.MaxHealth;
                healthSlider.value = player.CurrentHealth;
            }
        }

        private void OnPlayerDeath()
        {
            if (!isInitialized) return;

            // TODO: 게임 오버 UI 표시 등 추가적인 사망 처리 로직
        }
    }
} 