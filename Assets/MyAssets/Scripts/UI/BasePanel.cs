using UnityEngine;
using System.Collections;

namespace InvaderInsider.UI
{
    public abstract class BasePanel : MonoBehaviour
    {
        private const string LOG_PREFIX = "[UI] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "Panel {0} Awake",
            "Panel {0} Show",
            "Panel {0} Hide",
            "Panel {0} HideImmediate"
        };

        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected float fadeTime = 0.3f;
        [SerializeField] protected string panelName;
        
        private bool isInitialized;
        private string cachedName;
        private bool isVisible;
        
        protected virtual void Awake()
        {
            cachedName = gameObject.name;
            Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[0], cachedName));
            
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
            
            isInitialized = true;
            isVisible = false;
            Initialize();
        }

        public virtual void Show()
        {
            if (isVisible) return;

            Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[1], cachedName));
            gameObject.SetActive(true);
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            
            isVisible = true;
            OnShow();
        }

        public virtual void Hide()
        {
            if (!isVisible) return;

            Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[2], cachedName));
            if (!isInitialized)
            {
                HideImmediate();
                return;
            }

            HideImmediate();
        }

        public void HideImmediate()
        {
            Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[3], cachedName));
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            
            gameObject.SetActive(false);
            isVisible = false;
            OnHide();
        }

        public void ForceHide()
        {
            Debug.Log(LOG_PREFIX + "Force hiding panel: " + cachedName);
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            
            gameObject.SetActive(false);
            isVisible = false;
            OnHide();
        }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void Initialize() { }

        public bool IsVisible => isVisible;
    }
} 