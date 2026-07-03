using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ClashShared;

namespace ClashServer
{
  public class MatchManager
  {
    public event Action<CardId, Vector2, EntityTeam> SpellCastResolved;

    private readonly BoardManager boardManager;
    private readonly ILogger logger;
    private readonly Dictionary<int, List<MatchCommand>> commandQueue = new();

    private bool matchOver;
    private EntityTeam? winner;
    private int currentMatchTick;

    private const float MATCH_DURATION_SECONDS = 180f;
    private readonly int matchDurationTicks;

    public MatchManager(BoardManager boardManager, ILogger logger = null, int? randomSeed = null)
    {
      this.boardManager = boardManager ?? throw new ArgumentNullException(nameof(boardManager));
      this.logger = logger ?? new ConsoleLogger();
      matchDurationTicks = (int)(MATCH_DURATION_SECONDS / ServerTickSettings.FixedDeltaTime);

      this.logger.Log($"[MatchManager] Match duration: {MATCH_DURATION_SECONDS}s ({matchDurationTicks} ticks)");
    }

    public bool IsMatchOver => matchOver;
    public EntityTeam? Winner => winner;
    public int CurrentMatchTick => currentMatchTick;
    public int MatchDurationTicks => matchDurationTicks;
    public float RemainingTimeSeconds => (matchDurationTicks - currentMatchTick) * ServerTickSettings.FixedDeltaTime;
    public BoardManager BoardManager => boardManager;

    public void QueueCommand(MatchCommand command)
    {
      if (!commandQueue.ContainsKey(command.Tick))
        commandQueue[command.Tick] = new List<MatchCommand>();

      commandQueue[command.Tick].Add(command);
      logger.Log($"[MatchManager] Queued command for tick {command.Tick}: {command}");
    }

    public void ProcessCommandsForTick(int currentTick, GameplayDirector director)
    {
      if (matchOver || !commandQueue.TryGetValue(currentTick, out List<MatchCommand> commands))
        return;

      foreach (MatchCommand cmd in commands)
        ApplyCommand(cmd, director);

      commandQueue.Remove(currentTick);
    }

    private void ApplyCommand(MatchCommand cmd, GameplayDirector director)
    {
      try
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
      catch (Exception ex)
      {
        logger.LogWarning($"[MatchManager] Command failed (player {cmd.PlayerId}): {ex.Message}");
      }
    }

    private void ApplyPlayCard(MatchCommand cmd, GameplayDirector director)
    {
      EntityTeam team = cmd.PlayerId == 0 ? EntityTeam.Team1 : EntityTeam.Team2;

      if (!BattleArena.TryGetCardType(cmd.CardId, out CardType cardType) ||
          !ValidateSpawn(cmd.CardId, cardType, cmd.Position, team, ComputeDeployZoneState(director, team)))
      {
        logger.Log($"[MatchManager] Invalid spawn position {cmd.Position} for PlayCard. Command ignored.");
        return;
      }

      if (cardType == CardType.Spell)
      {
        director.ApplySpellEffect(cmd.CardId, cmd.Position, team);
        SpellCastResolved?.Invoke(cmd.CardId, cmd.Position, team);
        return;
      }

      bool isBuilding = cardType == CardType.Building;
      SpawnUnit[] recipe = isBuilding
        ? new[] { new SpawnUnit(cmd.CardId, SpawnFormation.Single) }
        : BattleArena.GetSpawnRecipe(cmd.CardId, team);

      var spawnedEntities = new List<ServerEntity>();
      foreach (SpawnUnit unit in recipe)
      {
        Vector2 unitPos = new Vector2(cmd.Position.X + unit.OffsetX, cmd.Position.Y + unit.OffsetY);
        spawnedEntities.AddRange(director.SpawnCard(unit.EntityType, unitPos, team, unit.Formation, isBuilding));
      }

      if (isBuilding && spawnedEntities.Count > 0)
        boardManager.PlaceBuilding(spawnedEntities[0]);
    }

    public bool TryValidatePlayCard(CardId cardId, Vector2 position, EntityTeam team, GameplayDirector director, out string failureReason)
    {
      failureReason = null;

      if (matchOver)
      {
        failureReason = "Match already ended";
        return false;
      }

      if (!BattleArena.TryGetCardType(cardId, out CardType cardType))
      {
        failureReason = "Unknown card";
        return false;
      }

      if (!ValidateSpawn(cardId, cardType, position, team, ComputeDeployZoneState(director, team)))
      {
        failureReason = "Invalid position";
        return false;
      }

      return true;
    }

    // Deploy-zone state for `team`, derived from which of the ENEMY's Princess towers survive.
    public DeployZoneState ComputeDeployZoneState(GameplayDirector director, EntityTeam team)
    {
      EntityTeam enemy = team == EntityTeam.Team1 ? EntityTeam.Team2 : EntityTeam.Team1;

      bool negXAlive = false;
      bool posXAlive = false;
      foreach (ServerEntity tower in director.GetEntitiesByTeam(enemy))
      {
        if (tower.Type != CardId.PrincessTower)
          continue;
        if (BattleArena.IsNegXLane(tower.Position.X))
          negXAlive = true;
        else
          posXAlive = true;
      }

      return BattleArena.GetDeployZoneState(negXAlive, posXAlive);
    }

    private bool ValidateSpawn(CardId cardId, CardType cardType, Vector2 position, EntityTeam team, DeployZoneState zone)
    {
      if (!BattleArena.IsInsideArena(position.X, position.Y))
        return false;

      if (cardType == CardType.Spell)
        return true;

      if (cardType == CardType.Troop)
        return BattleArena.IsInsideDeployZone(team, position.X, position.Y, zone)
            && !boardManager.IsTileOccupied(position);

      if (cardType == CardType.Building)
      {
        var (w, h) = BattleArena.GetBuildingSize(cardId);
        return BattleArena.IsInsideBuildingDeployZone(team, position.X, position.Y, w * 0.5f, h * 0.5f, zone)
            && !boardManager.IsTileOccupied(position);
      }

      return false;
    }

    private void ApplySurrender(MatchCommand cmd)
    {
      EntityTeam team = cmd.PlayerId == 0 ? EntityTeam.Team1 : EntityTeam.Team2;
      EntityTeam opposingTeam = team == EntityTeam.Team1 ? EntityTeam.Team2 : EntityTeam.Team1;

      matchOver = true;
      winner = opposingTeam;
      logger.Log($"[MatchManager] Player {cmd.PlayerId} surrendered. Winner: {opposingTeam}");
    }

    public void UpdateMatchState(GameplayDirector director)
    {
      if (matchOver)
        return;

      currentMatchTick++;

      List<ServerEntity> team1Towers = director.GetEntitiesByTeam(EntityTeam.Team1)
        .Where(entity => entity.Type == CardId.PrincessTower || entity.Type == CardId.KingTower)
        .ToList();
      List<ServerEntity> team2Towers = director.GetEntitiesByTeam(EntityTeam.Team2)
        .Where(entity => entity.Type == CardId.PrincessTower || entity.Type == CardId.KingTower)
        .ToList();

      if (team1Towers.Count == 0)
      {
        matchOver = true;
        winner = EntityTeam.Team2;
        logger.Log("[Server] Match Over! Team2 Wins! (All Team1 towers destroyed)");
        return;
      }

      if (team2Towers.Count == 0)
      {
        matchOver = true;
        winner = EntityTeam.Team1;
        logger.Log("[Server] Match Over! Team1 Wins! (All Team2 towers destroyed)");
        return;
      }

      if (currentMatchTick >= matchDurationTicks)
      {
        logger.Log($"[Server] Match time limit reached ({MATCH_DURATION_SECONDS}s). Checking win condition...");
        CheckTimeoutWinCondition(director);
      }
    }

    public void CheckTimeoutWinCondition(GameplayDirector director)
    {
      if (matchOver)
        return;

      List<ServerEntity> team1Towers = director.GetEntitiesByTeam(EntityTeam.Team1)
        .Where(entity => entity.Type == CardId.PrincessTower || entity.Type == CardId.KingTower)
        .ToList();
      List<ServerEntity> team2Towers = director.GetEntitiesByTeam(EntityTeam.Team2)
        .Where(entity => entity.Type == CardId.PrincessTower || entity.Type == CardId.KingTower)
        .ToList();

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
        float team1HP = team1Towers.Sum(tower => tower.Stats.CurrentHP);
        float team2HP = team2Towers.Sum(tower => tower.Stats.CurrentHP);

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
          winner = null;
          logger.Log("[Server] Match Over! Draw! (Same towers and HP)");
        }
      }
    }

    public void Reset()
    {
      matchOver = false;
      winner = null;
      currentMatchTick = 0;
      commandQueue.Clear();
      boardManager.Clear();
    }
  }
}
