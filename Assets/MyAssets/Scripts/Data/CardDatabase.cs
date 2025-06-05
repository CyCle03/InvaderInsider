using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Linq 사용을 위해 추가

namespace InvaderInsider.Data
{
    // 유니티 에디터에서 이 Scriptable Object를 생성할 수 있도록 메뉴 항목 추가
    [CreateAssetMenu(fileName = "CardDatabase", menuName = "InvaderInsider/Card Database")]
    public class CardDatabase : ScriptableObject
    {
        [Header("Card List")]
        public List<CardDBObject> cards; // 모든 CardDBObject 에셋 목록

        private Dictionary<int, CardDBObject> _cardDictionary; // 카드 ID로 CardDBObject를 빠르게 찾기 위한 캐시

        // 에디터에서 데이터 로드 시 중복 ID 체크 등의 유효성 검사 수행 (옵션)
        private void OnEnable()
        {
            // Dictionary를 초기화하고 CardDBObject 목록을 채웁니다.
            _cardDictionary = new Dictionary<int, CardDBObject>();
            if (cards != null)
            {
                foreach (var card in cards)
                {
                    if (card != null && !_cardDictionary.ContainsKey(card.cardId))
                    {
                        _cardDictionary.Add(card.cardId, card);
                    }
                    else if (card != null)
                    {
                        Debug.LogWarning($"CardDatabase: Duplicate card ID found ({card.cardId}) for card {card.cardName}. Skipping.");
                    }
                }
            }
            // TODO: 필요하다면 에디터 로드 시 유효성 검사 로직 추가
            // 예: 중복 cardId 체크 등
#if UNITY_EDITOR
            CheckForDuplicateIds();
#endif
        }

#if UNITY_EDITOR
        private void CheckForDuplicateIds()
        {
            if (cards == null) return;

            var duplicates = cards.Where(c => c != null)
                                  .GroupBy(c => c.cardId)
                                  .Where(g => g.Count() > 1)
                                  .ToList();

            if (duplicates.Any())
            {
                Debug.LogError("CardDatabase: Duplicate Card IDs found!");
                foreach (var dupGroup in duplicates)
                {
                    Debug.LogError($"Card ID {dupGroup.Key} is used by multiple cards: {string.Join(", ", dupGroup.Select(c => c.cardName))}");
                }
            }
        }
#endif

        // 카드 ID로 CardDBObject를 찾아 반환하는 함수
        public CardDBObject GetCardById(int cardId)
        {
            if (_cardDictionary == null || _cardDictionary.Count == 0)
            {
                // 캐시가 비어있다면 OnEnable이 호출되지 않았거나 데이터가 없는 경우입니다.
                // 런타임에는 이 상황이 발생하지 않아야 합니다.
                OnEnable(); // 캐시 재구축 시도
                if (_cardDictionary == null || _cardDictionary.Count == 0) return null;
            }

            // Dictionary를 사용하여 O(1) 시간 복잡도로 조회
            if (_cardDictionary.TryGetValue(cardId, out CardDBObject card))
            {
                return card;
            }
            return null;
        }

        // 모든 카드 목록을 반환하는 함수 (소환 등에서 사용)
        // public List<CardDBObject> GetAllCards()
        // {
        //     // 읽기 전용 복사본을 반환하여 외부에서 원본 목록 수정 방지
        //     return cards != null ? new List<CardDBObject>(cards) : new List<CardDBObject>();
        // }
    }
} 