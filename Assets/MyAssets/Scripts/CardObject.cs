using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardObject : MonoBehaviour//스크립터블
{
    public Card data = new Card();
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

[System.Serializable]
public class Card
{
    public string cName;
    public int cID = -1;
    public float range = 5f;
    public float fireRate = 1f;
    public float damage = 1f;
    public int grade = 0;

    public Card()
    {
        cName = "";
        cID = -1;
    }

    public Card(CardObject card)
    {
        cName = card.name;
        cID = card.data.cID;
        range = card.data.range;
        fireRate = card.data.fireRate;
        damage = card.data.damage;
        grade = card.data.grade;
    }

}
