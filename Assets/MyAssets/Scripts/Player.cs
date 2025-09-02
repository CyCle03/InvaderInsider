using UnityEngine;
using UnityEngine.UI; // UI 관련 기능을 사용하기 위해 추가
using TMPro; // TextMeshPro 기능을 사용하기 위해 추가
using InvaderInsider.UI; // Changed from InvaderInsider.Managers
using InvaderInsider; // IDamageable 인터페이스 사용을 위해 추가
using InvaderInsider.Managers; // LogManager 사용을 위해 추가
using InvaderInsider.Core; // DebugUtils와 GameConstants 사용을 위해 추가
using System; // Action 델리게이트 사용을 위해 추가
using InvaderInsider.Data;
using InvaderInsider.Cards;

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
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform projectileSpawnPoint;
        [SerializeField] private bool enableTestKeys = true;
        
        #endregion

        #region Runtime State
        
        private UIManager uiManager;
        private bool isOptimizedTargetingEnabled = false;
        
        #endregion

        #region Unity Lifecycle
        
        /// <summary>
        /// Unity Awake - BaseCharacter 초기화 후 플레이어 특화 초기화
        /// </summary>
        protected override void Awake()
        {
            base.Awake(); // BaseCharacter의 Awake 호출
            DebugUtils.Log(GameConstants.LOG_PREFIX_PLAYER, "Player Awake 시작");
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
            
            // BaseCharacter의 Initialize 호출 (이벤트 발생시킴)
            base.Initialize(null);
            
            // 강제 UI 동기화 (안전장치)
            ForceUISync();
            
            DebugUtils.Log(GameConstants.LOG_PREFIX_PLAYER, "Player 초기화 완료");
        }

        /// <summary>
        /// Unity Update - 매 프레임 호출
        /// </summary>
        private void Update()
        {
            FindAndAttackEnemies();

            if (Input.GetMouseButtonDown(0) && GameManager.Instance.SelectedTowerPrefab != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit) && GameManager.Instance.SelectedCardId != -1)
                {
                    CardDBObject cardData = CardManager.Instance.GetCardById(GameManager.Instance.SelectedCardId);
                    if (cardData != null && StageManager.Instance.CreateTower(cardData, hit.point))
                    {
                        CardManager.Instance.RemoveCardFromHand(GameManager.Instance.SelectedCardId);
                        GameManager.Instance.SelectedTowerPrefab = null; // 타워를 생성한 후 선택 해제
                        GameManager.Instance.SelectedCardId = -1; // 선택 카드 ID 초기화
                    }
                }
            }

            // 디버그 키는 에디터에서만 작동하도록 처리
            #if UNITY_EDITOR
            if (enableTestKeys)
            {
                HandleDebugInput();
            }
            #endif
        }
        
        #endregion

        #region Attack Logic

        private IDamageable currentTarget;
        private int enemyLayerMask;
        private readonly Collider[] detectionBuffer = new Collider[50]; // 최대 50개의 적을 감지

        /// <summary>
        /// 플레이어 전용 컴포넌트들을 초기화합니다.
        /// </summary>
        private void InitializePlayerComponents()
        {
            // UIManager 찾기
            uiManager = UIManager.Instance;
            if (uiManager == null)
            {
                DebugUtils.LogWarning(GameConstants.LOG_PREFIX_PLAYER, 
                string.Format(GameConstants.LogMessages.MANAGER_NOT_FOUND, "UIManager"));
            }

            // BottomBarPanel 찾기
            if (bottomBarPanel == null)
            {
                DebugUtils.LogWarning(GameConstants.LOG_PREFIX_PLAYER, 
                string.Format(GameConstants.LogMessages.COMPONENT_NOT_FOUND, "BottomBarPanel"));
            }

            // 공격을 위한 적 레이어 마스크 설정
            enemyLayerMask = LayerMask.GetMask(GameConstants.ENEMY_LAYER_NAME);

            // 이벤트 리스너 설정
            OnHealthChanged += HandleHealthChanged;
            OnDeath += HandlePlayerDeath;
        }

        /// <summary>
        /// 주변의 적을 찾아 공격합니다.
        /// </summary>
        private void FindAndAttackEnemies()
        {
            if (isOptimizedTargetingEnabled) return;
            if (!CanAttack()) return; // BaseCharacter의 공격 속도 체크

            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, AttackRange, detectionBuffer, enemyLayerMask);

            if (hitCount > 0)
            {
                // 가장 가까운 적을 찾습니다.
                float closestDistance = float.MaxValue;
                IDamageable nearestEnemy = null;

                for (int i = 0; i < hitCount; i++)
                {
                    if (detectionBuffer[i].TryGetComponent<IDamageable>(out var enemy))
                    {
                        float distance = Vector3.Distance(transform.position, detectionBuffer[i].transform.position);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            nearestEnemy = enemy;
                        }
                    }
                }

                if (nearestEnemy != null)
                {
                    currentTarget = nearestEnemy;
                    Attack(currentTarget);
                }
            }
        }

        /// <summary>
        /// 플레이어는 직접 공격합니다.
        /// </summary>
        public override void Attack(IDamageable target)
        {
            if (target == null || projectilePrefab == null) return;

            GameObject projectileGO = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.identity);
            Projectile projectile = projectileGO.GetComponent<Projectile>();

            if (projectile != null)
            {
                projectile.Launch(target, AttackDamage);
            }
            else
            {
                Debug.LogError("Projectile prefab is missing the Projectile component.");
                Destroy(projectileGO); // Cleanup if component is missing
            }

            SetNextAttackTime(); // 다음 공격 딜레이 설정

            if (showDebugInfo)
            {
                Debug.Log($"Player attacks {((MonoBehaviour)target).gameObject.name} by launching a projectile.");
            }
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
            DebugUtils.Log(GameConstants.LOG_PREFIX_PLAYER, "플레이어 사망!");
            
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
            
            DebugUtils.Log(GameConstants.LOG_PREFIX_PLAYER, "플레이어 체력 초기화 완료");
        }

        /// <summary>
        /// 데미지를 받습니다. BaseCharacter의 TakeDamage를 오버라이드합니다.
        /// </summary>
        /// <param name="damage">받을 데미지</param>
        public override void TakeDamage(float damage)
        {
            base.TakeDamage(damage); // BaseCharacter의 TakeDamage 호출
            
            DebugUtils.LogFormat(GameConstants.LOG_PREFIX_PLAYER, 
                GameConstants.LogMessages.DAMAGE_RECEIVED, gameObject.name, damage, CurrentHealth, MaxHealth);
        }

        /// <summary>
        /// UI 강제 동기화 (초기화 시 호출)
        /// </summary>
        private void ForceUISync()
        {
            if (!IsInitialized || MaxHealth <= 0)
            {
                DebugUtils.LogWarning(GameConstants.LOG_PREFIX_PLAYER, 
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

            DebugUtils.LogInfo(GameConstants.LOG_PREFIX_PLAYER, 
                $"Player HP 초기화 완료: {CurrentHealth}/{MaxHealth} ({healthRatio:F2})");
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
                DebugUtils.LogFormat(GameConstants.LOG_PREFIX_PLAYER, 
                    "테스트: 체력 {0} 감소 - 현재 체력: {1}/{2}", testDamage, CurrentHealth, MaxHealth);
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                // R 키로 체력 완전 회복 테스트
                ResetHealth();
                DebugUtils.LogFormat(GameConstants.LOG_PREFIX_PLAYER, 
                    "테스트: 체력 완전 회복 - 현재 체력: {0}/{1}", CurrentHealth, MaxHealth);
            }
        }
        #endif
        
        #endregion
    }
} 