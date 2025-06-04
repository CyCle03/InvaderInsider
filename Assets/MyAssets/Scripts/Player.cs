using UnityEngine;
using UnityEngine.UI; // UI 관련 기능을 사용하기 위해 추가
using TMPro; // TextMeshPro 기능을 사용하기 위해 추가
using InvaderInsider.UI; // Changed from InvaderInsider.Managers

public class Player : MonoBehaviour
{
    // 체력 관련 변수
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    // 체력 속성 추가
    public int CurrentHealth { get { return currentHealth; } }
    public int MaxHealth { get { return maxHealth; } }

    // 체력 UI 업데이트를 위한 TextMeshProUGUI 및 Slider 참조
    public TextMeshProUGUI healthText;
    //[SerializeField] private Slider healthSlider; // 체력을 표시할 UI Slider 요소

    // Start is called before the first frame update
    void Start()
    {
        // currentHealth = maxHealth; // 시작 시 체력을 최대로 설정
        
        // Slider 최대값 설정 (선택 사항, 에디터에서 설정해도 됨)
        // if (healthSlider != null)
        // {
        //     healthSlider.maxValue = maxHealth;
        // }

        // UpdateHealthUI(); // 초기 체력 UI 업데이트
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
        // UpdateHealthUI(); // UI 업데이트 로직은 BottomBarPanel로 이동
        if (FindObjectOfType<BottomBarPanel>() != null)
        {
            FindObjectOfType<BottomBarPanel>().UpdatePlayerHealthDisplay(currentHealth, maxHealth);
        }
        Debug.Log("Player health reset.");
    }

    // 체력을 감소시키는 함수
    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        if (currentHealth <= 0)
        {
            currentHealth = 0; // 체력이 0 이하로 내려가지 않도록 보정
            Die(); // 체력이 0 이하가 되면 죽음 처리
        }
        // UpdateHealthUI(); // UI 업데이트 로직은 BottomBarPanel로 이동
        if (FindObjectOfType<BottomBarPanel>() != null)
        {
            FindObjectOfType<BottomBarPanel>().UpdatePlayerHealthDisplay(currentHealth, maxHealth);
        }
    }

    // 주인공이 죽었을 때 처리하는 함수 (예정)
    void Die()
    {
        Debug.Log("Player Died!");
        
        // TODO: 게임 오버 처리 또는 리스폰 로직 구현
        // 죽었을 때 UI를 업데이트하거나 비활성화할 수 있습니다.
        // UpdateHealthUI(); // UI 업데이트 로직은 BottomBarPanel로 이동
        if (FindObjectOfType<BottomBarPanel>() != null)
        {
            FindObjectOfType<BottomBarPanel>().UpdatePlayerHealthDisplay(currentHealth, maxHealth);
        }

        // 메인 메뉴 패널 활성화
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowPanel("MainMenu"); // 메인 메뉴 패널 이름 사용
        }
        else
        {
            Debug.LogError("UIManager instance is null!");
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