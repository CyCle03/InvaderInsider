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
        public CardDBObject DraggedCardData { get; set; }
        public bool IsCardDragInProgress { get; set; } = false;
        public bool WasCardDroppedOnTower { get; set; } = false;

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
            }
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
                    stageManager.StartStageFrom(startStageIndex);
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

        public BaseCharacter DraggedUnit { get; set; }
        public BaseCharacter DroppedOnUnitTarget { get; set; }

        [Header("Card Placement Settings")]
        public LayerMask TileLayerMask;
        public Material ValidPlacementMaterial;
        public Material InvalidPlacementMaterial;
        public float PlacementYOffset = 0.0f;

        private GameObject placementPreviewInstance;
        private CardDBObject cardDataForPlacement;
        private Tile currentTargetTile;

        private void Update()
        {
            if (placementPreviewInstance != null)
            {
                UpdatePlacementPreview();
            }
            UpdateDebugSphere();
        }

        #region Card Placement Methods

        public void StartPlacementPreview(CardDBObject cardData)
        {
            if (cardData == null || cardData.cardPrefab == null) return;
            if (placementPreviewInstance != null) Destroy(placementPreviewInstance);
            cardDataForPlacement = cardData;
            placementPreviewInstance = Instantiate(cardData.cardPrefab);
            if (placementPreviewInstance.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent)) agent.enabled = false;
            if (placementPreviewInstance.TryGetComponent<BaseCharacter>(out var character)) character.enabled = false;
            foreach (var col in placementPreviewInstance.GetComponentsInChildren<Collider>()) col.enabled = false;
            foreach (var renderer in placementPreviewInstance.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = true;
            }
        }

        private void UpdatePlacementPreview()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 200f, TileLayerMask))
            {
                placementPreviewInstance.transform.position = hit.collider.transform.position + new Vector3(0, PlacementYOffset, 0);
                currentTargetTile = hit.collider.GetComponent<Tile>();
            }
            else
            {
                currentTargetTile = null;
                placementPreviewInstance.transform.position = new Vector3(0, -1000, 0);
            }
            UpdatePreviewVisuals();
        }

        private void UpdatePreviewVisuals()
        {
            bool isValid = currentTargetTile != null && currentTargetTile.tileType == TileType.Spawn && !currentTargetTile.IsOccupied;
            Material materialToApply = isValid ? ValidPlacementMaterial : InvalidPlacementMaterial;
            if (materialToApply != null)
            {
                foreach (var renderer in placementPreviewInstance.GetComponentsInChildren<Renderer>())
                {
                    renderer.material = materialToApply;
                    Debug.Log($"{LOG_PREFIX}Applying material {materialToApply.name} to renderer on {renderer.gameObject.name}");
                }
            }
        }

        public bool ConfirmPlacement(Tile placementTile)
        {
            if (placementPreviewInstance == null) return false;
            bool isValidTile = placementTile != null && placementTile.tileType == TileType.Spawn && !placementTile.IsOccupied;
            if (isValidTile)
            {
                GameObject spawnedObject = SpawnObject(cardDataForPlacement, placementTile);
                if (spawnedObject != null)
                {
                    CardManager.Instance.RemoveCardFromHand(cardDataForPlacement.cardId);
                    Destroy(placementPreviewInstance);
                    ResetPlacementState();
                    return true;
                }
                else
                {
                    Debug.LogError($"{LOG_PREFIX}Placement confirmed, but SpawnObject failed. Card will not be removed from hand.");
                    CancelPlacement();
                    return false;
                }
            }
            else
            {
                CancelPlacement();
                return false;
            }
        }

        public void CancelPlacement()
        {
            if (placementPreviewInstance != null) Destroy(placementPreviewInstance);
            ResetPlacementState();
        }

        private void ResetPlacementState()
        {
            placementPreviewInstance = null;
            cardDataForPlacement = null;
            currentTargetTile = null;
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
            // DraggableUnit 컴포넌트 추가 (필드에서 드래그 가능하게 함)
            DraggableUnit draggable = spawnedObject.GetComponent<DraggableUnit>();
            if (draggable == null)
            {
                draggable = spawnedObject.AddComponent<DraggableUnit>();
            }
            draggable.enabled = true;

            // UnitMergeTarget 컴포넌트 추가 (다른 유닛과 합칠 수 있게 함)
            UnitMergeTarget mergeTarget = spawnedObject.GetComponent<UnitMergeTarget>();
            if (mergeTarget == null)
            {
                mergeTarget = spawnedObject.AddComponent<UnitMergeTarget>();
            }
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

            foreach (BaseCharacter character in allCharacters)
            {
                if (character == null) continue;

                // DraggableUnit 컴포넌트 추가
                DraggableUnit draggable = character.GetComponent<DraggableUnit>();
                if (draggable == null)
                {
                    draggable = character.gameObject.AddComponent<DraggableUnit>();
                    enabledCount++;
                }
                draggable.enabled = true;

                // UnitMergeTarget 컴포넌트 추가
                UnitMergeTarget mergeTarget = character.GetComponent<UnitMergeTarget>();
                if (mergeTarget == null)
                {
                    mergeTarget = character.gameObject.AddComponent<UnitMergeTarget>();
                }
            }

            Debug.Log($"{LOG_PREFIX}필드의 {enabledCount}개 유닛에 드래그 기능을 활성화했습니다.");
        }
    }
}