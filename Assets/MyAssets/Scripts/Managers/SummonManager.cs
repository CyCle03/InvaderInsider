using UnityEngine;
using System.Collections.Generic;

namespace InvaderInsider.Managers
{
    public class SummonManager : MonoBehaviour
    {
        private static SummonManager instance;
        public static SummonManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<SummonManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("SummonManager");
                        instance = go.AddComponent<SummonManager>();
                    }
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
} 