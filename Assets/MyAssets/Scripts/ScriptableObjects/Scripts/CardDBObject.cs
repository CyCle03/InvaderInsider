using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Card Database", menuName = "Card System/CardDatabase")]
public class CardDBObject : ScriptableObject
{
    public int CardDeckID = -1;
    public GameObject[] Container;
}
