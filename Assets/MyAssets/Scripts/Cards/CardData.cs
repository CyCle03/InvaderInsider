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

    [CreateAssetMenu(fileName = "New Card", menuName = "InvaderInsider/Card")]
    public class CardData : ScriptableObject
    {
        [Header("Card Info")]
        public int cardId;  // 카드의 고유 ID
        public string cardName;
        
        [Header("Properties")]
        public CardType type;
        public CardRarity rarity;
    }
} 