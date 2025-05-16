using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SummonManager : MonoBehaviour
{

    public static SummonManager smm = null;

    public CardDBObject[] cardDB;

    private void Awake()
    {
        if (smm == null)
        {
            smm = this;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SummonCard()
    {
        
    }
}
