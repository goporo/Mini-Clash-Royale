using System.Linq;
using System.Numerics;

namespace ClashServer
{
  // Pure server simulation
  public class MatchManager
  {
    private bool matchOver = false;
    private EntityTeam? winner = null;
    private float aiSpawnCooldown = 0;
    private float aiSpawnInterval = 8f;
    private System.Random random = new System.Random();

    private ILogger logger;

    public bool IsMatchOver => matchOver;
    public EntityTeam? Winner => winner;

    public MatchManager(ILogger logger = null)
    {
      this.logger = logger ?? new ConsoleLogger();
    }

    public void UpdateMatchState(GameplayDirector director)
    {
      if (matchOver) return;

      var team1Towers = director.GetEntitiesByTeam(EntityTeam.Team1)
          .Where(e => e.Type.Contains("tower")).ToList();
      var team2Towers = director.GetEntitiesByTeam(EntityTeam.Team2)
          .Where(e => e.Type.Contains("tower")).ToList();

      if (team1Towers.Count == 0)
      {
        matchOver = true;
        winner = EntityTeam.Team2;
        logger.Log("[Server] Match Over! Team2 Wins!");
      }
      else if (team2Towers.Count == 0)
      {
        matchOver = true;
        winner = EntityTeam.Team1;
        logger.Log("[Server] Match Over! Team1 Wins!");
      }
    }

    public void UpdateAI(float deltaTime, GameplayDirector director)
    {
      aiSpawnCooldown -= deltaTime;
      if (aiSpawnCooldown <= 0)
      {
        // Spawn AI unit for Team2
        float randomX = (float)(random.NextDouble() * 4.0 - 2.0); // Range -2 to 2
        Vector2 spawnPos = new Vector2(randomX, 15f);
        director.SpawnEntity("knight", spawnPos, EntityTeam.Team2);
        aiSpawnCooldown = aiSpawnInterval;
      }
    }

    public void Reset()
    {
      matchOver = false;
      winner = null;
      aiSpawnCooldown = 0;
    }
  }
}
