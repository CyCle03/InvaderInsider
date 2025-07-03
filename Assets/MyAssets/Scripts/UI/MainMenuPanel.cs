using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using InvaderInsider.Data;
using InvaderInsider.Managers;
using TMPro;
using Cysharp.Threading.Tasks;
using System;


namespace InvaderInsider.UI
{
    public class MainMenuPanel : BasePanel
    {
        private const string LOG_TAG = "MainMenu";
        
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
                TryGetSaveDataManager().Forget();
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
                    TryGetSaveDataManager().Forget();
                }
            }
            
            // SaveDataManager가 있으면 강제로 데이터 재로드
            if (saveDataManager != null)
            {
                saveDataManager.LoadGameData();
            }
            
            // Continue 버튼 상태 업데이트
            UpdateContinueButton();
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

        private async UniTask TryGetSaveDataManager()
        {
            int attempts = 0;
            const int maxAttempts = 15; // 최대 시도 횟수 증가
            
            while (saveDataManager == null && attempts < maxAttempts)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.1f)); // 0.1초 대기
                
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
                    LogManager.Info(LOG_TAG, "SaveDataManager 찾기 시도 {0}/{1}", attempts, maxAttempts);
                }
            }
            
            if (saveDataManager != null)
            {
                LogManager.Info(LOG_TAG, "SaveDataManager 연결 성공");
                UpdateContinueButton();
            }
            else
            {
                LogManager.Warning(LOG_TAG, "SaveDataManager 연결 실패 - 백업 메커니즘 실행");
                
                // Continue 버튼을 일시적으로 비활성화
                if (continueButton != null)
                {
                    continueButton.interactable = false;
                }
                
                // 백업 메커니즘: 1초 후 다시 시도
                await UniTask.Delay(TimeSpan.FromSeconds(1f), ignoreTimeScale: true);
                
                // 마지막 시도
                saveDataManager = SaveDataManager.Instance;
                if (saveDataManager == null)
                {
                    saveDataManager = FindObjectOfType<SaveDataManager>();
                }
                
                if (saveDataManager != null)
                {
                    LogManager.Info(LOG_TAG, "SaveDataManager 백업 연결 성공");
                    UpdateContinueButton();
                }
                else
                {
                    LogManager.Error(LOG_TAG, "SaveDataManager를 찾을 수 없습니다. Continue 버튼이 비활성화됩니다.");
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
            else
            {
                Debug.LogError("Continue 버튼이 null입니다!");
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
                // 저장 데이터 상태 확인
                bool hasSaveData = saveDataManager.HasSaveData();
                continueButton.interactable = hasSaveData;
                
                LogManager.Info(LOG_TAG, "Continue 버튼 업데이트: HasSaveData = {0}, 버튼 활성화 = {1}", hasSaveData, continueButton.interactable);
            }
            else
            {
                LogManager.Warning(LOG_TAG, "Continue 버튼 업데이트 실패 - continueButton: {0}, saveDataManager: {1}", continueButton != null, saveDataManager != null);
            }
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
            LogManager.Info(LOG_TAG, "OnNewGameClicked 호출됨");
            
            // 즉시 중복 클릭 방지
            if (isGameStarting)
            {
                LogManager.Info(LOG_TAG, "OnNewGameClicked 무시됨 - 이미 게임 시작 중");
                return;
            }
            
            // 버튼 일시 비활성화로 물리적 중복 클릭 방지
            if (newGameButton != null)
            {
                newGameButton.interactable = false;
                ReEnableButtonAfterDelay(newGameButton, 1f).Forget();
            }
            
            StartNewGame();
        }

        private void OnContinueClicked()
        {
            LogManager.Info(LOG_TAG, "OnContinueClicked 호출됨!");
            
            // 즉시 중복 클릭 방지
            if (isGameStarting)
            {
                LogManager.Info(LOG_TAG, "OnContinueClicked 무시됨 - 이미 게임 시작 중");
                return;
            }
            
            // 버튼 일시 비활성화로 물리적 중복 클릭 방지
            if (continueButton != null)
            {
                continueButton.interactable = false;
                ReEnableButtonAfterDelay(continueButton, 1f).Forget();
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
                LogManager.Info(LOG_TAG, "클릭 쿨다운 중입니다. 남은 시간: {0:F1}초", CLICK_COOLDOWN - (currentTime - lastClickTime));
                return;
            }
            
            LogManager.Info(LOG_TAG, "StartNewGame 호출됨 - isGameStarting: {0}", isGameStarting);
            
            // 이미 게임 시작 중이면 무시
            if (isGameStarting) 
            {
                LogManager.Info(LOG_TAG, "이미 게임 시작 중입니다.");
                return;
            }
            
            isGameStarting = true;
            lastClickTime = currentTime;
            
            LogManager.Info(LOG_TAG, "GameManager.StartNewGame() 호출 시도");
                    
            // GameManager가 모든 게임 시작 로직을 담당
            var gameManager = InvaderInsider.Managers.GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.StartNewGame();
            }
            else
            {
                LogManager.Error(LOG_TAG, "GameManager를 찾을 수 없습니다!");
                isGameStarting = false; // 실패 시 플래그 리셋
            }
        }

        private void ContinueGame()
        {
            LogManager.Info(LOG_TAG, "ContinueGame 호출됨!");
            
            // 쿨다운 체크
            float currentTime = Time.unscaledTime;
            if (currentTime - lastClickTime < CLICK_COOLDOWN)
            {
                LogManager.Info(LOG_TAG, "클릭 쿨다운 중입니다. 남은시간: {0:F1}초", CLICK_COOLDOWN - (currentTime - lastClickTime));
                return;
            }
            
            // 이미 게임 시작 중이면 무시
            if (isGameStarting) 
            {
                LogManager.Info(LOG_TAG, "이미 게임 시작 중입니다.");
                return;
            }

            isGameStarting = true;
            lastClickTime = currentTime;
            
            LogManager.Info(LOG_TAG, "GameManager.StartContinueGame() 호출 시도");
            
            // GameManager가 모든 게임 계속하기 로직을 담당
            var gameManager = InvaderInsider.Managers.GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.StartContinueGame();
            }
            else
            {
                LogManager.Error(LOG_TAG, "GameManager를 찾을 수 없습니다!");
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
            // SaveDataManager 강제 재검색 (항상 최신 인스턴스 사용)
            saveDataManager = SaveDataManager.Instance;
            if (saveDataManager == null)
            {
                Debug.Log("[FORCE LOG] SaveDataManager가 null이므로 다시 찾기 시도");
                
                // 직접 검색
                saveDataManager = FindObjectOfType<SaveDataManager>();
                
                if (saveDataManager == null)
                {
                    Debug.LogError("[FORCE LOG] SaveDataManager를 찾을 수 없습니다!");
                    
                    // SaveDataManager가 없으면 Continue 버튼 비활성화
                    if (continueButton != null)
                    {
                        continueButton.interactable = false;
                    }
                    return;
                }
                else
                {
                    Debug.Log("[FORCE LOG] SaveDataManager 찾기 성공");
                }
            }
            
            // SaveDataManager가 있으면 데이터 강제 재로드
            if (saveDataManager != null)
            {
                Debug.Log("[FORCE LOG] SaveDataManager 데이터 강제 재로드 시작");
                saveDataManager.LoadGameData();
                Debug.Log("[FORCE LOG] SaveDataManager 데이터 강제 재로드 완료");
                
                // 저장 데이터 존재 여부 재확인
                bool hasSaveData = saveDataManager.HasSaveData();
                Debug.Log($"[FORCE LOG] 저장 데이터 확인 결과: {hasSaveData}");
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
        private async UniTask ReEnableButtonAfterDelay(Button button, float delay)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay), ignoreTimeScale: true);
            if (button != null)
            {
                button.interactable = true;
            }
        }
    }
} 