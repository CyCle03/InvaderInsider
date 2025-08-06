using UnityEngine;
using System;
using InvaderInsider.Data;
using UnityEngine.SceneManagement;
using InvaderInsider.Cards;
using InvaderInsider.UI; // UIManager 네임스페이스 추가

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
        public int SelectedCardId { get; set; } = -1; // -1 indicates no card is selected
        public CardDBObject DraggedCardData { get; set; }

        private UIManager uiManager; // UIManager 참조 추가

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded; // 씬 로드 이벤트 구독
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded; // 이벤트 구독 해제
        }

        private void Start()
        {
            SetGameState(GameState.MainMenu);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // UIManager 인스턴스를 직접 찾습니다.
            uiManager = FindObjectOfType<UIManager>();
            if (uiManager == null)
            {
                Debug.LogError($"{LOG_PREFIX}UIManager를 찾을 수 없습니다.");
                return;
            }

            InitializeUIForScene(scene.name); // 씬에 맞는 UI 초기화

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
            // 모든 BasePanel 컴포넌트를 찾습니다.
            BasePanel[] allPanels = FindObjectsOfType<BasePanel>(true);

            // UIManager에 패널들을 등록하고 초기 상태를 설정합니다.
            foreach (BasePanel panel in allPanels)
            {
                if (panel != null)
                {
                    uiManager.RegisterPanel(panel.name, panel); // 패널의 GameObject 이름을 키로 사용
                }
            }

            // 모든 패널을 비활성화합니다.
            foreach (BasePanel panel in allPanels)
            {
                if (panel != null)
                {
                    panel.gameObject.SetActive(false);
                }
            }

            // 씬에 따라 필요한 패널만 활성화합니다.
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
            // 기존 Player의 OnDeath 이벤트 구독 해제 (새 게임 시작 시)
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
                    // 스테이지 시작 후 UI를 즉시 업데이트
                    UpdateStageWaveUI(stageManager.GetCurrentStageIndex() + 1, stageManager.GetSpawnedEnemyCount(), stageManager.GetStageWaveCount(stageManager.GetCurrentStageIndex()));

                    // 새 Player 인스턴스의 OnDeath 이벤트 구독
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
            uiManager?.ShowPanel("GameOver"); // UIManager를 통해 패널 표시
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
            // UIManager를 통해 TopBarPanel의 UI를 업데이트하는 예시
            // TopBarPanel의 GameObject 이름이 "TopBarPanel"이라고 가정
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

        [Header("Card Placement Settings")]
        [SerializeField] private LayerMask tileLayerMask; // 타일 오브젝트들이 속한 레이어를 설정합니다.
        [SerializeField] private Material validPlacementMaterial; // 배치 가능 시 프리뷰에 적용할 반투명 초록색 재질
        [SerializeField] private Material invalidPlacementMaterial; // 배치 불가능 시 프리뷰에 적용할 반투명 빨간색 재질
        [SerializeField] private float placementYOffset = 0.0f; // 유닛 배치 시 Y축 오프셋

        private GameObject placementPreviewInstance;
        private CardDBObject cardDataForPlacement;
        private Tile currentTargetTile;


        private void Update()
        {
            if (placementPreviewInstance != null)
            {
                UpdatePlacementPreview();
            }
        }

        #region Card Placement Methods

        public void StartPlacementPreview(CardDBObject cardData)
        {
            if (cardData == null || cardData.cardPrefab == null) return;

            if (placementPreviewInstance != null) Destroy(placementPreviewInstance);

            cardDataForPlacement = cardData;
            placementPreviewInstance = Instantiate(cardData.cardPrefab);

            // 프리뷰가 게임 로직에 영향을 주지 않도록 주요 컴포넌트 비활성화
            if (placementPreviewInstance.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent)) agent.enabled = false;
            if (placementPreviewInstance.TryGetComponent<BaseCharacter>(out var character)) character.enabled = false;
            foreach (var col in placementPreviewInstance.GetComponentsInChildren<Collider>()) col.enabled = false;
        }

        private void UpdatePlacementPreview()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 200f, tileLayerMask))
            {
                placementPreviewInstance.transform.position = hit.collider.transform.position + new Vector3(0, placementYOffset, 0); // 타일 중앙에 스냅 및 Y축 오프셋 적용
                currentTargetTile = hit.collider.GetComponent<Tile>();
            }
            else
            {
                currentTargetTile = null;
                placementPreviewInstance.transform.position = new Vector3(0, -1000, 0); // 타일이 아니면 화면 밖으로
            }
            UpdatePreviewVisuals();
        }

        private void UpdatePreviewVisuals()
        {
            bool isValid = currentTargetTile != null && currentTargetTile.tileType == TileType.Spawn && !currentTargetTile.IsOccupied;
            Material materialToApply = isValid ? validPlacementMaterial : invalidPlacementMaterial;

            if (materialToApply != null)
            {
                foreach (var renderer in placementPreviewInstance.GetComponentsInChildren<Renderer>())
                {
                    renderer.material = materialToApply;
                }
            }
        }

        public bool ConfirmPlacement()
        {
            if (placementPreviewInstance == null) return false;

            bool isValidTile = currentTargetTile != null && currentTargetTile.tileType == TileType.Spawn && !currentTargetTile.IsOccupied;

            if (isValidTile)
            {
                GameObject spawnedObject = SpawnObject(cardDataForPlacement, currentTargetTile);

                if (spawnedObject != null)
                {
                    // 생성에 성공한 경우에만 카드를 제거하고 프리뷰를 정리합니다.
                    CardManager.Instance.RemoveCardFromHand(cardDataForPlacement.cardId);
                    Destroy(placementPreviewInstance);
                    ResetPlacementState();
                    return true;
                }
                else
                {
                    // 생성에 실패하면 프리뷰만 취소하고 카드는 핸드에 남겨둡니다.
                    Debug.LogError($"{LOG_PREFIX}Placement confirmed, but SpawnObject failed. Card will not be removed from hand.");
                    CancelPlacement();
                    return false;
                }
            }
            else
            {
                // 유효하지 않은 타일이므로 프리뷰를 취소합니다.
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
            spawnPosition.y += placementYOffset; // Y축 오프셋 적용

            GameObject spawnedObject = Instantiate(cardData.cardPrefab, spawnPosition, Quaternion.identity);
            if (spawnedObject == null)
            {
                Debug.LogError($"{LOG_PREFIX}Failed to instantiate prefab for card '{cardData.cardName}'.");
                return null;
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
                        tile.SetOccupied(true); // 캐릭터도 타일을 점유하도록 변경
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
                    SelectedTowerPrefab = card.cardPrefab;
                    SelectedCardId = card.cardId;
                    break;
                case CardType.Character:
                    // TODO: Implement character card logic
                    break;
                case CardType.Equipment:
                    // TODO: Implement equipment card logic
                    break;
            }
        }
    }
}