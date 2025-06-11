using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace InvaderInsider.UI
{
    public class MainMenuCanvasSetup : MonoBehaviour
    {
        private const string LOG_PREFIX = "[UI] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "MainMenu: No Canvas found",
            "MainMenu: Adding GraphicRaycaster",
            "MainMenu: Canvas mode {0}",
            "MainMenu: Canvas order {0}",
            "MainMenu: Canvas configured for 1920x1080",
            "MainMenu: No EventSystem"
        };

        private Canvas mainCanvas;
        private GraphicRaycaster graphicRaycaster;
        private CanvasScaler canvasScaler;

        private void Awake()
        {
            SetupCanvas();
        }

        public void SetupCanvas()
        {
            mainCanvas = GetComponent<Canvas>();
            if (mainCanvas == null)
            {
                mainCanvas = GetComponentInParent<Canvas>();
                if (mainCanvas == null)
                {
                    Debug.LogError(LOG_PREFIX + LOG_MESSAGES[0]);
                    return;
                }
            }

            SetupGraphicRaycaster();
            SetupCanvasScaler();
            ConfigureCanvas();

            if (EventSystem.current == null)
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[5]);
            }
        }

        private void SetupGraphicRaycaster()
        {
            graphicRaycaster = mainCanvas.GetComponent<GraphicRaycaster>();
            if (graphicRaycaster == null)
            {
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[1]);
                graphicRaycaster = mainCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void SetupCanvasScaler()
        {
            canvasScaler = mainCanvas.GetComponent<CanvasScaler>();
            if (canvasScaler == null)
            {
                canvasScaler = mainCanvas.gameObject.AddComponent<CanvasScaler>();
            }
        }

        private void ConfigureCanvas()
        {
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[2], mainCanvas.renderMode));
            Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[3], mainCanvas.sortingOrder));

            if (canvasScaler != null)
            {
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2(1920, 1080);
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                canvasScaler.matchWidthOrHeight = 1f;
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[4]);
            }
        }

        public Canvas GetCanvas() => mainCanvas;
        public GraphicRaycaster GetGraphicRaycaster() => graphicRaycaster;
        public CanvasScaler GetCanvasScaler() => canvasScaler;
    }
} 