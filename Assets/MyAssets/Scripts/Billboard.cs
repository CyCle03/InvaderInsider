using UnityEngine;
using InvaderInsider.Managers;
using InvaderInsider.Core;

namespace InvaderInsider
{
    /// <summary>
    /// 빌보드 컴포넌트 - 오브젝트가 항상 카메라를 향하도록 합니다.
    /// 성능 최적화된 버전으로 UI나 파티클 효과에 유용합니다.
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        [Header("Billboard Settings")]
        [SerializeField] private bool lockX = false; // X축 회전 잠금
        [SerializeField] private bool lockY = false; // Y축 회전 잠금  
        [SerializeField] private bool lockZ = false; // Z축 회전 잠금
        [SerializeField] private bool updateInFixedUpdate = false; // FixedUpdate에서 업데이트 할지
        [SerializeField] private float updateInterval = 0f; // 업데이트 간격 (0이면 매 프레임)
        
        private Camera targetCamera;
        private Transform cachedTransform;
        private float nextUpdateTime = 0f;
        
        private void Start()
        {
            // 메인 카메라 캐싱
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                LogManager.Warning(GameConstants.LOG_PREFIX_GAME, 
                    $"Billboard {gameObject.name}: Main Camera not found");
                enabled = false;
                return;
            }
            
            // Transform 캐싱
            cachedTransform = transform;
        }

        private void LateUpdate()
        {
            if (updateInFixedUpdate) return;
            UpdateBillboard();
        }
        
        private void FixedUpdate()
        {
            if (!updateInFixedUpdate) return;
            UpdateBillboard();
        }
        
        private void UpdateBillboard()
        {
            // 업데이트 간격 체크
            if (updateInterval > 0f && Time.time < nextUpdateTime)
                return;
                
            if (targetCamera == null || cachedTransform == null)
                return;
                
            // 카메라 방향 계산
            Vector3 targetDirection = targetCamera.transform.forward;
            
            // 축 잠금 적용
            if (lockX) targetDirection.x = 0f;
            if (lockY) targetDirection.y = 0f;
            if (lockZ) targetDirection.z = 0f;
            
            // 방향이 0이 아닌 경우에만 회전 적용
            if (targetDirection != Vector3.zero)
            {
                cachedTransform.forward = targetDirection;
            }
            
            // 다음 업데이트 시간 설정
            if (updateInterval > 0f)
            {
                nextUpdateTime = Time.time + updateInterval;
            }
        }
        
        /// <summary>
        /// 대상 카메라를 변경합니다.
        /// </summary>
        /// <param name="newCamera">새로운 대상 카메라</param>
        public void SetTargetCamera(Camera newCamera)
        {
            targetCamera = newCamera;
        }
        
        /// <summary>
        /// 현재 대상 카메라를 반환합니다.
        /// </summary>
        public Camera GetTargetCamera()
        {
            return targetCamera;
        }
    }
}

