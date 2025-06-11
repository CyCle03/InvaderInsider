using UnityEngine;
using UnityEngine.UI;
using InvaderInsider;

namespace InvaderInsider.UI
{
    public class EnemyHealthBarUI : MonoBehaviour
    {
        private const string LOG_PREFIX = "[UI] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "EnemyHealth: BaseCharacter component not found - {0}",
            "EnemyHealth: Health updated - {0}/{1}",
            "EnemyHealth: Health bar visibility changed - {0}"
        };

        [SerializeField] private Slider healthSlider; // 체력 슬라이더 UI
        [SerializeField] private GameObject healthBarObject; // 체력바 전체 오브젝트 (활성화/비활성화용)

        private BaseCharacter character; // 체력 정보를 가져올 BaseCharacter 참조
        private bool isInitialized = false;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (isInitialized) return;

            character = GetComponentInParent<BaseCharacter>(); // 부모 또는 자신에게서 BaseCharacter 찾기
            if (character == null)
            {
                if (Application.isPlaying)
                {
                    Debug.LogError(string.Format(LOG_PREFIX + LOG_MESSAGES[0], gameObject.name), this);
                }
                enabled = false; // 스크립트 비활성화
                return;
            }

            SetupEventListeners();
            UpdateHealthDisplay(character.CurrentHealth / character.MaxHealth);

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
            if (!isInitialized || character == null) return;

            character.OnHealthChanged += UpdateHealthDisplay;
            character.OnDeath += HideHealthBarOnDeath;
        }

        private void CleanupEventListeners()
        {
            if (character != null)
            {
                character.OnHealthChanged -= UpdateHealthDisplay;
                character.OnDeath -= HideHealthBarOnDeath;
            }
        }

        private void UpdateHealthDisplay(float healthRatio)
        {
            if (!isInitialized || character == null) return;

            if (healthSlider != null)
            {
                healthSlider.maxValue = character.MaxHealth;
                healthSlider.value = character.CurrentHealth;

                if (Application.isPlaying)
                {
                    Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[1], character.CurrentHealth, character.MaxHealth));
                }
            }

            if (healthBarObject != null)
            {
                bool isActive = character.CurrentHealth < character.MaxHealth && character.CurrentHealth > 0;
                if (Application.isPlaying)
                {
                    Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[2], isActive));
                }
                healthBarObject.SetActive(isActive);
            }
        }

        private void HideHealthBarOnDeath()
        {
            if (!isInitialized || healthBarObject == null) return;

            healthBarObject.SetActive(false);
        }
    }
} 