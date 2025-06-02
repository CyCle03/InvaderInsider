using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InvaderInsider.Data;
using InvaderInsider.UI;
using InvaderInsider.Managers;

namespace InvaderInsider
{
    public class StageManager : MonoBehaviour
    {
        private static StageManager instance;
        public static StageManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<StageManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("StageManager");
                        instance = go.AddComponent<StageManager>();
                    }
                }
                return instance;
            }
        }

        [Header("Stage Data")]
        [SerializeField] private StageList stageDataObject;
        private IStageData stageData;

        [Header("Stage Settings")]
        private const float STAGE_START_DELAY = 1f;
        private const float STAGE_END_DELAY = 3f;
        public List<Transform> wayPoints = new List<Transform>();
        public GameObject enemyPrefab;
        public int stageNum = 0;
        public int stageWave = 20;
        [Tooltip("Time between enemy spawns")]
        public float createTime = 1f;

        private float currentTime = 0f;
        private int enemyCount = 0;
        public int activeEnemyCount = 0;
        private Coroutine stageCoroutine = null;

        public enum StageState
        {
            Ready,
            Run,
            Wait,
            End,
            Over
        }

        public StageState currentState;
        private int clearedStageIndex; // 클리어된 스테이지 인덱스를 저장할 필드

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                
                stageData = stageDataObject;
                if (stageData == null)
                {
                    Debug.LogError("Stage data is not assigned in the inspector!");
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            if (stageData == null)
            {
                Debug.LogError("Stage data is not set!");
                return;
            }
        }

        public void InitializeStage()
        {
            Debug.Log("StageManager InitializeStage called");
            // 이 함수는 주로 '새 게임' 시작 시 사용되며, 항상 Stage 0부터 시작합니다.
            StartStageInternal(0); // Stage 0부터 시작
        }

        // 특정 스테이지부터 게임을 시작하는 함수 (저장된 게임 로드 시 사용)
        public void StartStageFrom(int stageIndex)
        {
             Debug.Log($"StageManager StartStageFrom called for stage {stageIndex}");
            // 유효한 스테이지 인덱스인지 확인
            if (stageData != null && stageIndex >= 0 && stageIndex < stageData.StageCount)
            {
                 StartStageInternal(stageIndex); // 인자로 받은 스테이지부터 시작
            }
            else
            {
                Debug.LogError($"Invalid stage index {stageIndex} provided for StartStageFrom.");
                // 유효하지 않은 경우, Stage 0부터 시작하거나 에러 처리
                 StartStageInternal(0); // 기본값으로 Stage 0부터 시작
            }
        }

        // 실제 스테이지 시작 로직을 포함하는 내부 함수
        private void StartStageInternal(int startStageIndex)
        {
             currentTime = 0f;
            enemyCount = 0;
            activeEnemyCount = 0;
            stageNum = startStageIndex; // 시작 스테이지 인덱스 설정
            if (stageData == null)
            {
                 Debug.LogError("Stage data is not set in StartStageInternal!");
                 return;
            }
            stageWave = stageData.GetStageWaveCount(stageNum);
            currentState = StageState.Ready;
            
            if (stageCoroutine != null)
            {
                StopCoroutine(stageCoroutine);
            }
            stageCoroutine = StartCoroutine(StageLoopCoroutine());
        }

        private IEnumerator StageLoopCoroutine()
        {
            Debug.Log("StageLoopCoroutine started");
            while (currentState != StageState.Over)
            {
                switch (currentState)
                {
                    case StageState.Ready:
                        Debug.Log($"Stage {stageNum + 1} Ready");
                        UIManager.Instance.UpdateStage(stageNum, GetStageCount());
                        yield return new WaitForSeconds(STAGE_START_DELAY);
                        currentState = StageState.Run;
                        break;

                    case StageState.Run:
                        currentTime += Time.deltaTime;
                        if(enemyCount < stageWave)
                        {
                            if (currentTime > createTime)
                            {
                                SpawnEnemy();
                                currentTime = 0f;
                            }
                        }
                        if (enemyCount >= stageWave && activeEnemyCount <= 0)
                        {
                            // 현재 스테이지 클리어! End 상태로 전환 전 클리어한 스테이지 인덱스 저장
                            clearedStageIndex = stageNum; // 필드에 값 할당
                            currentState = StageState.End;
                            Debug.Log($"Stage {clearedStageIndex + 1} cleared: All enemies spawned and defeated.");
                        }
                        yield return null;
                        break;

                    case StageState.End:
                        Debug.Log($"Stage {stageNum + 1} End");
                        // 클리어한 스테이지 정보 업데이트 및 저장
                        if (GameManager.Instance != null)
                        {
                             // StageCleared 함수에 전달할 별 개수 계산 로직 필요 (현재는 0으로 가정)
                            int stars = 0; 
                            // StageState.Run에서 저장한 clearedStageIndex를 사용하여 StageCleared 호출
                            GameManager.Instance.StageCleared(clearedStageIndex, stars); // 클리어된 스테이지 인덱스 전달
                            Debug.Log($"Stage {clearedStageIndex} progress updated and saved.");
                        }

                        stageNum++; // 다음 스테이지로 이동
                        currentTime = 0f;
                        enemyCount = 0;

                        // 스테이지 클리어 시 데이터 저장 (StageCleared에서 이미 호출됨)
                        // SaveDataManager.Instance.SaveGameData(); // 주석 처리
                        // Debug.Log("Game data saved after stage clear."); // 주석 처리

                        if (stageNum < stageData.StageCount)
                        {
                            stageWave = stageData.GetStageWaveCount(stageNum);
                            yield return new WaitForSeconds(STAGE_END_DELAY);
                            currentState = StageState.Ready;
                        }
                        else
                        {
                            stageWave = 0;
                            currentState = StageState.Over;
                            Debug.Log("All stages completed!");
                            // 모든 스테이지 클리어 시 데이터 저장 (StageCleared에서 마지막 스테이지 클리어 정보가 저장됨)
                            // SaveDataManager.Instance.SaveGameData(); // 주석 처리
                            // Debug.Log("Game data saved after all stages clear."); // 주석 처리
                            yield return new WaitForSeconds(STAGE_END_DELAY);
                            // 모든 스테이지 완료 후 3초 대기 후 메인 메뉴 패널 표시
                            if (UIManager.Instance != null)
                            {
                                UIManager.Instance.ShowPanel("MainMenu");
                            }
                        }
                        yield return null;
                        break;
                }
            }
             Debug.Log("StageLoopCoroutine finished");
        }

        public void SpawnEnemy()
        {
            if (wayPoints.Count == 0)
            {
                Debug.LogError("No waypoints set for enemy path!");
                return;
            }

            GameObject enemyPrefab = stageData.GetStageObject(stageNum, enemyCount);
            if (enemyPrefab != null)
            {
                GameObject enemy = Instantiate(enemyPrefab);
                enemy.transform.position = wayPoints[0].position;
                enemyCount++;
                activeEnemyCount++;
                UIManager.Instance.UpdateWave(enemyCount, stageWave);
                if (FindObjectOfType<BottomBarPanel>() != null)
                {
                     FindObjectOfType<BottomBarPanel>().UpdateMonsterCountDisplay(activeEnemyCount);
                }
                currentTime = 0f;
            }
        }

        public int GetStageCount()
        {
            return stageData?.StageCount ?? 0;
        }

        public int GetStageWaveCount(int stageIndex)
        {
            return stageData?.GetStageWaveCount(stageIndex) ?? 0;
        }

        public void DecreaseActiveEnemyCount()
        {
            activeEnemyCount--;
            if (FindObjectOfType<BottomBarPanel>() != null)
            {
                 FindObjectOfType<BottomBarPanel>().UpdateMonsterCountDisplay(activeEnemyCount);
            }
            Debug.Log($"Active enemies: {activeEnemyCount}");
        }
    }
}
