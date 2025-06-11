using UnityEngine;
using UnityEngine.UI;
using System;

namespace InvaderInsider.UI
{
    [Serializable]
    public class MainMenuLayoutSettings
    {
        public float buttonSpacing = 20f;
        public Vector2 buttonSize = new Vector2(500f, 80f);
        public Vector2 containerPadding = new Vector2(50f, 50f);
    }

    public class MainMenuLayout : MonoBehaviour
    {
        private const string LOG_PREFIX = "[UI] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "MainMenu: Container not found",
            "MainMenu: BottomBar not found"
        };

        [SerializeField] private MainMenuLayoutSettings settings;
        [SerializeField] private RectTransform mainMenuContainer;
        [SerializeField] private RectTransform bottomBar;

        private void Awake()
        {
            ValidateComponents();
        }

        public void SetupLayout(Button[] mainButtons, Button[] bottomButtons)
        {
            if (mainMenuContainer == null || bottomBar == null)
            {
                Debug.LogError(LOG_PREFIX + "Layout components not properly initialized");
                return;
            }

            float currentY = -settings.containerPadding.y;
            foreach (var button in mainButtons)
            {
                if (button != null)
                {
                    SetButtonPosition(button, ref currentY);
                }
            }

            if (bottomButtons != null && bottomButtons.Length > 0)
            {
                float bottomBarWidth = bottomBar.rect.width;
                float buttonSpacing = bottomBarWidth / (bottomButtons.Length + 1);
                float currentX = -bottomBarWidth / 2f + buttonSpacing;

                foreach (var button in bottomButtons)
                {
                    if (button != null)
                    {
                        SetBottomButtonPosition(button, currentX);
                        currentX += buttonSpacing;
                    }
                }
            }
        }

        private void SetButtonPosition(Button button, ref float currentY)
        {
            if (button == null) return;
            
            var rectTransform = button.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.sizeDelta = settings.buttonSize;
            rectTransform.anchoredPosition = new Vector2(0f, currentY);
            currentY -= (settings.buttonSize.y + settings.buttonSpacing);
        }

        private void SetBottomButtonPosition(Button button, float xPosition)
        {
            if (button == null) return;
            
            var rectTransform = button.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = settings.buttonSize;
            rectTransform.anchoredPosition = new Vector2(xPosition, 0f);
        }

        private void ValidateComponents()
        {
            if (mainMenuContainer == null)
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[0]);
            }

            if (bottomBar == null)
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[1]);
            }
        }

        public void UpdateLayout()
        {
            if (mainMenuContainer == null || bottomBar == null) return;

            // 레이아웃 업데이트가 필요한 경우 여기에 구현
            LayoutRebuilder.ForceRebuildLayoutImmediate(mainMenuContainer);
            LayoutRebuilder.ForceRebuildLayoutImmediate(bottomBar);
        }
    }
} 