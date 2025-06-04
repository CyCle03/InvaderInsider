using UnityEngine;

namespace InvaderInsider.Data
{
    // 유니티 에디터에서 이 Scriptable Object를 생성할 수 있도록 메뉴 항목 추가
    [CreateAssetMenu(fileName = "NewCard", menuName = "InvaderInsider/Card Data")]
    public class CardDBObject : ScriptableObject
    {
        [Header("Card Information")]
        public int cardId; // 카드를 식별할 고유 ID
        public string cardName; // 카드 이름
        [TextArea(3, 5)]
        public string cardDescription; // 카드 설명
        public CardRarity rarity; // 카드 등급
        public CardType cardType; // 카드 종류 추가

        [Header("Summon Settings")]
        [Tooltip("이 카드가 소환될 확률 가중치 (높을수록 잘 나옴)")]
        public float summonWeight = 1.0f; // 소환 확률 가중치

        [Header("Gameplay Settings")]
        [Tooltip("이 카드를 소환했을 때 생성될 게임 오브젝트 프리팹")]
        public GameObject cardPrefab; // 카드가 나타낼 게임 오브젝트 (예: 적 프리팹)

        // TODO: 필요에 따라 추가적인 카드 속성 정의
        // public int attackDamage; // 공격력
        // public int health; // 체력
        // public float cooldown; // 쿨다운
        // ... 등
    }

    // 카드 등급 Enum 정의
    public enum CardRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    // 카드 종류 Enum 정의
    public enum CardType
    {
        Character,
        Equipment,
        Tower
    }
} 