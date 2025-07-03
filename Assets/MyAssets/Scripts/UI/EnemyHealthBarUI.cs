using UnityEngine;
using UnityEngine.UI;
using InvaderInsider;
using Cysharp.Threading.Tasks;


namespace InvaderInsider.UI
{
    public class EnemyHealthBarUI : MonoBehaviour
    {
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
                enabled = false; // 스크립트 비활성화
                return;
            }

            SetupEventListeners();
            // 초기 체력 표시 - 즉시 호출
            DelayedHealthUpdate().Forget();

            isInitialized = true;
        }

        private async UniTask DelayedHealthUpdate()
        {
            // 한 프레임 대기 후 체력 업데이트
            await UniTask.Yield();
            UpdateHealthDisplay(character.CurrentHealth / character.MaxHealth);
        }

        private void OnEnable()
        {
            if (!isInitialized)
            {
                Initialize();
            }
            else if (character != null)
            {
                // 활성화될 때마다 체력 업데이트
                UpdateHealthDisplay(character.CurrentHealth / character.MaxHealth);
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
                // 슬라이더를 0~1 범위로 설정
                healthSlider.minValue = 0f;
                healthSlider.maxValue = 1f;
                healthSlider.value = healthRatio;
            }

            // 체력바는 항상 표시하되, 체력이 0이 되면 숨김
            if (healthBarObject != null)
            {
                bool isActive = character.CurrentHealth > 0;
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