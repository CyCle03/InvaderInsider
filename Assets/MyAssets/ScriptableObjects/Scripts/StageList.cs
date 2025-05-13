using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Stage List", menuName = "Stage System/StageList")]
public class StageList : ScriptableObject
{
    public StageDBObject[] stages;
}
