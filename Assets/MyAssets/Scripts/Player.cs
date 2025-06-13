using UnityEngine;
using UnityEngine.UI; // UI 관련 기능을 사용하기 위해 추가
using TMPro; // TextMeshPro 기능을 사용하기 위해 추가
using InvaderInsider.UI; // Changed from InvaderInsider.Managers
using InvaderInsider; // IDamageable 인터페이스 사용을 위해 추가
using System; // Action 델리게이트 사용을 위해 추가

namespace InvaderInsider
{
    public class Player : MonoBehaviour, IDamageable
    {
        private const string LOG_PREFIX = "[Player] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "Player health reset",
            "Player Died!",
            "BottomBarPanel instance not found",
            "UIManager instance not found"
        };

        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        private float currentHealth;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI healthText;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;

        public event Action<float> OnHealthChanged;
        public event Action OnDeath;

        private BottomBarPanel bottomBarPanel;
        private UIManager uiManager;
        private bool isInitialized = false;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (isInitialized) return;

            bottomBarPanel = FindObjectOfType<BottomBarPanel>();
            if (bottomBarPanel == null && Application.isPlaying)
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[2]);
                #endif
            }

            uiManager = UIManager.Instance;
            if (uiManager == null && Application.isPlaying)
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[3]);
                #endif
            }

            ResetHealth();
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

        private void CleanupEventListeners()
        {
            OnHealthChanged = null;
            OnDeath = null;
        }

        public void ResetHealth()
        {
            if (!isInitialized) return;

            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth / maxHealth);
        }

        public void TakeDamage(float damageAmount)
        {
            if (!isInitialized) return;

            currentHealth = Mathf.Max(0, currentHealth - damageAmount);
            OnHealthChanged?.Invoke(currentHealth / maxHealth);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (!isInitialized) return;

            OnDeath?.Invoke();

            if (uiManager != null)
            {
                uiManager.ShowPanel("MainMenu");
            }
        }
    }
} 