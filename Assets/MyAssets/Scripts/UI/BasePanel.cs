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
        
        protected virtual void Awake()
        {
            panelName = GetType().Name.Replace("Panel", "");
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            // MainMenu 패널을 제외하고는 기본적으로 숨김 상태로 시작
            if (panelName != "MainMenu")
            {
                ForceHide();
            }
            
            Initialize();
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            OnShow();
        }

        public virtual void Hide()
        {
            HideImmediate();
        }

        public virtual void HideImmediate()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            OnHide();
            gameObject.SetActive(false);
        }

        public virtual void ForceHide()
        {
            gameObject.SetActive(false);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            OnHide();
        }

        /// <summary>
        /// 패널의 현재 상태가 유효한지 확인합니다.
        /// </summary>
        /// <returns>상태가 유효하면 true</returns>
        public virtual bool IsValidState()
        {
            // 기본 구현: 게임오브젝트와 CanvasGroup이 유효한지 확인
            return gameObject != null && canvasGroup != null;
        }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void Initialize() { }
    }
} 