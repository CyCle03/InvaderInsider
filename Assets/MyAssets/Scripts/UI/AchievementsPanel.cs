using UnityEngine;

namespace InvaderInsider.UI
{
    public class AchievementsPanel : BasePanel
    {
        // Add UI elements specific to AchievementsPanel here
        // For example:
        // [SerializeField] private Transform achievementsListContainer;
        // [SerializeField] private Button backButton;

        protected override void Awake()
        {
            base.Awake();
            panelName = "Achievements"; // Unique name for this panel
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
            // For example, load and display achievements
        }

        protected override void OnHide()
        {
            base.OnHide();
            // Logic to execute when the panel is hidden
        }
    }
} 