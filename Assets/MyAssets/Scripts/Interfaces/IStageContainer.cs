using UnityEngine;

namespace InvaderInsider
{
    public interface IStageContainer
    {
        int StageID { get; }
        GameObject GetObject(int index);
        int ObjectCount { get; }
    }
} 