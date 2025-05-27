using UnityEngine;

namespace InvaderInsider.Cards
{
    public enum CardType
    {
        Unit,
        Equipment,
        Tower
    }

    public enum CardRarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2,
        Legendary = 3
    }

    [System.Serializable]
    public class CardData
    {
        [Header("Card Info")]
        public string cardName;
        public string description;
        public CardType cardType;
        public CardRarity rarity;
        public int cardId;
        public Sprite cardImage;
        
        [Header("Cost")]
        public int eDataCost;

        [Header("Prefab Reference")]
        public GameObject prefab;  // Unit, Tower, or Equipment prefab
    }
} 