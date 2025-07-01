using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InvaderInsider
{
    [CreateAssetMenu(fileName = "New Stage Database", menuName = "Stage System/StageDatabase")]
    public class StageDBObject : ScriptableObject, IStageContainer, IStageData
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

        public int StageCount => 1;

        public GameObject GetStageObject(int stageIndex, int objectIndex)
        {
            return GetObject(objectIndex);
        }

        public int GetStageWaveCount(int stageIndex)
        {
            return ObjectCount;
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
        private GameObject enemyPrefab;

        public GameObject EnemyPrefab => enemyPrefab;

        public WaveObject()
        {
            enemyPrefab = null;
        }

        public WaveObject(GameObject enemyPrefab)
        {
            UpdateListSlot(enemyPrefab);
        }

        public void SetParent(IStageContainer stageContainer)
        {
            parent = stageContainer;
        }

        public void UpdateListSlot(GameObject enemyPrefab)
        {
            this.enemyPrefab = enemyPrefab;
        }

        public void RemoveEnemy()
        {
            enemyPrefab = null;
        }
        
        public EnemyObject GetEnemyComponent()
        {
            if (enemyPrefab != null)
            {
                return enemyPrefab.GetComponent<EnemyObject>();
            }
            return null;
        }
        
        public bool IsValidEnemy()
        {
            return enemyPrefab != null && enemyPrefab.GetComponent<EnemyObject>() != null;
        }
    }
}


