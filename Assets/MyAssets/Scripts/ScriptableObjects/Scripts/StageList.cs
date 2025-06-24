using UnityEngine;
using InvaderInsider.Managers;

namespace InvaderInsider
{
    [CreateAssetMenu(fileName = "New Stage List", menuName = "Stage System/StageList")]
    public class StageList : ScriptableObject, IStageData
    {
        [SerializeField] private ScriptableObject[] stageContainers;
        private IStageContainer[] stages;

        private void OnEnable()
        {
            // ScriptableObject가 로드될 때 컨테이너들을 인터페이스로 변환
            if (stageContainers != null)
            {
                stages = new IStageContainer[stageContainers.Length];
                for (int i = 0; i < stageContainers.Length; i++)
                {
                    stages[i] = stageContainers[i] as IStageContainer;
                    if (stages[i] == null)
                    {
                        LogManager.Error("StageList", "Stage container at index {0} does not implement IStageContainer interface!", i);
                    }
                }
            }
        }

        public int StageCount => stages != null ? stages.Length : 0;

        public GameObject GetStageObject(int stageIndex, int objectIndex)
        {
            if (stages == null || stageIndex < 0 || stageIndex >= stages.Length)
                return null;

            var stage = stages[stageIndex];
            if (stage == null)
                return null;

            return stage.GetObject(objectIndex);
        }

        public int GetStageWaveCount(int stageIndex)
        {
            if (stages == null || stageIndex < 0 || stageIndex >= stages.Length)
                return 0;

            var stage = stages[stageIndex];
            return stage != null ? stage.ObjectCount : 0;
        }
    }
} 