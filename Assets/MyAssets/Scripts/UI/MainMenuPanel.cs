using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using InvaderInsider.Data;
using InvaderInsider.Managers;
using TMPro;
using System.Collections;

namespace InvaderInsider.UI
{
    public class MainMenuPanel : BasePanel
    {
        private const string LOG_PREFIX = "[MainMenu] ";

        [Header("Components")]
        [SerializeField] private MainMenuButtonHandler buttonHandler;

        [Header("Menu Buttons")]
        public Button newGameButton;
        public Button continueButton;
        public Button settingsButton;
        public Button deckButton;
        public Button achievementsButton;
        public Button exitButton;

        [Header("Version Info")]
        public Text versionText;

        private SaveDataManager saveDataManager;
        private bool isGameStarting = false; // 게임 시작 중복 방지 플래그

        private void Start()
        {
            Initialize();
            
            // SaveDataManager가 아직 없다면 코루틴으로 재시도
            if (saveDataManager == null)
            {
                StartCoroutine(TryGetSaveDataManager());
            }
        }

        private void OnEnable()
        {
            UpdateContinueButton();
        }

        protected override void Initialize()
        {
            saveDataManager = SaveDataManager.Instance;
            
            // ButtonHandler가 있으면 이벤트 구독, 없으면 직접 버튼 설정
            if (buttonHandler != null)
            {
                SetupButtonHandlerEvents();
            }
            else
            {
                SetupButtons();
            }
            
            UpdateContinueButton();
            UpdateVersionInfo();
        }

        private void SetupButtonHandlerEvents()
        {
            if (buttonHandler != null)
            {
                buttonHandler.OnNewGameClicked += StartNewGame;
                buttonHandler.OnLoadGameClicked += ContinueGame;
                // 다른 이벤트들은 ButtonHandler에서 직접 처리
            }
        }

        private System.Collections.IEnumerator TryGetSaveDataManager()
        {
            int attempts = 0;
            const int maxAttempts = 15; // 최대 시도 횟수 증가
            
            while (saveDataManager == null && attempts < maxAttempts)
            {
                yield return new WaitForSeconds(0.1f); // 0.1초 대기
                
                // 여러 방법으로 SaveDataManager 찾기 시도
                saveDataManager = SaveDataManager.Instance;
                
                if (saveDataManager == null)
                {
                    // 직접 찾기 시도
                    saveDataManager = FindObjectOfType<SaveDataManager>();
                }
                
                attempts++;
                
                #if UNITY_EDITOR
                if (saveDataManager == null)
                {
                    Debug.Log(LOG_PREFIX + $"SaveDataManager 찾기 시도 {attempts}/{maxAttempts}");
                }
                #endif
            }
            
            if (saveDataManager != null)
            {
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "SaveDataManager 연결 성공");
                #endif
                UpdateContinueButton();
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogWarning(LOG_PREFIX + "SaveDataManager 연결 실패 - SaveDataManager 생성을 시도합니다");
                #endif
                
                // SaveDataManager를 강제로 생성
                var saveDataManagerInstance = SaveDataManager.Instance; // 이것만으로도 인스턴스가 생성됨
                
                if (saveDataManagerInstance != null)
                {
                    saveDataManager = saveDataManagerInstance;
                    UpdateContinueButton();
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + "SaveDataManager 생성 후 연결 성공");
                    #endif
                }
                else
                {
                    if (continueButton != null)
                    {
                        continueButton.interactable = false;
                    }
                }
            }
        }

        private void SetupButtons()
        {
            // 기존 리스너 제거 후 새로 등록 (중복 방지)
            if (newGameButton != null)
            {
                newGameButton.onClick.RemoveListener(OnNewGameClicked);
                newGameButton.onClick.AddListener(OnNewGameClicked);
            }
            
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueClicked);
                continueButton.onClick.AddListener(OnContinueClicked);
            }
            
            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(OnSettingsClicked);
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }
            
            if (deckButton != null)
            {
                deckButton.onClick.RemoveListener(OnDeckClicked);
                deckButton.onClick.AddListener(OnDeckClicked);
            }
            
            if (achievementsButton != null)
            {
                achievementsButton.onClick.RemoveListener(OnAchievementsClicked);
                achievementsButton.onClick.AddListener(OnAchievementsClicked);
            }
            
            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(OnExitClicked);
                exitButton.onClick.AddListener(OnExitClicked);
            }
        }

        private void UpdateContinueButton()
        {
            if (continueButton != null && saveDataManager != null)
            {
                bool hasSaveData = saveDataManager.HasSaveData();
                continueButton.interactable = hasSaveData;
                
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + $"Continue 버튼 업데이트: HasSaveData = {hasSaveData}, 버튼 활성화 = {continueButton.interactable}");
                #endif
            }
            #if UNITY_EDITOR
            else
            {
                Debug.LogWarning(LOG_PREFIX + $"Continue 버튼 업데이트 실패 - continueButton: {continueButton != null}, saveDataManager: {saveDataManager != null}");
            }
            #endif
        }

        private void UpdateVersionInfo()
        {
            if (versionText != null)
            {
                versionText.text = $"v{Application.version}";
            }
        }

        // 버튼 이벤트 핸들러들
        private void OnNewGameClicked()
        {
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + "OnNewGameClicked 호출됨");
            #endif
            StartNewGame();
        }

        private void OnContinueClicked()
        {
            ContinueGame();
        }

        private void OnSettingsClicked()
        {
            UIManager.Instance?.ShowPanel("Settings");
        }

        private void OnDeckClicked()
        {
            UIManager.Instance?.ShowPanel("Deck");
        }

        private void OnAchievementsClicked()
        {
            UIManager.Instance?.ShowPanel("Achievements");
        }

        private void OnExitClicked()
        {
            ExitGame();
        }

        // 게임 로직 메서드들
        private void StartNewGame()
        {
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + $"StartNewGame 호출됨 - isGameStarting: {isGameStarting}");
            #endif
            
            // 이미 게임 시작 중이면 무시
            if (isGameStarting) 
            {
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "이미 게임 시작 중입니다.");
                #endif
                return;
            }
            
            isGameStarting = true;
            
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + "GameManager.StartNewGame() 호출 시도");
            #endif
            
            // GameManager가 모든 게임 시작 로직을 담당
            var gameManager = InvaderInsider.Managers.GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.StartNewGame();
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + "GameManager를 찾을 수 없습니다!");
                #endif
                isGameStarting = false; // 실패 시 플래그 리셋
            }
        }

        private void ContinueGame()
        {
            // 이미 게임 시작 중이면 무시
            if (isGameStarting) 
            {
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "이미 게임 시작 중입니다.");
                #endif
                return;
            }
            
            isGameStarting = true;
            
            // GameManager가 모든 게임 계속하기 로직을 담당
            var gameManager = InvaderInsider.Managers.GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.StartContinueGame();
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + "GameManager를 찾을 수 없습니다!");
                #endif
                isGameStarting = false; // 실패 시 플래그 리셋
            }
        }

        private void ExitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        // 외부에서 호출 가능한 공개 메서드들
        public void RefreshContinueButton()
        {
            UpdateContinueButton();
        }

        public void SetInteractable(bool interactable)
        {
            if (newGameButton != null) newGameButton.interactable = interactable;
            if (continueButton != null) continueButton.interactable = interactable && saveDataManager?.HasSaveData() == true;
            if (settingsButton != null) settingsButton.interactable = interactable;
            if (deckButton != null) deckButton.interactable = interactable;
            if (achievementsButton != null) achievementsButton.interactable = interactable;
            if (exitButton != null) exitButton.interactable = interactable;
        }

        private void OnDestroy()
        {
            // ButtonHandler 이벤트 구독 해제
            if (buttonHandler != null)
            {
                buttonHandler.OnNewGameClicked -= StartNewGame;
                buttonHandler.OnLoadGameClicked -= ContinueGame;
            }
        }
    }
} 