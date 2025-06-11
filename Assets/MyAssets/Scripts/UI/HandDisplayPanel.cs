using UnityEngine;
using UnityEngine.UI; // UI 요소 사용을 위해 추가
using System.Collections.Generic;
using InvaderInsider.Data; // CardDBObject, CardDatabase 사용을 위해 추가
using InvaderInsider.Managers; // SaveDataManager 사용을 위해 추가
using TMPro; // TextMeshPro 사용 시 추가
using InvaderInsider.Cards; // CardDrawUI, CardInteractionHandler 참조를 위해 추가

namespace InvaderInsider.UI
{
    // 핸드에 있는 카드들을 작게 표시하는 UI 패널 스크립트
    public class HandDisplayPanel : MonoBehaviour
    {
        private const string LOG_PREFIX = "[UI] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "Hand: SaveDataManager instance not found",
            "Hand: CardDrawUI instance not found",
            "Hand: Card Database not assigned",
            "Hand: UI update requested - {0} cards",
            "Hand: Card {0} ({1}) added to UI",
            "Hand: Card data not found for ID: {0}",
            "Hand: UI update completed - {0} items",
            "Hand: Full hand view requested",
            "Hand: Card {0} played/upgraded",
            "Hand: Card {0} interaction failed - {1}",
            "Hand: No CardInteractionHandler found for {0}"
        };

        [Header("UI Elements")]
        [SerializeField] private Transform handContainer; // 핸드 카드 아이템들이 배치될 부모 트랜스폼 (예: Horizontal Layout Group)
        [SerializeField] private GameObject handCardItemPrefab; // 핸드 카드 하나를 표시하는 UI 항목 프리팹
        [SerializeField] private Button fullViewButton; // 전체 핸드 보기 버튼 (선택 사항, 핸드 컨테이너 자체 클릭으로 대체 가능)

        private readonly List<GameObject> currentHandItems = new List<GameObject>(); // 현재 UI에 표시된 핸드 카드 아이템 목록

        private SaveDataManager saveManager; // SaveDataManager 인스턴스 참조
        private CardDrawUI cardDrawUI; // CardDrawUI 인스턴스 참조
        private bool isInitialized = false;

        [Header("Data References")]
        [SerializeField] private CardDatabase cardDatabase; // 카드 데이터베이스 Scriptable Object 참조
        private readonly string[] cachedStrings = new string[11];

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (isInitialized) return;

            saveManager = SaveDataManager.Instance;
            cardDrawUI = CardDrawUI.Instance;

            if (saveManager == null)
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[0]);
                return;
            }
            if (cardDrawUI == null)
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[1]);
                return;
            }
            if (cardDatabase == null)
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[2]);
                return;
            }

            if (saveManager != null)
            {
                saveManager.OnHandDataChanged += UpdateHandUI;
            }

            if (fullViewButton != null)
            {
                fullViewButton.onClick.RemoveAllListeners();
                fullViewButton.onClick.AddListener(ShowFullHandView);
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
            ClearHandItems();
        }

        private void CleanupEventListeners()
        {
            if (saveManager != null)
            {
                saveManager.OnHandDataChanged -= UpdateHandUI;
            }

            if (fullViewButton != null)
            {
                fullViewButton.onClick.RemoveAllListeners();
            }

            foreach (var item in currentHandItems)
            {
                if (item != null)
                {
                    var handler = item.GetComponent<CardInteractionHandler>();
                    if (handler != null)
                    {
                        handler.OnCardPlayInteractionCompleted.RemoveListener(HandleCardPlayInteractionCompleted);
                    }
                }
            }
        }

        private void ClearHandItems()
        {
            foreach (var item in currentHandItems)
            {
                if (item != null && cardDrawUI != null)
                {
                    var handler = item.GetComponent<CardInteractionHandler>();
                    if (handler != null)
                    {
                        handler.OnCardPlayInteractionCompleted.RemoveListener(HandleCardPlayInteractionCompleted);
                    }
                    cardDrawUI.ReturnPooledCard(item);
                }
            }
            currentHandItems.Clear();
        }

        // SaveDataManager의 OnHandDataChanged 이벤트 발생 시 호출될 함수
        private void UpdateHandUI(List<int> handCardIds)
        {
            if (!isInitialized) return;

            if (Application.isPlaying)
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[3], handCardIds.Count));
            }

            ClearHandItems();

            if (handContainer == null || handCardItemPrefab == null || cardDatabase == null || cardDrawUI == null) return;

            foreach (int cardId in handCardIds)
            {
                var cardData = cardDatabase.GetCardById(cardId);
                if (cardData == null)
                {
                    if (Application.isPlaying)
                    {
                        Debug.LogWarning(string.Format(LOG_PREFIX + LOG_MESSAGES[5], cardId));
                    }
                    continue;
                }

                var handItemGo = cardDrawUI.GetPooledCard();
                if (handItemGo == null) continue;

                handItemGo.transform.SetParent(handContainer);
                handItemGo.transform.localScale = Vector3.one;

                var display = handItemGo.GetComponent<CardDisplay>();
                if (display != null)
                {
                    display.SetupCard(cardData);
                }

                var handler = handItemGo.GetComponent<CardInteractionHandler>();
                if (handler != null)
                {
                    handler.OnCardPlayInteractionCompleted.AddListener(HandleCardPlayInteractionCompleted);
                }
                else if (Application.isPlaying)
                {
                    Debug.LogWarning(string.Format(LOG_PREFIX + LOG_MESSAGES[10], cardData.cardName));
                }

                currentHandItems.Add(handItemGo);

                if (Application.isPlaying)
                {
                    Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[4], cardData.cardName, cardId));
                }
            }

            if (Application.isPlaying)
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[6], currentHandItems.Count));
            }
        }

        // 전체 핸드 보기 UI를 활성화하는 함수 (SummonChoicePanel과 유사하게 UIManager 사용 가능)
        private void ShowFullHandView()
        {
            if (!isInitialized) return;

            if (Application.isPlaying)
            {
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[7]);
            }
        }

        // 카드가 플레이/업그레이드 상호작용을 완료했을 때 호출될 메서드
        private void HandleCardPlayInteractionCompleted(CardDisplay playedCardDisplay, CardPlacementResult result)
        {
            if (!isInitialized || saveManager == null) return;

            var playedCardData = playedCardDisplay.GetCardData();
            if (playedCardData == null) return;

            if (result == CardPlacementResult.Success_Place || result == CardPlacementResult.Success_Upgrade)
            {
                if (Application.isPlaying)
                {
                    Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[8], playedCardData.cardName));
                }
                saveManager.RemoveCardFromHand(playedCardData.cardId);
            }
            else if (Application.isPlaying)
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[9], playedCardData.cardName, result));
            }
        }

        // TODO: 핸드 카드 아이템 클릭 시 개별 카드 상세 정보 표시 로직 (선택 사항)
        // HandCardItemPrefab에 붙을 스크립트에서 구현하고 이벤트 등을 통해 이 함수 호출 가능
        // public void OnHandCardItemClicked(int cardId)
        // {
        //     Debug.Log($"핸드 카드 아이템 클릭됨: ID {cardId}");
        //     // TODO: CardDetailsPanel 등을 활성화하고 해당 카드 정보 전달
        //     // UIManager.Instance.ShowPanel("CardDetailsPanel"); // 예시
        //     // CardDetailsPanel.GetComponent<CardDetailsPanel>().DisplayCard(cardDatabaseManager?.GetCardData(cardId)); // 예시
        // }
    }

    // TODO: HandCardItemPrefab에 붙을 CardDisplayItem 스크립트 정의
    // 이 스크립트는 개별 카드의 이미지, 이름 등을 표시하고 클릭 이벤트를 처리합니다.

    // TODO: FullHandViewPanel 스크립트 정의 (전체 카드 UI를 관리할 스크립트)
    // 이 스크립트는 선택된 카드의 상세 정보를 받아와 크게 표시하는 역할을 합니다.
} 