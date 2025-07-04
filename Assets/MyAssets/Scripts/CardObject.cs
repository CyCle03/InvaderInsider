using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InvaderInsider.Data;

namespace InvaderInsider
{
    public class CardObject : MonoBehaviour
    {
        public CardDBObject cardData;

        // Start is called before the first frame update
        void Start()
        {
            // cardData가 설정되어 있는지 확인하거나, 필요한 경우 초기화 로직 추가
            if (cardData == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("CardObject에 CardDBObject가 할당되지 않았습니다: " + gameObject.name);
#endif
            }
        }

        // Update 메서드 제거 - 불필요한 빈 Update는 성능에 부정적 영향
    }
}
