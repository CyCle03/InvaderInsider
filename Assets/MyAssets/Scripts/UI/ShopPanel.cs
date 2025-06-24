using UnityEngine;

namespace InvaderInsider.UI
{
    public class ShopPanel : BasePanel
    {
        // Add UI elements specific to ShopPanel here
        // For example:
        // [SerializeField] private Button buyGemsButton;
        // [SerializeField] private Button backButton;

        protected override void Awake()
        {
            base.Awake();
            panelName = "Shop"; // Unique name for this panel
        }

        protected void Start()
        {
            Initialize();
        }

        protected override void Initialize()
        {
            // Initialize buttons and other UI elements
            // Example:

            // backButton?.onClick.AddListener(() => UIManager.Instance.GoBack());
        }

        protected override void OnShow()
        {
            base.OnShow();
            // Logic to execute when the panel is shown
        }

        protected override void OnHide()
        {
            base.OnHide();
            // Logic to execute when the panel is hidden
        }
    }
} 