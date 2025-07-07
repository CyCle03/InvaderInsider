using UnityEngine;
using System;
using InvaderInsider.Data;
using InvaderInsider.Cards;
using InvaderInsider.ScriptableObjects;
using InvaderInsider.Managers;
using InvaderInsider.Managers;

namespace InvaderInsider
{
    /// <summary>
    /// 모든 캐릭터(플레이어, 적, 타워)의 기본 클래스입니다.
    /// 체력, 공격력, 장비 시스템 등의 공통 기능을 제공하며, 이벤트 기반 시스템을 지원합니다.
    /// </summary>
    public abstract class BaseCharacter : MonoBehaviour, IDamageable, IAttacker
    {
        #region Constants & Log Messages
        
        // 공통 메시지는 GameConstants.LogMessages 사용
        
        #endregion

        #region Inspector Fields
        
        [Header("Base Stats")]
        [SerializeField] protected float maxHealth = GameConstants.DEFAULT_MAX_HEALTH;
        [SerializeField] protected float currentHealth;
        [SerializeField] protected float attackDamage = GameConstants.DEFAULT_ATTACK_DAMAGE;
        [SerializeField] protected float baseAttackRange = GameConstants.DEFAULT_ATTACK_RANGE;
        [SerializeField] protected float attackRate = GameConstants.DEFAULT_ATTACK_RATE;
        
        [Header("Debug Info")]
        [SerializeField] protected bool showDebugInfo = false;
        
        #endregion

        #region Runtime State
        
        protected float nextAttackTime;
        private bool _isInitialized = false;
        private GameConfigSO baseConfig;
        
        #endregion

        #region Events
        
        /// <summary>체력이 변경될 때 발생하는 이벤트 (체력 비율 0-1)</summary>
        public event Action<float> OnHealthChanged;
        
        /// <summary>데미지를 받을 때 발생하는 이벤트 (데미지량, 현재 체력, 최대 체력)</summary>
        public event Action<float, float, float> OnDamageReceived;
        
        /// <summary>사망 시 발생하는 이벤트</summary>
        public event Action OnDeath;
        
        /// <summary>장비 적용 시 발생하는 이벤트 (장비 카드)</summary>
        public event Action<CardDBObject> OnEquipmentApplied;
        
        #endregion

        #region Properties
        
        /// <summary>현재 체력</summary>
        public float CurrentHealth => currentHealth;
        
        /// <summary>최대 체력</summary>
        public float MaxHealth => maxHealth;
        
        /// <summary>공격력</summary>
        public float AttackDamage => attackDamage;
        
        /// <summary>공격 사거리 (가상 메서드로 자식 클래스에서 오버라이드 가능)</summary>
        public virtual float AttackRange => baseAttackRange;
        
        /// <summary>초기화 여부</summary>
        protected bool IsInitialized => _isInitialized;
        
        /// <summary>생존 여부</summary>
        public bool IsAlive => _isInitialized && currentHealth > 0f;
        
        /// <summary>체력 비율 (0-1)</summary>
        public float HealthRatio => _isInitialized ? (currentHealth / maxHealth) : 0f;
        
        #endregion

        #region Unity Lifecycle
        
        /// <summary>
        /// Unity Awake 메서드 - 설정 로드 및 기본 초기화
        /// </summary>
        protected virtual void Awake()
        {
            LoadConfig();
            ValidateInspectorValues();
        }
        
        /// <summary>
        /// Unity OnEnable 메서드 - 자식 클래스에서 필요 시 오버라이드
        /// </summary>
        protected virtual void OnEnable()
        {
            // 자식 클래스에서 적절한 타이밍에 Initialize() 호출하도록 함
        }
        
        /// <summary>
        /// Unity OnDisable 메서드 - 이벤트 정리
        /// </summary>
        protected virtual void OnDisable()
        {
            CleanupEventListeners();
        }
        
        /// <summary>
        /// Unity OnDestroy 메서드 - 최종 정리
        /// </summary>
        protected virtual void OnDestroy()
        {
            CleanupEventListeners();
        }
        
        #endregion

        #region Initialization

        /// <summary>
        /// 설정 파일에서 기본값들을 로드합니다.
        /// </summary>
        private void LoadConfig()
        {
            var configManager = ConfigManager.Instance;
            if (configManager != null && configManager.GameConfig != null)
            {
                baseConfig = configManager.GameConfig;
                
                // 기본값을 설정에서 가져오기 (매직 넘버 대신 상수 사용)
                if (Mathf.Approximately(maxHealth, GameConstants.DEFAULT_MAX_HEALTH)) 
                    maxHealth = baseConfig.defaultMaxHealth;
                if (Mathf.Approximately(attackDamage, GameConstants.DEFAULT_ATTACK_DAMAGE)) 
                    attackDamage = baseConfig.defaultAttackDamage;
                if (Mathf.Approximately(baseAttackRange, GameConstants.DEFAULT_ATTACK_RANGE)) 
                    baseAttackRange = baseConfig.defaultAttackRange;
                if (Mathf.Approximately(attackRate, GameConstants.DEFAULT_ATTACK_RATE)) 
                    attackRate = baseConfig.defaultAttackRate;
            }
            else
            {
                LogManager.Error(GameConstants.LOG_PREFIX_GAME, 
                    $"{gameObject.name}: ConfigManager 또는 GameConfig를 찾을 수 없습니다. 기본값을 사용합니다.");
                // 기본값으로 폴백
                baseConfig = ScriptableObject.CreateInstance<GameConfigSO>();
            }
        }

        /// <summary>
        /// Inspector에서 설정된 값들의 유효성을 검증합니다.
        /// </summary>
        private void ValidateInspectorValues()
        {
            maxHealth = Mathf.Max(GameConstants.MIN_MAX_HEALTH_VALUE, maxHealth);
            attackDamage = Mathf.Max(0f, attackDamage);
            baseAttackRange = Mathf.Max(0f, baseAttackRange);
            attackRate = Mathf.Max(0.1f, attackRate);
        }

        /// <summary>
        /// 캐릭터를 초기화합니다. 자식 클래스에서 적절한 타이밍에 호출해야 합니다.
        /// </summary>
        protected virtual void Initialize()
        {
            if (_isInitialized)
            {
                LogManager.Warning(GameConstants.LOG_PREFIX_GAME, 
                    $"{gameObject.name}: 이미 초기화된 캐릭터입니다.");
                return;
            }

            currentHealth = maxHealth;
            _isInitialized = true;
            
            // 초기화 완료 후 체력 변경 이벤트 발생 (UI 동기화)
            OnHealthChanged?.Invoke(HealthRatio);
            
            if (showDebugInfo)
            {
                LogManager.Info(GameConstants.LOG_PREFIX_GAME, 
                    $"{GameConstants.LogMessages.INITIALIZATION_SUCCESS} - 체력: {currentHealth}/{maxHealth}, 공격력: {attackDamage}");
            }
        }
        
        #endregion

        #region Health & Damage System

        /// <summary>
        /// 데미지를 받습니다. IDamageable 인터페이스 구현.
        /// </summary>
        /// <param name="damage">받을 데미지량</param>
        public virtual void TakeDamage(float damage)
        {
            TakeDamageFrom(damage, null);
        }

        /// <summary>
        /// 특정 공격자로부터 데미지를 받습니다.
        /// </summary>
        /// <param name="damage">받을 데미지량</param>
        /// <param name="attacker">공격자 (null 가능)</param>
        public virtual void TakeDamageFrom(float damage, GameObject attacker = null)
        {
            if (!_isInitialized)
            {
                LogManager.Warning(GameConstants.LOG_PREFIX_GAME, 
                    $"{gameObject.name}: 초기화되지 않은 상태에서 데미지를 받았습니다.");
                return;
            }

            if (damage < 0)
            {
                LogManager.Warning(GameConstants.LOG_PREFIX_GAME, 
                    $"{gameObject.name}: 음수 데미지를 받았습니다. 데미지: {damage}");
                return;
            }

            if (!IsAlive)
            {
                return; // 이미 죽은 캐릭터는 추가 데미지를 받지 않음
            }

            float oldHealth = currentHealth;
            float minHealth = baseConfig?.minHealthValue ?? GameConstants.MIN_HEALTH_VALUE;
            currentHealth = Mathf.Max(minHealth, currentHealth - damage);

            // 이벤트 발생
            OnHealthChanged?.Invoke(HealthRatio);
            OnDamageReceived?.Invoke(damage, currentHealth, maxHealth);

            if (showDebugInfo)
            {
                LogManager.Info(GameConstants.LOG_PREFIX_GAME, 
                    $"{gameObject.name}: 데미지 {damage} 받음 - 체력 {oldHealth} → {currentHealth}");
            }

            if (currentHealth <= minHealth)
            {
                Die();
            }
        }

        /// <summary>
        /// 체력을 회복합니다.
        /// </summary>
        /// <param name="healAmount">회복량</param>
        public virtual void Heal(float healAmount)
        {
            if (!_isInitialized || !IsAlive)
            {
                return;
            }

            if (healAmount <= 0)
            {
                return;
            }

            float oldHealth = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);

            OnHealthChanged?.Invoke(HealthRatio);

            if (showDebugInfo)
            {
                LogManager.Info(GameConstants.LOG_PREFIX_GAME, 
                    $"{gameObject.name}: 체력 {healAmount} 회복 - 체력 {oldHealth} → {currentHealth}");
            }
        }

        /// <summary>
        /// 캐릭터 사망 처리를 수행합니다.
        /// </summary>
        protected virtual void Die()
        {
            if (!_isInitialized)
            {
                LogManager.Warning(GameConstants.LOG_PREFIX_GAME, 
                $"{gameObject.name}: 초기화되지 않은 상태에서 사망 처리를 시도했습니다.");
                return;
            }

            if (showDebugInfo)
            {
                LogManager.Info(GameConstants.LOG_PREFIX_GAME, 
                    $"캐릭터 {gameObject.name} 사망 처리 완료");
            }

            OnDeath?.Invoke();
            
            // 자식 클래스에서 오버라이드할 수 있도록 virtual 메서드 호출
            OnBeforeDestroy();
            
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
            else
            {
                LogManager.Error(GameConstants.LOG_PREFIX_GAME, 
                    "GameObject가 null입니다. 사망 처리를 완료할 수 없습니다.");
            }
        }

        /// <summary>
        /// 오브젝트 파괴 직전에 호출되는 메서드입니다. 자식 클래스에서 오버라이드 가능.
        /// </summary>
        protected virtual void OnBeforeDestroy()
        {
            // 자식 클래스에서 필요한 정리 작업 수행
        }
        
        #endregion

        #region Event Management
        
        /// <summary>
        /// 이벤트 리스너들을 정리합니다.
        /// </summary>
        protected virtual void CleanupEventListeners()
        {
            OnHealthChanged = null;
            OnDamageReceived = null;
            OnDeath = null;
            OnEquipmentApplied = null;
        }
        
        #endregion

        #region Combat System

        /// <summary>
        /// 추상 공격 메서드 - 자식 클래스에서 반드시 구현해야 합니다.
        /// </summary>
        /// <param name="target">공격할 대상</param>
        public abstract void Attack(IDamageable target);

        /// <summary>
        /// 공격 가능 여부를 확인합니다.
        /// </summary>
        /// <returns>공격 가능하면 true</returns>
        public virtual bool CanAttack()
        {
            return _isInitialized && IsAlive && Time.time >= nextAttackTime;
        }

        /// <summary>
        /// 다음 공격 시간을 설정합니다.
        /// </summary>
        protected void SetNextAttackTime()
        {
            nextAttackTime = Time.time + attackRate;
        }
        
        #endregion

        #region Utility Methods

        /// <summary>
        /// 체력 변경 이벤트를 수동으로 발생시킵니다.
        /// </summary>
        protected void InvokeHealthChanged()
        {
            if (!_isInitialized)
            {
                LogManager.Warning(GameConstants.LOG_PREFIX_GAME, 
                    $"{gameObject.name}: 초기화되지 않은 상태에서 체력 변경 이벤트를 호출했습니다.");
                return;
            }

            OnHealthChanged?.Invoke(HealthRatio);
        }

        /// <summary>
        /// 레벨업 처리 - 자식 클래스에서 오버라이드 가능
        /// </summary>
        public virtual void LevelUp()
        {
            if (!_isInitialized)
            {
                LogManager.Warning(GameConstants.LOG_PREFIX_GAME, 
                    $"{gameObject.name}: 초기화되지 않은 상태에서 레벨업을 시도했습니다.");
                return;
            }
            
            // 기본 레벨업 로직 - 상속 클래스에서 오버라이드 가능
            if (showDebugInfo)
            {
                LogManager.Info(GameConstants.LOG_PREFIX_GAME, $"{gameObject.name}: 레벨업 완료");
            }
        }
        
        #endregion

        #region Equipment System

        /// <summary>
        /// 장비 카드를 적용하여 캐릭터 스탯을 향상시킵니다.
        /// </summary>
        /// <param name="equipmentCard">적용할 장비 카드</param>
        public virtual void ApplyEquipment(CardDBObject equipmentCard)
        {
            if (!ValidateEquipmentApplication(equipmentCard))
            {
                return;
            }

            // 장비 효과 적용 전 값 저장
            var statsBeforeEquipment = GetCurrentStats();

            // 장비 효과 적용
            ApplyEquipmentEffects(equipmentCard);

            // 변경된 값들 검증 및 보정
            ValidateAndClampStats();

            // 체력 변경 이벤트 발생
            InvokeHealthChanged();

            // 이벤트 발생
            OnEquipmentApplied?.Invoke(equipmentCard);

            // 로그 출력
            LogEquipmentApplication(equipmentCard, statsBeforeEquipment);
        }

        /// <summary>
        /// 장비 적용 유효성을 검사합니다.
        /// </summary>
        private bool ValidateEquipmentApplication(CardDBObject equipmentCard)
        {
            if (!_isInitialized)
            {
                LogManager.Warning(GameConstants.LOG_PREFIX_GAME, 
                    $"{gameObject.name}: 초기화되지 않은 상태에서 장비를 적용하려고 했습니다.");
                return false;
            }

            if (equipmentCard == null)
            {
                LogManager.Error(GameConstants.LOG_PREFIX_GAME, 
                    $"{gameObject.name}: 장비 카드가 null입니다.");
                return false;
            }

            if (equipmentCard.type != CardType.Equipment)
            {
                LogManager.Warning(GameConstants.LOG_PREFIX_GAME, 
                    $"비장비 카드 {equipmentCard.cardName}을(를) {gameObject.name}에게 적용하려고 했습니다");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 장비 효과를 실제로 적용합니다.
        /// </summary>
        private void ApplyEquipmentEffects(CardDBObject equipmentCard)
        {
            attackDamage += equipmentCard.equipmentBonusAttack;
            maxHealth += equipmentCard.equipmentBonusHealth;
            currentHealth += equipmentCard.equipmentBonusHealth;
        }

        /// <summary>
        /// 스탯들을 검증하고 유효한 범위로 제한합니다.
        /// </summary>
        private void ValidateAndClampStats()
        {
            attackDamage = Mathf.Max(0f, attackDamage);
            float minMaxHealth = baseConfig?.minMaxHealthValue ?? GameConstants.MIN_MAX_HEALTH_VALUE;
            float minHealth = baseConfig?.minHealthValue ?? GameConstants.MIN_HEALTH_VALUE;
            maxHealth = Mathf.Max(minMaxHealth, maxHealth);
            currentHealth = Mathf.Max(minHealth, currentHealth);
        }

        /// <summary>
        /// 현재 스탯 정보를 반환합니다.
        /// </summary>
        private (float attack, float maxHp, float currentHp) GetCurrentStats()
        {
            return (attackDamage, maxHealth, currentHealth);
        }

        /// <summary>
        /// 장비 적용 결과를 로그로 출력합니다.
        /// </summary>
        private void LogEquipmentApplication(CardDBObject equipmentCard, (float attack, float maxHp, float currentHp) before)
        {
            if (!showDebugInfo)
            {
                return;
            }

            LogManager.Info(GameConstants.LOG_PREFIX_GAME, 
                $"장비 {equipmentCard.cardName}이(가) {gameObject.name}에게 적용됨. 공격력: {attackDamage}, 최대체력: {maxHealth}");
            
            // 변경사항 상세 로그
            if (equipmentCard.equipmentBonusAttack != 0)
            {
                LogManager.Info(GameConstants.LOG_PREFIX_GAME, 
                    $"{gameObject.name}: 공격력 {before.attack} → {attackDamage} (+{equipmentCard.equipmentBonusAttack})");
            }
            
            if (equipmentCard.equipmentBonusHealth != 0)
            {
                LogManager.Info(GameConstants.LOG_PREFIX_GAME, 
                    $"{gameObject.name}: 최대 체력 {before.maxHp} → {maxHealth} (+{equipmentCard.equipmentBonusHealth})");
            }
        }
        
        #endregion

        #region Debug & Validation

        /// <summary>
        /// 디버그 정보를 출력합니다.
        /// </summary>
        [ContextMenu("Debug Character Info")]
        public void DebugCharacterInfo()
        {
            LogManager.Info(GameConstants.LOG_PREFIX_GAME, 
                $"=== {gameObject.name} 캐릭터 정보 ===\n" +
                $"체력: {currentHealth}/{maxHealth} ({HealthRatio:P1})\n" +
                $"공격력: {attackDamage}\n" +
                $"공격 사거리: {AttackRange}\n" +
                $"공격 속도: {attackRate}\n" +
                $"초기화됨: {_isInitialized}\n" +
                $"생존 상태: {IsAlive}");
        }

        /// <summary>
        /// 에디터에서 값 변경 시 유효성 검증 (Editor Only)
        /// </summary>
        protected virtual void OnValidate()
        {
            #if UNITY_EDITOR
            if (Application.isPlaying)
            {
                ValidateInspectorValues();
            }
            #endif
        }
        
        #endregion
    }
} 