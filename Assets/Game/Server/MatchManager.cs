using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ClashServer
{
  // Pure server simulation - no Mirror dependencies
  // Match end detection and AI logic
  public class MatchManager
  {
    private bool matchOver = false;
    private EntityTeam? winner = null;
    private float aiSpawnCooldown = 0;
    private float aiSpawnInterval = 8f; // Spawn AI unit every 8 seconds

    public bool IsMatchOver => matchOver;
    public EntityTeam? Winner => winner;

    // Check for match end conditions
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
        Debug.Log("[Server] Match Over! Team2 Wins!");
      }
      else if (team2Towers.Count == 0)
      {
        matchOver = true;
        winner = EntityTeam.Team1;
        Debug.Log("[Server] Match Over! Team1 Wins!");
      }
    }

    // Simple AI that spawns units
    public void UpdateAI(float deltaTime, GameplayDirector director)
    {
      aiSpawnCooldown -= deltaTime;
      if (aiSpawnCooldown <= 0)
      {
        // Spawn AI unit for Team2
        Vector2 spawnPos = new Vector2(Random.Range(-2f, 2f), 15f);
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
