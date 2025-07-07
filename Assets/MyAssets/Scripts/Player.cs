using UnityEngine;
using UnityEngine.UI; // UI 관련 기능을 사용하기 위해 추가
using TMPro; // TextMeshPro 기능을 사용하기 위해 추가
using InvaderInsider.UI; // Changed from InvaderInsider.Managers
using InvaderInsider; // IDamageable 인터페이스 사용을 위해 추가
using InvaderInsider.Managers;
using System; // Action 델리게이트 사용을 위해 추가

namespace InvaderInsider
{
    /// <summary>
    /// 플레이어 클래스 - BaseCharacter를 상속받아 일관된 캐릭터 시스템을 구현합니다.
    /// </summary>
    public class Player : BaseCharacter
    {
        #region Constants & Log Messages
        
        // 공통 메시지는 GameConstants.LogMessages 사용
        
        #endregion

        #region Inspector Fields
        
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private BottomBarPanel bottomBarPanel; // FindObjectOfType 대신 직접 할당
        
        [Header("Player Settings")]
        [SerializeField] private bool enableTestKeys = true;
        
        #endregion

        #region Runtime State
        
        private UIManager uiManager;
        
        #endregion

        #region Unity Lifecycle
        
        /// <summary>
        /// Unity Awake - BaseCharacter 초기화 후 플레이어 특화 초기화
        /// </summary>
        protected override void Awake()
        {
            base.Awake(); // BaseCharacter의 Awake 호출
            LogManager.Info(GameConstants.LOG_PREFIX_PLAYER, "Player Awake 시작");
        }

        /// <summary>
        /// Unity OnEnable - 활성화 시 초기화
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            InitializePlayerComponents();
        }

        /// <summary>
        /// Unity Start - 게임 시작 시 추가 설정
        /// </summary>
        private void Start()
        {
            // 플레이어 특화 초기화 (이벤트 리스너 먼저 설정)
            InitializePlayerComponents();
            SetupEventListeners();
            
            // BaseCharacter의 Initialize 호출 (이벤트 발생시킴)
            base.Initialize();
            
            // 강제 UI 동기화 (안전장치)
            ForceUISync();
            
            LogManager.Info(GameConstants.LOG_PREFIX_PLAYER, "Player 초기화 완료");
        }

        /// <summary>
        /// Unity Update - 디버그 키 처리 (에디터 전용)
        /// </summary>
        #if UNITY_EDITOR
        private void Update()
        {
            if (!enableTestKeys || !Application.isEditor)
            {
                return;
            }

            HandleDebugInput();
        }
        #endif
        
        #endregion

        #region Initialization
        
        /// <summary>
        /// 플레이어 전용 컴포넌트들을 초기화합니다.
        /// </summary>
        private void InitializePlayerComponents()
        {
            // UIManager 찾기
            uiManager = UIManager.Instance;
            if (uiManager == null)
            {
                LogManager.Warning(GameConstants.LOG_PREFIX_PLAYER, 
                $"매니저를 찾을 수 없습니다: UIManager");
            }

            // BottomBarPanel 찾기
            if (bottomBarPanel == null)
            {
                LogManager.Warning(GameConstants.LOG_PREFIX_PLAYER, 
                $"컴포넌트를 찾을 수 없습니다: BottomBarPanel");
            }
        }

        /// <summary>
        /// 이벤트 리스너들을 설정합니다.
        /// </summary>
        private void SetupEventListeners()
        {
            // BaseCharacter의 이벤트 구독
            OnHealthChanged += HandleHealthChanged;
            OnDeath += HandlePlayerDeath;
        }
        
        #endregion

        #region Event Handlers
        
        /// <summary>
        /// 체력 변경 이벤트 처리
        /// </summary>
        /// <param name="healthRatio">체력 비율 (0-1)</param>
        private void HandleHealthChanged(float healthRatio)
        {
            // UI 업데이트
            if (healthText != null)
            {
                healthText.text = $"{CurrentHealth:F0}/{MaxHealth:F0}";
            }

            // BottomBarPanel 업데이트
            if (bottomBarPanel != null)
            {
                bottomBarPanel.UpdateHealthDisplay(healthRatio);
            }
        }

        /// <summary>
        /// 플레이어 사망 이벤트 처리
        /// </summary>
        private void HandlePlayerDeath()
        {
            LogManager.Info(GameConstants.LOG_PREFIX_PLAYER, "플레이어 사망!");
            
            // GameManager에게 게임 종료 알림 (PausePanel과 함께 게임 오버 처리)
            var gameManager = InvaderInsider.Managers.GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.GameOver();
            }
            else if (uiManager != null)
            {
                // GameManager가 없으면 직접 메인 메뉴로
                uiManager.ShowPanel("MainMenu");
            }
        }
        
        #endregion

        #region Player Specific Methods
        
        /// <summary>
        /// 플레이어 체력을 완전히 회복합니다.
        /// </summary>
        public void ResetHealth()
        {
            Heal(MaxHealth); // BaseCharacter의 Heal 메서드 사용
            
            LogManager.Info(GameConstants.LOG_PREFIX_PLAYER, "플레이어 체력 초기화 완료");
        }

        /// <summary>
        /// 데미지를 받습니다. BaseCharacter의 TakeDamage를 오버라이드합니다.
        /// </summary>
        /// <param name="damage">받을 데미지</param>
        public override void TakeDamage(float damage)
        {
            base.TakeDamage(damage); // BaseCharacter의 TakeDamage 호출
            
            LogManager.Info(GameConstants.LOG_PREFIX_PLAYER, 
                $"{GameConstants.LogMessages.DAMAGE_RECEIVED}");
        }

        /// <summary>
        /// UI 강제 동기화 (초기화 시 호출)
        /// </summary>
        private void ForceUISync()
        {
            if (!IsInitialized || MaxHealth <= 0)
            {
                LogManager.Warning(GameConstants.LOG_PREFIX_PLAYER, 
                    "Player가 초기화되지 않았거나 MaxHealth가 0입니다.");
                return;
            }

            float healthRatio = HealthRatio;
            
            // UI 직접 업데이트
            if (healthText != null)
            {
                healthText.text = $"{CurrentHealth:F0}/{MaxHealth:F0}";
            }

            if (bottomBarPanel != null)
            {
                bottomBarPanel.UpdateHealthDisplay(healthRatio);
            }

            LogManager.Info(GameConstants.LOG_PREFIX_PLAYER, 
                $"Player HP 초기화 완료: {CurrentHealth}/{MaxHealth} ({healthRatio:F2})");
        }

        /// <summary>
        /// 플레이어는 공격하지 않으므로 빈 구현
        /// </summary>
        /// <param name="target">공격할 대상</param>
        public override void Attack(IDamageable target)
        {
            // 플레이어는 직접 공격하지 않음
            LogManager.Warning(GameConstants.LOG_PREFIX_PLAYER, 
                "플레이어는 직접 공격할 수 없습니다.");
        }
        
        #endregion

        #region Debug Methods
        
        /// <summary>
        /// 디버그 입력을 처리합니다. (에디터 전용)
        /// </summary>
        #if UNITY_EDITOR
        private void HandleDebugInput()
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                // H 키로 체력 10 감소 테스트
                const float testDamage = 10f;
                TakeDamage(testDamage);
                LogManager.Info(GameConstants.LOG_PREFIX_PLAYER, 
                    $"테스트: 체력 {testDamage} 감소 - 현재 체력: {CurrentHealth}/{MaxHealth}");
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                // R 키로 체력 완전 회복 테스트
                ResetHealth();
                LogManager.Info(GameConstants.LOG_PREFIX_PLAYER, 
                    $"테스트: 체력 완전 회복 - 현재 체력: {CurrentHealth}/{MaxHealth}");
            }
        }
        #endif
        
        #endregion
    }
} 