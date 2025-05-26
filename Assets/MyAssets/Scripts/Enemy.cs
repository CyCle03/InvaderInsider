using UnityEngine;

namespace InvaderInsider
{
    [System.Serializable]
    public class Enemy : IEnemy
    {
        [SerializeField] private int id = -1;
        [SerializeField] private string enemyName = "Unknown";
        [SerializeField] private float health = 1f;
        [SerializeField] private float damage = 1f;
        [SerializeField] private GameObject prefab;

        public int ID => id;
        public string Name => enemyName;
        public float Health => health;
        public float Damage => damage;
        public GameObject Prefab => prefab;

        public Enemy()
        {
            id = -1;
            enemyName = "Empty";
            health = 1;
            damage = 1;
            prefab = null;
        }
    }
} 