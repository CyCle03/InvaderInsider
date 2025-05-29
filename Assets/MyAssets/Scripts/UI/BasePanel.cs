using UnityEngine;
using System.Collections;

namespace InvaderInsider.UI
{
    public abstract class BasePanel : MonoBehaviour
    {
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected float fadeTime = 0.3f;
        private bool isInitialized = false;
        private bool isTransitioning = false;
        private Coroutine currentTransition = null;
        
        protected virtual void Awake()
        {
            Debug.Log($"[{gameObject.name}] BasePanel Awake");
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            // Initial hide without animation
            HideImmediate();
            isInitialized = true;
        }

        public virtual void Show()
        {
            Debug.Log($"[{gameObject.name}] Show called");
            if (isTransitioning)
            {
                Debug.LogWarning($"[{gameObject.name}] Show called while transitioning, stopping current transition");
                if (currentTransition != null)
                {
                    StopCoroutine(currentTransition);
                }
            }

            gameObject.SetActive(true);
            currentTransition = StartCoroutine(FadeIn());
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

            if (isTransitioning)
            {
                Debug.LogWarning($"[{gameObject.name}] Hide called while transitioning, stopping current transition");
                if (currentTransition != null)
                {
                    StopCoroutine(currentTransition);
                }
            }

            currentTransition = StartCoroutine(FadeOut());
            OnHide();
        }

        private void HideImmediate()
        {
            Debug.Log($"[{gameObject.name}] HideImmediate called");
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
            OnHide();
        }

        private IEnumerator FadeIn()
        {
            Debug.Log($"[{gameObject.name}] FadeIn started");
            isTransitioning = true;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            
            float elapsedTime = 0;
            float startAlpha = canvasGroup.alpha;

            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsedTime / fadeTime);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            isTransitioning = false;
            currentTransition = null;
            Debug.Log($"[{gameObject.name}] FadeIn completed");
        }

        private IEnumerator FadeOut()
        {
            Debug.Log($"[{gameObject.name}] FadeOut started");
            isTransitioning = true;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            
            float elapsedTime = 0;
            float startAlpha = canvasGroup.alpha;

            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeTime);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            isTransitioning = false;
            currentTransition = null;
            Debug.Log($"[{gameObject.name}] FadeOut completed");
        }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void Initialize() { }

        private void OnDestroy()
        {
            if (currentTransition != null)
            {
                StopCoroutine(currentTransition);
            }
        }
    }
} 