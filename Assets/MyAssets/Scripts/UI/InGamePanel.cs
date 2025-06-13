using UnityEngine;
using UnityEngine.UI;
using InvaderInsider.Managers;
using InvaderInsider.UI;
using InvaderInsider.Cards;

namespace InvaderInsider.UI
{
    public class InGamePanel : BasePanel
    {
        private const string LOG_PREFIX = "[UI] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "InGame: Pause button missing",
            "InGame: Summon button missing",
            "InGame: Pause clicked",
            "InGame: Summon clicked",
            "InGame: CardManager not found",
            "InGame: Panel shown",
            "InGame: Panel hidden"
        };

        [Header("Buttons")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button summonButton;

        private UIManager uiManager;

        protected override void Awake()
        {
            base.Awake();
            
            uiManager = UIManager.Instance;
            
            Initialize();
        }

        protected override void Initialize()
        {
            if (pauseButton == null)
            {
                #if UNITY_EDITOR
                Debug.LogWarning(LOG_PREFIX + LOG_MESSAGES[0]);
                #endif
            }
            else
            {
                pauseButton.onClick.AddListener(OnPauseButtonClicked);
            }

            if (summonButton != null)
            {
                summonButton.onClick.AddListener(OnSummonButtonClicked);
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogWarning(LOG_PREFIX + LOG_MESSAGES[1]);
                #endif
            }

            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + "InGame 패널 초기화 완료");
            #endif
        }

        private void OnPauseButtonClicked()
        {
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[2]);
            #endif
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PauseGame();
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + "GameManager를 찾을 수 없습니다");
                #endif
            }
        }

        private void OnSummonButtonClicked()
        {
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[3]);
            #endif
            if (CardManager.Instance != null)
            {
                CardManager.Instance.Summon();
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[4]);
                #endif
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
        }

        protected override void OnHide()
        {
            base.OnHide();
        }

        private void OnDestroy()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveAllListeners();
            }
            
            if (summonButton != null)
            {
                summonButton.onClick.RemoveAllListeners();
            }
        }
    }
} 