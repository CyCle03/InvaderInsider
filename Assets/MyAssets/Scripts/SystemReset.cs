using UnityEngine;

namespace InvaderInsider
{
    /// <summary>
    /// 드래그 시스템 완전 재설정
    /// </summary>
    public class SystemReset : MonoBehaviour
    {
        [ContextMenu("Complete System Reset")]
        public void CompleteSystemReset()
        {
            Debug.Log("[SystemReset] 시스템 완전 재설정 시작");
            
            // 1. 모든 BaseCharacter 찾기
            BaseCharacter[] allCharacters = FindObjectsOfType<BaseCharacter>();
            Debug.Log($"[SystemReset] 발견된 캐릭터: {allCharacters.Length}개");
            
            foreach (BaseCharacter character in allCharacters)
            {
                if (character == null) continue;
                
                // 2. 새로운 컴포넌트 추가
                SimpleDraggableUnit draggable = character.GetComponent<SimpleDraggableUnit>();
                if (draggable == null)
                {
                    draggable = character.gameObject.AddComponent<SimpleDraggableUnit>();
                    Debug.Log($"[SystemReset] SimpleDraggableUnit 추가: {character.name}");
                }
                
                SimpleMergeTarget mergeTarget = character.GetComponent<SimpleMergeTarget>();
                if (mergeTarget == null)
                {
                    mergeTarget = character.gameObject.AddComponent<SimpleMergeTarget>();
                    Debug.Log($"[SystemReset] SimpleMergeTarget 추가: {character.name}");
                }
                
                // 3. Collider 확인
                Collider col = character.GetComponent<Collider>();
                if (col == null)
                {
                    BoxCollider boxCol = character.gameObject.AddComponent<BoxCollider>();
                    boxCol.isTrigger = true;
                    Debug.Log($"[SystemReset] BoxCollider 추가: {character.name}");
                }
                else if (!col.isTrigger)
                {
                    col.isTrigger = true;
                    Debug.Log($"[SystemReset] Collider를 Trigger로 설정: {character.name}");
                }
            }
            
            // 4. DragAndMergeSystem 확인
            if (DragAndMergeSystem.Instance == null)
            {
                GameObject systemObj = new GameObject("DragAndMergeSystem");
                systemObj.AddComponent<DragAndMergeSystem>();
                Debug.Log("[SystemReset] DragAndMergeSystem 생성됨");
            }
            
            Debug.Log("[SystemReset] 시스템 완전 재설정 완료");
        }
        
        private void Update()
        {
            // F4 키로 완전 재설정
            if (Input.GetKeyDown(KeyCode.F4))
            {
                CompleteSystemReset();
            }
        }
    }
}