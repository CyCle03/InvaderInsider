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
        [SerializeField] private List<CardDBObject> allCards; // 모든 CardDBObject 에셋 목록

        // 에디터에서 데이터 로드 시 중복 ID 체크 등의 유효성 검사 수행 (옵션)
        private void OnEnable()
        {
            // TODO: 필요하다면 에디터 로드 시 유효성 검사 로직 추가
            // 예: 중복 cardId 체크 등
#if UNITY_EDITOR
            CheckForDuplicateIds();
#endif
        }

#if UNITY_EDITOR
        private void CheckForDuplicateIds()
        {
            if (allCards == null) return;

            var duplicates = allCards.Where(c => c != null)
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
            if (allCards == null) return null;

            // Linq를 사용하여 cardId가 일치하는 첫 번째 CardDBObject를 찾습니다.
            // TODO: 성능 개선을 위해 Dictionary 등으로 캐싱 고려 가능
            return allCards.FirstOrDefault(card => card != null && card.cardId == cardId);
        }

        // 모든 카드 목록을 반환하는 함수 (소환 등에서 사용)
        public List<CardDBObject> GetAllCards()
        {
            // 읽기 전용 복사본을 반환하여 외부에서 원본 목록 수정 방지
            return allCards != null ? new List<CardDBObject>(allCards) : new List<CardDBObject>();
        }
    }
} 