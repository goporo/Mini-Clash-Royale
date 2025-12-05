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
    // if (matchEnd.IsMatchOver)
    // {
    //   Debug.Log("Winner: " + matchEnd.Winner);
    //   return;
    // }

    float dt = Time.deltaTime;

    director.Tick(dt);
    ai.Tick(dt);
    matchEnd.Tick();

    if (Input.GetKeyDown(KeyCode.Space))
      playerSpawner.SpawnAt(new Vector3(4, 0, -1));
  }
}

