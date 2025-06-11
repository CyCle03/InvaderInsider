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
        private const string LOG_PREFIX = "[SummonChoice] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "SummonManager 인스턴스를 찾을 수 없습니다. SummonChoicePanel이 제대로 작동하지 않습니다.",
            "잘못된 수의 카드 선택지가 전달되었습니다. (필요: 3)",
            "선택 버튼 클릭됨: 인덱스 {0}, 카드: {1}",
            "잘못된 선택 버튼 인덱스: {0}"
        };

        [Header("UI Elements")]
        [SerializeField] private List<Button> choiceButtons; // 3개의 선택 버튼
        [SerializeField] private List<Image> cardImages; // 각 버튼에 표시할 카드 이미지
        [SerializeField] private List<TextMeshProUGUI> cardNames; // 각 버튼에 표시할 카드 이름 (TextMeshPro 사용 시)
        // [SerializeField] private List<Text> cardNamesLegacy; // 각 버튼에 표시할 카드 이름 (기본 Text 사용 시)

        private List<CardDBObject> currentChoices; // 현재 표시 중인 카드 선택지

        // SummonManager 인스턴스 참조 (Awake에서 찾도록 하거나 다른 방식으로 주입 가능)
        private SummonManager summonManager;
        private bool isInitialized = false;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (isInitialized) return;

            summonManager = SummonManager.Instance;
            if (summonManager == null)
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[0]);
                return;
            }

            if (choiceButtons != null)
            {
                for (int i = 0; i < choiceButtons.Count; i++)
                {
                    if (choiceButtons[i] != null)
                    {
                        int choiceIndex = i;
                        choiceButtons[i].onClick.RemoveAllListeners(); // 중복 등록 방지
                        choiceButtons[i].onClick.AddListener(() => OnChoiceButtonClicked(choiceIndex));
                    }
                }
            }

            isInitialized = true;
        }

        private void OnEnable()
        {
            if (!isInitialized)
            {
                Initialize();
            }
        }

        private void OnDisable()
        {
            CleanupEventListeners();
        }

        private void OnDestroy()
        {
            CleanupEventListeners();
            currentChoices?.Clear();
            currentChoices = null;
        }

        private void CleanupEventListeners()
        {
            if (choiceButtons != null)
            {
                foreach (var button in choiceButtons)
                {
                    if (button != null)
                    {
                        button.onClick.RemoveAllListeners();
                    }
                }
            }
        }

        // SummonManager에서 호출하여 선택지 카드 정보를 받아와 UI에 표시하는 함수
        public void DisplayChoices(List<CardDBObject> choices)
        {
            if (!isInitialized)
            {
                Initialize();
            }

            if (choices == null || choices.Count != 3)
            {
                if (Application.isPlaying)
                {
                    Debug.LogError(LOG_PREFIX + LOG_MESSAGES[1]);
                }
                gameObject.SetActive(false);
                return;
            }

            currentChoices = new List<CardDBObject>(choices);

            for (int i = 0; i < 3; i++)
            {
                bool hasValidChoice = i < choices.Count;
                UpdateChoiceUI(i, hasValidChoice ? choices[i] : null);
            }

            gameObject.SetActive(true);
        }

        private void UpdateChoiceUI(int index, CardDBObject card)
        {
            if (index < 0 || index >= 3) return;

            bool hasValidButton = choiceButtons != null && index < choiceButtons.Count;
            bool hasValidImage = cardImages != null && index < cardImages.Count;
            bool hasValidName = cardNames != null && index < cardNames.Count;

            if (hasValidButton)
            {
                choiceButtons[index].gameObject.SetActive(card != null);
            }

            if (card != null)
            {
                if (hasValidImage && card.artwork != null)
                {
                    cardImages[index].sprite = card.artwork;
                }

                if (hasValidName)
                {
                    cardNames[index].text = card.cardName;
                }
            }
        }

        // 버튼 클릭 시 호출될 함수
        private void OnChoiceButtonClicked(int choiceIndex)
        {
            if (!isInitialized || currentChoices == null)
            {
                return;
            }

            if (choiceIndex >= 0 && choiceIndex < currentChoices.Count)
            {
                CardDBObject selectedCard = currentChoices[choiceIndex];
                if (Application.isPlaying)
                {
                    Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[2], choiceIndex, selectedCard.cardName));
                }

                if (summonManager != null)
                {
                    summonManager.OnCardChoiceSelected(selectedCard);
                }
            }
            else if (Application.isPlaying)
            {
                Debug.LogError(string.Format(LOG_PREFIX + LOG_MESSAGES[3], choiceIndex));
            }
        }

        // TODO: 패널 비활성화/파괴 시 호출될 함수 (옵션)
        // public void Hide() { gameObject.SetActive(false); }
    }
} 