using UnityEngine;
using System.Collections;

namespace InvaderInsider.UI
{
    public abstract class BasePanel : MonoBehaviour
    {
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected float fadeTime = 0.3f;
        [SerializeField] public string panelName;
        private bool isInitialized = false;
        
        protected virtual void Awake()
        {
            Debug.Log($"[{gameObject.name}] BasePanel Awake");
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            // Initial hide without animation
            // HideImmediate();
            isInitialized = true;
        }

        public virtual void Show()
        {
            Debug.Log($"[{gameObject.name}] Show called");
            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            OnShow();
        }

        public virtual void Hide()
        {
            Debug.Log($"[{gameObject.name}] Hide called");
            if (!isInitialized)
            {
                HideImmediate();
                return;
            }

            HideImmediate();
        }

        public void HideImmediate()
        {
            Debug.Log($"[{gameObject.name}] HideImmediate called");
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
            OnHide();
        }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void Initialize() { }
    }
} 