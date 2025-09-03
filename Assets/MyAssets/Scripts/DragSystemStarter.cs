using UnityEngine;
using System.Collections;

namespace InvaderInsider
{
    /// <summary>
    /// 드래그 시스템을 자동으로 시작하고 설정하는 스크립트
    /// 이 스크립트를 씬에 추가하면 모든 것이 자동으로 설정됩니다.
    /// </summary>
    public class DragSystemStarter : MonoBehaviour
    {
        private const string LOG_PREFIX = "[DragSystemStarter] ";
        
        [Header("Auto Setup")]
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private bool showUI = true;
        [SerializeField] private bool enableDebugLogs = true;
        
        [Header("Components to Create")]
        [SerializeField] private bool createMasterController = true;
        [SerializeField] private bool createAllInOneFixer = true;
        [SerializeField] private bool createSystemTester = true;
        [SerializeField] private bool createSystemUI = true;
        
        private void Start()
        {
            if (autoSetupOnStart)
            {
                StartCoroutine(SetupEverything());
            }
        }
        
        private IEnumerator SetupEverything()
        {
            if (enableDebugLogs)
            {
                Debug.Log($"{LOG_PREFIX}🚀 드래그 시스템 자동 설정 시작 (디버그 로그 활성화)");
            }
            else
            {
                Debug.Log($"{LOG_PREFIX}🚀 드래그 시스템 자동 설정 시작");
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // 1. 핵심 시스템들 생성
            CreateCoreComponents();
            
            yield return new WaitForSeconds(0.5f);
            
            // 2. 모든 시스템 초기화
            InitializeAllSystems();
            
            yield return new WaitForSeconds(1f);
            
            // 3. UI 설정
            if (showUI && createSystemUI)
            {
                SetupUI();
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // 4. 최종 확인 및 안내
            ShowFinalInstructions();
            
            Debug.Log($"{LOG_PREFIX}🎉 드래그 시스템 자동 설정 완료!");
        }
        
        private void CreateCoreComponents()
        {
            Debug.Log($"{LOG_PREFIX}핵심 컴포넌트 생성 중...");
            
            // MasterSystemController 생성
            if (createMasterController && MasterSystemController.Instance == null)
            {
                GameObject masterObj = new GameObject("MasterSystemController");
                masterObj.AddComponent<MasterSystemController>();
                Debug.Log($"{LOG_PREFIX}✅ MasterSystemController 생성됨");
            }
            
            // AllInOneFixer 생성
            if (createAllInOneFixer && FindObjectOfType<AllInOneFixer>() == null)
            {
                GameObject fixerObj = new GameObject("AllInOneFixer");
                fixerObj.AddComponent<AllInOneFixer>();
                Debug.Log($"{LOG_PREFIX}✅ AllInOneFixer 생성됨");
            }
            
            // DragSystemTester 생성
            if (createSystemTester && FindObjectOfType<DragSystemTester>() == null)
            {
                GameObject testerObj = new GameObject("DragSystemTester");
                testerObj.AddComponent<DragSystemTester>();
                Debug.Log($"{LOG_PREFIX}✅ DragSystemTester 생성됨");
            }
            
            // QuickSystemTest 생성
            if (FindObjectOfType<QuickSystemTest>() == null)
            {
                GameObject quickTestObj = new GameObject("QuickSystemTest");
                quickTestObj.AddComponent<QuickSystemTest>();
                Debug.Log($"{LOG_PREFIX}✅ QuickSystemTest 생성됨");
            }
        }
        
        private void InitializeAllSystems()
        {
            Debug.Log($"{LOG_PREFIX}시스템 초기화 중...");
            
            // DragAndMergeSystem 확인 (자동 생성됨)
            if (DragAndMergeSystem.Instance != null)
            {
                Debug.Log($"{LOG_PREFIX}✅ DragAndMergeSystem 준비됨");
            }
            
            // GameManager 확인
            if (InvaderInsider.Managers.GameManager.Instance != null)
            {
                Debug.Log($"{LOG_PREFIX}✅ GameManager 연동됨");
            }
            
            // 모든 유닛에 컴포넌트 추가
            AddComponentsToAllUnits();
        }
        
        private void AddComponentsToAllUnits()
        {
            BaseCharacter[] allUnits = FindObjectsOfType<BaseCharacter>();
            int processedCount = 0;
            
            foreach (BaseCharacter unit in allUnits)
            {
                if (unit == null) continue;
                
                bool wasModified = false;
                
                // SimpleDraggableUnit 추가
                if (unit.GetComponent<SimpleDraggableUnit>() == null)
                {
                    unit.gameObject.AddComponent<SimpleDraggableUnit>();
                    wasModified = true;
                }
                
                // SimpleMergeTarget 추가
                if (unit.GetComponent<SimpleMergeTarget>() == null)
                {
                    unit.gameObject.AddComponent<SimpleMergeTarget>();
                    wasModified = true;
                }
                
                // Collider 설정
                Collider col = unit.GetComponent<Collider>();
                if (col == null)
                {
                    BoxCollider boxCol = unit.gameObject.AddComponent<BoxCollider>();
                    boxCol.isTrigger = true;
                    wasModified = true;
                }
                else if (!col.isTrigger)
                {
                    col.isTrigger = true;
                    wasModified = true;
                }
                
                if (wasModified)
                {
                    processedCount++;
                }
            }
            
            Debug.Log($"{LOG_PREFIX}✅ {processedCount}개 유닛에 드래그 기능 추가됨 (총 {allUnits.Length}개 중)");
        }
        
        private void SetupUI()
        {
            Debug.Log($"{LOG_PREFIX}UI 설정 중...");
            
            if (FindObjectOfType<DragSystemUI>() == null)
            {
                GameObject uiObj = new GameObject("DragSystemUI");
                DragSystemUI ui = uiObj.AddComponent<DragSystemUI>();
                Debug.Log($"{LOG_PREFIX}✅ DragSystemUI 생성됨");
            }
        }
        
        private void ShowFinalInstructions()
        {
            Debug.Log($"{LOG_PREFIX}");
            Debug.Log($"{LOG_PREFIX}🎮 === 드래그 시스템 사용법 ===");
            Debug.Log($"{LOG_PREFIX}");
            Debug.Log($"{LOG_PREFIX}✨ 기본 사용법:");
            Debug.Log($"{LOG_PREFIX}   • 카드를 드래그하여 타일에 배치");
            Debug.Log($"{LOG_PREFIX}   • 같은 ID/레벨 유닛에 드롭하면 레벨업");
            Debug.Log($"{LOG_PREFIX}   • 필드 유닛을 드래그하여 다른 유닛과 머지");
            Debug.Log($"{LOG_PREFIX}");
            Debug.Log($"{LOG_PREFIX}🔧 유용한 단축키:");
            Debug.Log($"{LOG_PREFIX}   ESC - 모든 드래그 취소");
            Debug.Log($"{LOG_PREFIX}   F5 - 시스템 테스트");
            Debug.Log($"{LOG_PREFIX}   F12 - 상태 UI 토글");
            Debug.Log($"{LOG_PREFIX}   Ctrl+F1 - 시스템 상태 확인");
            Debug.Log($"{LOG_PREFIX}   Ctrl+F2 - 시스템 재초기화");
            Debug.Log($"{LOG_PREFIX}   Ctrl+F3 - 긴급 복구");
            Debug.Log($"{LOG_PREFIX}   Ctrl+Shift+F - 전체 수정");
            Debug.Log($"{LOG_PREFIX}   Ctrl+T - 빠른 테스트");
            Debug.Log($"{LOG_PREFIX}");
            Debug.Log($"{LOG_PREFIX}🛠️ 문제 해결:");
            Debug.Log($"{LOG_PREFIX}   • 드래그가 안 되면: Ctrl+F2로 재초기화");
            Debug.Log($"{LOG_PREFIX}   • 유닛이 붙어있으면: ESC로 취소");
            Debug.Log($"{LOG_PREFIX}   • 시스템 오류 시: Ctrl+F3으로 긴급 복구");
            Debug.Log($"{LOG_PREFIX}");
            Debug.Log($"{LOG_PREFIX}📊 상태 확인:");
            Debug.Log($"{LOG_PREFIX}   • F12로 상태 UI 열기");
            Debug.Log($"{LOG_PREFIX}   • '시스템 수정' 버튼으로 자동 수정");
            Debug.Log($"{LOG_PREFIX}   • '테스트' 버튼으로 시스템 테스트");
            Debug.Log($"{LOG_PREFIX}");
            Debug.Log($"{LOG_PREFIX}🚀 이제 드래그 & 머지를 즐기세요!");
            Debug.Log($"{LOG_PREFIX}");
        }
        
        [ContextMenu("Setup Everything Now")]
        public void SetupEverythingNow()
        {
            StartCoroutine(SetupEverything());
        }
        
        [ContextMenu("Quick Fix All")]
        public void QuickFixAll()
        {
            // 빠른 수정
            AllInOneFixer fixer = FindObjectOfType<AllInOneFixer>();
            if (fixer == null)
            {
                GameObject fixerObj = new GameObject("AllInOneFixer");
                fixer = fixerObj.AddComponent<AllInOneFixer>();
            }
            fixer.FixEverythingNow();
        }
        
        private void Update()
        {
            // Ctrl + Shift + S: 전체 설정 실행
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.S))
            {
                SetupEverythingNow();
            }
        }
    }
}