using UnityEngine;

namespace InvaderInsider
{
    public interface IEnemy
    {
        int ID { get; }
        string Name { get; }
        float Health { get; }
        float Damage { get; }
        GameObject Prefab { get; }
    }
} 