using UnityEngine;

namespace InvaderInsider
{
    [CreateAssetMenu(fileName = "New Stage Database", menuName = "Stage System/StageDatabase")]
    public class StageDBObject : ScriptableObject, IStageContainer
    {
        [SerializeField] private int stageID = -1;
        [SerializeField] private GameObject[] container;

        public int StageID => stageID;
        public int ObjectCount => container != null ? container.Length : 0;

        public GameObject GetObject(int index)
        {
            if (container == null || index < 0 || index >= container.Length)
                return null;
            
            return container[index];
        }
    }
} 