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
            
            // InGame 패널은 초기에 숨김
            ForceHide();
            Debug.Log(LOG_PREFIX + "InGame panel registered and force hidden");
            
            Initialize();
        }

        protected override void Initialize()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(OnPauseButtonClicked);
            }
            else
            {
                Debug.LogWarning(LOG_PREFIX + LOG_MESSAGES[0]);
            }

            if (summonButton != null)
            {
                summonButton.onClick.AddListener(OnSummonButtonClicked);
            }
            else
            {
                Debug.LogWarning(LOG_PREFIX + LOG_MESSAGES[1]);
            }
        }

        private void OnPauseButtonClicked()
        {
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[2]);
            uiManager.ShowPanel("Pause");
        }

        private void OnSummonButtonClicked()
        {
            if (CardManager.Instance != null)
            {
                CardManager.Instance.Summon();
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[3]);
            }
            else
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[4]);
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[5]);
        }

        protected override void OnHide()
        {
            base.OnHide();
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[6]);
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