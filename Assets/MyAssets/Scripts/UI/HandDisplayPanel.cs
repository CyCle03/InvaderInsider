using UnityEngine;
using UnityEngine.UI; // UI 요소 사용을 위해 추가
using System.Collections.Generic;
using InvaderInsider.Data; // CardDBObject, CardDatabase 사용을 위해 추가
using InvaderInsider.Managers; // SaveDataManager 사용을 위해 추가
using TMPro; // TextMeshPro 사용 시 추가

namespace InvaderInsider.UI
{
    // 핸드에 있는 카드들을 작게 표시하는 UI 패널 스크립트
    public class HandDisplayPanel : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Transform handContainer; // 핸드 카드 아이템들이 배치될 부모 트랜스폼 (예: Horizontal Layout Group)
        [SerializeField] private GameObject handCardItemPrefab; // 핸드 카드 하나를 표시하는 UI 항목 프리팹
        [SerializeField] private Button fullViewButton; // 전체 핸드 보기 버튼 (선택 사항, 핸드 컨테이너 자체 클릭으로 대체 가능)

        private List<GameObject> currentHandItems = new List<GameObject>(); // 현재 UI에 표시된 핸드 카드 아이템 목록

        private SaveDataManager saveDataManager; // SaveDataManager 인스턴스 참조
        [Header("Data References")]
        [SerializeField] private CardDatabase cardDatabase; // 카드 데이터베이스 Scriptable Object 참조

        private void Awake()
        {
            saveDataManager = SaveDataManager.Instance; // SaveDataManager 인스턴스 찾기
            if (saveDataManager == null)
            {
                Debug.LogError("SaveDataManager 인스턴스를 찾을 수 없습니다. HandDisplayPanel이 제대로 작동하지 않습니다.");
            }

            // CardDatabase 참조 확인
            if (cardDatabase == null)
            {
                Debug.LogError("Card Database Scriptable Object is not assigned in the inspector for HandDisplayPanel!");
            }

            // SaveDataManager의 핸드 데이터 변경 이벤트 구독
            if (saveDataManager != null)
            {
                saveDataManager.OnHandDataChanged += UpdateHandUI;
            }

            // 전체 핸드 보기 버튼 이벤트 리스너 추가 (선택 사항)
            if (fullViewButton != null)
            {
                fullViewButton.onClick.AddListener(ShowFullHandView);
            } /* else if (handContainer != null) // 핸드 컨테이너 자체 클릭으로 전체 보기 구현 시
            {
                 // TODO: 핸드 컨테이너에 Button 또는 EventTrigger 추가 후 클릭 리스너 등록
            }*/
        }

        private void OnDestroy()
        {
            // SaveDataManager 이벤트 구독 해지
            if (saveDataManager != null)
            {
                saveDataManager.OnHandDataChanged -= UpdateHandUI;
            }

            // 전체 핸드 보기 버튼 이벤트 리스너 제거 (선택 사항)
            if (fullViewButton != null)
            {
                fullViewButton.onClick.RemoveListener(ShowFullHandView);
            }
        }

        // SaveDataManager의 OnHandDataChanged 이벤트 발생 시 호출될 함수
        private void UpdateHandUI(List<int> handCardIds)
        {
            Debug.Log($"핸드 UI 업데이트 요청됨. 현재 핸드 카드 수: {handCardIds.Count}");

            // 기존 핸드 카드 아이템 모두 제거
            foreach (var item in currentHandItems)
            {
                Destroy(item);
            }
            currentHandItems.Clear();

            if (handContainer == null || handCardItemPrefab == null || cardDatabase == null) return; // 필요한 요소가 없으면 중단

            // 핸드에 있는 카드 ID 목록을 기반으로 UI 아이템 생성 및 추가
            foreach (int cardId in handCardIds)
            {
                // CardDatabase를 사용하여 cardId로 CardDBObject 찾기
                InvaderInsider.Data.CardDBObject cardData = cardDatabase.GetCardById(cardId);

                if (cardData != null)
                {
                    GameObject handItemGo = Instantiate(handCardItemPrefab, handContainer);
                    // TODO: handItemGo에 붙어있는 CardDisplayItem 스크립트 등에 카드 데이터 전달 및 UI 업데이트 요청
                    // CardDisplayItem itemScript = handItemGo.GetComponent<CardDisplayItem>(); // 예시
                    // if (itemScript != null) itemScript.SetCardData(cardData); // 예시

                    currentHandItems.Add(handItemGo);
                     Debug.Log($"핸드 UI에 카드 {cardData.cardName} ({cardId}) 추가됨.");
                }
                 else
                 {
                      Debug.LogWarning($"Card data not found for ID: {cardId}");
                 }
            }
            Debug.Log($"핸드 UI 업데이트 완료. 표시된 아이템 수: {currentHandItems.Count}");
        }

        // 전체 핸드 보기 UI를 활성화하는 함수 (SummonChoicePanel과 유사하게 UIManager 사용 가능)
        private void ShowFullHandView()
        {
            Debug.Log("전체 핸드 보기 버튼 클릭됨");
            // TODO: FullHandViewPanel UI 활성화 및 현재 핸드 데이터 전달
            // UIManager.Instance.ShowPanel("FullHandViewPanel"); // 예시
            // FullHandViewPanel.GetComponent<FullHandViewPanel>().DisplayHand(saveDataManager.CurrentSaveData.deckData.handCardIds); // 예시
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