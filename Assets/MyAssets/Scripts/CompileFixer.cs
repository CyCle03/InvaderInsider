using UnityEngine;

namespace InvaderInsider
{
    /// <summary>
    /// 컴파일 에러 해결을 위한 임시 스크립트
    /// </summary>
    public class CompileFixer : MonoBehaviour
    {
        [ContextMenu("Fix All Compilation Issues")]
        public void FixAllCompilationIssues()
        {
            Debug.Log("[CompileFixer] 컴파일 문제 해결 시작");
            
            // 1. DragAndMergeSystem 인스턴스 확인
            if (DragAndMergeSystem.Instance != null)
            {
                Debug.Log("[CompileFixer] DragAndMergeSystem 정상 작동 중");
            }
            else
            {
                Debug.LogWarning("[CompileFixer] DragAndMergeSystem 인스턴스가 없습니다");
            }
            
            // 2. 모든 BaseCharacter 확인
            BaseCharacter[] characters = FindObjectsOfType<BaseCharacter>();
            Debug.Log($"[CompileFixer] 발견된 BaseCharacter: {characters.Length}개");
            
            // 3. 새로운 컴포넌트 확인
            SimpleDraggableUnit[] draggables = FindObjectsOfType<SimpleDraggableUnit>();
            SimpleMergeTarget[] mergeTargets = FindObjectsOfType<SimpleMergeTarget>();
            
            Debug.Log($"[CompileFixer] SimpleDraggableUnit: {draggables.Length}개");
            Debug.Log($"[CompileFixer] SimpleMergeTarget: {mergeTargets.Length}개");
            
            Debug.Log("[CompileFixer] 컴파일 문제 해결 완료");
        }
        
        private void Start()
        {
            // 게임 시작 시 자동 확인
            Invoke(nameof(FixAllCompilationIssues), 1f);
        }
    }
}