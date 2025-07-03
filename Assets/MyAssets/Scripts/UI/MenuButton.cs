using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using InvaderInsider.Data;
using Cysharp.Threading.Tasks;

namespace InvaderInsider.UI
{
    public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private const string LOG_PREFIX = "[MenuButton] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "Button component missing on {0}",
            "MenuManager not found for {0}",
            "UIManager not found for {0}"
        };

        [Header("Button Settings")]
        public ButtonType buttonType = ButtonType.NewGame;
        public bool useAnimation = true;
        public float animationDuration = 0.2f;

        [Header("Visual Effects")]
        public Color normalColor = Color.white;
        public Color highlightColor = Color.yellow;
        public Vector3 hoverScale = Vector3.one * 1.1f;

        private Button button;
        private Image buttonImage;
        private Text buttonText;
        private Vector3 originalScale;
        private MenuManager menuManager;
        private UIManager uiManager;
        private Coroutine currentAnimation;

        private void Awake()
        {
            InitializeComponents();
            FindManagers();
            SetupButton();
        }

        private void InitializeComponents()
        {
            button = GetComponent<Button>();
            buttonImage = GetComponent<Image>();
            buttonText = GetComponentInChildren<Text>();
            originalScale = transform.localScale;

            if (button == null || buttonImage == null)
            {
                #if UNITY_EDITOR
                Debug.LogWarning(string.Format(LOG_PREFIX + LOG_MESSAGES[0], gameObject.name));
                #endif
            }
        }

        private void FindManagers()
        {
            menuManager = FindObjectOfType<MenuManager>();
            uiManager = UIManager.Instance;

            if (menuManager == null)
            {
                #if UNITY_EDITOR
                Debug.LogError(string.Format(LOG_PREFIX + LOG_MESSAGES[1], gameObject.name));
                #endif
            }

            if (uiManager == null)
            {
                #if UNITY_EDITOR
                Debug.LogError(string.Format(LOG_PREFIX + LOG_MESSAGES[2], gameObject.name));
                #endif
            }
        }

        private void SetupButton()
        {
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClick);
            }

            if (buttonImage != null)
            {
                buttonImage.color = normalColor;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (useAnimation && buttonImage != null)
            {
                buttonImage.color = highlightColor;
                if (useAnimation)
                {
                    AnimateScale(hoverScale);
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (useAnimation && buttonImage != null)
            {
                buttonImage.color = normalColor;
                if (useAnimation)
                {
                    AnimateScale(originalScale);
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnButtonClick();
        }

        private void AnimateScale(Vector3 targetScale)
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }
            currentAnimation = ScaleAnimation(targetScale).Forget();
        }

        private async UniTask ScaleAnimation(Vector3 targetScale)
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / animationDuration;
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            transform.localScale = targetScale;
        }

        private void OnButtonClick()
        {
            switch (buttonType)
            {
                case ButtonType.NewGame:
                    StartNewGame();
                    break;
                case ButtonType.Continue:
                    ContinueGame();
                    break;
                case ButtonType.Settings:
                    ShowSettings();
                    break;
                case ButtonType.Deck:
                    ShowDeck();
                    break;
                case ButtonType.Achievements:
                    ShowAchievements();
                    break;
                case ButtonType.Exit:
                    ExitGame();
                    break;
            }
        }

        private void StartNewGame()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        }

        private void ContinueGame()
        {
            if (SaveDataManager.Instance?.HasSaveData() == true)
            {
                SaveDataManager.Instance.LoadGameData();
                UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
            }
        }

        private void ShowSettings()
        {
            menuManager?.ShowSettings();
        }

        private void ShowDeck()
        {
            menuManager?.ShowDeck();
        }

        private void ShowAchievements()
        {
            uiManager?.ShowPanel("Achievements");
        }

        private void ExitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        public void SetButtonType(ButtonType type)
        {
            buttonType = type;
        }

        public void SetColors(Color normal, Color highlight)
        {
            normalColor = normal;
            highlightColor = highlight;
            if (buttonImage != null)
            {
                buttonImage.color = normalColor;
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }

    public enum ButtonType
    {
        NewGame,
        Continue,
        Settings,
        Deck,
        Achievements,
        Exit
    }
} 