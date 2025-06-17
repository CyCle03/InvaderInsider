using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using InvaderInsider.Data;
using InvaderInsider.Managers;
using UnityEngine.Events;

namespace InvaderInsider.Cards
{
    public class CardDrawUI : MonoBehaviour
    {
        private static CardDrawUI instance; // 싱글톤 인스턴스
        public static CardDrawUI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<CardDrawUI>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("CardDrawUI");
                        instance = go.AddComponent<CardDrawUI>();
                        // DontDestroyOnLoad(go); // UI 매니저는 보통 씬에 고정되므로 DontDestroyOnLoad는 선택 사항
                    }
                }
                return instance;
            }
        }

        [Header("Draw Buttons")]
        [SerializeField] private Button singleDrawButton;
        [SerializeField] private Button multiDrawButton;
        [SerializeField] private TextMeshProUGUI singleDrawCostText;
        [SerializeField] private TextMeshProUGUI multiDrawCostText;

        [Header("Card Display")]
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Transform cardContainer;
        [SerializeField] private GameObject drawResultPanel;

        [Header("Object Pooling Settings")]
        [SerializeField] private int initialPoolSize = 10; // 초기 풀 크기
        private Queue<GameObject> cardPool = new Queue<GameObject>();

        private CardManager _cardManager;
        private GameManager _gameManager;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                // DontDestroyOnLoad(gameObject); // 선택 사항
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return; // 중복 인스턴스 파괴 후 즉시 종료
            }

            // 카드 풀만 먼저 초기화
            InitializeCardPool();
        }

        private void InitializeCardPool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                GameObject cardObj = Instantiate(cardPrefab, cardContainer);
                cardObj.SetActive(false); // 비활성화하여 풀에 보관
                cardPool.Enqueue(cardObj);
            }
        }

        private void Start()
        {
            // 매니저들 초기화 (Start에서 안전하게)
            InitializeManagers();
            InitializeUI();
            SubscribeToEvents();
        }

        private void InitializeManagers()
        {
            _cardManager = CardManager.Instance;
            _gameManager = GameManager.Instance;

            if (_cardManager == null)
            {
                Debug.LogError("CardManager instance not found!");
            }
            if (_gameManager == null)
            {
                Debug.LogError("GameManager instance not found!");
            }
        }

        private void InitializeUI()
        {
            // 비용 표시 업데이트
            UpdateCostTexts();

            // 버튼 이벤트 연결
            singleDrawButton.onClick.AddListener(() => _cardManager.DrawSingleCard());
            multiDrawButton.onClick.AddListener(() => _cardManager.DrawMultipleCards());

            // 초기에는 결과 패널 숨기기
            drawResultPanel.SetActive(false);
            
            // 초기 버튼 상태 설정
            RefreshButtonStates();
        }

        private void SubscribeToEvents()
        {
            // 카드 매니저 이벤트 구독
            _cardManager.OnCardDrawn.AddListener(new UnityEngine.Events.UnityAction<InvaderInsider.Data.CardDBObject>(ShowSingleCardResult));
            _cardManager.OnMultipleCardsDrawn.AddListener(new UnityEngine.Events.UnityAction<System.Collections.Generic.List<InvaderInsider.Data.CardDBObject>>(ShowMultipleCardResults));

            // eData는 이제 직접 호출 방식으로 업데이트됨 (이벤트 구독 제거)
            // UpdateButtonStates는 필요시 직접 호출
        }

        private void UpdateCostTexts()
        {
            singleDrawCostText.text = $"Draw 1 ({_cardManager.GetSingleDrawCost()} eData)";
            multiDrawCostText.text = $"Draw 5 ({_cardManager.GetMultiDrawCost()} eData)";
        }

        public void UpdateButtonStates(int currentEData)
        {
            singleDrawButton.interactable = currentEData >= _cardManager.GetSingleDrawCost();
            multiDrawButton.interactable = currentEData >= _cardManager.GetMultiDrawCost();
        }
        
        // eData 변경 없이도 버튼 상태를 업데이트하는 메서드
        public void RefreshButtonStates()
        {
            var saveDataManager = SaveDataManager.Instance;
            if (saveDataManager != null)
            {
                int currentEData = saveDataManager.GetCurrentEData();
                UpdateButtonStates(currentEData);
            }
        }

        private void ShowSingleCardResult(CardDBObject card)
        {
            ClearCardContainer();
            CreateCardDisplay(card);
            drawResultPanel.SetActive(true);
        }

        private void ShowMultipleCardResults(List<CardDBObject> cards)
        {
            ClearCardContainer();
            foreach (var card in cards)
            {
                CreateCardDisplay(card);
            }
            drawResultPanel.SetActive(true);
        }

        private GameObject CreateCardDisplay(CardDBObject card)
        {
            GameObject cardObj;
            if (cardPool.Count > 0)
            {
                cardObj = cardPool.Dequeue();
                cardObj.SetActive(true);
                cardObj.transform.SetParent(cardContainer);
                cardObj.transform.localScale = Vector3.one; // 크기 초기화 (필요시)
            }
            else
            {
                cardObj = Instantiate(cardPrefab, cardContainer);
            }
            
            CardDisplay display = cardObj.GetComponent<CardDisplay>();
            if (display != null)
            {
                display.SetupCard(card);
            }
            return cardObj; // 생성 또는 풀에서 가져온 GameObject 반환
        }

        // 외부에서 풀링된 카드를 가져갈 수 있도록 하는 메서드
        public GameObject GetPooledCard()
        {
            if (cardPool.Count > 0)
            {
                GameObject cardObj = cardPool.Dequeue();
                cardObj.SetActive(true); // 활성화
                return cardObj;
            }
            else
            {
                // 풀이 비어있으면 새로 생성
                GameObject cardObj = Instantiate(cardPrefab);
                Debug.LogWarning("Card pool exhausted, created a new card instance.");
                return cardObj;
            }
        }

        // 외부에서 사용이 끝난 카드를 풀로 반환하는 메서드
        public void ReturnPooledCard(GameObject cardObj)
        {
            cardObj.SetActive(false); // 비활성화
            cardObj.transform.SetParent(cardContainer); // 다시 풀 컨테이너로 이동 (선택 사항)
            cardPool.Enqueue(cardObj);
        }

        private void ClearCardContainer()
        {
            foreach (Transform child in cardContainer)
            {
                child.gameObject.SetActive(false);
                cardPool.Enqueue(child.gameObject);
            }
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (_cardManager != null)
            {
                _cardManager.OnCardDrawn.RemoveListener(new UnityEngine.Events.UnityAction<InvaderInsider.Data.CardDBObject>(ShowSingleCardResult));
                _cardManager.OnMultipleCardsDrawn.RemoveListener(new UnityEngine.Events.UnityAction<System.Collections.Generic.List<InvaderInsider.Data.CardDBObject>>(ShowMultipleCardResults));
            }

            // eData 이벤트 구독 해제 불필요 (직접 호출 방식으로 전환)
        }
    }
} 