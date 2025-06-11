using UnityEngine;
using UnityEngine.UI;
using InvaderInsider.Managers;

namespace InvaderInsider.UI
{
    [RequireComponent(typeof(Button))]
    public class MenuButton : MonoBehaviour
    {
        private const string LOG_PREFIX = "[UI] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "MenuButton: Target panel name not set - {0}",
            "MenuButton: Button component not found - {0}",
            "MenuButton: UIManager instance not found - {0}"
        };

        [SerializeField] private string targetPanelName;
        private Button button;
        private UIManager uiManager;
        private bool isInitialized = false;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (isInitialized) return;

            button = GetComponent<Button>();
            if (button == null)
            {
                if (Application.isPlaying)
                {
                    Debug.LogError(string.Format(LOG_PREFIX + LOG_MESSAGES[1], gameObject.name));
                }
                enabled = false;
                return;
            }

            uiManager = UIManager.Instance;
            if (uiManager == null)
            {
                if (Application.isPlaying)
                {
                    Debug.LogError(string.Format(LOG_PREFIX + LOG_MESSAGES[2], gameObject.name));
                }
                enabled = false;
                return;
            }

            SetupEventListeners();

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
            if (!isInitialized || button == null) return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OpenPanel);
        }

        private void CleanupEventListeners()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OpenPanel);
            }
        }

        private void OpenPanel()
        {
            if (!isInitialized || uiManager == null) return;

            if (!string.IsNullOrEmpty(targetPanelName))
            {
                uiManager.ShowPanel(targetPanelName);
            }
            else if (Application.isPlaying)
            {
                Debug.LogWarning(string.Format(LOG_PREFIX + LOG_MESSAGES[0], gameObject.name));
            }
        }
    }
} 