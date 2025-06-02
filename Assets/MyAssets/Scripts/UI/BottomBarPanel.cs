using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace InvaderInsider.UI
{
    public class BottomBarPanel : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI healthText; // 플레이어 체력 표시 Text UI
        [SerializeField] private Slider healthSlider; // 플레이어 체력 표시 Slider UI
        [SerializeField] private TextMeshProUGUI EnemyRemainText; // 몬스터 수 표시 Text UI

        private void Awake()
        {
            // 초기화 로직 (필요시 추가)
        }

        // 몬스터 수를 업데이트하는 함수
        public void UpdateMonsterCountDisplay(int count)
        {
            if (EnemyRemainText != null)
            {
                EnemyRemainText.text = $"Enemy: {count}";
            }
        }

        // 플레이어 체력 UI를 업데이트하는 함수
        public void UpdatePlayerHealthDisplay(int currentHealth, int maxHealth)
        {
            if (healthText != null)
            {
                healthText.text = $"HP: {currentHealth}/{maxHealth}";
            }
            if (healthSlider != null)
            {
                healthSlider.maxValue = maxHealth;
                healthSlider.value = currentHealth;
            }
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해지 등 (필요시 추가)
        }
    }
} 