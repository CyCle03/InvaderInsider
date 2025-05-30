using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InvaderInsider.Data;

namespace InvaderInsider.UI
{
    public class SettingsPanel : BasePanel
    {
        [Header("Settings Controls")]
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Button backButton;

        protected override void Awake()
        {
            base.Awake();
            panelName = "Settings";
            Initialize();
        }

        protected void Start()
        {
            // SettingsPanel specific Start logic can go here
        }

        protected override void Initialize()
        {
            LoadSettings();

            bgmVolumeSlider?.onValueChanged.AddListener(SetBGMVolume);
            sfxVolumeSlider?.onValueChanged.AddListener(SetSFXVolume);
            fullscreenToggle?.onValueChanged.AddListener(SetFullscreen);
            backButton?.onClick.AddListener(() => UIManager.Instance.GoBack());
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

        private void SetBGMVolume(float volume)
        {
            SaveDataManager.Instance.SaveSetting("BGMVolume", volume);
            // TODO: 실제 BGM 볼륨 조정 로직 추가
        }

        private void SetSFXVolume(float volume)
        {
            SaveDataManager.Instance.SaveSetting("SFXVolume", volume);
            // TODO: 실제 SFX 볼륨 조정 로직 추가
        }

        private void SetFullscreen(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
            SaveDataManager.Instance.SaveSetting("Fullscreen", isFullscreen);
        }
    }
} 