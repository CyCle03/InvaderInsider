using UnityEngine;
using UnityEngine.UI; // UI 요소 사용을 위해 추가

using InvaderInsider.Data; // CardDBObject, CardDatabase 사용을 위해 추가
using InvaderInsider.Managers; // SaveDataManager 사용을 위해 추가
using TMPro; // TextMeshPro 사용 시 추가
using InvaderInsider.Cards; // CardInteractionHandler 참조를 위해 추가
using System.Linq;

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
        [SerializeField] private GameObject cardDetailPrefab; // 카드 상세 프리팹

        [Header("헤더 UI")]
        [SerializeField] private TextMeshProUGUI titleText; // "핸드 (5/10)" 형태
        [SerializeField] private Button closeButton; // X 버튼

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
        private bool buttonsSetup = false; // 버튼 이벤트 등록 완료 플래그

        [Header("Data References")]
        [SerializeField] private CardDatabase cardDatabase;

        private GameObject currentDetailCardInstance; // 현재 생성된 상세 카드 인스턴스

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

            // 초기 핸드 상태 확인 추가
            CheckInitialHandState();

            // 카드 상세 패널 초기화
            if (cardDetailPrefab != null)
            {
                cardDetailPrefab.SetActive(false);
                
                // 상세 패널 클릭 시 닫기 기능 추가
                Button detailPanelButton = cardDetailPrefab.GetComponent<Button>();
                if (detailPanelButton == null)
                    detailPanelButton = cardDetailPrefab.AddComponent<Button>();
                detailPanelButton.onClick.AddListener(HideCardDetail);
            }

            isInitialized = true;
        }

        private void SetupButtons()
        {
            // 닫기 버튼
            if (closeButton != null)
                closeButton.onClick.AddListener(ClosePopup);

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
        }

        // 팝업 열기 (외부에서 호출)
        public void OpenPopup()
        {
            if (isPopupOpen || !isInitialized) return;

            if (popupOverlay != null)
                popupOverlay.SetActive(true);

            // 핸드 보기 시 어두운 배경 활성화
            if (darkBackground != null)
                darkBackground.SetActive(true);

            // 기존 상세 카드 인스턴스 제거
            if (currentDetailCardInstance != null)
            {
                Destroy(currentDetailCardInstance);
            }

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

                // 팝업 닫힐 때 어두운 배경도 비활성화
                if (darkBackground != null)
                    darkBackground.SetActive(false);

                // 상세 카드 인스턴스 제거
                if (currentDetailCardInstance != null)
                {
                    Destroy(currentDetailCardInstance);
                }

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
            // 카드 유무에 따른 패널 가시성 제어
            UpdatePanelVisibility(handCardIds);

            // 팝업이 열려있을 때만 업데이트
            if (isPopupOpen)
            {
                UpdatePopupContent(handCardIds);
            }
        }

        // 카드 유무에 따른 패널 가시성 제어 메서드 추가
        private void UpdatePanelVisibility(List<int> handCardIds)
        {
            bool hasCards = handCardIds != null && handCardIds.Count > 0;
            
            // 카드가 있으면 패널 활성화, 없으면 비활성화
            if (gameObject.activeSelf != hasCards)
            {
                gameObject.SetActive(hasCards);
                
                LogManager.Info(LOG_TAG, "패널 가시성 변경: {0} (카드 수: {1})", 
                    hasCards ? "활성화" : "비활성화", 
                    handCardIds?.Count ?? 0);
            }

            // 카드가 없으면 팝업도 자동으로 닫기
            if (!hasCards && isPopupOpen)
            {
                ClosePopup();
            }
        }

        // 초기 핸드 상태 확인 메서드 추가
        private void CheckInitialHandState()
        {
            if (cardManager != null)
            {
                var handCardIds = cardManager.GetHandCardIds();
                UpdatePanelVisibility(handCardIds);
            }
        }

        // 팝업 내용 업데이트
        private void UpdatePopupContent(List<int> handCardIds)
        {
            ClearHandItems();

            if (handCardIds == null || handCardIds.Count == 0)
            {
                UpdateTitle(0);
                return;
            }

            if (handContainer == null || cardPrefab == null || cardDatabase == null) return;

            // CardManager에서 실제로 선택된 카드만 필터링
            var selectedCardIds = handCardIds
                .Where(cardId => cardManager.IsCardSelectedInSummonChoice(cardId))
                .ToList();

            foreach (int cardId in selectedCardIds)
            {
                var cardData = cardDatabase.GetCardById(cardId);
                if (cardData != null)
                {
                    GameObject cardObj = GetPooledCard();
                    if (cardObj != null)
                    {
                        cardObj.transform.SetParent(handContainer, false);
                        cardObj.SetActive(true);

                        // CardIcon 컴포넌트 사용
                        CardIcon cardIcon = cardObj.GetComponent<CardIcon>();
                        if (cardIcon != null)
                        {
                            cardIcon.InitializeIcon(cardData);
                            cardIcon.OnCardClicked += ShowCardDetail; // 클릭 시 상세 정보 표시
                        }

                        currentHandItems.Add(cardObj);
                    }
                }
            }

            UpdateTitle(selectedCardIds.Count);
        }

        // 카드 상세 정보 표시
        private void ShowCardDetail(CardDBObject cardData)
        {
            // PopupOverlay 내부의 cardDetailPrefab 활성화
            if (cardDetailPrefab != null)
            {
                // 기존 상세 카드 인스턴스 제거
                if (currentDetailCardInstance != null)
                {
                    Destroy(currentDetailCardInstance);
                }

                // 메인 패널 비활성화
                if (mainPanel != null)
                    mainPanel.SetActive(false);

                // 카드 상세 보기 시 어두운 배경 활성화
                if (darkBackground != null)
                    darkBackground.SetActive(true);

                // 카드 데이터 설정 및 뷰 표시
                currentDetailCardInstance = Instantiate(cardDetailPrefab, popupOverlay.transform);
                CardIcon cardIcon = currentDetailCardInstance.GetComponent<CardIcon>();
                if (cardIcon != null)
                {
                    cardIcon.InitializeIcon(cardData);
                    cardIcon.OnCardClicked -= ShowCardDetail;
                }
                currentDetailCardInstance.SetActive(true);
            }
        }

        // 카드 상세 정보 숨기기
        public void HideCardDetail()
        {
            // PopupOverlay 내부의 cardDetailPrefab 비활성화
            if (currentDetailCardInstance != null)
            {
                // 메인 패널 다시 활성화
                if (mainPanel != null)
                    mainPanel.SetActive(true);

                // 상세 카드 인스턴스 제거
                Destroy(currentDetailCardInstance);
                currentDetailCardInstance = null;

                // 카드 상세 보기 닫을 때 어두운 배경 비활성화
                if (darkBackground != null)
                    darkBackground.SetActive(false);
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

        // 애니메이션 메서드들
        private void StartOpenAnimation()
        {
            if (mainPanel != null)
            {
                // 스케일 애니메이션으로 팝업 등장
                mainPanel.transform.localScale = Vector3.zero;
                AnimateScale(Vector3.zero, Vector3.one, animationDuration).Forget();
            }
        }

        private void StartCloseAnimation(System.Action onComplete)
        {
            if (mainPanel != null)
            {
                AnimateScale(Vector3.one, Vector3.zero, animationDuration, onComplete).Forget();
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        private async UniTask AnimateScale(Vector3 from, Vector3 to, float duration, System.Action onComplete = null)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime; // 게임 일시정지에 영향받지 않음
                float t = elapsed / duration;
                float curveValue = animationCurve.Evaluate(t);
                
                if (mainPanel != null)
                    mainPanel.transform.localScale = Vector3.Lerp(from, to, curveValue);
                
                await UniTask.Yield();
            }
            
            if (mainPanel != null)
                mainPanel.transform.localScale = to;
            
            onComplete?.Invoke();
        }

        private void ClearHandItems()
        {
            foreach (var item in currentHandItems)
            {
                if (item != null)
                {
                    // CardIcon 이벤트 정리
                    CardIcon cardIcon = item.GetComponent<CardIcon>();
                    if (cardIcon != null)
                    {
                        cardIcon.OnCardClicked -= ShowCardDetail;
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