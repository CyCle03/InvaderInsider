using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using InvaderInsider.Data;
using InvaderInsider.Cards;
using System.Collections.Generic;

namespace InvaderInsider.UI
{
    public class MenuManager : MonoBehaviour
    {
        [Header("Menu Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject deckPanel;
        [SerializeField] private GameObject stageSelectPanel;

        [Header("Main Menu")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button deckButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TextMeshProUGUI eDataText;

        [Header("Pause Menu")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Settings")]
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Button backButton;

        [Header("Deck Panel")]
        [SerializeField] private Transform deckCardContainer;
        [SerializeField] private Transform ownedCardContainer;
        [SerializeField] private GameObject cardPrefab;

        [Header("Stage Select")]
        [SerializeField] private Transform stageButtonContainer;
        [SerializeField] private GameObject stageButtonPrefab;
        [SerializeField] private int totalStages = 10;

        private void Start()
        {
            InitializeButtons();
            LoadSettings();
            UpdateEDataDisplay();
            SetGameState(GameState.MainMenu);
            InitializeStageButtons();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (GameManager.Instance.CurrentGameState == GameState.Playing)
                {
                    PauseGame();
                }
                else if (GameManager.Instance.CurrentGameState == GameState.Paused)
                {
                    ResumeGame();
                }
            }
        }

        private void InitializeButtons()
        {
            // 메인 메뉴 버튼
            startGameButton?.onClick.AddListener(() => SetGameState(GameState.Playing));
            deckButton?.onClick.AddListener(ShowDeckPanel);
            settingsButton?.onClick.AddListener(() => SetGameState(GameState.Settings));
            quitButton?.onClick.AddListener(QuitGame);

            // 일시정지 메뉴 버튼
            resumeButton?.onClick.AddListener(ResumeGame);
            restartButton?.onClick.AddListener(RestartGame);
            mainMenuButton?.onClick.AddListener(() => LoadScene("MainMenu"));

            // 설정 버튼
            backButton?.onClick.AddListener(() => SetGameState(GameState.MainMenu));
            if (bgmVolumeSlider != null)
                bgmVolumeSlider.onValueChanged.AddListener(SetBGMVolume);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        private void LoadSettings()
        {
            var saveManager = SaveDataManager.Instance;
            if (bgmVolumeSlider != null)
                bgmVolumeSlider.value = saveManager.GetSetting("BGMVolume", 1f);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = saveManager.GetSetting("SFXVolume", 1f);
            if (fullscreenToggle != null)
                fullscreenToggle.isOn = Screen.fullScreen;
        }

        private void UpdateEDataDisplay()
        {
            if (eDataText != null)
            {
                int currentEData = SaveDataManager.Instance.CurrentSaveData.progressData.currentEData;
                eDataText.text = $"eData: {currentEData}";
            }
        }

        public void SetGameState(GameState state)
        {
            GameManager.Instance.CurrentGameState = state;

            mainMenuPanel?.SetActive(state == GameState.MainMenu);
            pauseMenuPanel?.SetActive(state == GameState.Paused);
            settingsPanel?.SetActive(state == GameState.Settings);
            deckPanel?.SetActive(false);
            stageSelectPanel?.SetActive(false);

            Time.timeScale = (state == GameState.Playing) ? 1f : 0f;
        }

        private void ShowDeckPanel()
        {
            deckPanel.SetActive(true);
            mainMenuPanel.SetActive(false);
            RefreshDeckDisplay();
        }

        private void RefreshDeckDisplay()
        {
            // 기존 카드 UI 제거
            ClearContainer(deckCardContainer);
            ClearContainer(ownedCardContainer);

            var saveData = SaveDataManager.Instance.CurrentSaveData;

            // 덱에 있는 카드 표시
            foreach (int cardId in saveData.deckData.deckCardIds)
            {
                CreateCardUI(cardId, deckCardContainer, true);
            }

            // 보유 중인 카드 표시
            foreach (int cardId in saveData.deckData.ownedCardIds)
            {
                if (!saveData.deckData.deckCardIds.Contains(cardId))
                {
                    CreateCardUI(cardId, ownedCardContainer, false);
                }
            }
        }

        private void CreateCardUI(int cardId, Transform container, bool isInDeck)
        {
            GameObject cardObj = Instantiate(cardPrefab, container);
            CardDisplay cardDisplay = cardObj.GetComponent<CardDisplay>();
            if (cardDisplay != null)
            {
                CardData cardData = CardManager.Instance.GetCardById(cardId);
                cardDisplay.SetupCard(cardData);

                // 카드 클릭 이벤트 추가
                Button cardButton = cardObj.GetComponent<Button>();
                if (cardButton != null)
                {
                    cardButton.onClick.AddListener(() => OnCardClicked(cardId, isInDeck));
                }
            }
        }

        private void OnCardClicked(int cardId, bool isInDeck)
        {
            if (isInDeck)
            {
                SaveDataManager.Instance.RemoveCardFromDeck(cardId);
            }
            else
            {
                SaveDataManager.Instance.AddCardToDeck(cardId);
            }
            RefreshDeckDisplay();
        }

        private void ClearContainer(Transform container)
        {
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }

        #region Settings
        private void SetBGMVolume(float volume)
        {
            SaveDataManager.Instance.UpdateSetting("BGMVolume", volume);
            // TODO: 실제 BGM 볼륨 조정 로직 추가
        }

        private void SetSFXVolume(float volume)
        {
            SaveDataManager.Instance.UpdateSetting("SFXVolume", volume);
            // TODO: 실제 효과음 볼륨 조정 로직 추가
        }

        private void SetFullscreen(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
        }
        #endregion

        private void PauseGame()
        {
            SetGameState(GameState.Paused);
        }

        private void ResumeGame()
        {
            SetGameState(GameState.Playing);
        }

        private void RestartGame()
        {
            SetGameState(GameState.Playing);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void LoadScene(string sceneName)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        private void QuitGame()
        {
            SaveDataManager.Instance.SaveGameData();
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        private void InitializeStageButtons()
        {
            if (stageButtonContainer != null && stageButtonPrefab != null)
            {
                for (int i = 0; i < totalStages; i++)
                {
                    GameObject buttonObj = Instantiate(stageButtonPrefab, stageButtonContainer);
                    Button button = buttonObj.GetComponent<Button>();
                    TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

                    if (buttonText != null)
                        buttonText.text = $"Stage {i + 1}";

                    int stageIndex = i;
                    button.onClick.AddListener(() => LoadStage(stageIndex));
                }
            }
        }

        private void LoadStage(int stageIndex)
        {
            // 스테이지 로드 로직 구현
            GameManager.Instance.CurrentGameState = GameState.Playing;
            // TODO: 스테이지 씬 로드 또는 스테이지 설정
        }
    }
} 