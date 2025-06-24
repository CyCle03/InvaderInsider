using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using InvaderInsider.Managers;
using InvaderInsider.Cards; // CardType enum 사용을 위해 추가

namespace InvaderInsider.Data
{
    // 유니티 에디터에서 이 Scriptable Object를 생성할 수 있도록 메뉴 항목 추가
    [CreateAssetMenu(fileName = "CardDatabase", menuName = "InvaderInsider/Card Database")]
    public class CardDatabase : ScriptableObject
    {
        // 성능 최적화 상수들
        private const int EXPECTED_MIN_CARDS = 1;
        private const int EXPECTED_MAX_CARD_COST = 100;
        private const float MIN_VALID_STAT = 0f;
        
        [Header("Card Collections")]
        [SerializeField] private List<CardDBObject> allCards = new List<CardDBObject>();
        
        // 🚨 IMPORTANT: Unity Inspector 데이터 마이그레이션 안내
        // 기존에 타입별 리스트에 설정된 카드들이 있다면, 
        // 먼저 모든 카드를 allCards 리스트로 복사한 후 
        // 아래 주석된 필드들을 제거하세요.
        
        // 타입별 리스트는 제거됨 - 동적으로 제공됨 (2024.12.19)
        // [SerializeField] private List<CardDBObject> characterCards = new List<CardDBObject>();
        // [SerializeField] private List<CardDBObject> towerCards = new List<CardDBObject>();
        // [SerializeField] private List<CardDBObject> equipmentCards = new List<CardDBObject>();
        // [SerializeField] private List<CardDBObject> specialCards = new List<CardDBObject>();

        // 성능을 위한 캐싱된 딕셔너리
        private Dictionary<int, CardDBObject> cardLookup;
        private Dictionary<CardType, List<CardDBObject>> cardsByType;
        private bool isValidated = false;
        private bool isLookupTableBuilt = false; // 룩업 테이블 구축 상태 추가

        // Public 접근자들 - 캐싱된 딕셔너리에서 직접 반환
        public List<CardDBObject> AllCards => allCards;
        public List<CardDBObject> CharacterCards 
        {
            get
            {
                EnsureLookupTablesBuilt();
                return cardsByType?.GetValueOrDefault(CardType.Character, new List<CardDBObject>()) ?? new List<CardDBObject>();
            }
        }
        public List<CardDBObject> TowerCards 
        {
            get
            {
                EnsureLookupTablesBuilt();
                return cardsByType?.GetValueOrDefault(CardType.Tower, new List<CardDBObject>()) ?? new List<CardDBObject>();
            }
        }
        public List<CardDBObject> EquipmentCards 
        {
            get
            {
                EnsureLookupTablesBuilt();
                return cardsByType?.GetValueOrDefault(CardType.Equipment, new List<CardDBObject>()) ?? new List<CardDBObject>();
            }
        }

        private void OnEnable()
        {
            ValidateDatabase();
            BuildLookupTables();
        }

        // TODO 구현: 에디터 로드 시 유효성 검사 로직 추가
        [ContextMenu("Validate Database")]
        public bool ValidateDatabase()
        {
            if (isValidated) return true;

            bool isValid = true;
            var validationErrors = new List<string>();

            // 기본 유효성 검사
            if (allCards == null || allCards.Count < EXPECTED_MIN_CARDS)
            {
                validationErrors.Add("카드 데이터베이스가 비어있거나 null입니다.");
                isValid = false;
            }

            if (isValid)
            {
                // 개별 카드 유효성 검사
                var cardIds = new HashSet<int>();
                var cardNames = new HashSet<string>();

                for (int i = 0; i < allCards.Count; i++)
                {
                    var card = allCards[i];
                    if (card == null)
                    {
                        validationErrors.Add($"인덱스 {i}의 카드가 null입니다.");
                        isValid = false;
                        continue;
                    }

                    // ID 중복 검사
                    if (cardIds.Contains(card.cardId))
                    {
                        validationErrors.Add($"카드 ID {card.cardId}가 중복됩니다. (카드: {card.cardName})");
                        isValid = false;
                    }
                    else
                    {
                        cardIds.Add(card.cardId);
                    }

                    // 이름 중복 검사
                    if (!string.IsNullOrEmpty(card.cardName))
                    {
                        if (cardNames.Contains(card.cardName))
                        {
                            validationErrors.Add($"카드 이름 '{card.cardName}'이 중복됩니다.");
                            isValid = false;
                        }
                        else
                        {
                            cardNames.Add(card.cardName);
                        }
                    }

                    // 개별 카드 데이터 검증
                    if (!ValidateCardData(card, out string cardError))
                    {
                        validationErrors.Add($"카드 '{card.cardName}' 검증 실패: {cardError}");
                        isValid = false;
                    }
                }

                // 타입별 카드 리스트 검증
                ValidateCardTypeLists(validationErrors);
            }

            // 검증 결과 출력
            if (validationErrors.Count > 0)
            {
                LogManager.Error("CardDatabase", "유효성 검사 실패:\n{0}", string.Join("\n", validationErrors));
            }
            else
            {
                LogManager.Info("CardDatabase", "유효성 검사 통과: {0}개 카드", allCards.Count);
                isValidated = true;
            }

            return isValid;
        }

        private bool ValidateCardData(CardDBObject card, out string error)
        {
            error = string.Empty;

            // 필수 필드 검사
            if (string.IsNullOrEmpty(card.cardName))
            {
                error = "카드 이름이 비어있습니다.";
                return false;
            }

            if (card.cost < 0 || card.cost > EXPECTED_MAX_CARD_COST)
            {
                error = $"비용이 유효하지 않습니다 (0-{EXPECTED_MAX_CARD_COST}): {card.cost}";
                return false;
            }

            // 타입별 특별 검증
            switch (card.type)
            {
                case CardType.Character:
                case CardType.Tower:
                    // health, attack 속성이 CardDBObject에 없으므로 제거
                    // 필요시 TODO 주석의 속성들로 대체
                    break;

                case CardType.Equipment:
                    // 장비는 최소한 하나의 보너스 스탯이 있어야 함
                    if (card.equipmentBonusAttack <= 0 && card.equipmentBonusHealth <= 0)
                    {
                        error = "장비 카드는 최소한 하나의 보너스 스탯이 있어야 합니다.";
                        return false;
                    }
                    break;
            }

            return true;
        }

        private void ValidateCardTypeLists(List<string> validationErrors)
        {
            // 이제 단일 소스(allCards)에서 파생되므로 타입별 일관성은 자동으로 보장됨
            // 기본적인 타입 분포만 확인
            var typeDistribution = allCards.Where(c => c != null)
                                          .GroupBy(c => c.type)
                                          .ToDictionary(g => g.Key, g => g.Count());

            if (typeDistribution.Count == 0)
            {
                validationErrors.Add("카드 타입 분포가 비어있습니다.");
                return;
            }

            LogManager.Info("CardDatabase", "카드 타입 분포: {0}", string.Join(", ", typeDistribution.Select(kvp => $"{kvp.Key}: {kvp.Value}개")));
        }

        private void BuildLookupTables()
        {
            if (allCards == null) return;

            // ID 기반 빠른 검색을 위한 딕셔너리 구축
            cardLookup = new Dictionary<int, CardDBObject>(allCards.Count);
            cardsByType = new Dictionary<CardType, List<CardDBObject>>();

            foreach (var card in allCards)
            {
                if (card == null) continue;

                // ID 기반 룩업
                if (!cardLookup.ContainsKey(card.cardId))
                {
                    cardLookup[card.cardId] = card;
                }

                // 타입 기반 룩업
                if (!cardsByType.ContainsKey(card.type))
                {
                    cardsByType[card.type] = new List<CardDBObject>();
                }
                cardsByType[card.type].Add(card);
            }

            LogManager.Info("CardDatabase", "룩업 테이블 구축 완료: {0}개 카드, {1}개 타입", cardLookup.Count, cardsByType.Count);
            isLookupTableBuilt = true;
        }

        private void EnsureLookupTablesBuilt()
        {
            if (!isLookupTableBuilt)
            {
                BuildLookupTables();
            }
        }

        // 성능 최적화된 카드 검색 메서드들
        public CardDBObject GetCardById(int cardId)
        {
            if (cardLookup == null) BuildLookupTables();
            return cardLookup?.GetValueOrDefault(cardId);
        }

        public List<CardDBObject> GetCardsByType(CardType cardType)
        {
            if (cardsByType == null) BuildLookupTables();
            return cardsByType?.GetValueOrDefault(cardType, new List<CardDBObject>());
        }

        public List<CardDBObject> GetRandomCards(int count, CardType? filterType = null)
        {
            var sourceList = filterType.HasValue ? GetCardsByType(filterType.Value) : allCards;
            
            if (sourceList == null || sourceList.Count == 0) 
                return new List<CardDBObject>();

            var validCards = sourceList.Where(c => c != null).ToList();
            if (validCards.Count == 0) 
                return new List<CardDBObject>();

            // Fisher-Yates 셔플 알고리즘으로 랜덤 선택 최적화
            var result = new List<CardDBObject>(Mathf.Min(count, validCards.Count));
            var indices = Enumerable.Range(0, validCards.Count).ToList();
            
            for (int i = 0; i < count && i < validCards.Count; i++)
            {
                int randomIndex = Random.Range(i, indices.Count);
                (indices[i], indices[randomIndex]) = (indices[randomIndex], indices[i]);
                result.Add(validCards[indices[i]]);
            }

            return result;
        }

        // 에디터 전용 유틸리티 메서드들
        [ContextMenu("Rebuild Lookup Tables")]
        public void RebuildLookupTables()
        {
            InvalidateCache();
            BuildLookupTables();
        }

        [ContextMenu("Show Database Statistics")]
        public void ShowDatabaseStatistics()
        {
            if (!ValidateDatabase()) return;

            var stats = new System.Text.StringBuilder();
            stats.AppendLine("=== 카드 데이터베이스 통계 ===");
            stats.AppendLine($"총 카드 수: {allCards.Count}");
            
            EnsureLookupTablesBuilt();
            foreach (var kvp in cardsByType)
            {
                stats.AppendLine($"{kvp.Key}: {kvp.Value.Count}개");
            }

            LogManager.Info("CardDatabase", "{0}", stats.ToString());
        }

        // 캐시 무효화 메서드 (데이터 수정 시 호출)
        private void InvalidateCache()
        {
            cardLookup?.Clear();
            cardsByType?.Clear();
            isValidated = false;
            isLookupTableBuilt = false;
        }

        // 런타임에서 카드 추가/제거 시 사용할 메서드들
        public void AddCard(CardDBObject card)
        {
            if (card == null || allCards.Contains(card)) return;
            
            allCards.Add(card);
            InvalidateCache();
        }

        public bool RemoveCard(CardDBObject card)
        {
            if (card == null) return false;
            
            bool removed = allCards.Remove(card);
            if (removed)
            {
                InvalidateCache();
            }
            return removed;
        }

        public bool RemoveCardById(int cardId)
        {
            var card = GetCardById(cardId);
            return card != null && RemoveCard(card);
        }
    }
} 