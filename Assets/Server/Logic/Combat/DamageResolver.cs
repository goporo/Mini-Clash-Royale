using System.Collections.Generic;
using System.Numerics;
using ClashShared;

namespace ClashServer
{
  public sealed class DamageResolver
  {
    private readonly ILogger logger;

    public DamageResolver(ILogger logger = null)
    {
      this.logger = logger ?? new ConsoleLogger();
    }

    public List<SpawnRequest> ApplyPendingDamage(IReadOnlyList<PendingDamage> pendingDamage, IReadOnlyList<ServerEntity> allEntities)
    {
      var justDied = new List<ServerEntity>();

      foreach (PendingDamage hit in pendingDamage)
      {
        if (!hit.Target.IsAlive)
          continue;

        hit.Target.TakeDamage(hit.Damage);
        if (!hit.Target.IsAlive)
          justDied.Add(hit.Target);
      }

      var spawnRequests = new List<SpawnRequest>();
      foreach (ServerEntity dead in justDied)
      {
        EntityEffectContext ctx = dead.HandleDeath(allEntities);
        if (ctx != null)
          spawnRequests.AddRange(ctx.SpawnRequests);
      }
      return spawnRequests;
    }

    public void ApplySpellEffect(CardId spellCardId, Vector2 position, EntityTeam team, IEnumerable<ServerEntity> entities)
    {
      switch (spellCardId)
      {
        case CardId.Fireball:
          const float fireballRadius = 2.5f;
          const float fireballDamage = 150f;

          foreach (ServerEntity entity in entities)
          {
            if (!entity.IsAlive || entity.Team == team)
              continue;

            if ((entity.Position - position).Length() > fireballRadius + entity.FootprintRadius)
              continue;

            entity.TakeDamage(fireballDamage);
            logger.Log($"[Server] Fireball hit Entity {entity.Id} for {fireballDamage} damage");
          }
          break;
      }
    }
  }
}
