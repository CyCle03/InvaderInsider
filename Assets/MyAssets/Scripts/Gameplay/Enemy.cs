using UnityEngine;
using InvaderInsider.Managers;

namespace InvaderInsider.Gameplay
{
    public class Enemy : MonoBehaviour
    {
        [SerializeField] private int eDataDropAmount = 10;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Projectile"))
            {
                // 발사체에 의한 파괴 - eData 드랍
                GameManager.Instance.AddEData(eDataDropAmount);
                Destroy(other.gameObject);
                Destroy(gameObject);
            }
            else if (other.name == "WayPoint2")
            {
                // 목적지 도달 - eData 드랍하지 않음
                Destroy(gameObject);
            }
        }
    }
} 