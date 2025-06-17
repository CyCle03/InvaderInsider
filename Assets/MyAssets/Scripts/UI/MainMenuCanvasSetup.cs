using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

namespace InvaderInsider.UI
{
    public class MainMenuCanvasSetup : MonoBehaviour
    {
        private const string LOG_PREFIX = "[CanvasSetup] ";
        
        // 에러 상황에만 LOG_MESSAGES 사용
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "Main Canvas not found in scene", // 0
            "Canvas setup completed", // 1
            "Render Mode: {0}", // 2
            "Sorting Order: {0}", // 3
            "EventSystem validated", // 4
            "Canvas Scaler configured" // 5
        };

        [Header("Canvas Configuration")]
        public bool autoSetupOnAwake = true;
        public RenderMode preferredRenderMode = RenderMode.ScreenSpaceOverlay;
        public int sortingOrder = 0;

        [Header("Canvas Scaler Settings")]
        public CanvasScaler.ScaleMode scaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        public Vector2 referenceResolution = new Vector2(1920, 1080);
        public float matchWidthOrHeight = 0.5f;

        private Canvas mainCanvas;
        private CanvasScaler canvasScaler;
        private GraphicRaycaster graphicRaycaster;

        private void Awake()
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            
            // MainMenuPanel이 있는 씬 목록
            string[] menuScenes = { "Main" };
            
            if (menuScenes.Contains(currentScene))
            {
                // 메뉴 씬에서는 정상적으로 설정
                SetupCanvas();
            }
            else
            {
                // 게임 씬에서는 MainMenuPanel 비활성화
                var mainMenuPanel = FindObjectOfType<MainMenuPanel>();
                if (mainMenuPanel != null)
                {
                    mainMenuPanel.gameObject.SetActive(false);
                }
            }
        }

        public void SetupCanvas()
        {
            GetCanvasComponents();
            
            if (mainCanvas == null)
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[0]);
                #endif
                return;
            }

            ConfigureCanvas();
            ConfigureCanvasScaler();
            ValidateEventSystem();


        }

        private void GetCanvasComponents()
        {
            mainCanvas = GetComponent<Canvas>();
            if (mainCanvas == null)
            {
                mainCanvas = GetComponentInParent<Canvas>();
            }

            if (mainCanvas != null)
            {
                canvasScaler = mainCanvas.GetComponent<CanvasScaler>();
                graphicRaycaster = mainCanvas.GetComponent<GraphicRaycaster>();
            }
        }

        private void ConfigureCanvas()
        {
            if (mainCanvas == null) return;

            mainCanvas.renderMode = preferredRenderMode;
            mainCanvas.sortingOrder = sortingOrder;

            // Graphic Raycaster 설정
            if (graphicRaycaster == null)
            {
                graphicRaycaster = mainCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }


        }

        private void ConfigureCanvasScaler()
        {
            if (canvasScaler == null && mainCanvas != null)
            {
                canvasScaler = mainCanvas.gameObject.AddComponent<CanvasScaler>();
            }

            if (canvasScaler != null)
            {
                canvasScaler.uiScaleMode = scaleMode;
                canvasScaler.referenceResolution = referenceResolution;
                canvasScaler.matchWidthOrHeight = matchWidthOrHeight;


            }
        }

        private void ValidateEventSystem()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }


        }

        public void SetRenderMode(RenderMode renderMode)
        {
            preferredRenderMode = renderMode;
            if (mainCanvas != null)
            {
                mainCanvas.renderMode = renderMode;
            }
        }

        public void SetSortingOrder(int order)
        {
            sortingOrder = order;
            if (mainCanvas != null)
            {
                mainCanvas.sortingOrder = order;
            }
        }

        public Canvas GetMainCanvas()
        {
            return mainCanvas;
        }

        public CanvasScaler GetCanvasScaler()
        {
            return canvasScaler;
        }
    }
} 