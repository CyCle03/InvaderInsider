using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using InvaderInsider.Managers;

namespace InvaderInsider.UI
{
    public class SettingsPanel : BasePanel
    {
        [Header("Audio Settings")]
        public Slider masterVolumeSlider;
        public Slider musicVolumeSlider;
        public Slider sfxVolumeSlider;
        public AudioMixer audioMixer;

        [Header("Graphics Settings")]
        public Toggle fullscreenToggle;

        [Header("Control Buttons")]
        public Button applyButton;
        public Button cancelButton;

        protected override void Initialize()
        {
            LogManager.Log($"[SettingsPanel] Initialize called. audioMixer is null: {audioMixer == null}");
            base.Initialize();
            SetupUI();
            LoadSettings();
        }

        private void SetupUI()
        {
            masterVolumeSlider?.onValueChanged.AddListener(OnMasterVolumeChanged);
            musicVolumeSlider?.onValueChanged.AddListener(OnMusicVolumeChanged);
            sfxVolumeSlider?.onValueChanged.AddListener(OnSfxVolumeChanged);
            fullscreenToggle?.onValueChanged.AddListener(OnFullscreenToggleChanged);
            applyButton?.onClick.AddListener(OnApplySettings);
            cancelButton?.onClick.AddListener(OnCancelSettings);
        }

        private void LoadSettings()
        {
            LogManager.Log($"[SettingsPanel] LoadSettings called. audioMixer is null: {audioMixer == null}");
            if (masterVolumeSlider != null) masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
            if (musicVolumeSlider != null) musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = PlayerPrefs.GetFloat("SfxVolume", 0.75f);
            if (fullscreenToggle != null) fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        }

        private void OnMasterVolumeChanged(float volume)
        {
            LogManager.Log($"[SettingsPanel] OnMasterVolumeChanged called. audioMixer is null: {audioMixer == null}");
            if (audioMixer != null) audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20f);
        }

        private void OnMusicVolumeChanged(float volume)
        {
            LogManager.Log($"[SettingsPanel] OnMusicVolumeChanged called. audioMixer is null: {audioMixer == null}");
            if (audioMixer != null) audioMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20f);
        }

        private void OnSfxVolumeChanged(float volume)
        {
            LogManager.Log($"[SettingsPanel] OnSfxVolumeChanged called. audioMixer is null: {audioMixer == null}");
            if (audioMixer != null) audioMixer.SetFloat("SfxVolume", Mathf.Log10(volume) * 20f);
        }

        private void OnFullscreenToggleChanged(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
        }

        private void OnApplySettings()
        {
            PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
            PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
            PlayerPrefs.SetFloat("SfxVolume", sfxVolumeSlider.value);
            PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);
            PlayerPrefs.Save();
            Hide();
        }

        private void OnCancelSettings()
        {
            LoadSettings(); // 저장된 설정으로 되돌림
            Hide();
        }

        public override void Show()
        {
            base.Show();
            GameManager.Instance?.SetGameState(GameState.Paused);
        }

        public override void Hide()
        {
            base.Hide();
            // 닫힐 때의 상태 변경은 이 패널을 연 다른 패널(예: PausePanel)이 담당합니다.
            // 예: UIManager.Instance.GoBack();
        }
    }
}