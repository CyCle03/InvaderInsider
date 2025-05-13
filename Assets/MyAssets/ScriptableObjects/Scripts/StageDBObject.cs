using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Stage Database", menuName = "Stage System/StageDatabase")]
public class StageDBObject : ScriptableObject
{
    public int StageID = -1;
    public GameObject[] Container;
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
    public StageDBObject parant;
    [System.NonSerialized]
    public int indexNum;

    public Enemy enemy;

    public EnemyObject EnemyObj
    {
        get
        {
            if (enemy.eID >= 0)
            {
                return parant.Container[enemy.eID].GetComponent<EnemyObject>();
            }
            return null;
        }
    }

    public WaveObject()
    {
        UpdateListSlot(new Enemy());
    }

    public WaveObject(Enemy _enemy)
    {
        UpdateListSlot(_enemy);
    }

    public void UpdateListSlot(Enemy _enemy)
    {
        enemy = _enemy;
    }

    public void RemoveEnemy()
    {
        UpdateListSlot(new Enemy());
    }
}


