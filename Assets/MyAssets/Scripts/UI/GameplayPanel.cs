using UnityEngine;
using InvaderInsider.Managers;

namespace InvaderInsider.UI
{
    public class GameplayPanel : BasePanel
    {
        protected override void Awake()
        {
            base.Awake();
            panelName = "Gameplay"; // 패널 이름 설정
            // HideImmediate(); // 초기화 시 숨기지 않음
        }

        protected override void OnShow()
        {
            base.OnShow();
            Time.timeScale = 1f; // 게임 재개
            Debug.Log($"Time.timeScale set to: {Time.timeScale} in GameplayPanel.OnShow");
            UIManager.Instance.SetControlButtonsActive(true); // 제어 버튼 활성화
        }

        protected override void OnHide()
        {
            base.OnHide();
            Time.timeScale = 0f; // 게임 일시정지 또는 메뉴 진입
            Debug.Log($"Time.timeScale set to: {Time.timeScale} in GameplayPanel.OnHide");
            UIManager.Instance.SetControlButtonsActive(false); // 제어 버튼 비활성화
        }
    }
} 