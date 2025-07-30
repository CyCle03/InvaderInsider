using UnityEngine;

namespace InvaderInsider
{
    public enum TileType
    {
        Route,
        Spawn
    }

    public class Tile : MonoBehaviour
    {
        public TileType tileType;
    }
}
