using UnityEngine;
using System.Collections;

namespace InvaderInsider.Core
{
    public class PooledObject : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Pool Settings")]
        [SerializeField] private float autoReturnTime = 5f;
        [SerializeField] private bool autoReturnOnDisable = true;
        [SerializeField] private float autoReturnDelay = 0f;
        
        [Header("Debug Info")]
        [SerializeField] private bool showDebugInfo = false;
        
        #endregion
        
        #region Private & Public Fields
        
        private ObjectPoolManager poolManager;
        private Coroutine autoReturnCoroutine;
        public System.Type ComponentType { get; private set; } // Public Property
        private bool isInPool = true;
        private float spawnTime;
        
        #endregion
        
        #region Properties
        
        public bool IsInPool => isInPool;
        public float SpawnTime => spawnTime;
        public float ActiveTime => isInPool ? 0f : Time.time - spawnTime;
        
        #endregion
        
        #region Unity Events
        
        private void Awake()
        {
            DetectComponentType();
        }
        
        private void OnDisable()
        {
            if (autoReturnOnDisable && !isInPool)
            {
                if (autoReturnDelay > 0f)
                {
                    Invoke(nameof(ReturnToPool), autoReturnDelay);
                }
                else
                {
                    ReturnToPool();
                }
            }
            
            if (autoReturnCoroutine != null)
            {
                StopCoroutine(autoReturnCoroutine);
                autoReturnCoroutine = null;
            }
        }
        
        #endregion
        
        #region Pool Management
        
        public void Initialize(ObjectPoolManager manager)
        {
            poolManager = manager;
        }

        public void OnObjectSpawned()
        {
            isInPool = false;
            spawnTime = Time.time;
            
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }
            
            if (autoReturnTime > 0)
            {
                autoReturnCoroutine = StartCoroutine(AutoReturnRoutine());
            }
        }

        public void ReturnToPool()
        {
            if (isInPool) return;
            
            Debug.Log($"[PooledObject] ReturnToPool() called on {gameObject.name}. Detected ComponentType: {(ComponentType?.Name ?? "NULL")}");

            if (autoReturnCoroutine != null)
            {
                StopCoroutine(autoReturnCoroutine);
                autoReturnCoroutine = null;
            }
            CancelInvoke(nameof(ReturnToPool));
            
            isInPool = true;
            
            if (ObjectPoolManager.Instance != null)
            {
                Debug.Log("[PooledObject] Calling ObjectPoolManager.Instance.ReturnObject.");
                ObjectPoolManager.Instance.ReturnObject(this);
            }
            else
            {
                Debug.LogError("[PooledObject] ObjectPoolManager.Instance is NULL. Deactivating object as a fallback.");
                gameObject.SetActive(false);
            }
        }
        
        public void DestroyImmediately()
        {
            isInPool = true;
            CancelInvoke();
            if (autoReturnCoroutine != null)
            {
                StopCoroutine(autoReturnCoroutine);
                autoReturnCoroutine = null;
            }
            Destroy(gameObject);
        }
        
        #endregion
        
        #region Helper Methods
        
        private IEnumerator AutoReturnRoutine()
        {
            yield return new WaitForSeconds(autoReturnTime);
            ReturnToPool();
        }
        
        private void DetectComponentType()
        {
            if (GetComponent<Projectile>() != null)
            {
                ComponentType = typeof(Projectile);
                return;
            }
            
            if (GetComponent<EnemyObject>() != null)
            {
                ComponentType = typeof(EnemyObject);
                return;
            }
            
            foreach (var component in GetComponents<Component>())
            {
                var type = component.GetType();
                if (type != typeof(Transform) && type != typeof(PooledObject))
                {
                    ComponentType = type;
                    break;
                }
            }
        }
        
        public void LogDebugInfo()
        {
            string status = isInPool ? "풀에 있음" : "활성 상태";
            float activeTime = isInPool ? 0f : Time.time - spawnTime;
            // Debug.Log($"[{gameObject.name}] 상태: {status}, 활성 시간: {activeTime:F2}초, 타입: {ComponentType?.Name}");
        }
        
        #endregion
        
        #region Editor Support
        
        #if UNITY_EDITOR
        [Header("Editor Tools")]
        [SerializeField] private bool testReturnToPool = false;
        
        private void OnValidate()
        {
            if (testReturnToPool)
            {
                testReturnToPool = false;
                if (Application.isPlaying)
                {
                    ReturnToPool();
                }
            }
        }
        #endif
        
        #endregion
    }
}
