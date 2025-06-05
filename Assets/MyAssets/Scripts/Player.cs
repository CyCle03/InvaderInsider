using UnityEngine;
using UnityEngine.UI; // UI 관련 기능을 사용하기 위해 추가
using TMPro; // TextMeshPro 기능을 사용하기 위해 추가
using InvaderInsider.UI; // Changed from InvaderInsider.Managers
using InvaderInsider; // IDamageable 인터페이스 사용을 위해 추가
using System; // Action 델리게이트 사용을 위해 추가

public class Player : MonoBehaviour, IDamageable
{
    // 체력 관련 변수
    [SerializeField] private float maxHealth = 100f; // float로 변경
    private float currentHealth;

    // 체력 속성 추가 (IDamageable 인터페이스 구현)
    public float CurrentHealth { get { return currentHealth; } }
    public float MaxHealth { get { return maxHealth; } }

    // IDamageable 인터페이스 이벤트 구현
    public event Action<float> OnHealthChanged; // 현재 체력 비율을 전달
    public event Action OnDeath;

    // 체력 UI 업데이트를 위한 TextMeshProUGUI 및 Slider 참조
    public TextMeshProUGUI healthText;
    //[SerializeField] private Slider healthSlider; // 체력을 표시할 UI Slider 요소

    private BottomBarPanel _bottomBarPanel; // BottomBarPanel 인스턴스 캐싱
    private UIManager _uiManager; // UIManager 인스턴스 캐싱

    // Start is called before the first frame update
    void Start()
    {
        _bottomBarPanel = FindObjectOfType<BottomBarPanel>(); // BottomBarPanel 인스턴스 찾기
        if (_bottomBarPanel == null)
        {
            Debug.LogError("BottomBarPanel 인스턴스를 찾을 수 없습니다.");
        }

        _uiManager = UIManager.Instance; // UIManager 인스턴스 찾기
        if (_uiManager == null)
        {
            Debug.LogError("UIManager 인스턴스를 찾을 수 없습니다.");
        }
        
        ResetHealth(); // Start에서 체력 초기화 및 UI 업데이트
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 체력을 최대로 초기화하고 UI를 업데이트하는 함수
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth / maxHealth); // 체력 변경 이벤트 발생 (비율 전달)
        Debug.Log("Player health reset.");
    }

    // 체력을 감소시키는 함수 (IDamageable 인터페이스 구현)
    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        if (currentHealth <= 0)
        {
            currentHealth = 0; // 체력이 0 이하로 내려가지 않도록 보정
            Die(); // 체력이 0 이하가 되면 죽음 처리
        }
        OnHealthChanged?.Invoke(currentHealth / maxHealth); // 체력 변경 이벤트 발생 (비율 전달)
    }

    // 주인공이 죽었을 때 처리하는 함수 (예정)
    void Die()
    {
        Debug.Log("Player Died!");
        OnDeath?.Invoke(); // 사망 이벤트 발생

        // TODO: 게임 오버 처리 또는 리스폰 로직 구현
        if (_uiManager != null)
        {
            _uiManager.ShowPanel("MainMenu"); // 메인 메뉴 패널 이름 사용
        }

        // 게임 일시 정지 (선택 사항)
        // Time.timeScale = 0f;

        // 주인공 오브젝트를 즉시 파괴하지 않도록 처리 (씬 전환 등으로 처리될 것이므로)
        // gameObject.SetActive(false); 
    }

    // 체력 UI를 업데이트하는 함수 (더 이상 사용하지 않음)
    // private void UpdateHealthUI()
    // {
    //     // TextMeshPro UI 업데이트
    //     if (healthText != null)
    //     {
    //         //healthText.text = "Health: " + currentHealth.ToString();
    //         healthText.text = $"HP: {currentHealth}/{maxHealth}"; // 최대 체력과 함께 표시
    //     }

    //     // Slider UI 업데이트
    //     if (healthSlider != null)
    //     {
    //         healthSlider.value = currentHealth; // 현재 체력 값을 Slider value에 반영
    //     }
    // }
} 