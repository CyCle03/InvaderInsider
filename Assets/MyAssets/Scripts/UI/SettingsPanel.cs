using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InvaderInsider.Data;
using InvaderInsider.Managers;
using InvaderInsider.UI;

namespace InvaderInsider.UI
{
    public class SettingsPanel : BasePanel
    {
        private const string LOG_PREFIX = "[UI] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "Settings: Back clicked",
            "Settings: Panel shown",
            "Settings: Panel hidden",
            "Settings: BGM volume {0}",
            "Settings: SFX volume {0}",
            "Settings: Fullscreen {0}"
        };

        private const string SETTING_BGM_VOLUME = "BGMVolume";
        private const string SETTING_SFX_VOLUME = "SFXVolume";
        private const string SETTING_FULLSCREEN = "Fullscreen";
        private const float DEFAULT_VOLUME = 1f;

        [Header("Settings Controls")]
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle fullscreenToggle;
        
        [Header("Buttons")]
        [SerializeField] private Button backButton;

        private UIManager uiManager;
        private SaveDataManager saveManager;

        protected override void Awake()
        {
            base.Awake();
            
            uiManager = UIManager.Instance;
            saveManager = SaveDataManager.Instance;
            
            uiManager.RegisterPanel("Settings", this);
            Initialize();
        }

        protected override void Initialize()
        {
            LoadSettings();

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.onValueChanged.AddListener(SetBGMVolume);
            }
            
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
            }
            
            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
            }
            
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackButtonClicked);
            }
        }

        private void LoadSettings()
        {
            if (saveManager == null) return;

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.value = saveManager.GetSetting(SETTING_BGM_VOLUME, DEFAULT_VOLUME);
            }
            
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = saveManager.GetSetting(SETTING_SFX_VOLUME, DEFAULT_VOLUME);
            }
            
            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = Screen.fullScreen;
            }
        }

        private void SetBGMVolume(float volume)
        {
            if (saveManager == null) return;
            
            saveManager.SaveSetting(SETTING_BGM_VOLUME, volume);
            Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[3], volume));
        }

        private void SetSFXVolume(float volume)
        {
            if (saveManager == null) return;
            
            saveManager.SaveSetting(SETTING_SFX_VOLUME, volume);
            Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[4], volume));
        }

        private void SetFullscreen(bool isFullscreen)
        {
            if (saveManager == null) return;
            
            Screen.fullScreen = isFullscreen;
            saveManager.SaveSetting(SETTING_FULLSCREEN, isFullscreen);
            Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[5], isFullscreen));
        }

        private void OnBackButtonClicked()
        {
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[0]);
            uiManager.GoBack();
        }

        protected override void OnShow()
        {
            base.OnShow();
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[1]);
        }

        protected override void OnHide()
        {
            base.OnHide();
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[2]);
        }

        private void OnDestroy()
        {
            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.onValueChanged.RemoveAllListeners();
            }
            
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            }
            
            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.RemoveAllListeners();
            }
            
            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
            }
        }
    }
} 