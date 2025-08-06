using UnityEngine;
using InvaderInsider.Cards; // CardRarity 및 CardType 열거형 사용을 위해 추가

namespace InvaderInsider.Data
{
    // 유니티 에디터에서 이 Scriptable Object를 생성할 수 있도록 메뉴 항목 추가
    [CreateAssetMenu(fileName = "NewCard", menuName = "InvaderInsider/Card Data")]
    public class CardDBObject : ScriptableObject
    {
        [Header("Card Information")]
        public int cardId; // 카드를 식별할 고유 ID
        public int level = 1; // 카드 레벨
        public string cardName; // 카드 이름
        [TextArea(3, 5)]
        public string description; // 카드 설명
        public Sprite artwork; // 카드 아트워크
        public int cost; // 카드 비용
        public int power; // 카드 능력치
        public CardRarity rarity; // 카드 등급
        public CardType type; // 카드 종류
        public EquipmentTargetType equipmentTarget; // 장비 아이템 적용 대상 추가

        [Header("Equipment Properties (if type is Equipment)")]
        public int equipmentBonusAttack; // 장비 아이템이 부여하는 추가 공격력
        public int equipmentBonusHealth; // 장비 아이템이 부여하는 추가 체력

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
} 