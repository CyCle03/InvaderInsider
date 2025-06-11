using UnityEngine;
using TMPro;
using UnityEngine.UI;
using InvaderInsider; // IDamageable 인터페이스 사용을 위해 추가

namespace InvaderInsider.UI
{
    public class BottomBarPanel : MonoBehaviour
    {
        private const string LOG_PREFIX = "[UI] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "BottomBar: Player instance not found",
            "BottomBar: Player death handled",
            "BottomBar: Health updated - {0}/{1}",
            "BottomBar: Enemy count updated - {0}"
        };

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI healthText; // 플레이어 체력 표시 Text UI
        [SerializeField] private Slider healthSlider; // 플레이어 체력 표시 Slider UI
        [SerializeField] private TextMeshProUGUI enemyRemainText; // 몬스터 수 표시 Text UI

        private Player player;
        private bool isInitialized = false;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (isInitialized) return;

            player = FindObjectOfType<Player>();
            if (player == null)
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[0]);
                return;
            }

            SetupEventListeners();
            UpdateHealthDisplay(player.CurrentHealth / player.MaxHealth);

            isInitialized = true;
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

        // 몬스터 수를 업데이트하는 함수
        public void UpdateMonsterCountDisplay(int count)
        {
            if (!isInitialized || enemyRemainText == null) return;

            if (Application.isPlaying)
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[3], count));
            }
            enemyRemainText.text = $"Enemy: {count}";
        }

        // 플레이어 체력 UI를 업데이트하는 함수 (이벤트 핸들러)
        private void OnPlayerHealthChanged(float healthRatio)
        {
            if (!isInitialized || player == null) return;

            UpdateHealthDisplay(healthRatio);
        }

        private void UpdateHealthDisplay(float healthRatio)
        {
            if (!isInitialized || player == null) return;

            if (healthText != null)
            {
                if (Application.isPlaying)
                {
                    Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[2], player.CurrentHealth, player.MaxHealth));
                }
                healthText.text = $"HP: {player.CurrentHealth}/{player.MaxHealth}";
            }
            if (healthSlider != null)
            {
                healthSlider.maxValue = player.MaxHealth;
                healthSlider.value = player.CurrentHealth;
            }
        }

        // 플레이어 사망 시 호출될 함수
        private void OnPlayerDeath()
        {
            if (!isInitialized) return;

            if (Application.isPlaying)
            {
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[1]);
            }
            // TODO: 게임 오버 UI 표시 등 추가적인 사망 처리 로직
        }
    }
} 