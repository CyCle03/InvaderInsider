using UnityEngine;
using TMPro;
using InvaderInsider.Data;

namespace InvaderInsider.UI
{
    public class TopBarPanel : MonoBehaviour
    {
        private const string LOG_PREFIX = "[UI] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "TopBar: SaveDataManager instance not found",
            "TopBar: E-Data updated - {0:N0}",
            "TopBar: Player info updated - {0}"
        };

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI eDataText;
        [SerializeField] private TextMeshProUGUI playerNameText;

        private SaveDataManager saveManager;
        private bool isInitialized = false;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (isInitialized) return;

            saveManager = SaveDataManager.Instance;
            if (saveManager == null)
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[0]);
                return;
            }

            SetupEDataDisplay();
            UpdatePlayerInfo();

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

        private void CleanupEventListeners()
        {
            if (saveManager != null)
            {
                saveManager.OnEDataChanged -= UpdateEDataDisplay;
            }
        }

        private void SetupEDataDisplay()
        {
            if (!isInitialized || eDataText == null || saveManager == null) return;

            saveManager.OnEDataChanged += UpdateEDataDisplay;
            UpdateEDataDisplay(saveManager.GetCurrentEData());
        }

        private void UpdatePlayerInfo()
        {
            if (!isInitialized || playerNameText == null || saveManager == null) return;

            string playerName = saveManager.GetSetting("PlayerName", "Player");
            if (Application.isPlaying)
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[2], playerName));
            }
            playerNameText.text = playerName;
        }

        private void UpdateEDataDisplay(int amount)
        {
            if (!isInitialized || eDataText == null) return;

            if (Application.isPlaying)
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[1], amount));
            }
            eDataText.text = $"eData: {amount:N0}";
        }
    }
} 