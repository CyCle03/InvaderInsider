using UnityEngine;
using TMPro;
using UnityEngine.UI;
using InvaderInsider; // IDamageable 인터페이스 사용을 위해 추가

namespace InvaderInsider.UI
{
    public class BottomBarPanel : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI healthText; // 플레이어 체력 표시 Text UI
        [SerializeField] private Slider healthSlider; // 플레이어 체력 표시 Slider UI
        [SerializeField] private TextMeshProUGUI EnemyRemainText; // 몬스터 수 표시 Text UI

        private Player _player; // Player 인스턴스 참조

        private void Awake()
        {
            _player = FindObjectOfType<Player>();
            if (_player == null)
            {
                Debug.LogError("Player 인스턴스를 찾을 수 없습니다. 체력 UI 업데이트가 불가능합니다.");
                return;
            }
            
            // Player의 OnHealthChanged 이벤트 구독
            _player.OnHealthChanged += OnPlayerHealthChanged;
            _player.OnDeath += OnPlayerDeath; // 플레이어 사망 이벤트 구독

            // 초기 체력 UI 업데이트
            OnPlayerHealthChanged(_player.CurrentHealth / _player.MaxHealth);
        }

        // 몬스터 수를 업데이트하는 함수
        public void UpdateMonsterCountDisplay(int count)
        {
            if (EnemyRemainText != null)
            {
                EnemyRemainText.text = $"Enemy: {count}";
            }
        }

        // 플레이어 체력 UI를 업데이트하는 함수 (이벤트 핸들러)
        private void OnPlayerHealthChanged(float healthRatio)
        {
            if (healthText != null)
            {
                healthText.text = $"HP: {_player.CurrentHealth}/{_player.MaxHealth}";
            }
            if (healthSlider != null)
            {
                healthSlider.maxValue = _player.MaxHealth;
                healthSlider.value = _player.CurrentHealth;
            }
        }

        // 플레이어 사망 시 호출될 함수
        private void OnPlayerDeath()
        {
            Debug.Log("플레이어 사망! BottomBarPanel에서 처리됨.");
            // TODO: 게임 오버 UI 표시 등 추가적인 사망 처리 로직
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해지
            if (_player != null)
            {
                _player.OnHealthChanged -= OnPlayerHealthChanged;
                _player.OnDeath -= OnPlayerDeath;
            }
        }
    }
} 