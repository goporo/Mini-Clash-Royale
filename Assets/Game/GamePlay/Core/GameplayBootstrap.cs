using System.Reflection;
using UnityEngine;

public class GameplayBootstrap : MonoBehaviour
{
  [SerializeField] private EntityViewFactory viewFactory;
  [SerializeField] private CardConfig playerKnightCard;
  [SerializeField] private UnitConfig aiKnightConfig;

  private GameplayDirector director;
  private PlayerSpawner playerSpawner;
  private AIController ai;
  private MatchEndDetector matchEnd;

  [SerializeField] BoardConfig boardConfig;
  [SerializeField] CardPlacementPreview placementPreview;
  [SerializeField] private BuildingConfig kingTowerCfg;
  [SerializeField] private BuildingConfig princessTowerCfg;

  SpawnController spawnController;
  Board board;

  void Awake()
  {
    GameplayCommandBus.Instance.AutoRegisterHandlers(Assembly.GetExecutingAssembly());


  }

  private void OnEnable()
  {
    GameplayEvents.Subscribe<CardPlayedEvent>(OnCardPlayed);
  }

  private void OnDisable()
  {
    GameplayEvents.Unsubscribe<CardPlayedEvent>(OnCardPlayed);
  }

  private void OnCardPlayed(CardPlayedEvent evt)
  {
    Debug.Log("PlayerSpawner received CardPlayedEvent");
  }

  void Start()
  {
    board = new Board(boardConfig);

    director = new GameplayDirector();
    spawnController = new SpawnController(director, viewFactory);

    playerSpawner = new PlayerSpawner(spawnController, playerKnightCard, board);
    ai = new AIController(spawnController, aiKnightConfig);
    matchEnd = new MatchEndDetector(director);

    placementPreview.Init(playerSpawner);

    spawnController.SpawnBuilding(princessTowerCfg, new Vector3(-5.5f, 0.5f, -3f), EntityTeam.Team1);
    spawnController.SpawnBuilding(princessTowerCfg, new Vector3(5.5f, 0.5f, -3f), EntityTeam.Team1);
    spawnController.SpawnBuilding(kingTowerCfg, new Vector3(0, 0.5f, -6.5f), EntityTeam.Team1);
    spawnController.SpawnBuilding(kingTowerCfg, new Vector3(0, 0.5f, 17.5f), EntityTeam.Team2);
    spawnController.SpawnBuilding(princessTowerCfg, new Vector3(-5.5f, 0.5f, 15f), EntityTeam.Team2);
    spawnController.SpawnBuilding(princessTowerCfg, new Vector3(5.5f, 0.5f, 15f), EntityTeam.Team2);


  }

  void Update()
  {
    // ════════════════════════════════════════════════════════════════
    // THIN CLIENT MODE - Server runs all gameplay logic
    // ════════════════════════════════════════════════════════════════
    // Unity only handles:
    // 1. Visual updates from server state
    // 2. Player input
    // 3. UI updates
    // ════════════════════════════════════════════════════════════════

    float dt = Time.deltaTime;



    // 2. Handle player input (send commands to server)
    HandlePlayerInput();

    // 3. Update UI
    // TODO: UIController.UpdateUI(dt);
  }

  /// <summary>
  /// Handle player input and send commands to server.
  /// </summary>
  private void HandlePlayerInput()
  {
    // Example: Send spawn command to server when space is pressed
    if (Input.GetKeyDown(KeyCode.Space))
    {
      Vector3 spawnPos = new Vector3(4, 0, -1);
      SendSpawnCommandToServer(spawnPos);
    }

    // TODO: Handle card dragging, placement preview, etc.
  }

  /// <summary>
  /// Send a spawn command to the Colyseus server.
  /// </summary>
  private void SendSpawnCommandToServer(Vector3 position)
  {
    if (NetworkClient.Instance?.Room == null)
    {
      Debug.LogWarning("Not connected to server!");
      return;
    }

    // Send spawn command to server
    NetworkClient.Instance.Room.Send("spawn-unit", new
    {
      x = position.x,
      y = position.y,
      unitType = "knight" // TODO: Get from selected card
    });

    Debug.Log($"[Client] Sent spawn command to server at {position}");
  }
}

