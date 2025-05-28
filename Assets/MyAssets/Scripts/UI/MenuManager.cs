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
        [Header("Panels")]
        [SerializeField] private MainMenuPanel mainMenuPanel;
        [SerializeField] private SettingsPanel settingsPanel;
        [SerializeField] private DeckPanel deckPanel;

        private void Start()
        {
            InitializeUI();
            ShowMainMenu();
        }

        private void InitializeUI()
        {
            // 패널들을 UIManager에 등록
            if (mainMenuPanel != null)
                UIManager.Instance.RegisterPanel("MainMenu", mainMenuPanel);
            if (settingsPanel != null)
                UIManager.Instance.RegisterPanel("Settings", settingsPanel);
            if (deckPanel != null)
                UIManager.Instance.RegisterPanel("Deck", deckPanel);
        }

        private void ShowMainMenu()
        {
            UIManager.Instance.ShowPanel("MainMenu");
        }

        private void Update()
        {
            // ESC 키로 뒤로가기
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UIManager.Instance.GoBack();
            }
        }
    }
} 