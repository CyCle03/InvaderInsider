using UnityEngine;
using UnityEngine.EventSystems;
using InvaderInsider.Data;
using InvaderInsider.Managers;
using InvaderInsider.Cards;
using System.Collections;

namespace InvaderInsider
{
    /// <summary>
    /// 통합 드래그 & 머지 시스템
    /// 카드 드래그, 유닛 드래그, 머지 기능을 모두 관리합니다.
    /// </summary>
    public class DragAndMergeSystem : MonoBehaviour
    {
        private const string LOG_PREFIX = "[DragAndMergeSystem] ";
        
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showVisualFeedback = true;
        
        // 싱글톤 패턴
        private static DragAndMergeSystem _instance;
        public static DragAndMergeSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<DragAndMergeSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("DragAndMergeSystem");
                        _instance = go.AddComponent<DragAndMergeSystem>();
                    }
                }
                return _instance;
            }
        }
        
        // 드래그 상태 관리
        public enum DragType { None, Card, Unit }
        public DragType CurrentDragType { get; private set; } = DragType.None;
        
        // 카드 드래그 관련
        public CardDBObject DraggedCardData { get; private set; }
        public GameObject CardPreviewInstance { get; private set; }
        
        // 유닛 드래그 관련
        public BaseCharacter DraggedUnit { get; private set; }
        public Vector3 DraggedUnitOriginalPosition { get; private set; }
        
        // 머지 결과
        public bool WasDropSuccessful { get; private set; }
        public BaseCharacter MergeTargetUnit { get; private set; }
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            LogDebug("DragAndMergeSystem 초기화 완료 - DontDestroyOnLoad 적용됨");
        }
        
        private void Update()
        {
            // ESC 키로 모든 드래그 취소
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelAllDrags();
            }
            
            // 카드 프리뷰 업데이트
            if (CurrentDragType == DragType.Card && CardPreviewInstance != null)
            {
                UpdateCardPreview();
            }
        }
        
        #region 카드 드래그 관리
        
        /// <summary>
        /// 카드 드래그 시작
        /// </summary>
        public void StartCardDrag(CardDBObject cardData)
        {
            if (CurrentDragType != DragType.None)
            {
                LogDebug("이미 다른 드래그가 진행 중입니다.");
                return;
            }
            
            if (cardData == null || cardData.cardPrefab == null)
            {
                LogDebug("유효하지 않은 카드 데이터입니다.");
                return;
            }
            
            CurrentDragType = DragType.Card;
            DraggedCardData = cardData;
            
            // 프리뷰 생성
            CreateCardPreview(cardData);
            
            LogDebug($"카드 드래그 시작: {cardData.cardName}");
        }
        
        /// <summary>
        /// 카드 프리뷰 생성
        /// </summary>
        private void CreateCardPreview(CardDBObject cardData)
        {
            if (CardPreviewInstance != null)
            {
                Destroy(CardPreviewInstance);
            }
            
            CardPreviewInstance = Instantiate(cardData.cardPrefab);
            CardPreviewInstance.name = $"CardPreview_{cardData.cardName}";
            
            // 프리뷰 설정
            SetupPreviewObject(CardPreviewInstance);
        }
        
        /// <summary>
        /// 프리뷰 오브젝트 설정
        /// </summary>
        private void SetupPreviewObject(GameObject previewObj)
        {
            try
            {
                // AI 비활성화
                if (previewObj.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
                    agent.enabled = false;
                
                // 캐릭터 컴포넌트 비활성화
                if (previewObj.TryGetComponent<BaseCharacter>(out var character))
                    character.enabled = false;
                
                // 모든 콜라이더 비활성화
                Collider[] colliders = previewObj.GetComponentsInChildren<Collider>();
                foreach (var col in colliders)
                {
                    if (col != null)
                        col.enabled = false;
                }
                
                // 렌더러는 활성화 유지
                Renderer[] renderers = previewObj.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer != null)
                        renderer.enabled = true;
                }
                
                // 누락된 스크립트 컴포넌트 제거 시도
                RemoveMissingScripts(previewObj);
                
                LogDebug($"프리뷰 오브젝트 설정 완료: {previewObj.name}");
            }
            catch (System.Exception e)
            {
                LogDebug($"프리뷰 오브젝트 설정 중 오류: {e.Message}");
            }
        }
        
        /// <summary>
        /// 누락된 스크립트 컴포넌트 제거
        /// </summary>
        private void RemoveMissingScripts(GameObject obj)
        {
            try
            {
                Component[] components = obj.GetComponents<Component>();
                for (int i = components.Length - 1; i >= 0; i--)
                {
                    if (components[i] == null)
                    {
                        // 누락된 컴포넌트 발견
                        LogDebug($"누락된 컴포넌트 발견: {obj.name}");
                        // 런타임에서는 직접 제거할 수 없으므로 로그만 출력
                    }
                }
            }
            catch (System.Exception e)
            {
                LogDebug($"누락된 스크립트 확인 중 오류: {e.Message}");
            }
        }
        
        /// <summary>
        /// 카드 프리뷰 위치 업데이트
        /// </summary>
        private void UpdateCardPreview()
        {
            if (CardPreviewInstance == null) return;
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 200f, GameManager.Instance.TileLayerMask))
            {
                Vector3 targetPos = hit.collider.transform.position + new Vector3(0, GameManager.Instance.PlacementYOffset, 0);
                CardPreviewInstance.transform.position = targetPos;
                
                // 배치 가능 여부에 따른 시각적 피드백
                Tile targetTile = hit.collider.GetComponent<Tile>();
                UpdatePreviewVisuals(targetTile);
            }
            else
            {
                // 타일이 아닌 곳에서는 프리뷰 숨김
                CardPreviewInstance.transform.position = new Vector3(0, -1000, 0);
            }
        }
        
        /// <summary>
        /// 프리뷰 시각적 피드백 업데이트
        /// </summary>
        private void UpdatePreviewVisuals(Tile targetTile)
        {
            if (!showVisualFeedback || CardPreviewInstance == null) return;
            
            bool isValid = targetTile != null && targetTile.tileType == TileType.Spawn && !targetTile.IsOccupied;
            Material materialToApply = isValid ? GameManager.Instance.ValidPlacementMaterial : GameManager.Instance.InvalidPlacementMaterial;
            
            if (materialToApply != null)
            {
                foreach (var renderer in CardPreviewInstance.GetComponentsInChildren<Renderer>())
                {
                    renderer.material = materialToApply;
                }
            }
        }
        
        /// <summary>
        /// 카드 배치 시도
        /// </summary>
        public bool TryPlaceCard(Vector3 screenPosition)
        {
            if (CurrentDragType != DragType.Card || DraggedCardData == null)
                return false;
            
            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                // 유닛에 드롭 시도 - BaseCharacter로 직접 확인
                BaseCharacter targetUnit = hit.collider.GetComponent<BaseCharacter>();
                if (targetUnit != null)
                {
                    return TryMergeCardWithUnit(targetUnit);
                }
                
                // 타일에 배치 시도
                Tile targetTile = hit.collider.GetComponent<Tile>();
                if (targetTile != null)
                {
                    return TryPlaceCardOnTile(targetTile);
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 카드를 유닛과 합치기 시도
        /// </summary>
        private bool TryMergeCardWithUnit(BaseCharacter targetCharacter)
        {
            if (targetCharacter == null || !targetCharacter.IsInitialized)
            {
                LogDebug("대상 유닛이 유효하지 않습니다.");
                return false;
            }
            
            // 머지 조건 확인 (같은 ID, 같은 레벨)
            if (targetCharacter.CardId == DraggedCardData.cardId && targetCharacter.Level == DraggedCardData.level)
            {
                LogDebug($"카드 머지 성공: {DraggedCardData.cardName} -> {targetCharacter.name}");
                
                // 레벨업
                targetCharacter.LevelUp();
                
                // 카드 소모
                CardManager.Instance.RemoveCardFromHand(DraggedCardData.cardId);
                
                WasDropSuccessful = true;
                MergeTargetUnit = targetCharacter;
                return true;
            }
            else
            {
                LogDebug($"머지 조건 불일치 - 카드: ID={DraggedCardData.cardId}, Level={DraggedCardData.level} / 유닛: ID={targetCharacter.CardId}, Level={targetCharacter.Level}");
                return false;
            }
        }
        
        /// <summary>
        /// 카드를 타일에 배치 시도
        /// </summary>
        private bool TryPlaceCardOnTile(Tile targetTile)
        {
            if (targetTile.tileType != TileType.Spawn || targetTile.IsOccupied)
            {
                LogDebug("배치할 수 없는 타일입니다.");
                return false;
            }
            
            GameObject spawnedObject = GameManager.Instance.SpawnObject(DraggedCardData, targetTile);
            if (spawnedObject != null)
            {
                LogDebug($"카드 배치 성공: {DraggedCardData.cardName}");
                
                // 카드 소모
                CardManager.Instance.RemoveCardFromHand(DraggedCardData.cardId);
                
                WasDropSuccessful = true;
                return true;
            }
            
            return false;
        }
        
        #endregion
        
        #region 유닛 드래그 관리
        
        /// <summary>
        /// 유닛 드래그 시작
        /// </summary>
        public bool StartUnitDrag(BaseCharacter unit)
        {
            LogDebug($"StartUnitDrag called for {(unit != null ? unit.name : "a null unit")}");

            if (CurrentDragType != DragType.None)
            {
                LogDebug("이미 다른 드래그가 진행 중입니다. CurrentDragType: " + CurrentDragType);
                return false;
            }
            
            if (unit == null)
            {
                LogDebug("유닛이 null입니다. 드래그를 시작할 수 없습니다.");
                return false;
            }

            if (!unit.IsInitialized)
            {
                LogDebug($"유닛 '{unit.name}'이 초기화되지 않았습니다 (IsInitialized = false). 드래그를 시작할 수 없습니다.");
                return false;
            }
            
            // 드래그 결과 리셋
            WasDropSuccessful = false;
            MergeTargetUnit = null;
            
            CurrentDragType = DragType.Unit;
            DraggedUnit = unit;
            DraggedUnitOriginalPosition = unit.transform.position;
            
            // 물리 설정
            if (unit.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = true;
            }
            
            LogDebug($"유닛 드래그 시작: {unit.name} (ID: {unit.CardId}, Level: {unit.Level}) 원래 위치: {DraggedUnitOriginalPosition}");
            return true;
        }
        
        /// <summary>
        /// 유닛 드래그 중 위치 업데이트
        /// </summary>
        public void UpdateUnitDrag(Vector3 screenPosition)
        {
            if (CurrentDragType != DragType.Unit || DraggedUnit == null) return;
            
            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, GameManager.Instance.TileLayerMask))
            {
                Vector3 newPosition = hit.point + new Vector3(0, GameManager.Instance.PlacementYOffset, 0);
                DraggedUnit.transform.position = newPosition;
            }
        }
        
        /// <summary>
        /// 유닛 드롭 시도
        /// </summary>
        public bool TryDropUnit(Vector3 screenPosition)
        {
            if (CurrentDragType != DragType.Unit || DraggedUnit == null)
                return false;
            
            // "Unit" 레이어만 대상으로 레이캐스트
            int unitLayerMask = 1 << LayerMask.NameToLayer("Unit");
            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, unitLayerMask);
            
            // 모든 히트된 오브젝트를 확인하여 BaseCharacter 찾기
            BaseCharacter targetUnit = null;
            foreach (RaycastHit hit in hits)
            {
                BaseCharacter character = hit.collider.GetComponent<BaseCharacter>();
                if (character != null && character != DraggedUnit)
                {
                    targetUnit = character;
                    LogDebug($"드롭 타겟 발견: {targetUnit.name} (ID: {targetUnit.CardId}, Level: {targetUnit.Level})");
                    break;
                }
            }
            
            if (targetUnit != null)
            {
                bool mergeSuccess = TryMergeUnits(targetUnit);
                if (!mergeSuccess)
                {
                    // 머지 실패 시 원래 위치로 복귀
                    StartCoroutine(ReturnUnitToOriginalPosition());
                }
                return mergeSuccess;
            }
            
            // 타겟이 없으면 원래 위치로 복귀
            LogDebug("드롭 타겟을 찾을 수 없습니다. 원래 위치로 복귀합니다.");
            StartCoroutine(ReturnUnitToOriginalPosition());
            return false;
        }
        
        /// <summary>
        /// 유닛끼리 합치기 시도
        /// </summary>
        private bool TryMergeUnits(BaseCharacter targetUnit)
        {
            if (targetUnit == null)
            {
                LogDebug("타겟 유닛이 null입니다.");
                return false;
            }
            
            if (!targetUnit.IsInitialized)
            {
                LogDebug($"타겟 유닛 {targetUnit.name}이 초기화되지 않았습니다.");
                return false;
            }
            
            if (targetUnit == DraggedUnit)
            {
                LogDebug("자기 자신에게는 머지할 수 없습니다.");
                return false;
            }
            
            LogDebug($"머지 시도 - 드래그 유닛: {DraggedUnit.name} (ID: {DraggedUnit.CardId}, Level: {DraggedUnit.Level}) -> 타겟: {targetUnit.name} (ID: {targetUnit.CardId}, Level: {targetUnit.Level})");
            
            // 머지 조건 확인 (같은 ID, 같은 레벨)
            if (DraggedUnit.CardId == targetUnit.CardId && DraggedUnit.Level == targetUnit.Level)
            {
                LogDebug($"✅ 유닛 머지 성공: {DraggedUnit.name} -> {targetUnit.name}");
                
                // 대상 유닛 레벨업
                targetUnit.LevelUp();
                
                // 드래그된 유닛 제거
                Destroy(DraggedUnit.gameObject);
                
                WasDropSuccessful = true;
                MergeTargetUnit = targetUnit;
                return true;
            }
            else
            {
                LogDebug($"❌ 머지 조건 불일치 - 드래그 유닛: ID={DraggedUnit.CardId}, Level={DraggedUnit.Level} / 대상 유닛: ID={targetUnit.CardId}, Level={targetUnit.Level}");
                return false;
            }
        }
        
        /// <summary>
        /// 유닛을 원래 위치로 복귀
        /// </summary>
        private IEnumerator ReturnUnitToOriginalPosition()
        {
            // 프레임이 끝날 때까지 기다려 다른 로직이 먼저 처리되도록 함
            yield return new WaitForEndOfFrame();
            
            if (DraggedUnit != null && !WasDropSuccessful)
            {
                LogDebug($"유닛을 원래 위치로 복귀: {DraggedUnit.name} ({DraggedUnitOriginalPosition})");
                DraggedUnit.transform.position = DraggedUnitOriginalPosition;
                
                // 물리 설정 복원
                if (DraggedUnit.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.isKinematic = false;
                }
            }
            
            // 코루틴이 끝난 후 드래그 상태를 완전히 리셋
            CurrentDragType = DragType.None;
            DraggedUnit = null;
            DraggedUnitOriginalPosition = Vector3.zero;
            LogDebug("ReturnUnitToOriginalPosition 코루틴 후 상태 리셋 완료.");
        }
        
        #endregion
        
        #region 드래그 종료 및 정리
        
        /// <summary>
        /// 카드 드래그 종료
        /// </summary>
        public void EndCardDrag()
        {
            if (CurrentDragType != DragType.Card) return;
            
            // 프리뷰 정리
            if (CardPreviewInstance != null)
            {
                Destroy(CardPreviewInstance);
                CardPreviewInstance = null;
            }
            
            // 상태 리셋
            CurrentDragType = DragType.None;
            DraggedCardData = null;
            
            LogDebug("카드 드래그 종료");
        }
        
        /// <summary>
        /// 유닛 드래그 종료
        /// </summary>
        public void EndUnitDrag()
        {
            if (CurrentDragType != DragType.Unit) return;
            
            LogDebug($"유닛 드래그 종료 로직 시작 - 성공: {WasDropSuccessful}, 유닛: {(DraggedUnit != null ? DraggedUnit.name : "null")}");

            // 드롭이 성공했거나, 유닛이 이미 null(머지 성공으로 파괴됨)이면 즉시 상태 리셋
            if (WasDropSuccessful || DraggedUnit == null)
            {
                CurrentDragType = DragType.None;
                DraggedUnit = null;
                DraggedUnitOriginalPosition = Vector3.zero;
                LogDebug("유닛 드래그 상태 즉시 리셋 완료 (성공 또는 파괴됨).");
            }
            // 드롭이 실패한 경우, ReturnUnitToOriginalPosition 코루틴이 상태를 리셋할 것임
            else
            {
                LogDebug("드롭 실패. ReturnUnitToOriginalPosition 코루틴이 후처리를 담당합니다.");
            }
        }
        
        /// <summary>
        /// 모든 드래그 취소
        /// </summary>
        public void CancelAllDrags()
        {
            LogDebug("모든 드래그 취소");
            
            // 카드 드래그 취소
            if (CurrentDragType == DragType.Card)
            {
                EndCardDrag();
            }
            
            // 유닛 드래그 취소
            if (CurrentDragType == DragType.Unit)
            {
                if (DraggedUnit != null)
                {
                    DraggedUnit.transform.position = DraggedUnitOriginalPosition;
                }
                EndUnitDrag();
            }
            
            // 결과 상태 리셋
            WasDropSuccessful = false;
            MergeTargetUnit = null;
        }
        
        /// <summary>
        /// 드래그 결과 리셋
        /// </summary>
        public void ResetDragResult()
        {
            WasDropSuccessful = false;
            MergeTargetUnit = null;
        }
        
        #endregion
        
        #region 유틸리티
        
        /// <summary>
        /// 현재 드래그 중인지 확인
        /// </summary>
        public bool IsDragging => CurrentDragType != DragType.None;
        
        /// <summary>
        /// 카드 드래그 중인지 확인
        /// </summary>
        public bool IsCardDragging => CurrentDragType == DragType.Card;
        
        /// <summary>
        /// 유닛 드래그 중인지 확인
        /// </summary>
        public bool IsUnitDragging => CurrentDragType == DragType.Unit;
        
        /// <summary>
        /// 디버그 로그 출력
        /// </summary>
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"{LOG_PREFIX}{message}");
            }
        }
        
        #endregion
    }
}