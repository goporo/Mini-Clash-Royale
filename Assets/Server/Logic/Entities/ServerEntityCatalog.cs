using System;
using ClashShared;

namespace ClashServer
{
  public static class ServerEntityCatalog
  {
    private static int Ticks(float seconds) =>
      (int)MathF.Round(seconds / ServerTickSettings.FixedDeltaTime);

    public static ServerEntityDefinition Get(CardId cardId)
    {
      CardStats s = CardStatsTable.Get(cardId);
      return cardId switch
      {
        CardId.PrincessTower => new ServerEntityDefinition
        {
          CardId = cardId,
          CardType = CardType.Building,
          Stats = new EntityStats(s.MaxHP, s.MoveSpeed),
          FootprintRadius = s.FootprintRadius,
          CollisionRadius = s.CollisionRadius,
          PushWeight = s.PushWeight,
          Movement = MovementKind.Stationary,
          Targeting = new TargetingRule { Category = TargetCategory.AnyEnemy, Layers = TargetLayer.GroundAndAir },
          Attack = new AttackRule
          {
            Kind = AttackKind.Projectile,
            DamagePattern = s.DamagePattern,
            Damage = s.AttackDamage,
            Range = s.AttackRange,
            SplashRadius = s.SplashRadius,
            CooldownTicks = Ticks(s.AttackCooldown),
            AffectedLayers = TargetLayer.GroundAndAir
          }
        },

        CardId.KingTower => new ServerEntityDefinition
        {
          CardId = cardId,
          CardType = CardType.Building,
          Stats = new EntityStats(s.MaxHP, s.MoveSpeed),
          FootprintRadius = s.FootprintRadius,
          CollisionRadius = s.CollisionRadius,
          PushWeight = s.PushWeight,
          Movement = MovementKind.Stationary,
          Targeting = new TargetingRule { Category = TargetCategory.AnyEnemy, Layers = TargetLayer.GroundAndAir },
          Attack = new AttackRule
          {
            Kind = AttackKind.Projectile,
            DamagePattern = s.DamagePattern,
            Damage = s.AttackDamage,
            Range = s.AttackRange,
            SplashRadius = s.SplashRadius,
            CooldownTicks = Ticks(s.AttackCooldown),
            AffectedLayers = TargetLayer.GroundAndAir
          }
        },

        CardId.Knight => new ServerEntityDefinition
        {
          CardId = cardId,
          CardType = CardType.Troop,
          Stats = new EntityStats(s.MaxHP, s.MoveSpeed),
          FootprintRadius = s.FootprintRadius,
          CollisionRadius = s.CollisionRadius,
          PushWeight = s.PushWeight,
          Movement = MovementKind.Ground,
          Targeting = new TargetingRule { Category = TargetCategory.AnyEnemy, Layers = TargetLayer.GroundAndAir },
          Attack = new AttackRule
          {
            Kind = AttackKind.Melee,
            DamagePattern = s.DamagePattern,
            Damage = s.AttackDamage,
            Range = s.AttackRange,
            SplashRadius = s.SplashRadius,
            CooldownTicks = Ticks(s.AttackCooldown),
            AffectedLayers = TargetLayer.Ground
          }
        },

        CardId.Archer => new ServerEntityDefinition
        {
          CardId = cardId,
          CardType = CardType.Troop,
          Stats = new EntityStats(s.MaxHP, s.MoveSpeed),
          FootprintRadius = s.FootprintRadius,
          CollisionRadius = s.CollisionRadius,
          PushWeight = s.PushWeight,
          Movement = MovementKind.Ground,
          Targeting = new TargetingRule { Category = TargetCategory.AnyEnemy, Layers = TargetLayer.GroundAndAir },
          Attack = new AttackRule
          {
            Kind = AttackKind.Projectile,
            DamagePattern = s.DamagePattern,
            Damage = s.AttackDamage,
            Range = s.AttackRange,
            SplashRadius = s.SplashRadius,
            CooldownTicks = Ticks(s.AttackCooldown),
            AffectedLayers = TargetLayer.GroundAndAir
          }
        },

        CardId.Giant => new ServerEntityDefinition
        {
          CardId = cardId,
          CardType = CardType.Troop,
          Stats = new EntityStats(s.MaxHP, s.MoveSpeed),
          FootprintRadius = s.FootprintRadius,
          CollisionRadius = s.CollisionRadius,
          PushWeight = s.PushWeight,
          Movement = MovementKind.Ground,
          Targeting = new TargetingRule { Category = TargetCategory.BuildingsOnly, Layers = TargetLayer.Ground },
          Attack = new AttackRule
          {
            Kind = AttackKind.Melee,
            DamagePattern = s.DamagePattern,
            Damage = s.AttackDamage,
            Range = s.AttackRange,
            SplashRadius = s.SplashRadius,
            CooldownTicks = Ticks(s.AttackCooldown),
            AffectedLayers = TargetLayer.Ground
          }
        },

        CardId.Cannon => new ServerEntityDefinition
        {
          CardId = cardId,
          CardType = CardType.Building,
          Stats = new EntityStats(s.MaxHP, s.MoveSpeed),
          FootprintRadius = s.FootprintRadius,
          CollisionRadius = s.CollisionRadius,
          PushWeight = s.PushWeight,
          Movement = MovementKind.Stationary,
          Targeting = new TargetingRule { Category = TargetCategory.AnyEnemy, Layers = TargetLayer.Ground },
          Attack = new AttackRule
          {
            Kind = AttackKind.Projectile,
            DamagePattern = s.DamagePattern,
            Damage = s.AttackDamage,
            Range = s.AttackRange,
            SplashRadius = s.SplashRadius,
            CooldownTicks = Ticks(s.AttackCooldown),
            AffectedLayers = TargetLayer.Ground
          }
        },

        CardId.Goblin => new ServerEntityDefinition
        {
          CardId = cardId,
          CardType = CardType.Troop,
          Stats = new EntityStats(s.MaxHP, s.MoveSpeed),
          FootprintRadius = s.FootprintRadius,
          CollisionRadius = s.CollisionRadius,
          PushWeight = s.PushWeight,
          Movement = MovementKind.Ground,
          Targeting = new TargetingRule { Category = TargetCategory.AnyEnemy, Layers = TargetLayer.GroundAndAir },
          Attack = new AttackRule
          {
            Kind = AttackKind.Melee,
            DamagePattern = s.DamagePattern,
            Damage = s.AttackDamage,
            Range = s.AttackRange,
            SplashRadius = s.SplashRadius,
            CooldownTicks = Ticks(s.AttackCooldown),
            AffectedLayers = TargetLayer.Ground
          }
        },

        CardId.Musketeer => new ServerEntityDefinition
        {
          CardId = cardId,
          CardType = CardType.Troop,
          Stats = new EntityStats(s.MaxHP, s.MoveSpeed),
          FootprintRadius = s.FootprintRadius,
          CollisionRadius = s.CollisionRadius,
          PushWeight = s.PushWeight,
          Movement = MovementKind.Ground,
          Targeting = new TargetingRule { Category = TargetCategory.AnyEnemy, Layers = TargetLayer.GroundAndAir },
          Attack = new AttackRule
          {
            Kind = AttackKind.Projectile,
            DamagePattern = s.DamagePattern,
            Damage = s.AttackDamage,
            Range = s.AttackRange,
            SplashRadius = s.SplashRadius,
            CooldownTicks = Ticks(s.AttackCooldown),
            AffectedLayers = TargetLayer.GroundAndAir
          }
        },

        CardId.MiniPekka => new ServerEntityDefinition
        {
          CardId = cardId,
          CardType = CardType.Troop,
          Stats = new EntityStats(s.MaxHP, s.MoveSpeed),
          FootprintRadius = s.FootprintRadius,
          CollisionRadius = s.CollisionRadius,
          PushWeight = s.PushWeight,
          Movement = MovementKind.Ground,
          Targeting = new TargetingRule { Category = TargetCategory.AnyEnemy, Layers = TargetLayer.GroundAndAir },
          Attack = new AttackRule
          {
            Kind = AttackKind.Melee,
            DamagePattern = s.DamagePattern,
            Damage = s.AttackDamage,
            Range = s.AttackRange,
            SplashRadius = s.SplashRadius,
            CooldownTicks = Ticks(s.AttackCooldown),
            AffectedLayers = TargetLayer.Ground
          }
        },

        CardId.Valkyrie => new ServerEntityDefinition
        {
          CardId = cardId,
          CardType = CardType.Troop,
          Stats = new EntityStats(s.MaxHP, s.MoveSpeed),
          FootprintRadius = s.FootprintRadius,
          CollisionRadius = s.CollisionRadius,
          PushWeight = s.PushWeight,
          Movement = MovementKind.Ground,
          Targeting = new TargetingRule { Category = TargetCategory.AnyEnemy, Layers = TargetLayer.GroundAndAir },
          Attack = new AttackRule
          {
            Kind = AttackKind.Melee,
            DamagePattern = s.DamagePattern,
            Damage = s.AttackDamage,
            Range = s.AttackRange,
            SplashRadius = s.SplashRadius,
            CooldownTicks = Ticks(s.AttackCooldown),
            AffectedLayers = TargetLayer.Ground
          }
        },

        CardId.Bomber => new ServerEntityDefinition
        {
          CardId = cardId,
          CardType = CardType.Troop,
          Stats = new EntityStats(s.MaxHP, s.MoveSpeed),
          FootprintRadius = s.FootprintRadius,
          CollisionRadius = s.CollisionRadius,
          PushWeight = s.PushWeight,
          Movement = MovementKind.Ground,
          Targeting = new TargetingRule { Category = TargetCategory.AnyEnemy, Layers = TargetLayer.Ground },
          Attack = new AttackRule
          {
            Kind = AttackKind.Projectile,
            DamagePattern = s.DamagePattern,
            Damage = s.AttackDamage,
            Range = s.AttackRange,
            SplashRadius = s.SplashRadius,
            CooldownTicks = Ticks(s.AttackCooldown),
            WindupTicks = 6,
            AffectedLayers = TargetLayer.Ground,
            ProjectileType = ProjectileType.Bomb
          }
        },

        CardId.IceWizard => new ServerEntityDefinition
        {
          CardId = cardId,
          CardType = CardType.Troop,
          Stats = new EntityStats(s.MaxHP, s.MoveSpeed),
          FootprintRadius = s.FootprintRadius,
          CollisionRadius = s.CollisionRadius,
          PushWeight = s.PushWeight,
          Movement = MovementKind.Ground,
          Targeting = new TargetingRule { Category = TargetCategory.AnyEnemy, Layers = TargetLayer.GroundAndAir },
          Attack = new AttackRule
          {
            Kind = AttackKind.Projectile,
            DamagePattern = s.DamagePattern,
            Damage = s.AttackDamage,
            Range = s.AttackRange,
            SplashRadius = s.SplashRadius,
            CooldownTicks = Ticks(s.AttackCooldown),
            AffectedLayers = TargetLayer.GroundAndAir,
            OnHitStatus = new StatusOnHit
            {
              Kind = StatusKind.Slow,
              DurationTicks = Ticks(s.OnHitSlowDuration),
              Magnitude = s.OnHitSlowMagnitude,
              Radius = s.SplashRadius,
              AffectedLayers = TargetLayer.GroundAndAir
            }
          },
          SpawnEffects = new IEntityEffect[]
          {
            new AreaDamageEffect
            {
              Radius = s.SplashRadius, Damage = s.SpawnNovaDamage, AffectedLayers = TargetLayer.GroundAndAir
            },
            new AreaApplyStatusEffect
            {
              Radius = s.SplashRadius, Kind = StatusKind.Slow,
              DurationTicks = Ticks(s.SpawnNovaSlowDuration), Magnitude = s.SpawnNovaSlowMagnitude,
              AffectedLayers = TargetLayer.GroundAndAir
            }
          }
        },

        CardId.IceGolem => new ServerEntityDefinition
        {
          CardId = cardId,
          CardType = CardType.Troop,
          Stats = new EntityStats(s.MaxHP, s.MoveSpeed),
          FootprintRadius = s.FootprintRadius,
          CollisionRadius = s.CollisionRadius,
          PushWeight = s.PushWeight,
          Movement = MovementKind.Ground,
          Targeting = new TargetingRule { Category = TargetCategory.BuildingsOnly, Layers = TargetLayer.Ground },
          Attack = new AttackRule
          {
            Kind = AttackKind.Melee,
            DamagePattern = s.DamagePattern,
            Damage = s.AttackDamage,
            Range = s.AttackRange,
            CooldownTicks = Ticks(s.AttackCooldown),
            AffectedLayers = TargetLayer.Ground
          },
          DeathEffects = new IEntityEffect[]
          {
            new AreaDamageEffect
            {
              Radius = s.DeathNovaRadius, Damage = s.DeathNovaDamage, AffectedLayers = TargetLayer.Ground
            },
            new AreaApplyStatusEffect
            {
              Radius = s.DeathNovaRadius, Kind = StatusKind.Slow,
              DurationTicks = Ticks(s.DeathNovaSlowDuration), Magnitude = s.DeathNovaSlowMagnitude,
              AffectedLayers = TargetLayer.GroundAndAir
            }
          }
        },

        CardId.WallBreakers => new ServerEntityDefinition
        {
          CardId = cardId,
          CardType = CardType.Troop,
          Stats = new EntityStats(s.MaxHP, s.MoveSpeed),
          FootprintRadius = s.FootprintRadius,
          CollisionRadius = s.CollisionRadius,
          PushWeight = s.PushWeight,
          Movement = MovementKind.Ground,
          Targeting = new TargetingRule { Category = TargetCategory.BuildingsOnly, Layers = TargetLayer.Ground },
          Attack = new AttackRule
          {
            Kind = AttackKind.SelfDestruct,
            DamagePattern = DamagePattern.SingleTarget,
            Range = s.AttackRange,
            CooldownTicks = 0,
            AffectedLayers = TargetLayer.Ground
          },
          SelfDestructEffects = new IEntityEffect[]
          {
            new AreaDamageEffect
            {
              Radius = s.DeathNovaRadius, Damage = s.DeathNovaDamage, AffectedLayers = TargetLayer.Ground
            }
          }
        },

        CardId.Skeletons => new ServerEntityDefinition
        {
          CardId = cardId,
          CardType = CardType.Troop,
          Stats = new EntityStats(s.MaxHP, s.MoveSpeed),
          FootprintRadius = s.FootprintRadius,
          CollisionRadius = s.CollisionRadius,
          PushWeight = s.PushWeight,
          Movement = MovementKind.Ground,
          Targeting = new TargetingRule { Category = TargetCategory.AnyEnemy, Layers = TargetLayer.GroundAndAir },
          Attack = new AttackRule
          {
            Kind = AttackKind.Melee,
            DamagePattern = s.DamagePattern,
            Damage = s.AttackDamage,
            Range = s.AttackRange,
            CooldownTicks = Ticks(s.AttackCooldown),
            AffectedLayers = TargetLayer.Ground
          }
        },

        CardId.SpearGoblins => new ServerEntityDefinition
        {
          CardId = cardId,
          CardType = CardType.Troop,
          Stats = new EntityStats(s.MaxHP, s.MoveSpeed),
          FootprintRadius = s.FootprintRadius,
          CollisionRadius = s.CollisionRadius,
          PushWeight = s.PushWeight,
          Movement = MovementKind.Ground,
          Targeting = new TargetingRule { Category = TargetCategory.AnyEnemy, Layers = TargetLayer.GroundAndAir },
          Attack = new AttackRule
          {
            Kind = AttackKind.Projectile,
            DamagePattern = s.DamagePattern,
            Damage = s.AttackDamage,
            Range = s.AttackRange,
            CooldownTicks = Ticks(s.AttackCooldown),
            AffectedLayers = TargetLayer.GroundAndAir
          }
        },

        CardId.Minions => new ServerEntityDefinition
        {
          CardId = cardId,
          CardType = CardType.Troop,
          Stats = new EntityStats(s.MaxHP, s.MoveSpeed),
          FootprintRadius = s.FootprintRadius,
          CollisionRadius = s.CollisionRadius,
          PushWeight = s.PushWeight,
          Movement = MovementKind.Air,
          Targeting = new TargetingRule { Category = TargetCategory.AnyEnemy, Layers = TargetLayer.GroundAndAir },
          Attack = new AttackRule
          {
            Kind = AttackKind.Projectile,
            DamagePattern = s.DamagePattern,
            Damage = s.AttackDamage,
            Range = s.AttackRange,
            CooldownTicks = Ticks(s.AttackCooldown),
            AffectedLayers = TargetLayer.GroundAndAir
          }
        },

        CardId.SkeletonBarrel => new ServerEntityDefinition
        {
          CardId = cardId,
          CardType = CardType.Troop,
          Stats = new EntityStats(s.MaxHP, s.MoveSpeed),
          FootprintRadius = s.FootprintRadius,
          CollisionRadius = s.CollisionRadius,
          PushWeight = s.PushWeight,
          Movement = MovementKind.Air,
          Targeting = new TargetingRule { Category = TargetCategory.BuildingsOnly, Layers = TargetLayer.Ground },
          Attack = new AttackRule
          {
            Kind = AttackKind.SelfDestruct,
            DamagePattern = DamagePattern.SingleTarget,
            Range = s.AttackRange,
            CooldownTicks = 0,
            AffectedLayers = TargetLayer.Ground
          },
          DeathEffects = new IEntityEffect[]
          {
            new AreaDamageEffect
            {
              Radius = s.DeathNovaRadius, Damage = s.DeathNovaDamage, AffectedLayers = TargetLayer.Ground
            },
            new SpawnEntitiesEffect { EntityType = CardId.Skeletons, Count = 7, SpreadRadius = 1.2f }
          }
        },

        _ => throw new ArgumentException($"Unknown entity type: {cardId}")
      };
    }
  }
}
