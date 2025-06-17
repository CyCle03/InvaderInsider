using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

namespace InvaderInsider.UI
{
    public class SettingsPanel : BasePanel
    {
        private const string LOG_PREFIX = "[Settings] ";
        
        [Header("Audio Settings")]
        public Slider masterVolumeSlider;
        public Slider musicVolumeSlider;
        public Slider sfxVolumeSlider;
        public AudioMixer audioMixer;

        [Header("Graphics Settings")]
        public Toggle fullscreenToggle;
        public Dropdown resolutionDropdown;
        public Dropdown qualityDropdown;

        [Header("Control Buttons")]
        public Button applyButton;
        public Button cancelButton;
        public Button defaultsButton;

        private float originalMasterVolume;
        private float originalMusicVolume;
        private float originalSfxVolume;
        private bool originalFullscreen;

        private void Start()
        {
            SetupUI();
            LoadSettings();
        }

        private void SetupUI()
        {
            // 볼륨 슬라이더 설정
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);

            // 그래픽 설정
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);

            // 버튼 설정
            if (applyButton != null)
                applyButton.onClick.AddListener(OnApplySettings);
            
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelSettings);
            
            if (defaultsButton != null)
                defaultsButton.onClick.AddListener(OnResetToDefaults);
        }

        private void LoadSettings()
        {
            // 저장된 설정 로드
            float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
            float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
            float sfxVolume = PlayerPrefs.GetFloat("SfxVolume", 0.75f);
            bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;

            // 원본값 저장
            originalMasterVolume = masterVolume;
            originalMusicVolume = musicVolume;
            originalSfxVolume = sfxVolume;
            originalFullscreen = isFullscreen;

            // UI 업데이트
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = masterVolume;
            
            if (musicVolumeSlider != null)
                musicVolumeSlider.value = musicVolume;
            
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = sfxVolume;
            
            if (fullscreenToggle != null)
                fullscreenToggle.isOn = isFullscreen;
        }

        private void OnMasterVolumeChanged(float volume)
        {
            if (audioMixer != null)
            {
                float dbValue = volume > 0.001f ? Mathf.Log10(volume) * 20f : -80f;
                audioMixer.SetFloat("MasterVolume", dbValue);
            }
        }

        private void OnMusicVolumeChanged(float volume)
        {
            if (audioMixer != null)
            {
                float dbValue = volume > 0.001f ? Mathf.Log10(volume) * 20f : -80f;
                audioMixer.SetFloat("MusicVolume", dbValue);
            }
        }

        private void OnSfxVolumeChanged(float volume)
        {
            if (audioMixer != null)
            {
                float dbValue = volume > 0.001f ? Mathf.Log10(volume) * 20f : -80f;
                audioMixer.SetFloat("SfxVolume", dbValue);
            }
        }

        private void OnFullscreenToggleChanged(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
        }

        private void OnApplySettings()
        {
            SaveSettings();
            Hide();
        }

        private void OnCancelSettings()
        {
            RestoreOriginalSettings();
            Hide();
        }

        private void OnResetToDefaults()
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = 0.75f;
            
            if (musicVolumeSlider != null)
                musicVolumeSlider.value = 0.75f;
            
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = 0.75f;
            
            if (fullscreenToggle != null)
                fullscreenToggle.isOn = true;
        }

        private void SaveSettings()
        {
            if (masterVolumeSlider != null)
                PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
            
            if (musicVolumeSlider != null)
                PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
            
            if (sfxVolumeSlider != null)
                PlayerPrefs.SetFloat("SfxVolume", sfxVolumeSlider.value);
            
            if (fullscreenToggle != null)
                PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);

            PlayerPrefs.Save();
        }

        private void RestoreOriginalSettings()
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = originalMasterVolume;
            
            if (musicVolumeSlider != null)
                musicVolumeSlider.value = originalMusicVolume;
            
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = originalSfxVolume;
            
            if (fullscreenToggle != null)
                fullscreenToggle.isOn = originalFullscreen;
        }

        public override void Show()
        {
            base.Show();
            LoadSettings(); // 패널이 열릴 때마다 최신 설정 로드
            
            // GameManager 상태를 Settings로 변경
            var gameManager = InvaderInsider.Managers.GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.SetGameState(InvaderInsider.Managers.GameState.Settings);
            }
        }

        public override void Hide()
        {
            base.Hide();
            
            // GameState 변경 제거 - GameManager에서 직접 관리하도록 함
            // 무한 루프 방지를 위해 SettingsPanel에서는 상태를 변경하지 않음
        }
    }
} 