using UnityEngine;

namespace InvaderInsider
{
    /// <summary>
    /// 프리팹의 누락된 스크립트 참조를 정리하는 도구
    /// </summary>
    public class PrefabCleaner : MonoBehaviour
    {
        private const string LOG_PREFIX = "[PrefabCleaner] ";
        
        [Header("Auto Clean")]
        [SerializeField] private bool autoCleanOnStart = true;
        [SerializeField] private float cleanDelay = 0.5f;
        
        private void Start()
        {
            if (autoCleanOnStart)
            {
                Invoke(nameof(CleanAllPrefabReferences), cleanDelay);
            }
        }
        
        /// <summary>
        /// 모든 프리팹 참조 정리
        /// </summary>
        [ContextMenu("Clean All Prefab References")]
        public void CleanAllPrefabReferences()
        {
            Debug.Log($"{LOG_PREFIX}프리팹 참조 정리 시작");
            
            // 모든 GameObject 찾기
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            int cleanedCount = 0;
            
            foreach (GameObject obj in allObjects)
            {
                if (obj == null) continue;
                
                // 누락된 컴포넌트 제거
                bool wasCleaned = RemoveMissingComponents(obj);
                if (wasCleaned)
                {
                    cleanedCount++;
                }
            }
            
            Debug.Log($"{LOG_PREFIX}프리팹 참조 정리 완료: {cleanedCount}개 오브젝트 정리됨");
        }
        
        /// <summary>
        /// 특정 오브젝트의 누락된 컴포넌트 제거
        /// </summary>
        private bool RemoveMissingComponents(GameObject obj)
        {
            Component[] components = obj.GetComponents<Component>();
            bool wasCleaned = false;
            
            for (int i = components.Length - 1; i >= 0; i--)
            {
                if (components[i] == null)
                {
                    Debug.Log($"{LOG_PREFIX}누락된 컴포넌트 제거: {obj.name}");
                    
                    // Unity에서 누락된 컴포넌트를 제거하는 방법
                    var serializedObject = new UnityEditor.SerializedObject(obj);
                    var prop = serializedObject.FindProperty("m_Component");
                    
                    for (int j = prop.arraySize - 1; j >= 0; j--)
                    {
                        var componentProp = prop.GetArrayElementAtIndex(j);
                        var componentRef = componentProp.FindPropertyRelative("component");
                        
                        if (componentRef.objectReferenceValue == null)
                        {
                            prop.DeleteArrayElementAtIndex(j);
                            wasCleaned = true;
                        }
                    }
                    
                    serializedObject.ApplyModifiedProperties();
                    break;
                }
            }
            
            return wasCleaned;
        }
        
        private void Update()
        {
            // Ctrl + Alt + C: 프리팹 정리
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.C))
            {
                CleanAllPrefabReferences();
            }
        }
    }
}