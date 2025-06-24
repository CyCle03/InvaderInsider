using UnityEngine;

namespace InvaderInsider.UI
{
    public class StageSelectPanel : BasePanel
    {
        // Add UI elements specific to StageSelectPanel here
        // For example:
        // [SerializeField] private Button stage1Button;
        // [SerializeField] private Button backButton;

        protected override void Awake()
        {
            base.Awake();
            panelName = "StageSelect"; // Unique name for this panel
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
            // For example, load stage progression data
        }

        protected override void OnHide()
        {
            base.OnHide();
            // Logic to execute when the panel is hidden
        }
    }
} 