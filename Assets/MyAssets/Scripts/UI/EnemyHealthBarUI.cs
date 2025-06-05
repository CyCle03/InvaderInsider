using UnityEngine;
using UnityEngine.UI;
using InvaderInsider;

namespace InvaderInsider.UI
{
    public class EnemyHealthBarUI : MonoBehaviour
    {
        [SerializeField] private Slider healthSlider; // 체력 슬라이더 UI
        [SerializeField] private GameObject healthBarObject; // 체력바 전체 오브젝트 (활성화/비활성화용)

        private BaseCharacter _character; // 체력 정보를 가져올 BaseCharacter 참조

        private void Awake()
        {
            _character = GetComponentInParent<BaseCharacter>(); // 부모 또는 자신에게서 BaseCharacter 찾기
            if (_character == null)
            {
                Debug.LogError($"EnemyHealthBarUI: BaseCharacter 컴포넌트를 찾을 수 없습니다. ({gameObject.name})", this);
                enabled = false; // 스크립트 비활성화
                return;
            }

            // 체력 변경 이벤트 구독
            _character.OnHealthChanged += UpdateHealthDisplay;

            // 사망 이벤트도 구독하여 체력바를 비활성화할 수 있습니다.
            _character.OnDeath += HideHealthBarOnDeath;

            // 초기 체력 상태 업데이트
            UpdateHealthDisplay(_character.CurrentHealth / _character.MaxHealth);
        }

        private void UpdateHealthDisplay(float healthRatio)
        {
            if (healthSlider != null)
            {
                healthSlider.maxValue = _character.MaxHealth;
                healthSlider.value = _character.CurrentHealth;
            }

            // 체력이 최대 체력과 다를 때만 체력바를 표시
            if (healthBarObject != null)
            {
                bool isActive = _character.CurrentHealth < _character.MaxHealth && _character.CurrentHealth > 0;
                healthBarObject.SetActive(isActive);
            }
        }

        private void HideHealthBarOnDeath()
        {
            if (healthBarObject != null)
            {
                healthBarObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해지 (메모리 누수 방지)
            if (_character != null)
            {
                _character.OnHealthChanged -= UpdateHealthDisplay;
                _character.OnDeath -= HideHealthBarOnDeath;
            }
        }
    }
} 