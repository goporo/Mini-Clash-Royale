using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ClashShared;

namespace ClashServer
{
  internal sealed class SimpleMatchAi
  {
    private enum Lane
    {
      Left,
      Center,
      Right
    }

    private readonly ILogger logger;
    private readonly Random random;
    private SimpleAiPlayerState state;
    private int nextDecisionTick;

    public SimpleMatchAi(ILogger logger = null, int? randomSeed = null)
    {
      this.logger = logger ?? new ConsoleLogger();
      random = new Random(randomSeed ?? 12345);
      state = new SimpleAiPlayerState();
      nextDecisionTick = 0;
    }

    public void TickRegen()
    {
      state.ElixirState.TickRegen();
    }

    public void Reset()
    {
      state = new SimpleAiPlayerState();
      nextDecisionTick = 0;
    }

    public MatchCommand? TryCreateCommand(int executionTick, GameplayDirector director, MatchManager matchManager)
    {
      if (matchManager == null || matchManager.IsMatchOver)
        return null;

      if (executionTick < nextDecisionTick)
        return null;

      if (TryCreateFireballCommand(executionTick, director, matchManager, out MatchCommand command) ||
          TryCreateDefenseCommand(executionTick, director, matchManager, out command) ||
          TryCreateSupportCommand(executionTick, director, matchManager, out command) ||
          TryCreateTankPushCommand(executionTick, director, matchManager, out command) ||
          TryCreateBridgePressureCommand(executionTick, director, matchManager, out command))
      {
        return command;
      }

      nextDecisionTick = executionTick + RandomTicks(4, 7);
      return null;
    }

    private bool TryCreateFireballCommand(int executionTick, GameplayDirector director, MatchManager matchManager, out MatchCommand command)
    {
      command = default;
      if (!state.Deck.IsInHand(CardId.Fireball) || !state.CanAfford(CardId.Fireball))
        return false;

      List<ServerEntity> enemies = director.GetEntitiesByTeam(EntityTeam.Team1)
        .Where(entity => entity.IsAlive)
        .ToList();

      if (enemies.Count == 0)
        return false;

      float bestScore = 0f;
      Vector2 bestPosition = Vector2.Zero;

      foreach (ServerEntity center in enemies)
      {
        float score = EvaluateFireballScore(center.Position, enemies);
        if (score > bestScore)
        {
          bestScore = score;
          bestPosition = center.Position;
        }
      }

      if (bestScore < 2.75f)
        return false;

      return TryCommitPlay(executionTick, CardId.Fireball, new[] { bestPosition }, matchManager, RandomTicks(6, 10), out command);
    }

    private bool TryCreateDefenseCommand(int executionTick, GameplayDirector director, MatchManager matchManager, out MatchCommand command)
    {
      command = default;

      ServerEntity threat = director.GetEntitiesByTeam(EntityTeam.Team1)
        .Where(entity => entity.IsAlive && !IsTower(entity))
        .OrderByDescending(GetThreatScore)
        .FirstOrDefault();

      if (threat == null || GetThreatScore(threat) < 6f)
        return false;

      Lane lane = GetLane(threat.Position.X);
      bool isHeavyThreat = threat.Type == CardId.Giant || threat.Type == CardId.MiniPekka || threat.IsBuilding;

      CardId[] priorities = isHeavyThreat
        ? new[] { CardId.Cannon, CardId.MiniPekka, CardId.Knight, CardId.Musketeer, CardId.Archer, CardId.Goblin }
        : new[] { CardId.Knight, CardId.Archer, CardId.Goblin, CardId.Musketeer, CardId.Cannon, CardId.MiniPekka };

      foreach (CardId cardId in priorities)
      {
        if (!state.Deck.IsInHand(cardId) || !state.CanAfford(cardId))
          continue;

        IEnumerable<Vector2> candidates = cardId == CardId.Cannon
          ? GetDefensiveBuildingCandidates(lane)
          : GetDefensiveTroopCandidates(threat.Position, cardId);

        if (TryCommitPlay(executionTick, cardId, candidates, matchManager, RandomTicks(4, 7), out command))
          return true;
      }

      return false;
    }

    private bool TryCreateSupportCommand(int executionTick, GameplayDirector director, MatchManager matchManager, out MatchCommand command)
    {
      command = default;

      ServerEntity giant = director.GetEntitiesByTeam(EntityTeam.Team2)
        .Where(entity => entity.IsAlive && entity.Type == CardId.Giant)
        .OrderBy(entity => entity.Position.Y)
        .FirstOrDefault();

      if (giant == null)
        return false;

      CardId[] supportOrder = { CardId.Musketeer, CardId.Archer, CardId.Knight, CardId.Goblin, CardId.MiniPekka };
      foreach (CardId cardId in supportOrder)
      {
        if (!state.Deck.IsInHand(cardId) || !state.CanAfford(cardId))
          continue;

        if (TryCommitPlay(
          executionTick,
          cardId,
          GetSupportCandidates(giant.Position, GetLane(giant.Position.X)),
          matchManager,
          RandomTicks(5, 8),
          out command))
        {
          return true;
        }
      }

      return false;
    }

    private bool TryCreateTankPushCommand(int executionTick, GameplayDirector director, MatchManager matchManager, out MatchCommand command)
    {
      command = default;
      if (!state.Deck.IsInHand(CardId.Giant) || !state.CanAfford(CardId.Giant))
        return false;

      int activeGiants = director.GetEntitiesByTeam(EntityTeam.Team2)
        .Count(entity => entity.IsAlive && entity.Type == CardId.Giant);

      if (activeGiants >= 1)
        return false;

      Lane lane = GetPreferredAttackLane(director);
      return TryCommitPlay(
        executionTick,
        CardId.Giant,
        GetBacklinePushCandidates(lane),
        matchManager,
        RandomTicks(8, 12),
        out command);
    }

    private bool TryCreateBridgePressureCommand(int executionTick, GameplayDirector director, MatchManager matchManager, out MatchCommand command)
    {
      command = default;
      Lane lane = GetPreferredAttackLane(director);

      List<CardId> pressureOrder = new()
      {
        CardId.MiniPekka,
        CardId.Knight,
        CardId.Musketeer,
        CardId.Archer,
        CardId.Goblin
      };

      foreach (CardId cardId in pressureOrder)
      {
        if (!state.Deck.IsInHand(cardId) || !state.CanAfford(cardId))
          continue;

        if (TryCommitPlay(
          executionTick,
          cardId,
          GetBridgePressureCandidates(lane, cardId),
          matchManager,
          RandomTicks(5, 9),
          out command))
        {
          return true;
        }
      }

      return false;
    }

    private bool TryCommitPlay(
      int executionTick,
      CardId cardId,
      IEnumerable<Vector2> candidatePositions,
      MatchManager matchManager,
      int cooldownTicks,
      out MatchCommand command)
    {
      command = default;

      foreach (Vector2 candidate in candidatePositions)
      {
        if (!matchManager.TryValidatePlayCard(cardId, candidate, state.Team, out _))
          continue;

        if (!state.CommitPlay(cardId))
          return false;

        nextDecisionTick = executionTick + cooldownTicks;
        command = MatchCommand.PlayCard(executionTick, state.PlayerId, cardId, candidate);
        logger.Log($"[AI] Queued {cardId} at {candidate}");
        return true;
      }

      return false;
    }

    private float EvaluateFireballScore(Vector2 center, IReadOnlyList<ServerEntity> enemies)
    {
      const float radius = 2.5f;
      float score = 0f;

      foreach (ServerEntity enemy in enemies)
      {
        if ((enemy.Position - center).Length() > radius + enemy.FootprintRadius)
          continue;

        score += enemy.IsBuilding ? 1.35f : 1f;

        if (enemy.Type == CardId.Musketeer || enemy.Type == CardId.MiniPekka || enemy.Type == CardId.Giant)
          score += 0.8f;

        if (enemy.Position.Y > 1f)
          score += 0.4f;
      }

      return score;
    }

    private float GetThreatScore(ServerEntity enemy)
    {
      if (enemy.Position.Y <= 1f)
        return 0f;

      float score = enemy.Position.Y * 0.8f;
      score += enemy.Stats.AttackDamage * 0.08f;
      score += enemy.Stats.CurrentHP * 0.02f;

      if (enemy.Type == CardId.Giant)
        score += 5f;

      if (enemy.Type == CardId.MiniPekka)
        score += 3f;

      if (enemy.IsBuilding)
        score += 2f;

      return score;
    }

    private Lane GetPreferredAttackLane(GameplayDirector director)
    {
      List<ServerEntity> enemyPrincessTowers = director.GetEntitiesByTeam(EntityTeam.Team1)
        .Where(entity => entity.IsAlive && entity.Type == CardId.PrincessTower)
        .OrderBy(entity => entity.Stats.CurrentHP)
        .ToList();

      if (enemyPrincessTowers.Count > 0)
        return GetLane(enemyPrincessTowers[0].Position.X);

      ServerEntity advancedFriendly = director.GetEntitiesByTeam(EntityTeam.Team2)
        .Where(entity => entity.IsAlive && !entity.IsBuilding && entity.Type != CardId.KingTower && entity.Type != CardId.PrincessTower)
        .OrderBy(entity => entity.Position.Y)
        .FirstOrDefault();

      if (advancedFriendly != null)
        return GetLane(advancedFriendly.Position.X);

      return random.NextDouble() < 0.5 ? Lane.Left : Lane.Right;
    }

    private IEnumerable<Vector2> GetDefensiveBuildingCandidates(Lane lane)
    {
      float x = lane switch
      {
        Lane.Left => -2.5f,
        Lane.Right => 2.5f,
        _ => 0f
      };

      yield return new Vector2(x, 8.5f);
      yield return new Vector2(x * 0.75f, 7.5f);
      yield return new Vector2(0f, 8f);
    }

    private IEnumerable<Vector2> GetDefensiveTroopCandidates(Vector2 threatPosition, CardId cardId)
    {
      float baseY = Math.Clamp(threatPosition.Y + (IsRangedSupport(cardId) ? 3f : 2f), 4f, 13.5f);
      float clampedX = Math.Clamp(threatPosition.X, -6f, 6f);
      float spread = cardId == CardId.Goblin || cardId == CardId.Archer ? 0.9f : 0.5f;

      yield return new Vector2(clampedX, baseY);
      yield return new Vector2(Math.Clamp(clampedX - spread, -7f, 7f), Math.Clamp(baseY + 0.6f, 1.2f, 15f));
      yield return new Vector2(Math.Clamp(clampedX + spread, -7f, 7f), Math.Clamp(baseY + 0.3f, 1.2f, 15f));
      yield return new Vector2(clampedX * 0.6f, Math.Clamp(baseY + 1f, 1.2f, 15f));
    }

    private IEnumerable<Vector2> GetSupportCandidates(Vector2 giantPosition, Lane lane)
    {
      float behindY = Math.Clamp(giantPosition.Y + 2.2f, 3.5f, 14f);
      float laneX = GetLaneX(lane);
      float mixX = Math.Clamp((giantPosition.X + laneX) * 0.5f, -6.5f, 6.5f);

      yield return new Vector2(mixX, behindY);
      yield return new Vector2(Math.Clamp(mixX - 0.8f, -7f, 7f), Math.Clamp(behindY + 0.3f, 1.2f, 15f));
      yield return new Vector2(Math.Clamp(mixX + 0.8f, -7f, 7f), Math.Clamp(behindY + 0.6f, 1.2f, 15f));
    }

    private IEnumerable<Vector2> GetBacklinePushCandidates(Lane lane)
    {
      float laneX = GetLaneX(lane);
      yield return new Vector2(laneX, 12.5f);
      yield return new Vector2(laneX * 0.8f, 11.5f);
      yield return new Vector2(laneX * 1.1f, 13.5f);
    }

    private IEnumerable<Vector2> GetBridgePressureCandidates(Lane lane, CardId cardId)
    {
      float laneX = GetLaneX(lane);
      float nearBridgeY = IsRangedSupport(cardId) ? 4.5f : 3.2f;
      float flankOffset = cardId == CardId.Goblin ? 1f : 0.6f;

      yield return new Vector2(laneX, nearBridgeY);
      yield return new Vector2(Math.Clamp(laneX - flankOffset, -7f, 7f), nearBridgeY + 0.5f);
      yield return new Vector2(Math.Clamp(laneX + flankOffset, -7f, 7f), nearBridgeY + 0.25f);
    }

    private static bool IsTower(ServerEntity entity)
    {
      return entity.Type == CardId.KingTower || entity.Type == CardId.PrincessTower;
    }

    private static bool IsRangedSupport(CardId cardId)
    {
      return cardId == CardId.Archer || cardId == CardId.Musketeer;
    }

    private static Lane GetLane(float x)
    {
      if (x < -1.5f)
        return Lane.Left;
      if (x > 1.5f)
        return Lane.Right;
      return Lane.Center;
    }

    private static float GetLaneX(Lane lane)
    {
      return lane switch
      {
        Lane.Left => -3.5f,
        Lane.Right => 3.5f,
        _ => 0f
      };
    }

    private int RandomTicks(int minInclusive, int maxInclusive)
    {
      return random.Next(minInclusive, maxInclusive + 1);
    }
  }
}
