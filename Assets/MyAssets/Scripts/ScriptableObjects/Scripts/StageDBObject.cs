using System.Collections;
using System.Collections.Generic;
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

    [System.Serializable]
    public class WaveList
    {
        public WaveObject[] ListSlots = new WaveObject[20];
        
        public void Clear()
        {
            for (int i = 0; i < ListSlots.Length; i++)
            {
                ListSlots[i].RemoveEnemy();
            }
        }
    }

    [System.Serializable]
    public class WaveObject
    {
        [System.NonSerialized]
        private IStageContainer parent;
        public int indexNum;

        [SerializeField]
        private Enemy enemyData;

        public IEnemy Enemy => enemyData;

        public GameObject EnemyPrefab
        {
            get
            {
                if (parent != null && enemyData != null && enemyData.ID >= 0)
                {
                    return parent.GetObject(enemyData.ID);
                }
                return null;
            }
        }

        public WaveObject()
        {
            enemyData = new Enemy();
        }

        public WaveObject(Enemy enemy)
        {
            UpdateListSlot(enemy);
        }

        public void SetParent(IStageContainer stageContainer)
        {
            parent = stageContainer;
        }

        public void UpdateListSlot(Enemy enemy)
        {
            enemyData = enemy;
        }

        public void RemoveEnemy()
        {
            enemyData = new Enemy();
        }
    }
}


