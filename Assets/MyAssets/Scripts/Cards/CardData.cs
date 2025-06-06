using UnityEngine;

namespace InvaderInsider.Cards
{
    public enum CardRarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2,
        Legendary = 3
    }

    // 카드 타입 열거형
    public enum CardType
    {
        Character,
        Equipment,
        Tower
    }

    public enum EquipmentTargetType // 장비 아이템 대상 타입 열거형
    {
        None, // 장비가 아님
        Character, // 캐릭터에 적용 가능
        Tower // 타워에 적용 가능
    }

    [CreateAssetMenu(fileName = "New Card", menuName = "InvaderInsider/Card")]
    public class CardData : ScriptableObject
    {
        [Header("Card Info")]
        public int cardId;  // 카드의 고유 ID
        public string cardName;
        
        [Header("Properties")]
        public CardType type;
        public CardRarity rarity;
        public EquipmentTargetType equipmentTarget; // 장비 아이템 적용 대상
    }
} 