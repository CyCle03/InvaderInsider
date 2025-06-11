using UnityEngine;
using System.Collections.Generic;
using InvaderInsider.Data; // SaveData 구조체, CardDBObject, CardDatabase 사용을 위해 추가
using InvaderInsider.Managers; // SaveDataManager 클래스 사용을 위해 추가
using InvaderInsider.UI; // UI 연동을 위해 추가
using System.Linq; // Linq 사용을 위해 추가 (SelectRandomCards 함수에서 사용)

namespace InvaderInsider.Managers
{
    public class SummonManager : MonoBehaviour
    {
        private const string LOG_PREFIX = "[Summon] ";
        private static readonly string[] LOG_MESSAGES = new string[]
        {
            "Card Database Scriptable Object is not assigned in the inspector!",
            "소환 데이터 로드: 횟수 = {0}, 비용 = {1}",
            "소환 데이터 없음: 횟수 = {0}, 비용 = {1}",
            "소환 데이터 저장: 횟수 = {0}",
            "SaveDataManager 인스턴스를 찾을 수 없습니다. 소환 데이터 저장 실패.",
            "SaveDataManager 인스턴스를 찾을 수 없습니다.",
            "Card Database is not assigned!",
            "eData 차감: {0}, 남은 eData: {1}",
            "소환 성공! 현재 횟수: {0}, 다음 소환 비용: {1}",
            "eData 부족! 현재 eData: {0}, 필요 비용: {1}",
            "Card data list from database is empty!",
            "Not enough cards available to select or total weight is zero.",
            "Weight calculation issue, selecting a random available card.",
            "총 {0}개의 카드가 선택되었습니다.",
            "Summon Choice Panel Prefab is not assigned!"
        };

        private static SummonManager instance;
        private static readonly object _lock = new object();
        private static bool isQuitting = false;

        public static SummonManager Instance
        {
            get
            {
                if (isQuitting) return null;

                lock (_lock)
                {
                    if (instance == null)
                    {
                        instance = FindObjectOfType<SummonManager>();
                        if (instance == null && !isQuitting)
                        {
                            GameObject go = new GameObject("SummonManager");
                            instance = go.AddComponent<SummonManager>();
                        }
                    }
                    return instance;
                }
            }
        }

        [Header("Summon Settings")]
        [SerializeField] private int initialSummonCost = 10; // 첫 소환 비용
        [SerializeField] private int summonCostIncrease = 1; // 소환 시 비용 증가량
        private int currentSummonCost; // 현재 소환 비용
        private int summonCount = 0; // 총 소환 횟수

        // 카드 데이터 관련 필드
        [Header("Card Data")]
        // 모든 CardDBObject 에셋 목록 대신 CardDatabase Scriptable Object를 참조합니다.
        [SerializeField] private CardDatabase cardDatabase; // 모든 카드 데이터를 담고 있는 CardDatabase Scriptable Object

        // 선택지 UI 관련 필드 (UI 패널 및 버튼 등)
        [Header("UI Settings")]
        [SerializeField] private GameObject summonChoicePanelPrefab; // 예시: 소환 결과 선택 UI 패널 프리팹
        private SummonChoicePanel currentSummonChoicePanel; // 생성된 소환 선택 패널 인스턴스

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                LoadSummonData();

                if (cardDatabase == null)
                {
                    Debug.LogError(LOG_PREFIX + LOG_MESSAGES[0]);
                }
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (currentSummonChoicePanel != null)
            {
                Destroy(currentSummonChoicePanel.gameObject);
                currentSummonChoicePanel = null;
            }
            isQuitting = true;
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        // 게임 데이터 로드 시 호출되어야 할 함수
        public void LoadSummonData()
        {
            if (SaveDataManager.Instance != null && SaveDataManager.Instance.CurrentSaveData != null)
            {
                summonCount = SaveDataManager.Instance.CurrentSaveData.progressData.summonCount;
                currentSummonCost = initialSummonCost + summonCount * summonCostIncrease;
                if (Application.isPlaying)
                {
                    Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[1], summonCount, currentSummonCost));
                }
            }
            else
            {
                summonCount = 0;
                currentSummonCost = initialSummonCost;
                if (Application.isPlaying)
                {
                    Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[2], summonCount, currentSummonCost));
                }
            }
        }

        // 게임 데이터 저장 시 호출되어야 할 함수
        public void SaveSummonData()
        {
            if (SaveDataManager.Instance != null && SaveDataManager.Instance.CurrentSaveData != null)
            {
                SaveDataManager.Instance.CurrentSaveData.progressData.summonCount = summonCount;
                SaveDataManager.Instance.SaveGameData();
                if (Application.isPlaying)
                {
                    Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[3], summonCount));
                }
            }
            else
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[4]);
            }
        }

        // 소환 실행 함수
        public void Summon()
        {
            if (SaveDataManager.Instance == null)
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[5]);
                return;
            }

            if (cardDatabase == null)
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[6]);
                return;
            }

            if (SaveDataManager.Instance.CurrentSaveData.progressData.currentEData >= currentSummonCost)
            {
                SaveDataManager.Instance.UpdateEData(-currentSummonCost);
                if (Application.isPlaying)
                {
                    Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[7], 
                        currentSummonCost, 
                        SaveDataManager.Instance.CurrentSaveData.progressData.currentEData));
                }

                summonCount++;
                currentSummonCost = initialSummonCost + summonCount * summonCostIncrease;
                if (Application.isPlaying)
                {
                    Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[8], summonCount, currentSummonCost));
                }

                List<CardDBObject> selectedCards = SelectRandomCards(3);
                DisplaySummonChoices(selectedCards);
            }
            else
            {
                if (Application.isPlaying)
                {
                    Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[9], 
                        SaveDataManager.Instance.CurrentSaveData.progressData.currentEData, 
                        currentSummonCost));
                }
            }
        }

        // 확률에 따라 카드를 선택하는 함수 (CardDatabase 사용)
        private List<CardDBObject> SelectRandomCards(int count)
        {
            List<CardDBObject> result = new List<CardDBObject>();

            if (cardDatabase == null)
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[6]);
                return result;
            }

            List<CardDBObject> allAvailableCards = cardDatabase.cards.ToList();

            if (allAvailableCards == null || allAvailableCards.Count == 0)
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[10]);
                return result;
            }

            List<CardDBObject> availableCards = new List<CardDBObject>(allAvailableCards);
            float totalWeight = availableCards.Sum(card => card != null ? card.summonWeight : 0f);

            System.Random rng = new System.Random();

            for (int i = 0; i < count; i++)
            {
                if (availableCards.Count == 0 || totalWeight <= 0)
                {
                    if (Application.isPlaying)
                    {
                        Debug.LogWarning(LOG_PREFIX + LOG_MESSAGES[11]);
                    }
                    break;
                }

                float randomWeight = (float)rng.NextDouble() * totalWeight;
                float currentWeight = 0f;
                CardDBObject selectedCard = null;

                for (int j = 0; j < availableCards.Count; j++)
                {
                    if (availableCards[j] == null) continue;

                    currentWeight += availableCards[j].summonWeight;
                    if (randomWeight <= currentWeight)
                    {
                        selectedCard = availableCards[j];
                        result.Add(selectedCard);
                        totalWeight -= selectedCard.summonWeight;
                        availableCards.RemoveAt(j);
                        break;
                    }
                }

                if (selectedCard == null && availableCards.Count > 0)
                {
                    if (Application.isPlaying)
                    {
                        Debug.LogWarning(LOG_PREFIX + LOG_MESSAGES[12]);
                    }

                    int randomIndex = rng.Next(availableCards.Count);
                    while(availableCards[randomIndex] == null && availableCards.Count > 0)
                    {
                        availableCards.RemoveAt(randomIndex);
                        if (availableCards.Count == 0) break;
                        randomIndex = rng.Next(availableCards.Count);
                    }

                    if (availableCards.Count > 0)
                    {
                        selectedCard = availableCards[randomIndex];
                        result.Add(selectedCard);
                        totalWeight -= selectedCard.summonWeight;
                        availableCards.RemoveAt(randomIndex);
                    }
                }
            }

            if (Application.isPlaying)
            {
                Debug.Log(string.Format(LOG_PREFIX + LOG_MESSAGES[13], result.Count));
            }

            return result;
        }

        // 뽑은 카드 선택지를 UI로 표시하는 함수
        private void DisplaySummonChoices(List<CardDBObject> choices)
        {
            if (summonChoicePanelPrefab == null)
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[14]);
                return;
            }

            if (currentSummonChoicePanel != null)
            {
                Destroy(currentSummonChoicePanel.gameObject);
                currentSummonChoicePanel = null;
            }

            // 패널 인스턴스 생성 및 설정
            GameObject panelGo = Instantiate(summonChoicePanelPrefab);
            currentSummonChoicePanel = panelGo.GetComponent<SummonChoicePanel>(); // SummonChoicePanel 스크립트가 있다고 가정

            if (currentSummonChoicePanel != null)
            {
                // 선택지 패널에 카드 데이터 전달 및 표시 요청
                currentSummonChoicePanel.DisplayChoices(choices);
                // TODO: 패널을 적절한 위치와 부모(Canvas 등) 아래 배치
            }
            else
            {
                Debug.LogError(LOG_PREFIX + LOG_MESSAGES[14]);
                Destroy(panelGo); // 생성된 게임 오브젝트 파괴
            }
        }

        // 플레이어가 소환 결과 중 하나의 카드를 선택했을 때 호출될 함수
        public void OnCardChoiceSelected(CardDBObject selectedCard)
        {
            if (selectedCard == null)
            {
                Debug.LogError("Selected card is null!");
                return;
            }

            Debug.Log($"플레이어가 {selectedCard.cardName} 카드를 선택했습니다.");

            // 선택된 카드를 핸드 및 소유 목록에 추가
            if (SaveDataManager.Instance != null)
            {
                SaveDataManager.Instance.AddCardToHandAndOwned(selectedCard.cardId);
                SaveSummonData(); // 카드 선택 후 소환 데이터 저장
            }
            else
            {
                Debug.LogError("SaveDataManager 인스턴스를 찾을 수 없습니다. 카드 추가 실패.");
            }

            // 소환 선택 UI 패널 닫기 (파괴 또는 비활성화)
            if (currentSummonChoicePanel != null)
            {
                 Destroy(currentSummonChoicePanel.gameObject); // 또는 currentSummonChoicePanel.Hide() 등
                 currentSummonChoicePanel = null;
            }

            // TODO: 핸드 UI 업데이트 이벤트 발생 또는 직접 호출
            // SaveDataManager.AddCardToHandAndOwned 내부에서 이벤트를 발생시키는 것이 좋음
        }

        // 게임 시작 시 (StageManager 초기화 등) 소환 데이터 로드를 호출해야 합니다.
        // 메인 메뉴에서 '새 게임' 또는 '불러오기' 시 SummonManager.Instance.LoadSummonData() 호출 필요
    }

    // TODO: SummonChoicePanel 스크립트 정의 (UI 패널에 붙여서 사용할 스크립트) - 이미 생성됨!
    // 이 스크립트는 선택지 카드 정보를 받아와 UI 요소를 업데이트하고
    // 플레이어의 선택을 SummonManager.OnCardChoiceSelected로 전달하는 역할을 합니다.

    // TODO: HandDisplayPanel 스크립트 정의 (핸드 UI를 관리할 스크립트) - 이미 생성됨!
    // 이 스크립트는 SaveDataManager의 핸드 데이터 변경 이벤트를 구독하여
    // 핸드에 있는 카드들을 작은 이미지로 표시하고, 클릭 시 전체 카드 UI를 보여주는 로직을 구현합니다.

    // TODO: FullHandViewPanel 스크립트 정의 (전체 카드 UI를 관리할 스크립트)
    // 이 스크립트는 선택된 카드의 상세 정보를 받아와 크게 표시하는 역할을 합니다.
}