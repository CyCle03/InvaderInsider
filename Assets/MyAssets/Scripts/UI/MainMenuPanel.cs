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
        private float lastClickTime = 0f; // 마지막 클릭 시간
        private const float CLICK_COOLDOWN = 0.2f; // 클릭 쿨다운 (0.2초)
        private bool buttonsSetup = false; // 버튼 이벤트 등록 완료 플래그

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
            // 씬 전환 후 플래그 리셋
            isGameStarting = false;
            
            // Main 씬으로 돌아올 때 SaveDataManager 재확인
            if (saveDataManager == null)
            {
                saveDataManager = SaveDataManager.Instance;
                if (saveDataManager == null)
                {
                    StartCoroutine(TryGetSaveDataManager());
                }
            }
            
            // Continue 버튼 상태 업데이트
            UpdateContinueButton();
            
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + $"OnEnable - SaveDataManager: {(saveDataManager != null ? "존재" : "null")}");
            #endif
        }

        protected override void Initialize()
        {
            saveDataManager = SaveDataManager.Instance;
            
            // 버튼 이벤트를 한 번만 등록
            if (!buttonsSetup)
            {
                SetupButtons();
                buttonsSetup = true;
            }
            
            UpdateContinueButton();
            UpdateVersionInfo();
        }

        // ButtonHandler 이벤트는 더 이상 사용하지 않음 (MainMenuPanel에서 직접 처리)

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
                
                if (saveDataManager == null)
                {
                    LogManager.Info("MainMenu", $"SaveDataManager 찾기 시도 {attempts}/{maxAttempts}");
                }
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
                Debug.LogWarning(LOG_PREFIX + "SaveDataManager 연결 실패 - 백업 메커니즘 실행");
                #endif
                
                // Continue 버튼을 일시적으로 비활성화
                if (continueButton != null)
                {
                    continueButton.interactable = false;
                }
                
                // 백업 메커니즘: 1초 후 다시 시도
                yield return new WaitForSecondsRealtime(1f);
                
                // 마지막 시도
                saveDataManager = SaveDataManager.Instance;
                if (saveDataManager == null)
                {
                    saveDataManager = FindObjectOfType<SaveDataManager>();
                }
                
                if (saveDataManager != null)
                {
                    #if UNITY_EDITOR
                    Debug.Log(LOG_PREFIX + "SaveDataManager 백업 연결 성공");
                    #endif
                    UpdateContinueButton();
                }
                else
                {
                    #if UNITY_EDITOR
                    Debug.LogError(LOG_PREFIX + "SaveDataManager를 찾을 수 없습니다. Continue 버튼이 비활성화됩니다.");
                    #endif
                }
            }
        }

        private void SetupButtons()
        {
            // 이벤트를 한 번만 등록 (RemoveAllListeners 제거)
            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(OnNewGameClicked);
            }
            
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }
            
            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }
            
            if (deckButton != null)
            {
                deckButton.onClick.AddListener(OnDeckClicked);
            }
            
            if (achievementsButton != null)
            {
                achievementsButton.onClick.AddListener(OnAchievementsClicked);
            }
            
            if (exitButton != null)
            {
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
                var saveData = saveDataManager.CurrentSaveData;
                int highestCleared = saveData?.progressData?.highestStageCleared ?? 0;
                Debug.Log(LOG_PREFIX + $"Continue 버튼 업데이트: HasSaveData = {hasSaveData}, 버튼 활성화 = {continueButton.interactable}, 최고 클리어 스테이지 = {highestCleared}");
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
            
            // 즉시 중복 클릭 방지
            if (isGameStarting)
            {
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "OnNewGameClicked 무시됨 - 이미 게임 시작 중");
                #endif
                return;
            }
            
            // 버튼 일시 비활성화로 물리적 중복 클릭 방지
            if (newGameButton != null)
            {
                newGameButton.interactable = false;
                StartCoroutine(ReEnableButtonAfterDelay(newGameButton, 1f));
            }
            
            StartNewGame();
        }

        private void OnContinueClicked()
        {
            // 즉시 중복 클릭 방지
            if (isGameStarting)
            {
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "OnContinueClicked 무시됨 - 이미 게임 시작 중");
                #endif
                return;
            }
            
            // 버튼 일시 비활성화로 물리적 중복 클릭 방지
            if (continueButton != null)
            {
                continueButton.interactable = false;
                StartCoroutine(ReEnableButtonAfterDelay(continueButton, 1f));
            }
            
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
            // 쿨다운 체크
            float currentTime = Time.unscaledTime;
            if (currentTime - lastClickTime < CLICK_COOLDOWN)
            {
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + $"클릭 쿨다운 중입니다. 남은 시간: {CLICK_COOLDOWN - (currentTime - lastClickTime):F1}초");
                #endif
                return;
            }
            
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
            lastClickTime = currentTime;
            
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
            // 쿨다운 체크
            float currentTime = Time.unscaledTime;
            if (currentTime - lastClickTime < CLICK_COOLDOWN)
            {
            #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + $"클릭 쿨다운 중입니다. 남은 시간: {CLICK_COOLDOWN - (currentTime - lastClickTime):F1}초");
            #endif
                return;
            }
            
            // 이미 게임 시작 중이면 무시
            if (isGameStarting) 
            {
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + "이미 게임 시작 중입니다.");
                #endif
                return;
        }

            isGameStarting = true;
            lastClickTime = currentTime;
            
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
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + "RefreshContinueButton 호출됨");
            #endif
            
            // SaveDataManager 재확인
            if (saveDataManager == null)
            {
                saveDataManager = SaveDataManager.Instance;
                if (saveDataManager == null)
                {
                    saveDataManager = FindObjectOfType<SaveDataManager>();
                }
            }
            
            // SaveDataManager가 있으면 데이터 강제 재로드
            if (saveDataManager != null)
            {
                saveDataManager.LoadGameData();
            }
            
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
            // 버튼 이벤트 정리
            CleanupButtonEvents();
        }

        private void OnDisable()
        {
            // 패널이 비활성화될 때 플래그 리셋
            isGameStarting = false;
        }
        
        private void CleanupButtonEvents()
        {
            if (buttonsSetup)
            {
                if (newGameButton != null)
                    newGameButton.onClick.RemoveListener(OnNewGameClicked);
                if (continueButton != null)
                    continueButton.onClick.RemoveListener(OnContinueClicked);
                if (settingsButton != null)
                    settingsButton.onClick.RemoveListener(OnSettingsClicked);
                if (deckButton != null)
                    deckButton.onClick.RemoveListener(OnDeckClicked);
                if (achievementsButton != null)
                    achievementsButton.onClick.RemoveListener(OnAchievementsClicked);
                if (exitButton != null)
                    exitButton.onClick.RemoveListener(OnExitClicked);
                
                buttonsSetup = false;
            }
        }
        
        // 버튼 재활성화 코루틴
        private System.Collections.IEnumerator ReEnableButtonAfterDelay(Button button, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (button != null)
            {
                button.interactable = true;
            }
        }
    }
} 