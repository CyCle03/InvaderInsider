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
            #if UNITY_EDITOR
            Debug.Log($"{LOG_PREFIX}Player Awake 시작");
            #endif
            
            Initialize();
            
            #if UNITY_EDITOR
            Debug.Log($"{LOG_PREFIX}Player Awake 완료");
            #endif
        }

        private void Initialize()
        {
            if (isInitialized) return;

            // UIManager를 통해 UI 참조 가져오기 (태그 의존성 제거)
            uiManager = UIManager.Instance;
            if (uiManager != null)
            {
                // UIManager를 통해 BottomBarPanel 찾기 시도
                var panels = FindObjectsOfType<BottomBarPanel>(true);
                bottomBarPanel = panels.Length > 0 ? panels[0] : null;
            }
            else
            {
                // UIManager가 없을 경우 직접 찾기
                bottomBarPanel = FindObjectOfType<BottomBarPanel>(true);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (bottomBarPanel == null && Application.isPlaying)
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[2]);
            }

            if (uiManager == null && Application.isPlaying)
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[3]);
            }
#endif

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

        private void Start()
        {
            // 게임 씬 시작 시 체력 재설정 (다른 컴포넌트들이 준비된 후)
            if (isInitialized)
            {
                ResetHealth();
                #if UNITY_EDITOR
                Debug.Log($"{LOG_PREFIX}Start에서 체력 재설정 완료");
                #endif
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
            currentHealth = maxHealth;
            
            #if UNITY_EDITOR
            Debug.Log($"{LOG_PREFIX}체력 초기화: {currentHealth}/{maxHealth}");
            #endif
            
            // 이벤트 발생 (초기화 상태와 무관하게)
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

        #if UNITY_EDITOR
        private void Update()
        {
            // 에디터에서만 작동하는 테스트 키
            if (Input.GetKeyDown(KeyCode.H))
            {
                // H 키로 체력 10 감소 테스트
                TakeDamage(10f);
                Debug.Log($"{LOG_PREFIX}테스트: 체력 10 감소 - 현재 체력: {currentHealth}/{maxHealth}");
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                // R 키로 체력 완전 회복 테스트
                ResetHealth();
                Debug.Log($"{LOG_PREFIX}테스트: 체력 완전 회복 - 현재 체력: {currentHealth}/{maxHealth}");
            }
        }
        #endif
    }
} 