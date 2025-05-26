using UnityEngine;

namespace InvaderInsider
{
    public interface IStageData
    {
        int StageCount { get; }
        GameObject GetStageObject(int stageIndex, int objectIndex);
        int GetStageWaveCount(int stageIndex);
    }
} 