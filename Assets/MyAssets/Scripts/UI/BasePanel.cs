using UnityEngine;
using System.Collections;

namespace InvaderInsider.UI
{
    public abstract class BasePanel : MonoBehaviour
    {
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected float fadeTime = 0.3f;
        
        protected virtual void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            Hide();
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            StartCoroutine(FadeIn());
            OnShow();
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
            OnHide();
        }

        private IEnumerator FadeIn()
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            
            float elapsedTime = 0;
            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeTime);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOut()
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            
            float elapsedTime = 0;
            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeTime);
                yield return null;
            }
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void Initialize() { }
    }
} 