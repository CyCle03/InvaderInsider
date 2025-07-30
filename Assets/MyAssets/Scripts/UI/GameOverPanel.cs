using UnityEngine;
using UnityEngine.UI;
using InvaderInsider.Managers;

namespace InvaderInsider.UI
{
    public class GameOverPanel : BasePanel
    {
        [Header("UI References")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        protected override void Initialize()
        {
            base.Initialize();
            restartButton?.onClick.AddListener(OnRestartButtonClicked);
            mainMenuButton?.onClick.AddListener(OnMainMenuButtonClicked);
        }

        private void OnRestartButtonClicked()
        {
            // 게임 재시작 로직은 GameManager가 담당
            GameManager.Instance?.StartNewGame();
        }

        private void OnMainMenuButtonClicked()
        {
            // 메인 메뉴 로딩 로직은 GameManager가 담당
            GameManager.Instance?.LoadMainMenuScene();
        }

        protected override void OnShow()
        {
            base.OnShow();
            // 게임 오버 패널이 보일 때 게임 시간을 정지
            Time.timeScale = 0f;
        }

        protected override void OnHide()
        {
            base.OnHide();
            // 다른 패널이 활성화될 때 게임 시간을 원상 복구
            Time.timeScale = 1f;
        }
    }
}
