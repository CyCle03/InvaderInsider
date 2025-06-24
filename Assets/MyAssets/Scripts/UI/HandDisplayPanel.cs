using UnityEngine;
using UnityEngine.UI; // UI 요소 사용을 위해 추가
using System.Collections.Generic;
using InvaderInsider.Data; // CardDBObject, CardDatabase 사용을 위해 추가
using InvaderInsider.Managers; // SaveDataManager 사용을 위해 추가
using TMPro; // TextMeshPro 사용 시 추가
using InvaderInsider.Cards; // CardInteractionHandler 참조를 위해 추가

namespace InvaderInsider.UI
{
    // 핸드에 있는 카드들을 작게 표시하는 UI 패널 스크립트
    public class HandDisplayPanel : BasePanel
    {
        private const string LOG_TAG = "HandDisplay";

        [Header("팝업 오버레이 UI")]
        [SerializeField] private GameObject popupOverlay; // 전체 팝업 오버레이
        [SerializeField] private GameObject darkBackground; // 어두운 배경 (클릭 시 닫기)
        [SerializeField] private GameObject mainPanel; // 메인 패널
        [SerializeField] private Transform handContainer; // 카드들이 배치될 컨테이너 (Grid Layout)
        [SerializeField] private GameObject cardPrefab; // 카드 프리팹

        [Header("헤더 UI")]
        [SerializeField] private TextMeshProUGUI titleText; // "핸드 (5/10)" 형태
        [SerializeField] private Button closeButton; // X 버튼

        [Header("정렬 버튼들")]
        [SerializeField] private Button sortByTypeButton; // 타입별 정렬
        [SerializeField] private Button sortByCostButton; // 비용별 정렬
        [SerializeField] private Button sortByRarityButton; // 등급별 정렬
        [SerializeField] private Button sortByNameButton; // 이름별 정렬

        [Header("애니메이션 설정")]
        [SerializeField] private float animationDuration = 0.3f; // 팝업 애니메이션 시간
        [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private readonly List<GameObject> currentHandItems = new List<GameObject>(); // 현재 표시된 카드들
        private SaveDataManager saveManager;
        private CardManager cardManager;
        private bool isInitialized = false;
        
        // Object Pool for card display (CardDrawUI 제거로 인한 독립 풀링)
        private Queue<GameObject> cardDisplayPool = new Queue<GameObject>();
        private int initialPoolSize = 10;
        private bool isPopupOpen = false;
        private HandSortType currentSortType = HandSortType.None;
        private bool buttonsSetup = false; // 버튼 이벤트 등록 완료 플래그

        [Header("Data References")]
        [SerializeField] private CardDatabase cardDatabase;

        public enum HandSortType
        {
            None,
            ByType,
            ByCost,
            ByRarity,
            ByName
        }

        protected override void Initialize()
        {
            if (isInitialized) return;

            saveManager = SaveDataManager.Instance;
            cardManager = CardManager.Instance;

            if (saveManager == null)
            {
                LogManager.Error(LOG_TAG, "SaveDataManager instance not found");
                return;
            }
            if (cardManager == null)
            {
                LogManager.Error(LOG_TAG, "CardManager instance not found");
                return;
            }
            if (cardDatabase == null)
            {
                LogManager.Error(LOG_TAG, "Card Database not assigned");
                return;
            }

            // 카드 표시용 Object Pool 초기화 (CardDrawUI 대신)
            InitializeCardDisplayPool();

            // 이벤트 구독
            if (saveManager != null)
            {
                saveManager.OnHandDataChanged += OnHandDataChanged;
            }

            // 버튼 이벤트를 한 번만 등록
            if (!buttonsSetup)
            {
                SetupButtons();
                buttonsSetup = true;
            }
            
            SetupInitialState();

            isInitialized = true;
        }

        private void SetupButtons()
        {
            // 닫기 버튼
            if (closeButton != null)
                closeButton.onClick.AddListener(ClosePopup);

            // 정렬 버튼들
            if (sortByTypeButton != null)
                sortByTypeButton.onClick.AddListener(() => SortHand(HandSortType.ByType));

            if (sortByCostButton != null)
                sortByCostButton.onClick.AddListener(() => SortHand(HandSortType.ByCost));

            if (sortByRarityButton != null)
                sortByRarityButton.onClick.AddListener(() => SortHand(HandSortType.ByRarity));

            if (sortByNameButton != null)
                sortByNameButton.onClick.AddListener(() => SortHand(HandSortType.ByName));

            // 어두운 배경 클릭 시 닫기
            if (darkBackground != null)
            {
                var backgroundButton = darkBackground.GetComponent<Button>();
                if (backgroundButton == null)
                    backgroundButton = darkBackground.AddComponent<Button>();
                backgroundButton.onClick.AddListener(ClosePopup);
            }
        }

        private void SetupInitialState()
        {
            // 초기에는 팝업 숨김
            if (popupOverlay != null)
                popupOverlay.SetActive(false);

            isPopupOpen = false;
            currentSortType = HandSortType.None;
        }

        // 팝업 열기 (외부에서 호출)
        public void OpenPopup()
        {
            if (isPopupOpen || !isInitialized) return;

            if (popupOverlay != null)
                popupOverlay.SetActive(true);

            isPopupOpen = true;

            // 현재 핸드 데이터로 팝업 업데이트
            if (cardManager != null)
            {
                var handCardIds = cardManager.GetHandCardIds();
                UpdatePopupContent(handCardIds);
            }

            // 애니메이션 시작
            StartOpenAnimation();

            if (Application.isPlaying)
            {
                var cardCount = cardManager?.GetHandCardIds()?.Count ?? 0;
                LogManager.Info(LOG_TAG, "팝업 열림 - 카드 수: {0}", cardCount);
            }
        }

        // 팝업 닫기
        public void ClosePopup()
        {
            if (!isPopupOpen) return;

            StartCloseAnimation(() =>
            {
                if (popupOverlay != null)
                    popupOverlay.SetActive(false);

                isPopupOpen = false;
                ClearHandItems();

                if (Application.isPlaying)
                {
                    LogManager.Info(LOG_TAG, "팝업 닫힘");
                }
            });
        }

        // 핸드 데이터 변경 시 호출 (SaveDataManager 이벤트)
        private void OnHandDataChanged(List<int> handCardIds)
        {
            // 팝업이 열려있을 때만 업데이트
            if (isPopupOpen)
            {
                UpdatePopupContent(handCardIds);
            }
        }

        // 팝업 내용 업데이트
        private void UpdatePopupContent(List<int> handCardIds)
        {
            if (!isInitialized) return;

            ClearHandItems();
            UpdateTitle(handCardIds.Count);

            if (handContainer == null || cardPrefab == null || cardDatabase == null) return;

            // 정렬 적용
            var sortedCardIds = SortCardIds(handCardIds, currentSortType);

            foreach (int cardId in sortedCardIds)
            {
                var cardData = cardDatabase.GetCardById(cardId);
                if (cardData == null)
                {
                    if (Application.isPlaying)
                    {
                        LogManager.Warning(LOG_TAG, "카드 데이터를 찾을 수 없음 - ID: {0}", cardId);
                    }
                    continue;
                }

                var cardObj = GetPooledCard();
                if (cardObj == null) continue;

                cardObj.transform.SetParent(handContainer);
                cardObj.transform.localScale = Vector3.one;

                var display = cardObj.GetComponent<CardDisplay>();
                if (display != null)
                {
                    display.SetupCard(cardData);
                }

                // 드래그 앤 드롭 활성화
                var handler = cardObj.GetComponent<CardInteractionHandler>();
                if (handler != null)
                {
                    handler.enabled = true;
                    handler.OnCardPlayInteractionCompleted.AddListener(HandleCardPlayInteractionCompleted);
                }
                else if (Application.isPlaying)
                {
                    LogManager.Warning(LOG_TAG, "CardInteractionHandler가 없음: {0}", cardData.cardName);
                }

                currentHandItems.Add(cardObj);

                if (Application.isPlaying)
                {
                    LogManager.Info(LOG_TAG, "카드 추가됨: {0} (ID: {1})", cardData.cardName, cardId);
                }
            }
        }

        // 타이틀 업데이트 ("핸드 (5/10)" 형태)
        private void UpdateTitle(int handCount)
        {
            if (titleText != null)
            {
                int maxHandSize = 10; // 최대 핸드 크기 (설정 가능)
                titleText.text = $"핸드 ({handCount}/{maxHandSize})";
            }
        }

        // 정렬 기능
        public void SortHand(HandSortType sortType)
        {
            currentSortType = sortType;

            if (isPopupOpen && cardManager != null)
            {
                var handCardIds = cardManager.GetHandCardIds();
                UpdatePopupContent(handCardIds);
            }

            if (Application.isPlaying)
            {
                LogManager.Info(LOG_TAG, "정렬 적용됨: {0}", sortType.ToString());
            }
        }

        private List<int> SortCardIds(List<int> cardIds, HandSortType sortType)
        {
            var sortedIds = new List<int>(cardIds);

            switch (sortType)
            {
                case HandSortType.ByType:
                    sortedIds.Sort((id1, id2) =>
                    {
                        var card1 = cardDatabase.GetCardById(id1);
                        var card2 = cardDatabase.GetCardById(id2);
                        if (card1 == null || card2 == null) return 0;
                        return card1.type.CompareTo(card2.type);
                    });
                    break;

                case HandSortType.ByCost:
                    sortedIds.Sort((id1, id2) =>
                    {
                        var card1 = cardDatabase.GetCardById(id1);
                        var card2 = cardDatabase.GetCardById(id2);
                        if (card1 == null || card2 == null) return 0;
                        return card1.cost.CompareTo(card2.cost);
                    });
                    break;

                case HandSortType.ByRarity:
                    sortedIds.Sort((id1, id2) =>
                    {
                        var card1 = cardDatabase.GetCardById(id1);
                        var card2 = cardDatabase.GetCardById(id2);
                        if (card1 == null || card2 == null) return 0;
                        return card1.rarity.CompareTo(card2.rarity);
                    });
                    break;

                case HandSortType.ByName:
                    sortedIds.Sort((id1, id2) =>
                    {
                        var card1 = cardDatabase.GetCardById(id1);
                        var card2 = cardDatabase.GetCardById(id2);
                        if (card1 == null || card2 == null) return 0;
                        return string.Compare(card1.cardName, card2.cardName, System.StringComparison.Ordinal);
                    });
                    break;
            }

            return sortedIds;
        }

        // 애니메이션 메서드들
        private void StartOpenAnimation()
        {
            if (mainPanel != null)
            {
                // 스케일 애니메이션으로 팝업 등장
                mainPanel.transform.localScale = Vector3.zero;
                StartCoroutine(AnimateScale(Vector3.zero, Vector3.one, animationDuration));
            }
        }

        private void StartCloseAnimation(System.Action onComplete)
        {
            if (mainPanel != null)
            {
                StartCoroutine(AnimateScale(Vector3.one, Vector3.zero, animationDuration, onComplete));
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        private System.Collections.IEnumerator AnimateScale(Vector3 from, Vector3 to, float duration, System.Action onComplete = null)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime; // 게임 일시정지에 영향받지 않음
                float t = elapsed / duration;
                float curveValue = animationCurve.Evaluate(t);
                
                if (mainPanel != null)
                    mainPanel.transform.localScale = Vector3.Lerp(from, to, curveValue);
                
                yield return null;
            }
            
            if (mainPanel != null)
                mainPanel.transform.localScale = to;
            
            onComplete?.Invoke();
        }

        // 카드 상호작용 완료 처리
        private void HandleCardPlayInteractionCompleted(CardDisplay playedCardDisplay, CardPlacementResult result)
        {
            if (!isInitialized || cardManager == null) return;

            var playedCardData = playedCardDisplay.GetCardData();
            if (playedCardData == null) return;

            if (result == CardPlacementResult.Success_Place || result == CardPlacementResult.Success_Upgrade)
            {
                if (Application.isPlaying)
                {
                    LogManager.Info(LOG_TAG, "카드 {0} 플레이/업그레이드됨", playedCardData.cardName);
                }
                cardManager.RemoveCardFromHand(playedCardData.cardId);
            }
            else if (Application.isPlaying)
            {
                LogManager.Info(LOG_TAG, "카드 {0} 상호작용 실패: {1}", playedCardData.cardName, result);
            }
        }

        private void ClearHandItems()
        {
            foreach (var item in currentHandItems)
            {
                if (item != null)
                {
                    var handler = item.GetComponent<CardInteractionHandler>();
                    if (handler != null)
                    {
                        handler.OnCardPlayInteractionCompleted.RemoveListener(HandleCardPlayInteractionCompleted);
                    }
                    ReturnPooledCard(item);
                }
            }
            currentHandItems.Clear();
        }

        private void CleanupEventListeners()
        {
            if (saveManager != null)
            {
                saveManager.OnHandDataChanged -= OnHandDataChanged;
            }
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

        private void CleanupButtonEvents()
        {
            if (buttonsSetup)
            {
                if (closeButton != null)
                    closeButton.onClick.RemoveListener(ClosePopup);
                if (sortByTypeButton != null)
                    sortByTypeButton.onClick.RemoveAllListeners();
                if (sortByCostButton != null)
                    sortByCostButton.onClick.RemoveAllListeners();
                if (sortByRarityButton != null)
                    sortByRarityButton.onClick.RemoveAllListeners();
                if (sortByNameButton != null)
                    sortByNameButton.onClick.RemoveAllListeners();
                
                // 동적으로 생성된 배경 버튼 처리
                if (darkBackground != null)
                {
                    var backgroundButton = darkBackground.GetComponent<Button>();
                    if (backgroundButton != null)
                        backgroundButton.onClick.RemoveListener(ClosePopup);
                }
                
                buttonsSetup = false;
            }
        }

        private void OnDestroy()
        {
            CleanupButtonEvents();
            CleanupEventListeners();
            ClearHandItems();
        }

        protected override void OnShow()
        {
            base.OnShow();
            OpenPopup();
        }

        protected override void OnHide()
        {
            base.OnHide();
            ClosePopup();
        }

        // ==================== Object Pool 메서드들 (CardDrawUI 대신) ====================
        
        private void InitializeCardDisplayPool()
        {
            if (cardPrefab == null)
            {
                LogManager.Warning(LOG_TAG, "cardPrefab이 할당되지 않아 카드 풀 초기화를 건너뜁니다.");
                return;
            }

            for (int i = 0; i < initialPoolSize; i++)
            {
                GameObject cardObj = Instantiate(cardPrefab);
                cardObj.SetActive(false);
                cardDisplayPool.Enqueue(cardObj);
            }
        }

        private GameObject GetPooledCard()
        {
            if (cardDisplayPool.Count > 0)
            {
                var cardObj = cardDisplayPool.Dequeue();
                cardObj.SetActive(true);
                return cardObj;
            }
            else if (cardPrefab != null)
            {
                // 풀이 비어있으면 새로 생성
                return Instantiate(cardPrefab);
            }
            return null;
        }

        private void ReturnPooledCard(GameObject cardObj)
        {
            if (cardObj != null)
            {
                cardObj.SetActive(false);
                cardObj.transform.SetParent(null);
                cardDisplayPool.Enqueue(cardObj);
            }
        }
    }

    // TODO: HandCardItemPrefab에 붙을 CardDisplayItem 스크립트 정의
    // 이 스크립트는 개별 카드의 이미지, 이름 등을 표시하고 클릭 이벤트를 처리합니다.

    // TODO: FullHandViewPanel 스크립트 정의 (전체 카드 UI를 관리할 스크립트)
    // 이 스크립트는 선택된 카드의 상세 정보를 받아와 크게 표시하는 역할을 합니다.
} 