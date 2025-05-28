using UnityEngine;
using TMPro;
using InvaderInsider.Data;

namespace InvaderInsider.UI
{
    public class TopBarPanel : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI eDataText;
        [SerializeField] private TextMeshProUGUI playerNameText;

        private void Start()
        {
            SetupEDataDisplay();
            UpdatePlayerInfo();
        }

        private void SetupEDataDisplay()
        {
            if (eDataText != null)
            {
                SaveDataManager.Instance.OnEDataChanged += UpdateEDataDisplay;
                UpdateEDataDisplay(SaveDataManager.Instance.GetCurrentEData());
            }
        }

        private void UpdatePlayerInfo()
        {
            if (playerNameText != null)
            {
                string playerName = SaveDataManager.Instance.GetSetting("PlayerName", "Player");
                playerNameText.text = playerName;
            }
        }

        private void UpdateEDataDisplay(int amount)
        {
            if (eDataText != null)
            {
                eDataText.text = $"eData: {amount:N0}";
            }
        }

        private void OnDestroy()
        {
            if (SaveDataManager.Instance != null)
            {
                SaveDataManager.Instance.OnEDataChanged -= UpdateEDataDisplay;
            }
        }
    }
} 