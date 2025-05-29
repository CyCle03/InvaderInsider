using UnityEngine;
using UnityEngine.UI;
using InvaderInsider.Managers;

namespace InvaderInsider.UI
{
    [RequireComponent(typeof(Button))]
    public class MenuButton : MonoBehaviour
    {
        [SerializeField] private string targetPanelName;
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OpenPanel);
        }

        private void OpenPanel()
        {
            if (!string.IsNullOrEmpty(targetPanelName))
            {
                UIManager.Instance.ShowPanel(targetPanelName);
            }
            else
            {
                Debug.LogWarning("Target panel name is not set on MenuButton: " + gameObject.name);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OpenPanel);
            }
        }
    }
} 