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
        [Header("UI Elements")]
        [SerializeField] private Transform handContainer; // 핸드 카드 아이템들이 배치될 부모 트랜스폼 (예: Horizontal Layout Group)
        [SerializeField] private GameObject handCardItemPrefab; // 핸드 카드 하나를 표시하는 UI 항목 프리팹
        [SerializeField] private Button fullViewButton; // 전체 핸드 보기 버튼 (선택 사항, 핸드 컨테이너 자체 클릭으로 대체 가능)

        private List<GameObject> currentHandItems = new List<GameObject>(); // 현재 UI에 표시된 핸드 카드 아이템 목록

        private SaveDataManager saveDataManager; // SaveDataManager 인스턴스 참조
        private CardDrawUI cardDrawUI; // CardDrawUI 인스턴스 참조
        [Header("Data References")]
        [SerializeField] private CardDatabase cardDatabase; // 카드 데이터베이스 Scriptable Object 참조

        private void Awake()
        {
            saveDataManager = SaveDataManager.Instance; // SaveDataManager 인스턴스 찾기
            cardDrawUI = CardDrawUI.Instance; // CardDrawUI 인스턴스 찾기

            if (saveDataManager == null)
            {
                Debug.LogError("SaveDataManager 인스턴스를 찾을 수 없습니다. HandDisplayPanel이 제대로 작동하지 않습니다.");
            }
            if (cardDrawUI == null)
            {
                Debug.LogError("CardDrawUI 인스턴스를 찾을 수 없습니다. HandDisplayPanel이 제대로 작동하지 않습니다.");
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

            // CardInteractionHandler 이벤트 구독 해지 (currentHandItems에 남아있는 것들)
            foreach (var item in currentHandItems)
            {
                CardInteractionHandler handler = item.GetComponent<CardInteractionHandler>();
                if (handler != null)
                {
                    handler.OnCardPlayInteractionCompleted.RemoveListener(HandleCardPlayInteractionCompleted);
                }
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

            // 기존 핸드 카드 아이템 모두 제거 (풀로 반환)
            foreach (var item in currentHandItems)
            {
                // TODO: 이전에 CardInteractionHandler 이벤트 구독 해지
                CardInteractionHandler handler = item.GetComponent<CardInteractionHandler>();
                if (handler != null)
                {
                    handler.OnCardPlayInteractionCompleted.RemoveListener(HandleCardPlayInteractionCompleted); // 이벤트 구독 해지
                }
                cardDrawUI.ReturnPooledCard(item); // 풀로 반환
            }
            currentHandItems.Clear();

            if (handContainer == null || handCardItemPrefab == null || cardDatabase == null || cardDrawUI == null) return;

            foreach (int cardId in handCardIds)
            {
                InvaderInsider.Data.CardDBObject cardData = cardDatabase.GetCardById(cardId);

                if (cardData != null)
                {
                    GameObject handItemGo = cardDrawUI.GetPooledCard(); // 풀에서 카드 가져오기
                    handItemGo.transform.SetParent(handContainer); // 핸드 컨테이너의 자식으로 설정
                    handItemGo.transform.localScale = Vector3.one; // 크기 초기화

                    CardDisplay display = handItemGo.GetComponent<CardDisplay>();
                    if (display != null)
                    {
                        display.SetupCard(cardData);
                    }

                    // CardInteractionHandler 이벤트 구독
                    CardInteractionHandler handler = handItemGo.GetComponent<CardInteractionHandler>();
                    if (handler != null)
                    {
                        handler.OnCardPlayInteractionCompleted.AddListener(HandleCardPlayInteractionCompleted); // 이벤트 구독
                    }
                    else
                    {
                        Debug.LogWarning($"No CardInteractionHandler found on hand card prefab for {cardData.cardName}.");
                    }

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

        // 카드가 플레이/업그레이드 상호작용을 완료했을 때 호출될 메서드
        private void HandleCardPlayInteractionCompleted(InvaderInsider.Cards.CardDisplay playedCardDisplay, InvaderInsider.UI.CardPlacementResult result)
        {
            InvaderInsider.Data.CardDBObject playedCardData = playedCardDisplay.GetCardData();

            // 성공적으로 필드에 놓이거나 업그레이드된 경우에만 핸드에서 제거
            if (result == CardPlacementResult.Success_Place || result == CardPlacementResult.Success_Upgrade)
            {
                Debug.Log($"Card {playedCardData.cardName} played/upgraded. Removing from hand.");
                // SaveDataManager에서 핸드 데이터 업데이트 (실제 카드 데이터 제거)
                saveDataManager.RemoveCardFromHand(playedCardData.cardId); 
                // HandDisplayPanel의 currentHandItems 리스트에서 직접 제거 (UpdateHandUI가 다시 호출될 것이므로 필수 아닐 수 있으나 명시적으로)
                // currentHandItems.Remove(playedCardDisplay.gameObject); // UpdateHandUI가 전체 갱신하므로 필요 없음
                // 풀로 반환은 CardInteractionHandler에서 이미 비활성화했으므로 여기서는 명시적으로 호출 안 함
            }
             else
             {
                 Debug.Log($"Card {playedCardData.cardName} interaction failed or returned to hand. Result: {result}");
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