using UnityEngine;
using System;
using InvaderInsider.Data;
using UnityEngine.SceneManagement;
using InvaderInsider.Cards;
using InvaderInsider.UI;

namespace InvaderInsider.Managers
{
    public enum GameState
    {
        None,
        MainMenu,
        Loading,
        Playing,
        Paused,
        GameOver
    }

    public class GameManager : MonoBehaviour
    {
        private const string LOG_PREFIX = "[GameManager] ";
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        public GameState CurrentGameState { get; private set; }
        public event Action<GameState> OnGameStateChanged;

        public GameObject SelectedTowerPrefab { get; set; }
        public int SelectedCardId { get; set; } = -1;
        // 기존 드래그 관련 프로퍼티들은 새로운 시스템으로 대체됨
        public CardDBObject DraggedCardData => DragAndMergeSystem.Instance?.DraggedCardData;
        public bool IsCardDragInProgress => DragAndMergeSystem.Instance?.IsCardDragging ?? false;
        public bool WasCardDroppedOnTower => DragAndMergeSystem.Instance?.WasDropSuccessful ?? false;

        private UIManager uiManager;
        private GameObject debugSphereInstance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // DragAndMergeSystem 자동 초기화
            if (DragAndMergeSystem.Instance == null)
            {
                Debug.Log($"{LOG_PREFIX}DragAndMergeSystem 자동 생성 중...");
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            // Programmatically create the debug sphere for raycast visualization
            debugSphereInstance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            debugSphereInstance.name = "Mouse Raycast Debug Sphere";
            debugSphereInstance.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            Collider sphereCollider = debugSphereInstance.GetComponent<Collider>();
            if (sphereCollider != null)
            {
                sphereCollider.enabled = false; // Prevent it from interfering with other raycasts
            }
            Renderer sphereRenderer = debugSphereInstance.GetComponent<Renderer>();
            if (sphereRenderer != null)
            {
                Material debugMaterial = new Material(Shader.Find("Unlit/Color"));
                debugMaterial.color = Color.red;
                sphereRenderer.material = debugMaterial;
            }

            // Set the GameManager as the parent to persist across scenes
            debugSphereInstance.transform.SetParent(this.transform);

            SetGameState(GameState.MainMenu);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            uiManager = FindObjectOfType<UIManager>();
            if (uiManager == null)
            {
                Debug.LogError($"{LOG_PREFIX}UIManager를 찾을 수 없습니다.");
                return;
            }

            InitializeUIForScene(scene.name);

            if (scene.name == "Game")
            {
                Player player = FindObjectOfType<Player>();
                if (player != null)
                {
                    player.OnDeath += HandlePlayerDeath;
                }

                // Game 씬 로드 시 드래그 시스템 초기화
                InitializeDragSystemForGameScene();
            }
        }

        /// <summary>
        /// Game 씬에서 드래그 시스템 초기화
        /// </summary>
        private void InitializeDragSystemForGameScene()
        {
            Debug.Log($"{LOG_PREFIX}Game 씬에서 드래그 시스템 초기화 시작");

            // DragSystemInitializer가 없으면 자동 생성
            DragSystemInitializer initializer = FindObjectOfType<DragSystemInitializer>();
            if (initializer == null)
            {
                GameObject initializerObj = new GameObject("DragSystemInitializer");
                initializer = initializerObj.AddComponent<DragSystemInitializer>();
                Debug.Log($"{LOG_PREFIX}DragSystemInitializer 자동 생성됨");
            }

            // 1초 후 초기화 실행 (다른 오브젝트들이 준비될 시간 확보)
            StartCoroutine(DelayedDragSystemInitialization());
        }

        private System.Collections.IEnumerator DelayedDragSystemInitialization()
        {
            yield return new WaitForSeconds(1f);

            // 기존 유닛들에 드래그 컴포넌트 자동 추가
            EnableDraggingForAllFieldUnits();
            
            // 추가로 2초 후에도 한 번 더 확인 (유닛이 늦게 생성되는 경우 대비)
            yield return new WaitForSeconds(2f);
            EnableDraggingForAllFieldUnits();

            Debug.Log($"{LOG_PREFIX}Game 씬 드래그 시스템 초기화 완료");
        }

        private void InitializeUIForScene(string sceneName)
        {
            BasePanel[] allPanels = FindObjectsOfType<BasePanel>(true);
            foreach (BasePanel panel in allPanels)
            {
                if (panel != null)
                {
                    uiManager.RegisterPanel(panel.name, panel);
                }
            }
            foreach (BasePanel panel in allPanels)
            {
                if (panel != null)
                {
                    panel.gameObject.SetActive(false);
                }
            }

            if (sceneName == "Main")
            {
                uiManager.ShowPanel("MainMenu");
            }
            else if (sceneName == "Game")
            {
                uiManager.ShowPanelConcurrent("TopBar");
                uiManager.ShowPanelConcurrent("BottomBar");
                uiManager.ShowPanelConcurrent("InGame");
            }
        }

        public void SetGameState(GameState newState)
        {
            if (CurrentGameState == newState) return;
            CurrentGameState = newState;
            OnGameStateChanged?.Invoke(newState);
            Debug.Log($"{LOG_PREFIX}Game state changed to: {newState}");
        }

        public void StartNewGame()
        {
            SaveDataManager.Instance?.ResetGameData();
            CardManager.Instance?.ClearHand();
            StartGame(0);
        }

        public void StartContinueGame()
        {
            var saveData = SaveDataManager.Instance?.CurrentSaveData;
            int startStage = 0;
            if (saveData != null)
            {
                startStage = saveData.progressData.highestStageCleared;
            }
            StartGame(startStage);
        }

        public void StartGame(int startStageIndex)
        {
            Player existingPlayer = FindObjectOfType<Player>();
            if (existingPlayer != null) existingPlayer.OnDeath -= HandlePlayerDeath;

            SetGameState(GameState.Loading);
            SceneManager.LoadSceneAsync("Game").completed += (asyncOperation) =>
            {
                var stageManager = FindObjectOfType<StageManager>();
                if (stageManager != null)
                {
                    // stageManager.StartStageFrom(startStageIndex);
                    SetGameState(GameState.Playing);
                    UpdateStageWaveUI(stageManager.GetCurrentStageIndex() + 1, stageManager.GetSpawnedEnemyCount(), stageManager.GetStageWaveCount(stageManager.GetCurrentStageIndex()));
                    Player newPlayer = FindObjectOfType<Player>();
                    if (newPlayer != null)
                    {
                        newPlayer.OnDeath += HandlePlayerDeath;
                    }
                }
                else
                {
                    Debug.LogError($"{LOG_PREFIX}StageManager를 찾을 수 없습니다.");
                }
            };
        }

        public void PauseGame(bool showPauseUI = true)
        {
            if (CurrentGameState != GameState.Playing) return;
            Time.timeScale = 0f;
            SetGameState(GameState.Paused);
            if (showPauseUI) uiManager?.ShowPanelConcurrent("Pause");
        }

        public void ResumeGame()
        {
            if (CurrentGameState != GameState.Paused) return;
            Time.timeScale = 1f;
            SetGameState(GameState.Playing);
            uiManager?.HidePanel("Pause");
        }

        public void GameOver()
        {
            if (CurrentGameState == GameState.GameOver) return;
            SetGameState(GameState.GameOver);
            Debug.Log($"{LOG_PREFIX} 게임 오버");
            uiManager?.ShowPanel("GameOver");
        }

        private void HandlePlayerDeath()
        {
            GameOver();
        }

        public void StageCleared(int clearedStageNumber)
        {
            Debug.Log($"{LOG_PREFIX}스테이지 {clearedStageNumber} 클리어! 데이터 저장을 시작합니다.");
            CardManager.Instance?.SaveCards();
            SaveDataManager.Instance?.UpdateStageProgress(clearedStageNumber, true);
        }

        public void LoadMainMenuScene()
        {
            Player player = FindObjectOfType<Player>();
            if (player != null) player.OnDeath -= HandlePlayerDeath;

            Time.timeScale = 1f;
            SetGameState(GameState.Loading);
            SceneManager.LoadScene("Main");
            SetGameState(GameState.MainMenu);
        }

        public void UpdateStageWaveUI(int stage, int currentWave, int maxWave)
        {
            var topBarPanel = uiManager?.GetPanel("TopBar") as TopBarPanel;
            if (topBarPanel != null)
            {
                topBarPanel.UpdateStageWaveUI(stage, currentWave, maxWave);
            }
        }

        public void AddEData(int amount, bool saveImmediately)
        {
            SaveDataManager.Instance?.UpdateEData(amount, saveImmediately);
        }

        public void UpdateEDataUI(int amount)
        {
            var topBarPanel = uiManager?.GetPanel("TopBar") as TopBarPanel;
            if (topBarPanel != null)
            {
                topBarPanel.UpdateEData(amount);
            }
        }

        // 기존 유닛 드래그 관련 프로퍼티들도 새로운 시스템으로 대체됨
        public BaseCharacter DraggedUnit => DragAndMergeSystem.Instance?.DraggedUnit;
        public BaseCharacter DroppedOnUnitTarget => DragAndMergeSystem.Instance?.MergeTargetUnit;

        [Header("Card Placement Settings")]
        public LayerMask TileLayerMask;
        public Material ValidPlacementMaterial;
        public Material InvalidPlacementMaterial;
        public float PlacementYOffset = 0.0f;

        // 기존 배치 관련 변수들은 새로운 시스템에서 관리됨

        private void Update()
        {
            UpdateDebugSphere();
            
            // ESC 키로 드래그 상태 강제 정리 (새로운 시스템 사용)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                DragAndMergeSystem.Instance?.CancelAllDrags();
            }
        }

        #region Card Placement Methods (Legacy - 새로운 시스템으로 대체됨)

        // 기존 배치 관련 메서드들은 호환성을 위해 유지하되, 새로운 시스템으로 리다이렉트
        public void StartPlacementPreview(CardDBObject cardData)
        {
            // 새로운 시스템에서는 자동으로 처리됨
            Debug.Log($"{LOG_PREFIX}StartPlacementPreview called - handled by DragAndMergeSystem");
        }

        private void UpdatePlacementPreview()
        {
            // 새로운 시스템에서 자동으로 처리됨
        }

        public bool ConfirmPlacement(Tile placementTile)
        {
            // 호환성을 위해 유지 - 새로운 시스템에서 직접 호출됨
            if (placementTile == null || placementTile.tileType != TileType.Spawn || placementTile.IsOccupied)
                return false;

            CardDBObject cardData = DragAndMergeSystem.Instance?.DraggedCardData;
            if (cardData == null) return false;

            GameObject spawnedObject = SpawnObject(cardData, placementTile);
            return spawnedObject != null;
        }

        public void CancelPlacement()
        {
            // 새로운 시스템에서 자동으로 처리됨
            Debug.Log($"{LOG_PREFIX}CancelPlacement called - handled by DragAndMergeSystem");
        }

        private void ResetPlacementState()
        {
            // 새로운 시스템에서 자동으로 처리됨
        }

        #endregion

        public GameObject SpawnObject(CardDBObject cardData, Tile tile)
        {
            if (cardData == null || cardData.cardPrefab == null)
            {
                Debug.LogError($"{LOG_PREFIX}카드 데이터 또는 프리팹이 유효하지 않습니다.");
                return null;
            }
            if (tile == null)
            {
                Debug.LogWarning($"{LOG_PREFIX}타일이 유효하지 않습니다.");
                return null;
            }
            if (tile.tileType != TileType.Spawn)
            {
                Debug.LogWarning($"{LOG_PREFIX}배치할 수 없는 타일입니다. (타일 타입: {tile.tileType})");
                return null;
            }
            if (tile.IsOccupied)
            {
                Debug.LogWarning($"{LOG_PREFIX}이미 점유된 타일입니다.");
                return null;
            }
            Debug.Log($"{LOG_PREFIX}Attempting to spawn '{cardData.cardName}' on tile '{tile.name}'.");
            Vector3 spawnPosition = tile.transform.position;
            spawnPosition.y += PlacementYOffset;
            GameObject spawnedObject = Instantiate(cardData.cardPrefab, spawnPosition, Quaternion.identity);
            if (spawnedObject == null)
            {
                Debug.LogError($"{LOG_PREFIX}Failed to instantiate prefab for card '{cardData.cardName}'.");
                return null;
            }
            // 드래그 시스템 컴포넌트 자동 추가
            EnsureDragComponents(spawnedObject);
            Debug.Log($"{LOG_PREFIX}Successfully instantiated prefab '{spawnedObject.name}'. Now initializing...");
            switch (cardData.type)
            {
                case CardType.Tower:
                    Tower towerComponent = spawnedObject.GetComponent<Tower>();
                    if (towerComponent != null)
                    {
                        towerComponent.Initialize(cardData);
                        tile.SetOccupied(true);
                        Debug.Log($"{LOG_PREFIX}Initialized '{spawnedObject.name}' as Tower.");
                    }
                    else
                    {
                        Debug.LogError($"{LOG_PREFIX}Spawned object for '{cardData.cardName}' is missing the 'Tower' component. Destroying object.");
                        Destroy(spawnedObject);
                        return null;
                    }
                    break;
                case CardType.Character:
                    BaseCharacter characterComponent = spawnedObject.GetComponent<BaseCharacter>();
                    if (characterComponent != null)
                    {
                        characterComponent.Initialize(cardData);
                        tile.SetOccupied(true);
                        Debug.Log($"{LOG_PREFIX}Initialized '{spawnedObject.name}' as Character.");
                    }
                    else
                    {
                        Debug.LogError($"{LOG_PREFIX}Spawned object for '{cardData.cardName}' is missing a 'BaseCharacter' (or subclass) component. Destroying object.");
                        Destroy(spawnedObject);
                        return null;
                    }
                    break;
            }
            Debug.Log($"{LOG_PREFIX}{cardData.cardName} was successfully summoned on tile {tile.name}.");
            return spawnedObject;
        }

        public void PlayCard(CardDBObject card)
        {
            if (card == null) return;
            switch (card.type)
            {
                case CardType.Tower:
                case CardType.Character:
                    StartPlacementPreview(card);
                    break;
                case CardType.Equipment:
                    break;
            }
        }

        private void UpdateDebugSphere()
        {
            if (debugSphereInstance == null) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Use the same layer mask as the tile placement to ensure we hit the ground
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, TileLayerMask))
            {
                // Position the sphere at the exact hit point on the tile
                debugSphereInstance.transform.position = hit.point;
            }
            else
            {
                // If not hitting a tile, hide the sphere far away
                debugSphereInstance.transform.position = new Vector3(0, -1000, 0);
            }
        }

        /// <summary>
        /// 씬에 있는 모든 BaseCharacter 객체에 드래그 및 머지 컴포넌트를 추가합니다.
        /// 이미 필드에 있는 유닛들을 드래그 가능하게 만들 때 사용합니다.
        /// </summary>
        [ContextMenu("Enable Dragging for All Field Units")]
        public void EnableDraggingForAllFieldUnits()
        {
            BaseCharacter[] allCharacters = FindObjectsOfType<BaseCharacter>();
            int enabledCount = 0;

            Debug.Log($"{LOG_PREFIX}필드의 {allCharacters.Length}개 유닛에 드래그 기능 추가 시작");

            foreach (BaseCharacter character in allCharacters)
            {
                if (character == null) continue;

                EnsureDragComponents(character.gameObject);
                enabledCount++;
            }

            Debug.Log($"{LOG_PREFIX}필드의 {enabledCount}개 유닛에 드래그 기능을 활성화했습니다.");
        }

        /// <summary>
        /// 모든 드래그 관련 상태와 프리뷰를 강제로 정리합니다.
        /// 드래그가 제대로 끝나지 않았을 때 사용합니다.
        /// </summary>
        [ContextMenu("Force Clear All Drag States")]
        public void ForceClearAllDragStates()
        {
            Debug.Log($"{LOG_PREFIX}Forcing clear of all drag states - using new system");
            DragAndMergeSystem.Instance?.CancelAllDrags();
        }
        
        /// <summary>
        /// 유닛에 드래그 컴포넌트들을 자동으로 추가
        /// </summary>
        private void EnsureDragComponents(GameObject unitObject)
        {
            if (unitObject == null) return;
            
            // SimpleDraggableUnit 추가
            SimpleDraggableUnit draggable = unitObject.GetComponent<SimpleDraggableUnit>();
            if (draggable == null)
            {
                draggable = unitObject.AddComponent<SimpleDraggableUnit>();
                Debug.Log($"{LOG_PREFIX}SimpleDraggableUnit 추가됨: {unitObject.name}");
            }

            // SimpleMergeTarget 추가
            SimpleMergeTarget mergeTarget = unitObject.GetComponent<SimpleMergeTarget>();
            if (mergeTarget == null)
            {
                mergeTarget = unitObject.AddComponent<SimpleMergeTarget>();
                Debug.Log($"{LOG_PREFIX}SimpleMergeTarget 추가됨: {unitObject.name}");
            }
            
            // 콜라이더 확인 및 추가/수정
            Collider col = unitObject.GetComponent<Collider>();
            if (col == null)
            {
                BoxCollider boxCol = unitObject.AddComponent<BoxCollider>();
                Debug.Log($"{LOG_PREFIX}BoxCollider 추가됨: {unitObject.name}");
                col = boxCol;
            }
            
            // 콜라이더가 항상 활성화되고, 트리거이며, 위치/크기가 적절한지 확인
            col.enabled = true;
            col.isTrigger = true;

            if (col is BoxCollider box)
            {
                box.size = new Vector3(1.5f, 3.0f, 1.5f); // 높이를 약간 늘려 겹침 방지
                box.center = new Vector3(0, 1.5f, 0);   // 중심을 약간 위로
            }
            Debug.Log($"{LOG_PREFIX}Collider 설정 업데이트됨: {unitObject.name}");
            
            // 레이어를 "Unit"으로 설정
            int unitLayer = LayerMask.NameToLayer("Unit");
            if (unitObject.layer != unitLayer)
            {
                unitObject.layer = unitLayer;
                Debug.Log($"{LOG_PREFIX}레이어를 'Unit'으로 설정: {unitObject.name}");
            }
        }
    }
}