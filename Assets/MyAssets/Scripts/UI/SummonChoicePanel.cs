using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using InvaderInsider.Cards;
using InvaderInsider.Data;
using InvaderInsider.Managers;
using System.Linq;
using System.Text;

namespace InvaderInsider.UI
{
    public class SummonChoicePanel : BasePanel
    {
        // 성능 최적화 상수들
        private const int INITIAL_CARD_CAPACITY = 8;
        private const float CARD_COST_MULTIPLIER = 2f;
        
        private new const string LOG_PREFIX = "[SummonChoice] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "카드 매니저가 null입니다.",
            "{0}개의 카드를 표시합니다.",
            "카드 비용 {0}로 변경됨",
            "카드 클릭됨: {0}",
            "카드를 소환했습니다: {0}",
            "패널이 숨겨짐",
            "카드 버튼 프리팹이 null입니다.",
            "카드 데이터가 null입니다.",
            "카드 버튼 생성 실패",
            "게임을 일시정지했습니다.",
            "게임을 재개했습니다."
        };

        // 메모리 할당 최적화 - StringBuilder 재사용
        private static readonly StringBuilder stringBuilder = new StringBuilder(256);

        [Header("UI References")]
        [SerializeField] private Transform cardContainer;
        [SerializeField] private GameObject cardButtonPrefab;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button hideButton;
        [SerializeField] private Button showButton;
        [SerializeField] private GameObject backgroundOverlay; // 회색 배경 오버레이

        [Header("Debugging")]
        [SerializeField] private Button debugButton;
        [SerializeField] private Button demoButton;

        [Header("Hand Information")]
        [SerializeField] private TextMeshProUGUI handInfoText; // 핸드 정보 표시용 텍스트

        [Header("Card Settings")]
        // [SerializeField] private int maxDisplayCards = 3; // 사용되지 않으므로 주석 처리

        private List<CardDBObject> availableCards;
        private List<Button> cardButtons = new List<Button>(INITIAL_CARD_CAPACITY);
        private List<CardButton> cardButtonComponents = new List<CardButton>(INITIAL_CARD_CAPACITY);
        private UIManager uiManager;
        private CardManager cardManager;
        private GameManager gameManager;
        private bool wasGamePaused = false; // 원래 게임이 일시정지 상태였는지 추적
        private bool isPanelTemporarilyHidden = false; // 일시적으로 숨겨진 상태인지 추적
        private bool isInitialized = false; // 초기화 중복 방지 플래그
        private bool buttonsSetup = false; // 버튼 이벤트 등록 완료 플래그

        // 컴포넌트 캐싱 (BasePanel.canvasGroup 사용)
        private Canvas panelCanvas;

        protected override void Awake()
        {
            base.Awake();
            
            // 패널을 즉시 숨김 상태로 설정
            gameObject.SetActive(false);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            
            uiManager = UIManager.Instance;
            cardManager = CardManager.Instance;
            gameManager = GameManager.Instance;
            
            // BasePanel.Awake()에서 이미 Initialize()가 호출되므로 중복 호출 제거
        }

        protected override void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            base.Initialize();

            // 컴포넌트 캐싱
            panelCanvas = GetComponent<Canvas>();

            // 매니저 참조 캐싱
            cardManager = FindObjectOfType<CardManager>();
            gameManager = GameManager.Instance;
            uiManager = UIManager.Instance;

            if (cardManager == null)
            {
#if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[0]);
#endif
                return;
            }

            // 버튼 이벤트 설정
            SetupButtons();
            SetupCanvasProperties();
            isInitialized = true;
        }

        private void SetupCanvasProperties()
        {
            if (panelCanvas != null)
            {
                panelCanvas.sortingOrder = 10;
                panelCanvas.overrideSorting = true;
            }
        }

        public void SetupCards(List<CardDBObject> cards)
        {
            if (cards == null || cards.Count == 0)
            {
                Debug.LogError($"{LOG_PREFIX} 카드 리스트가 null이거나 비어있습니다.");
                return;
            }

            availableCards = cards;
            Debug.Log($"{LOG_PREFIX} 카드 수: {cards.Count}");

            if (cardContainer == null)
            {
                Debug.LogError($"{LOG_PREFIX} cardContainer가 null입니다.");
                return;
            }

            // 기존 카드 버튼 제거
            ClearCardButtons();

            // 새로운 카드 버튼 생성
            foreach (var card in cards)
            {
                CreateCardButton(card);
            }
        }

        private void ClearCardButtons()
        {
            foreach (var button in cardButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    Destroy(button.gameObject);
                }
            }
            cardButtons.Clear();
            cardButtonComponents.Clear(); // CardButton 컴포넌트 참조도 초기화
        }

        private void CreateCardButton(CardDBObject card)
        {
            if (cardButtonPrefab == null)
            {
                Debug.LogError($"{LOG_PREFIX} cardButtonPrefab이 null입니다.");
                return;
            }

            GameObject buttonObj = Instantiate(cardButtonPrefab, cardContainer);
            
            Button cardButton = buttonObj.GetComponent<Button>();
            CardButton cardButtonComponent = buttonObj.GetComponent<CardButton>();

            if (cardButton == null || cardButtonComponent == null)
            {
                Debug.LogError($"{LOG_PREFIX} 카드 버튼 컴포넌트를 찾을 수 없습니다. 프리팹 설정을 확인하세요.");
                
                // 생성된 오브젝트 정리
                if (buttonObj != null)
                {
                    Destroy(buttonObj);
                }
                return;
            }

            // CardButton 컴포넌트 초기화
            cardButtonComponent.Initialize(card);
            
            // 클릭 이벤트 연결
            cardButton.onClick.AddListener(() => HandleCardSelect(card));
            
            // 리스트에 추가
            cardButtons.Add(cardButton);
            cardButtonComponents.Add(cardButtonComponent);

            // 명시적으로 활성화
            buttonObj.SetActive(true);
        }

        private void HandleCardSelect(CardDBObject selectedCard)
        {
            if (cardManager != null)
            {
                cardManager.OnCardChoiceSelected(selectedCard);
            }
        }

        private void HandleCloseClick()
        {
            if (cardManager != null)
            {
                cardManager.OnCardChoiceSelected(null);
            }
        }

        private void HandleHideClick()
        {
            isPanelTemporarilyHidden = true;
            
            // 패널 UI 숨기기 (카드 컨테이너와 다른 UI 요소들)
            if (cardContainer != null)
            {
                cardContainer.gameObject.SetActive(false);
            }
            
            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(false);
            }
            
            if (hideButton != null)
            {
                hideButton.gameObject.SetActive(false);
            }
            
            // 배경 오버레이 숨기기 (필드를 클릭할 수 있도록)
            if (backgroundOverlay != null)
            {
                backgroundOverlay.SetActive(false);
            }
            
            // Show 버튼 활성화
            if (showButton != null)
            {
                showButton.gameObject.SetActive(true);
            }
        }

        private void HandleShowClick()
        {
            isPanelTemporarilyHidden = false;
            
            // 패널 UI 다시 보이기
            if (cardContainer != null)
            {
                cardContainer.gameObject.SetActive(true);
            }
            
            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(true);
            }
            
            if (hideButton != null)
            {
                hideButton.gameObject.SetActive(true);
            }
            
            // 배경 오버레이 다시 보이기
            if (backgroundOverlay != null)
            {
                backgroundOverlay.SetActive(true);
            }
            
            // Show 버튼 숨기기
            if (showButton != null)
            {
                showButton.gameObject.SetActive(false);
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
            
            // 게임 일시정지 (기존 상태 저장) - Pause UI 표시하지 않음
            if (GameManager.Instance != null)
            {
                wasGamePaused = GameManager.Instance.CurrentGameState == GameState.Paused;
                if (!wasGamePaused)
                {
                    GameManager.Instance.PauseGame(false); // Pause UI 표시하지 않음
                    Debug.Log("[SummonChoice] 게임을 일시정지했습니다.");
                }
            }

            // 핸드 정보 업데이트
            UpdateHandInfo();

            Debug.Log("[SummonChoice] 패널이 표시되었습니다.");
        }

        protected override void OnHide()
        {
            base.OnHide();
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + LOG_MESSAGES[5]);
            #endif
            
            // 게임 재개 (원래 일시정지 상태가 아니었던 경우에만)
            if (gameManager != null && !wasGamePaused && gameManager.CurrentGameState == GameState.Paused)
            {
                gameManager.ResumeGame();
                #if UNITY_EDITOR
                Debug.Log(LOG_PREFIX + LOG_MESSAGES[10]);
                #endif
            }
            
            // 상태 초기화
            isPanelTemporarilyHidden = false;
            wasGamePaused = false;
            
            // 배경 오버레이 상태 복구
            if (backgroundOverlay != null)
            {
                backgroundOverlay.SetActive(true);
            }
            
            ClearCardButtons();
        }

        // 외부에서 호출할 수 있는 공용 메서드들
        public void TemporarilyHidePanel()
        {
            HandleHideClick();
        }

        public void ShowPanelAgain()
        {
            HandleShowClick();
        }

        public bool IsPanelTemporarilyHidden()
        {
            return isPanelTemporarilyHidden;
        }

        public bool HasAvailableCards()
        {
            return availableCards != null && availableCards.Count > 0;
        }

        private void SetupButtons()
        {
            // 이미 설정되었으면 중복 설정 방지
            if (buttonsSetup)
            {
                return;
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HandleCloseClick);
            }
            
            // Hide/Show 버튼 이벤트 연결
            if (hideButton != null)
            {
                hideButton.onClick.AddListener(HandleHideClick);
            }
            
            if (showButton != null)
            {
                showButton.onClick.AddListener(HandleShowClick);
                showButton.gameObject.SetActive(false); // 처음에는 숨김
            }
            
            buttonsSetup = true;
        }
        
        private void CleanupButtonEvents()
        {
            if (buttonsSetup)
            {
                if (closeButton != null)
                    closeButton.onClick.RemoveListener(HandleCloseClick);
                if (hideButton != null)
                    hideButton.onClick.RemoveListener(HandleHideClick);
                if (showButton != null)
                    showButton.onClick.RemoveListener(HandleShowClick);
                
                buttonsSetup = false;
            }
        }

        private void OnDestroy()
        {
            // 버튼 이벤트 정리
            CleanupButtonEvents();
            
            // 명시적으로 리스트 정리
            cardButtons?.Clear();
            cardButtonComponents?.Clear();
            availableCards?.Clear();
        }

        /// <summary>
        /// 특정 인덱스의 카드 데이터를 변경
        /// </summary>
        public void UpdateCardData(int cardIndex, CardDBObject newCardData)
        {
            if (cardIndex < 0 || cardIndex >= cardButtonComponents.Count)
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + $"잘못된 카드 인덱스: {cardIndex}");
                #endif
                return;
            }

            if (newCardData == null)
            {
                #if UNITY_EDITOR
                Debug.LogError(LOG_PREFIX + "새로운 카드 데이터가 null입니다.");
                #endif
                return;
            }

            // 리스트의 카드 데이터 업데이트
            if (cardIndex < availableCards.Count)
            {
                availableCards[cardIndex] = newCardData;
            }

            // UI 업데이트
            cardButtonComponents[cardIndex].UpdateCardData(newCardData);
            
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + $"카드 {cardIndex} 데이터가 업데이트됨: {newCardData.cardName}");
            #endif
        }

        /// <summary>
        /// 모든 카드의 비용을 변경
        /// </summary>
        public void UpdateAllCardsCost(int newCost)
        {
            for (int i = 0; i < cardButtonComponents.Count; i++)
            {
                cardButtonComponents[i].UpdateCardCost(newCost);
                if (i < availableCards.Count)
                {
                    availableCards[i].cost = newCost;
                }
            }
            
            #if UNITY_EDITOR
            Debug.Log(LOG_PREFIX + $"모든 카드 비용이 {newCost}로 변경됨");
            #endif
        }

        /// <summary>
        /// 특정 카드의 이미지를 변경
        /// </summary>
        public void UpdateCardImage(int cardIndex, Sprite newSprite)
        {
            if (cardIndex < 0 || cardIndex >= cardButtonComponents.Count) return;
            
            cardButtonComponents[cardIndex].UpdateCardImage(newSprite);
            
            if (cardIndex < availableCards.Count)
            {
                availableCards[cardIndex].artwork = newSprite;
            }
        }

        /// <summary>
        /// 특정 카드의 설명을 변경
        /// </summary>
        public void UpdateCardDescription(int cardIndex, string newDescription)
        {
            if (cardIndex < 0 || cardIndex >= cardButtonComponents.Count) return;
            
            cardButtonComponents[cardIndex].UpdateCardDescription(newDescription);
            
            if (cardIndex < availableCards.Count)
            {
                availableCards[cardIndex].description = newDescription;
            }
        }

        /// <summary>
        /// 카드 선택 가능 상태 설정
        /// </summary>
        public void SetCardInteractable(int cardIndex, bool interactable)
        {
            if (cardIndex < 0 || cardIndex >= cardButtonComponents.Count) return;
            
            cardButtonComponents[cardIndex].SetInteractable(interactable);
        }

        /// <summary>
        /// 모든 카드의 선택 가능 상태 설정
        /// </summary>
        public void SetAllCardsInteractable(bool interactable)
        {
            foreach (var cardButton in cardButtonComponents)
            {
                cardButton.SetInteractable(interactable);
            }
        }

        /// <summary>
        /// 카드 선택 상태 표시
        /// </summary>
        public void SetCardSelected(int cardIndex, bool selected)
        {
            if (cardIndex < 0 || cardIndex >= cardButtonComponents.Count) return;
            
            cardButtonComponents[cardIndex].SetSelected(selected);
        }

        /// <summary>
        /// 현재 표시중인 카드 개수 반환
        /// </summary>
        public int GetCardCount()
        {
            return cardButtonComponents.Count;
        }

        /// <summary>
        /// 특정 인덱스의 카드 데이터 반환
        /// </summary>
        public CardDBObject GetCardData(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= availableCards.Count) return null;
            
            return availableCards[cardIndex];
        }

        /// <summary>
        /// 모든 카드 데이터 반환
        /// </summary>
        public List<CardDBObject> GetAllCardData()
        {
            return new List<CardDBObject>(availableCards);
        }

        // ============= Unity 에디터 도우미 기능 =============
        
        /// <summary>
        /// 에디터용: CardButton 프리팹 진단 및 수정 가이드
        /// </summary>
        #if UNITY_EDITOR
        [System.Obsolete("에디터 전용 도우미 메서드입니다.")]
        public void DiagnoseCardButtonPrefab()
        {
            if (cardButtonPrefab == null)
            {
                Debug.LogError(LOG_PREFIX + "cardButtonPrefab이 할당되지 않았습니다!");
                Debug.LogError(LOG_PREFIX + "해결 방법: SummonChoicePanel의 Card Button Prefab 필드에 프리팹을 할당하세요.");
                return;
            }

            Debug.Log(LOG_PREFIX + "=== CardButton 프리팹 진단 시작 ===");
            Debug.Log(LOG_PREFIX + $"프리팹 이름: {cardButtonPrefab.name}");
            
            // 컴포넌트 확인
            Button buttonComp = cardButtonPrefab.GetComponent<Button>();
            CardButton cardButtonComp = cardButtonPrefab.GetComponent<CardButton>();
            
            Debug.Log(LOG_PREFIX + $"Button 컴포넌트: {(buttonComp != null ? "✓ 있음" : "✗ 없음")}");
            Debug.Log(LOG_PREFIX + $"CardButton 컴포넌트: {(cardButtonComp != null ? "✓ 있음" : "✗ 없음")}");
            
            if (cardButtonComp == null)
            {
                Debug.LogError(LOG_PREFIX + "CardButton 컴포넌트가 없습니다!");
                Debug.LogError(LOG_PREFIX + "해결 방법: 프리팹에 CardButton 스크립트를 추가하세요.");
                return;
            }
            
            if (buttonComp == null)
            {
                Debug.LogError(LOG_PREFIX + "Button 컴포넌트가 없습니다!");
                Debug.LogError(LOG_PREFIX + "해결 방법: 프리팹에 Button 컴포넌트를 추가하세요.");
                return;
            }
            
            // UI 요소 진단
            Debug.Log(LOG_PREFIX + "=== 필수 UI 요소 진단 ===");
            CheckPrefabUIElement(cardButtonPrefab, "cardImage", typeof(UnityEngine.UI.Image));
            CheckPrefabUIElement(cardButtonPrefab, "cardNameText", typeof(TMPro.TextMeshProUGUI));
            CheckPrefabUIElement(cardButtonPrefab, "cardDescriptionText", typeof(TMPro.TextMeshProUGUI));
            CheckPrefabUIElement(cardButtonPrefab, "cardCostText", typeof(TMPro.TextMeshProUGUI));
            CheckPrefabUIElement(cardButtonPrefab, "rarityFrame", typeof(UnityEngine.UI.Image));
            
            Debug.Log(LOG_PREFIX + "=== 선택적 UI 요소 진단 ===");
            CheckPrefabUIElement(cardButtonPrefab, "cardPowerText", typeof(TMPro.TextMeshProUGUI));
            CheckPrefabUIElement(cardButtonPrefab, "cardTypeText", typeof(TMPro.TextMeshProUGUI));
            CheckPrefabUIElement(cardButtonPrefab, "rarityIcon", typeof(UnityEngine.UI.Image));
            CheckPrefabUIElement(cardButtonPrefab, "equipmentBonusPanel", typeof(GameObject));
            
            Debug.Log(LOG_PREFIX + "=== 진단 완료 ===");
            Debug.Log(LOG_PREFIX + "위의 ✗ 표시된 요소들을 프리팹에서 할당해주세요.");
        }
        
        private void CheckPrefabUIElement(GameObject prefab, string elementName, System.Type expectedType)
        {
            // 프리팹의 자식 오브젝트들에서 해당 이름의 요소 찾기
            Transform found = FindChildRecursive(prefab.transform, elementName);
            
            if (found != null)
            {
                Component expectedComp = found.GetComponent(expectedType);
                bool hasExpectedComponent = expectedComp != null;
                
                Debug.Log(LOG_PREFIX + $"{elementName}: {(hasExpectedComponent ? "✓ 있음" : "✗ 컴포넌트 없음")} (오브젝트는 있음)");
                
                if (!hasExpectedComponent)
                {
                    Debug.LogWarning(LOG_PREFIX + $"  └ 해결: '{found.name}' 오브젝트에 {expectedType.Name} 컴포넌트를 추가하세요.");
                }
            }
            else
            {
                Debug.Log(LOG_PREFIX + $"{elementName}: ✗ 없음");
                Debug.LogWarning(LOG_PREFIX + $"  └ 해결: 프리팹에 '{elementName}' 이름의 자식 오브젝트를 추가하세요.");
            }
        }
        
        private Transform FindChildRecursive(Transform parent, string childName)
        {
            // 직접 자식부터 확인
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }
            
            // 재귀적으로 하위 자식들 확인
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                Transform found = FindChildRecursive(child, childName);
                if (found != null)
                {
                    return found;
                }
            }
            
            return null;
        }
        #endif



        private void UpdateHandInfo()
        {
            if (handInfoText == null) return;

            if (CardManager.Instance != null)
            {
                var handCards = CardManager.Instance.GetHandCards();
                int handCount = handCards.Count;
                
                string handInfo = $"핸드: {handCount}장";
                if (handCount > 0)
                {
                    handInfo += "\n카드 목록:";
                    for (int i = 0; i < Mathf.Min(handCount, 3); i++) // 최대 3개까지만 표시
                    {
                        handInfo += $"\n• {handCards[i].cardName}";
                    }
                    if (handCount > 3)
                    {
                        handInfo += $"\n• ... (+{handCount - 3}장 더)";
                    }
                }

                handInfoText.text = handInfo;
                
            }
            else
            {
                handInfoText.text = "핸드: 정보 없음";
            }
        }

        private GameObject FindChildByName(string childName)
        {
            Transform found = FindChildRecursive(transform, childName);
            return found != null ? found.gameObject : null;
        }
        
        private void SetupCanvasSortingOrder()
        {
            // 패널 자체의 Canvas를 확인하고 없으면 추가
            Canvas panelCanvas = GetComponent<Canvas>();
            if (panelCanvas == null)
            {
                panelCanvas = gameObject.AddComponent<Canvas>();
                
                // GraphicRaycaster도 필요
                if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                {
                    gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                }
            }
            
            // Sorting Order를 높게 설정하여 다른 UI보다 위에 표시
            panelCanvas.overrideSorting = true;
            panelCanvas.sortingOrder = 100; // HealthSlider보다 높은 값
            
            #if UNITY_EDITOR
            LogManager.Info(LOG_PREFIX + "패널 Canvas Sorting Order 설정 완료: " + panelCanvas.sortingOrder);
            #endif
        }

        // 패널 숨기기 메서드 수정
        public override void Hide()
        {
            base.Hide(); // 부모 클래스의 Hide 메서드 호출
            
            // 필요한 경우 추가 정리 작업
            ClearCardButtons();
            
            // 게임 상태 복원 등 추가 로직
            if (gameManager != null && !wasGamePaused)
            {
                gameManager.ResumeGame();
            }
        }
    }
} 