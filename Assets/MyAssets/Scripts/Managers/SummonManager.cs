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
        private static SummonManager instance;
        public static SummonManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<SummonManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("SummonManager");
                        instance = go.AddComponent<SummonManager>();
                    }
                }
                return instance;
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

                // CardDatabase 참조 확인
                if (cardDatabase == null)
                {
                    Debug.LogError("Card Database Scriptable Object is not assigned in the inspector!");
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // 게임 데이터 로드 시 호출되어야 할 함수
        public void LoadSummonData()
        {
            if (SaveDataManager.Instance != null && SaveDataManager.Instance.CurrentSaveData != null)
            {
                 // 수정: progressData를 통해 summonCount 접근
                 summonCount = SaveDataManager.Instance.CurrentSaveData.progressData.summonCount;
                 currentSummonCost = initialSummonCost + summonCount * summonCostIncrease;
                 Debug.Log($"소환 데이터 로드: 횟수 = {summonCount}, 비용 = {currentSummonCost}");
            }
            else
            {
                // 저장 데이터가 없거나 로드되지 않은 경우
                summonCount = 0;
                currentSummonCost = initialSummonCost;
                Debug.Log($"소환 데이터 없음: 횟수 = {summonCount}, 비용 = {currentSummonCost}");
            }
        }

        // 게임 데이터 저장 시 호출되어야 할 함수
        public void SaveSummonData()
        {
             if (SaveDataManager.Instance != null && SaveDataManager.Instance.CurrentSaveData != null)
            {
                // 수정: progressData를 통해 summonCount 접근
                SaveDataManager.Instance.CurrentSaveData.progressData.summonCount = summonCount;
                SaveDataManager.Instance.SaveGameData(); // SaveDataManager에서 저장 호출
                 Debug.Log($"소환 데이터 저장: 횟수 = {summonCount}");
            }
             else
            {
                Debug.LogError("SaveDataManager 인스턴스를 찾을 수 없습니다. 소환 데이터 저장 실패.");
            }
        }


        // 소환 실행 함수
        public void Summon()
        {
            if (SaveDataManager.Instance == null)
            {
                Debug.LogError("SaveDataManager 인스턴스를 찾을 수 없습니다.");
                // UIManager.Instance.ShowMessagePanel("게임 데이터 관리자 오류!"); // 예시
                return;
            }

            if (cardDatabase == null)
            {
                Debug.LogError("Card Database is not assigned!");
                // UI로 메시지 표시
                // UIManager.Instance.ShowMessagePanel("카드 데이터베이스 오류!"); // 예시
                return;
            }

            // eData 확인
            if (SaveDataManager.Instance.CurrentSaveData.progressData.currentEData >= currentSummonCost)
            {
                // eData 차감 (선택지 확정 후 차감하도록 변경할 수 있음)
                SaveDataManager.Instance.UpdateEData(-currentSummonCost); // 음수 값을 전달하여 차감
                Debug.Log($"eData 차감: {currentSummonCost}, 남은 eData: {SaveDataManager.Instance.CurrentSaveData.progressData.currentEData}");

                // 소환 횟수 및 비용 증가 (선택지 확정 후 증가하도록 변경할 수 있음)
                summonCount++;
                currentSummonCost = initialSummonCost + summonCount * summonCostIncrease;
                Debug.Log($"소환 성공! 현재 횟수: {summonCount}, 다음 소환 비용: {currentSummonCost}");

                // 소환 데이터 저장 (선택지 확정 후 저장하도록 변경할 수 있음)
                // SaveSummonData();

                // 카드 데이터 로드 및 확률에 따라 3가지 선택지 생성 로직 구현
                List<CardDBObject> selectedCards = SelectRandomCards(3); // 실제 CardDBObject 반환하도록 수정

                // 3가지 선택지를 UI로 보여주는 로직 구현
                DisplaySummonChoices(selectedCards);

                // UI 업데이트 (eData는 SaveDataManager에서 이미 업데이트 이벤트 발생)
                // UIManager.Instance.UpdateSummonUI(summonCount, currentSummonCost); // 예시
            }
            else
            {
                // eData 부족
                Debug.Log($"eData 부족! 현재 eData: {SaveDataManager.Instance.CurrentSaveData.progressData.currentEData}, 필요 비용: {currentSummonCost}");
                // UIManager.Instance.ShowMessagePanel("eData가 부족합니다!"); // 예시
            }
        }

        // 확률에 따라 카드를 선택하는 함수 (CardDatabase 사용)
        private List<CardDBObject> SelectRandomCards(int count)
        {
            List<CardDBObject> result = new List<CardDBObject>();

            if (cardDatabase == null)
            {
                Debug.LogError("Card Database is not assigned!");
                return result; // 빈 리스트 반환
            }

            // CardDatabase에서 전체 카드 목록 가져오기
            List<InvaderInsider.Data.CardDBObject> allAvailableCards = cardDatabase.cards.ToList();

            if (allAvailableCards == null || allAvailableCards.Count == 0)
            {
                Debug.LogError("Card data list from database is empty!");
                return result; // 빈 리스트 반환
            }

            // 소환 가능한 카드 목록 (선택된 카드는 제외하고 복사본 사용)
            List<CardDBObject> availableCards = new List<CardDBObject>(allAvailableCards);

            // 누적 가중치 계산
            float totalWeight = availableCards.Sum(card => card != null ? card.summonWeight : 0f); // null 체크 추가

            System.Random rng = new System.Random();

            for (int i = 0; i < count; i++)
            {
                if (availableCards.Count == 0 || totalWeight <= 0)
                {
                    Debug.LogWarning("Not enough cards available to select or total weight is zero.");
                    break; // 더 이상 뽑을 카드가 없거나 가중치 총합이 0이면 중단
                }

                float randomWeight = (float)rng.NextDouble() * totalWeight;
                float currentWeight = 0f;
                CardDBObject selectedCard = null;

                // 가중치를 기반으로 카드 선택
                for (int j = 0; j < availableCards.Count; j++)
                {
                    if (availableCards[j] == null) continue; // null 카드 스킵

                    currentWeight += availableCards[j].summonWeight;
                    if (randomWeight <= currentWeight)
                    {
                        selectedCard = availableCards[j];
                        result.Add(selectedCard);

                        // 선택된 카드는 다음 선택에서 제외하고 총 가중치 업데이트
                        totalWeight -= selectedCard.summonWeight;
                        availableCards.RemoveAt(j);
                        break; // 현재 선택 루프 종료
                    }
                }

                // 만약 카드가 선택되지 않았다면 (가중치 문제 등)
                if (selectedCard == null && availableCards.Count > 0)
                {
                     Debug.LogWarning("Weight calculation issue, selecting a random available card.");
                     // 남은 카드 중 하나를 강제로 선택하는 예외 처리
                     int randomIndex = rng.Next(availableCards.Count);
                     // null이 아닌 카드를 찾을 때까지 반복 (극히 드물겠지만 안전 장치)
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
                         totalWeight -= selectedCard.summonWeight; // 해당 카드의 가중치만큼 차감
                         availableCards.RemoveAt(randomIndex);
                     }
                }
            }

            Debug.Log($"총 {result.Count}개의 카드가 선택되었습니다.");

            return result;
        }

        // 뽑은 카드 선택지를 UI로 표시하는 함수
        private void DisplaySummonChoices(List<CardDBObject> choices)
        {
            if (summonChoicePanelPrefab == null)
            {
                Debug.LogError("Summon Choice Panel Prefab is not assigned!");
                // UI로 메시지 표시 (예: UI 오류)
                // UIManager.Instance.ShowMessagePanel("소환 UI 오류!"); // 예시
                return;
            }

            // 기존 패널이 있으면 파괴 (또는 비활성화)
            if (currentSummonChoicePanel != null)
            {
                Destroy(currentSummonChoicePanel.gameObject);
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
                Debug.LogError("Summon Choice Panel Prefab does not have a SummonChoicePanel component!");
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