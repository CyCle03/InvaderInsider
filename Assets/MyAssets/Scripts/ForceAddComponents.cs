using UnityEngine;

namespace InvaderInsider
{
    /// <summary>
    /// 강제로 모든 타워에 컴포넌트를 추가하는 스크립트
    /// </summary>
    public class ForceAddComponents : MonoBehaviour
    {
        private void Update()
        {
            // F1 키를 누르면 강제로 모든 타워에 컴포넌트 추가
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ForceAddToAllTowers();
            }
        }

        [ContextMenu("Force Add Components to All Towers")]
        public void ForceAddToAllTowers()
        {
            // 모든 Tower 찾기
            Tower[] towers = FindObjectsOfType<Tower>();
            Debug.Log($"[ForceAdd] 발견된 타워 수: {towers.Length}");

            foreach (Tower tower in towers)
            {
                if (tower == null) continue;

                Debug.Log($"[ForceAdd] 처리 중인 타워: {tower.name}");

                // DraggableUnit 컴포넌트 확인/추가
                DraggableUnit draggable = tower.GetComponent<DraggableUnit>();
                if (draggable == null)
                {
                    draggable = tower.gameObject.AddComponent<DraggableUnit>();
                    Debug.Log($"[ForceAdd] DraggableUnit 추가됨: {tower.name}");
                }
                else
                {
                    Debug.Log($"[ForceAdd] DraggableUnit 이미 있음: {tower.name}");
                }
                draggable.enabled = true;

                // UnitMergeTarget 컴포넌트 확인/추가
                UnitMergeTarget mergeTarget = tower.GetComponent<UnitMergeTarget>();
                if (mergeTarget == null)
                {
                    mergeTarget = tower.gameObject.AddComponent<UnitMergeTarget>();
                    Debug.Log($"[ForceAdd] UnitMergeTarget 추가됨: {tower.name}");
                }
                else
                {
                    Debug.Log($"[ForceAdd] UnitMergeTarget 이미 있음: {tower.name}");
                }

                // Collider 확인
                Collider col = tower.GetComponent<Collider>();
                if (col == null)
                {
                    BoxCollider boxCol = tower.gameObject.AddComponent<BoxCollider>();
                    boxCol.isTrigger = true;
                    Debug.Log($"[ForceAdd] BoxCollider 추가됨: {tower.name}");
                }
                else
                {
                    col.isTrigger = true;
                    Debug.Log($"[ForceAdd] Collider isTrigger 설정됨: {tower.name}");
                }

                // 최종 상태 확인
                Debug.Log($"[ForceAdd] {tower.name} 최종 상태 - " +
                         $"Draggable: {tower.GetComponent<DraggableUnit>() != null}, " +
                         $"MergeTarget: {tower.GetComponent<UnitMergeTarget>() != null}, " +
                         $"Collider: {tower.GetComponent<Collider>() != null}");
            }

            Debug.Log($"[ForceAdd] 모든 타워 처리 완료!");
        }
    }
}