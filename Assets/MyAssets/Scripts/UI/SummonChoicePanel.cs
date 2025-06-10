using UnityEngine;
using UnityEngine.UI; // UI 요소 사용을 위해 추가
using System.Collections.Generic;
using InvaderInsider.Data; // CardDBObject 사용을 위해 추가
using InvaderInsider.Managers; // SummonManager 사용을 위해 추가
using TMPro; // TextMeshPro 사용 시 추가

namespace InvaderInsider.UI
{
    public class SummonChoicePanel : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private List<Button> choiceButtons; // 3개의 선택 버튼
        [SerializeField] private List<Image> cardImages; // 각 버튼에 표시할 카드 이미지
        [SerializeField] private List<TextMeshProUGUI> cardNames; // 각 버튼에 표시할 카드 이름 (TextMeshPro 사용 시)
        // [SerializeField] private List<Text> cardNamesLegacy; // 각 버튼에 표시할 카드 이름 (기본 Text 사용 시)

        [Header("Field View Settings")]
        [SerializeField] private Button viewFieldButton; // 필드 보기 버튼
        [SerializeField] private GameObject gameplayFieldRoot; // 게임 필드의 루트 GameObject (활성/비활성화 대상)

        private List<CardDBObject> currentChoices; // 현재 표시 중인 카드 선택지

        // SummonManager 인스턴스 참조 (Awake에서 찾도록 하거나 다른 방식으로 주입 가능)
        private SummonManager summonManager;

        private void Awake()
        {
            // SummonManager 인스턴스 찾기
            summonManager = SummonManager.Instance; // FindObjectOfType 대신 Instance 사용
            if (summonManager == null)
            {
                Debug.LogError("SummonManager 인스턴스를 찾을 수 없습니다. SummonChoicePanel이 제대로 작동하지 않습니다.");
            }

            // 각 버튼에 클릭 이벤트 리스너 추가
            for (int i = 0; i < choiceButtons.Count; i++)
            {
                int choiceIndex = i; // 클로저를 위해 지역 변수 사용
                choiceButtons[i].onClick.AddListener(() => OnChoiceButtonClicked(choiceIndex));
            }

            // 필드 보기 버튼에 클릭 이벤트 리스너 추가
            if (viewFieldButton != null)
            {
                viewFieldButton.onClick.AddListener(OnViewFieldButtonClicked);
            }
        }

        private void OnDestroy()
        {
            // 리스너 제거 (씬 전환 등에서 오류 방지)
             for (int i = 0; i < choiceButtons.Count; i++)
            {
                choiceButtons[i].onClick.RemoveAllListeners();
            }
        }

        // SummonManager에서 호출하여 선택지 카드 정보를 받아와 UI에 표시하는 함수
        public void DisplayChoices(List<CardDBObject> choices)
        {
            if (choices == null || choices.Count != 3)
            {
                Debug.LogError("잘못된 수의 카드 선택지가 전달되었습니다. (필요: 3)");
                // 패널 비활성화 또는 오류 메시지 표시
                gameObject.SetActive(false); // 예시
                return;
            }

            currentChoices = choices; // 선택지 목록 저장

            // 각 버튼의 UI 요소 업데이트
            for (int i = 0; i < 3; i++)
            {
                if (i < choices.Count)
                {
                    CardDBObject card = choices[i];
                    // 실제 카드 이미지 로드 및 표시 로직
                    if (cardImages != null && cardImages.Count > i && card.artwork != null)
                    {
                        cardImages[i].sprite = card.artwork;
                    }

                    if (cardNames != null && cardNames.Count > i && cardNames[i] != null)
                    {
                        cardNames[i].text = card.cardName; // 카드 이름 표시
                    } /* else if (cardNamesLegacy != null && cardNamesLegacy[i] != null)
                    {
                         cardNamesLegacy[i].text = card.cardName; // 기본 Text 사용 시
                    }*/

                    // 버튼 활성화
                    if (choiceButtons != null && choiceButtons.Count > i)
                    {
                         choiceButtons[i].gameObject.SetActive(true);
                    }

                } else
                {
                     // 카드가 3개 미만이면 해당 버튼 비활성화
                     if (choiceButtons != null && choiceButtons.Count > i)
                     {
                         choiceButtons[i].gameObject.SetActive(false);
                     }
                }
            }

            // 패널 활성화 (UICanvas 아래에 적절히 배치 필요)
            gameObject.SetActive(true);

            // 필드 보기 버튼 활성화
            if (viewFieldButton != null)
            {
                viewFieldButton.gameObject.SetActive(true);
            }

            // 게임 필드는 기본적으로 비활성화 (선택 패널이 나타나면)
            if (gameplayFieldRoot != null)
            {
                gameplayFieldRoot.SetActive(false);
            }
        }

        // 버튼 클릭 시 호출될 함수
        private void OnChoiceButtonClicked(int choiceIndex)
        {
            if (choiceIndex >= 0 && choiceIndex < currentChoices.Count)
            {
                CardDBObject selectedCard = currentChoices[choiceIndex];
                Debug.Log($"선택 버튼 클릭됨: 인덱스 {choiceIndex}, 카드: {selectedCard.cardName}");

                // 선택된 카드를 SummonManager에 전달
                if (summonManager != null)
                {
                    summonManager.OnCardChoiceSelected(selectedCard);
                }

                // 선택 완료 후 패널 비활성화 또는 파괴 (SummonManager에서도 처리)
                // gameObject.SetActive(false); // 예시
                // Destroy(gameObject); // SummonManager에서 처리하는 것이 일반적

                // 카드 선택 완료 시 게임 필드 다시 활성화 (선택 패널이 숨겨지므로)
                if (gameplayFieldRoot != null)
                {
                    gameplayFieldRoot.SetActive(true);
                }
            }
            else
            {
                Debug.LogError($"잘못된 선택 버튼 인덱스: {choiceIndex}");
            }
        }

        // 필드 보기 버튼 클릭 시 호출될 함수
        private void OnViewFieldButtonClicked()
        {
            if (gameplayFieldRoot != null)
            {
                gameplayFieldRoot.SetActive(!gameplayFieldRoot.activeSelf); // 현재 상태 토글
                Debug.Log($"필드 보기 버튼 클릭. 게임 필드 활성화 상태: {gameplayFieldRoot.activeSelf}");

                // 패널은 계속 활성화 상태 유지
                gameObject.SetActive(true); // SummonChoicePanel을 계속 표시
            }
            else
            {
                Debug.LogError("게임 필드 루트 GameObject가 할당되지 않았습니다. 필드 보기 기능이 작동하지 않습니다.");
            }
        }

        // TODO: 패널 비활성화/파괴 시 호출될 함수 (옵션)
        // public void Hide() { gameObject.SetActive(false); }
    }
} 