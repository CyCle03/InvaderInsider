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
        public bool IsOccupied { get; private set; }

        public void SetOccupied(bool isOccupied)
        {
            IsOccupied = isOccupied;
        }
    }
}
