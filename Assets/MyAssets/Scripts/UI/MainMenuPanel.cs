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
        
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "Scene transition initiated", // 0
            "No save data found", // 1
            "Loading stage: {0}" // 2
        };

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
        private bool isLoadingScene = false; // 씬 로딩 중복 방지 플래그

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
            
            SetupButtons();
            UpdateContinueButton();
            UpdateVersionInfo();
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
            if (newGameButton != null)
                newGameButton.onClick.AddListener(OnNewGameClicked);
            
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);
            
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
            
            if (deckButton != null)
                deckButton.onClick.AddListener(OnDeckClicked);
            
            if (achievementsButton != null)
                achievementsButton.onClick.AddListener(OnAchievementsClicked);
            
            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);
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
            if (saveDataManager != null)
            {
                saveDataManager.ResetGameData();
                #if UNITY_EDITOR
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[2], 1));
                #endif
            }
            
            // 새 게임은 항상 첫 번째 스테이지(인덱스 0)부터 시작
            InvaderInsider.Managers.GameManager.SetRequestedStartStage(0);
            
            LoadGameScene();
        }

        private void ContinueGame()
        {
            // SaveDataManager가 없다면 즉시 다시 시도
            if (saveDataManager == null)
            {
                saveDataManager = SaveDataManager.Instance;
            }
            
            if (saveDataManager != null && saveDataManager.HasSaveData())
            {
                saveDataManager.LoadGameData();
                var saveData = saveDataManager.CurrentSaveData;
                if (saveData != null)
                {
                    // Continue는 클리어한 최고 스테이지의 다음 스테이지부터 시작
                    int highestCleared = saveData.progressData.highestStageCleared;
                    int nextStage = highestCleared + 1; // 클리어한 스테이지의 다음 스테이지
                    #if UNITY_EDITOR
                    Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[2], nextStage));
                    Debug.Log(LOG_PREFIX + $"최고 클리어 스테이지: {highestCleared}, 시작할 스테이지: {nextStage}");
                    #endif
                    
                    // GameManager에 시작할 스테이지 설정 (인덱스는 0부터 시작하므로 nextStage - 1)
                    InvaderInsider.Managers.GameManager.SetRequestedStartStage(nextStage - 1);
                }
                
                LoadGameScene();
            }
            else
            {
                #if UNITY_EDITOR
                if (saveDataManager == null)
                {
                    Debug.LogWarning(LOG_PREFIX + "SaveDataManager를 찾을 수 없습니다.");
                }
                else
                {
                    Debug.Log(LOG_PREFIX + LOG_MESSAGES[1]);
                }
                #endif
                return;
            }
        }

        private void LoadGameScene()
        {
            if (isLoadingScene) return;
            isLoadingScene = true;

            Time.timeScale = 1f;
            
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[0]);
            #endif

            // 간단한 씬 전환으로 변경 (성능 최적화)
            StartCoroutine(LoadGameSceneOptimized());
        }

        private System.Collections.IEnumerator LoadGameSceneOptimized()
        {
            // UI 정리
            var uiManager = UIManager.Instance;
            if (uiManager != null)
            {
                uiManager.Cleanup();
            }
            
            yield return null; // 한 프레임 대기
            
            // 비동기로 Game 씬 로드
            var asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Game");
            
            // 씬 로딩 완료까지 대기
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            yield return new WaitForEndOfFrame(); // 모든 오브젝트 초기화 대기
            
            isLoadingScene = false;
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
    }
} 