using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InvaderInsider.Data; // CardDBObject 사용을 위해 추가

public class CardObject : MonoBehaviour//ũͺ
{
    public CardDBObject cardData; // Card 클래스 대신 CardDBObject 사용

    // Start is called before the first frame update
    void Start()
    {
        // cardData가 설정되어 있는지 확인하거나, 필요한 경우 초기화 로직 추가
        if (cardData == null)
        {
            Debug.LogError("CardObject에 CardDBObject가 할당되지 않았습니다: " + gameObject.name);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
