using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using Unity.Services.Relay.Models;
using System.Diagnostics;
using ClashShared;

namespace ClashServer
{
  // Pure server simulation
  public class MatchManager
  {
    private bool matchOver = false;
    private EntityTeam? winner = null;
    private int aiSpawnCooldownTicks = 0;
    private int aiSpawnIntervalTicks = (int)(8f / ServerMatchController.FIXED_DT); // 8 seconds at 0.1s per tick
    private System.Random random;
    private readonly BoardManager boardManager = new();

    private ILogger logger;

    // Command queue for tick-based execution
    private Dictionary<int, List<MatchCommand>> commandQueue = new Dictionary<int, List<MatchCommand>>();

    // Match timing (Clash Royale style: 3 minutes)
    private const float MATCH_DURATION_SECONDS = 180f;
    private readonly int matchDurationTicks;
    private int currentMatchTick = 0;

    public bool IsMatchOver => matchOver;
    public EntityTeam? Winner => winner;
    public int CurrentMatchTick => currentMatchTick;
    public int MatchDurationTicks => matchDurationTicks;
    public float RemainingTimeSeconds => (matchDurationTicks - currentMatchTick) * ServerMatchController.FIXED_DT;
    public BoardManager BoardManager => boardManager;

    public struct RegionBounds
    {
      public float Left;
      public float Right;
      public float Bottom;
      public float Top;
      public float RiverBottom;
      public float RiverTop;
    }

    private enum CardType
    {
      Spell,
      Troop,
      Building
    }

    public MatchManager(ILogger logger = null, int? randomSeed = null)
    {
      this.logger = logger ?? new ConsoleLogger();
      matchDurationTicks = (int)(MATCH_DURATION_SECONDS / ServerMatchController.FIXED_DT);

      // Use deterministic seeded random for AI
      int seed = randomSeed ?? 12345;
      random = new System.Random(seed);

      logger.Log($"[MatchManager] Match duration: {MATCH_DURATION_SECONDS}s ({matchDurationTicks} ticks)");
      logger.Log($"[MatchManager] AI Random seed: {seed}");
    }

    /// <summary>
    /// Queue a command to be executed on a specific tick.
    /// Commands are NOT executed immediately - they wait for their tick.
    /// </summary>
    public void QueueCommand(MatchCommand command)
    {
      if (!commandQueue.ContainsKey(command.Tick))
      {
        commandQueue[command.Tick] = new List<MatchCommand>();
      }
      commandQueue[command.Tick].Add(command);
      logger.Log($"[MatchManager] Queued command for tick {command.Tick}: {command}");
    }

    /// <summary>
    /// Execute all commands queued for the current tick.
    /// Call this at the START of each tick, before simulation update.
    /// </summary>
    public void ProcessCommandsForTick(int currentTick, GameplayDirector director)
    {
      // Don't process commands if match is over
      if (matchOver)
        return;

      if (!commandQueue.TryGetValue(currentTick, out var commands))
        return;

      foreach (var cmd in commands)
      {
        ApplyCommand(cmd, director);
      }

      commandQueue.Remove(currentTick);
    }

    private void ApplyCommand(MatchCommand cmd, GameplayDirector director)
    {
      switch (cmd.Type)
      {
        case CommandType.PlayCard:
          ApplyPlayCard(cmd, director);
          break;
        case CommandType.Surrender:
          ApplySurrender(cmd);
          break;
        case CommandType.Emote:
          break;
      }
    }

    private void ApplyPlayCard(MatchCommand cmd, GameplayDirector director)
    {
      EntityTeam team = cmd.PlayerId == 0 ? EntityTeam.Team1 : EntityTeam.Team2;

      RegionBounds bounds = new()
      {
        Left = -9f,
        Right = 9f,
        Bottom = -16f,
        Top = 16f,
        RiverBottom = -1f,
        RiverTop = 1f
      };
      CardType cardType = MapCardIdToCardType(cmd.CardId);
      if (!ValidateSpawn(cardType, cmd.Position, team, bounds))
      {
        logger.Log($"[MatchManager] Invalid spawn position {cmd.Position} for PlayCard. Command ignored.");
        return;
      }

      bool isBuilding = cardType == CardType.Building;
      var entity = director.SpawnEntity(cmd.CardId, cmd.Position, team, isBuilding);
      if (isBuilding)
        boardManager.PlaceBuilding(entity);
    }

    private bool ValidateSpawn(CardType cardType, Vector2 pos, EntityTeam team, RegionBounds bounds)
    {
      if (!IsInsideBounds(pos, bounds))
        return false;

      return cardType switch
      {
        CardType.Spell => true,
        CardType.Troop => IsInsideBounds(pos, GetDeployRegion(team, bounds))
                          && !boardManager.IsTileOccupied(pos),
        CardType.Building => IsInsideBounds(pos, GetDeployRegion(team, bounds))
                             && !boardManager.IsTileOccupied(pos),
        _ => false
      };
    }

    private bool IsInsideBounds(Vector2 pos, RegionBounds bounds)
    {
      return pos.X >= bounds.Left &&
             pos.X <= bounds.Right &&
             pos.Y >= bounds.Bottom &&
             pos.Y <= bounds.Top;
    }

    private RegionBounds GetDeployRegion(EntityTeam team, RegionBounds bounds)
    {
      if (team == EntityTeam.Team1)
      {
        return new RegionBounds
        {
          Left = bounds.Left,
          Right = bounds.Right,
          Bottom = bounds.Bottom,
          Top = bounds.RiverBottom
        };
      }
      else
      {
        return new RegionBounds
        {
          Left = bounds.Left,
          Right = bounds.Right,
          Bottom = bounds.RiverTop,
          Top = bounds.Top
        };
      }
    }

    private void ApplySurrender(MatchCommand cmd)
    {
      EntityTeam team = cmd.PlayerId == 0 ? EntityTeam.Team1 : EntityTeam.Team2;
      EntityTeam opposingTeam = team == EntityTeam.Team1 ? EntityTeam.Team2 : EntityTeam.Team1;

      matchOver = true;
      winner = opposingTeam;
      logger.Log($"[MatchManager] Player {cmd.PlayerId} surrendered. Winner: {opposingTeam}");
    }

    private CardType MapCardIdToCardType(CardId cardId)
    {
      return cardId switch
      {
        CardId.Cannon => CardType.Building,
        // CardId.Fireball => CardType.Spell,  // uncomment when spell logic is added
        _ => CardType.Troop
      };
    }

    public void UpdateMatchState(GameplayDirector director)
    {
      if (matchOver) return;

      currentMatchTick++;

      var team1Towers = director.GetEntitiesByTeam(EntityTeam.Team1)
          .Where(e => e.Type == CardId.PrincessTower || e.Type == CardId.KingTower).ToList();
      var team2Towers = director.GetEntitiesByTeam(EntityTeam.Team2)
          .Where(e => e.Type == CardId.PrincessTower || e.Type == CardId.KingTower).ToList();

      // Win condition 1: All towers destroyed (immediate win)
      if (team1Towers.Count == 0)
      {
        matchOver = true;
        winner = EntityTeam.Team2;
        logger.Log("[Server] Match Over! Team2 Wins! (All Team1 towers destroyed)");
        return;
      }
      else if (team2Towers.Count == 0)
      {
        matchOver = true;
        winner = EntityTeam.Team1;
        logger.Log("[Server] Match Over! Team1 Wins! (All Team2 towers destroyed)");
        return;
      }

      // Win condition 2: Time limit reached (180 seconds)
      if (currentMatchTick >= matchDurationTicks)
      {
        logger.Log($"[Server] Match time limit reached ({MATCH_DURATION_SECONDS}s). Checking win condition...");
        CheckTimeoutWinCondition(director);
      }
    }

    /// <summary>
    /// Check win condition at match timeout (Clash Royale style).
    /// 1. Team with more towers destroyed wins
    /// 2. If tie, team with more total tower HP wins
    /// 3. If still tie, it's a draw
    /// </summary>
    public void CheckTimeoutWinCondition(GameplayDirector director)
    {
      if (matchOver) return;

      var team1Towers = director.GetEntitiesByTeam(EntityTeam.Team1)
          .Where(e => e.Type == CardId.PrincessTower || e.Type == CardId.KingTower).ToList();
      var team2Towers = director.GetEntitiesByTeam(EntityTeam.Team2)
          .Where(e => e.Type == CardId.PrincessTower || e.Type == CardId.KingTower).ToList();

      // More towers standing = winner (fewer destroyed)
      if (team1Towers.Count > team2Towers.Count)
      {
        matchOver = true;
        winner = EntityTeam.Team1;
        logger.Log($"[Server] Match Over! Team1 Wins! (More towers: {team1Towers.Count} vs {team2Towers.Count})");
      }
      else if (team2Towers.Count > team1Towers.Count)
      {
        matchOver = true;
        winner = EntityTeam.Team2;
        logger.Log($"[Server] Match Over! Team2 Wins! (More towers: {team2Towers.Count} vs {team1Towers.Count})");
      }
      else
      {
        // Same number of towers - check total HP
        float team1HP = team1Towers.Sum(t => t.Stats.CurrentHP);
        float team2HP = team2Towers.Sum(t => t.Stats.CurrentHP);

        if (team1HP > team2HP)
        {
          matchOver = true;
          winner = EntityTeam.Team1;
          logger.Log($"[Server] Match Over! Team1 Wins! (More HP: {team1HP:F0} vs {team2HP:F0})");
        }
        else if (team2HP > team1HP)
        {
          matchOver = true;
          winner = EntityTeam.Team2;
          logger.Log($"[Server] Match Over! Team2 Wins! (More HP: {team2HP:F0} vs {team1HP:F0})");
        }
        else
        {
          matchOver = true;
          winner = null; // Draw
          logger.Log("[Server] Match Over! Draw! (Same towers and HP)");
        }
      }
    }

    /// <summary>
    /// Update AI and return any commands the AI wants to execute.
    /// Call this and queue the returned command if present.
    /// </summary>
    public MatchCommand? UpdateAI(int currentTick)
    {
      // Stop AI when match is over
      if (matchOver)
        return null;

      aiSpawnCooldownTicks--;
      if (aiSpawnCooldownTicks <= 0)
      {
        // Create AI command for Team2 (PlayerId = 1)
        float randomX = (float)(random.NextDouble() * 4.0 - 2.0); // Range -2 to 2
        Vector2 spawnPos = new Vector2(randomX, 15f);

        aiSpawnCooldownTicks = aiSpawnIntervalTicks;

        // Return command to be queued
        return MatchCommand.PlayCard(currentTick, 1, CardId.Knight, spawnPos);
      }

      return null;
    }

    public void Reset()
    {
      matchOver = false;
      winner = null;
      aiSpawnCooldownTicks = 0;
      currentMatchTick = 0;
      commandQueue.Clear();
      boardManager.Clear();
    }
  }
}
