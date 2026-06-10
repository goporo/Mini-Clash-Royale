namespace ClashShared
{
  public enum DamagePattern { SingleTarget, RadiusAroundSelf, RadiusAroundTarget }

  public readonly struct CardStats
  {
    // Combat
    public float MaxHP { get; init; }
    public float AttackDamage { get; init; }
    public float AttackRange { get; init; }
    public float AttackCooldown { get; init; }
    public float SplashRadius { get; init; }
    public DamagePattern DamagePattern { get; init; }

    // Movement / Physics
    public float MoveSpeed { get; init; }
    public float FootprintRadius { get; init; }
    public float CollisionRadius { get; init; }
    public float PushWeight { get; init; }

    // On-hit status (e.g. Ice Wizard attack)
    public float OnHitSlowDuration { get; init; }
    public float OnHitSlowMagnitude { get; init; }

    // Spawn nova (e.g. Ice Wizard landing)
    public float SpawnNovaDamage { get; init; }
    public float SpawnNovaSlowDuration { get; init; }
    public float SpawnNovaSlowMagnitude { get; init; }

    // Death nova (e.g. Ice Golem)
    public float DeathNovaRadius { get; init; }
    public float DeathNovaDamage { get; init; }
    public float DeathNovaSlowDuration { get; init; }
    public float DeathNovaSlowMagnitude { get; init; }
  }

  public static class CardStatsTable
  {
    // Stats at Level 11
    public static CardStats Get(CardId cardId) => cardId switch
    {
      CardId.PrincessTower => new CardStats
      {
        MaxHP = 3052,
        MoveSpeed = 0f,
        AttackDamage = 109f,
        CollisionRadius = 1.5f,
        PushWeight = 100f,
        FootprintRadius = 1.5f,
        AttackRange = 7.5f,
        AttackCooldown = 0.8f,
        DamagePattern = DamagePattern.SingleTarget,
      },

      CardId.KingTower => new CardStats
      {
        MaxHP = 4824,
        MoveSpeed = 0f,
        AttackDamage = 109f,
        CollisionRadius = 2f,
        PushWeight = 120f,
        FootprintRadius = 2f,
        AttackRange = 7.0f,
        AttackCooldown = 1.0f,
        DamagePattern = DamagePattern.SingleTarget,
      },

      CardId.Knight => new CardStats
      {
        MaxHP = 1766,
        MoveSpeed = 60f, // Trung bình (Medium)
        AttackDamage = 202f,
        CollisionRadius = 0.45f,
        PushWeight = 1.8f,
        AttackRange = 1.2f, // Cận chiến tầm trung (Melee Medium)
        AttackCooldown = 1.2f,
        DamagePattern = DamagePattern.SingleTarget,
      },

      CardId.Archer => new CardStats
      {
        MaxHP = 304,
        MoveSpeed = 60f, // Trung bình
        AttackDamage = 118f,
        CollisionRadius = 0.35f,
        PushWeight = 0.9f,
        AttackRange = 5.0f,
        AttackCooldown = 0.9f,
        DamagePattern = DamagePattern.SingleTarget,
      },

      CardId.Giant => new CardStats
      {
        MaxHP = 4091,
        MoveSpeed = 45f, // Chậm (Slow)
        AttackDamage = 254f,
        CollisionRadius = 0.7f,
        PushWeight = 10f,
        AttackRange = 1.2f,
        AttackCooldown = 1.5f,
        DamagePattern = DamagePattern.SingleTarget,
      },

      CardId.Cannon => new CardStats
      {
        MaxHP = 926,
        MoveSpeed = 0f,
        AttackDamage = 212f,
        CollisionRadius = 1.0f,
        PushWeight = 100f,
        FootprintRadius = 1.0f,
        AttackRange = 5.5f,
        AttackCooldown = 0.9f,
        DamagePattern = DamagePattern.SingleTarget,
      },

      CardId.Goblin => new CardStats
      {
        MaxHP = 202,
        MoveSpeed = 120f, // Rất nhanh
        AttackDamage = 120f,
        CollisionRadius = 0.3f,
        PushWeight = 0.6f,
        AttackRange = 0.5f,
        AttackCooldown = 1.1f,
        DamagePattern = DamagePattern.SingleTarget,
      },

      CardId.Musketeer => new CardStats
      {
        MaxHP = 721,
        MoveSpeed = 60f, // Trung bình
        AttackDamage = 217f,
        CollisionRadius = 0.4f,
        PushWeight = 1.2f,
        AttackRange = 6.0f,
        AttackCooldown = 1.0f,
        DamagePattern = DamagePattern.SingleTarget,
      },

      CardId.MiniPekka => new CardStats
      {
        MaxHP = 1361,
        MoveSpeed = 90f, // Nhanh (Fast)
        AttackDamage = 798f,
        CollisionRadius = 0.4f,
        PushWeight = 2.2f,
        AttackRange = 1.2f,
        AttackCooldown = 1.6f,
        DamagePattern = DamagePattern.SingleTarget,
      },

      CardId.Valkyrie => new CardStats
      {
        MaxHP = 1908,
        MoveSpeed = 60f, // Trung bình
        AttackDamage = 267f,
        CollisionRadius = 0.5f,
        PushWeight = 2.5f,
        AttackRange = 1.2f,
        AttackCooldown = 1.5f,
        SplashRadius = 1.5f,
        DamagePattern = DamagePattern.RadiusAroundSelf,
      },

      CardId.Bomber => new CardStats
      {
        MaxHP = 311,
        MoveSpeed = 60f, // Trung bình
        AttackDamage = 271f,
        CollisionRadius = 0.4f,
        PushWeight = 1.0f,
        AttackRange = 4.5f,
        AttackCooldown = 1.8f,
        SplashRadius = 1.5f,
        DamagePattern = DamagePattern.RadiusAroundTarget,
      },

      CardId.IceWizard => new CardStats
      {
        MaxHP = 720,
        MoveSpeed = 60f, // Trung bình
        AttackDamage = 91f,
        CollisionRadius = 0.4f,
        PushWeight = 1.2f,
        AttackRange = 5.5f,
        AttackCooldown = 1.7f,
        SplashRadius = 1.5f,
        DamagePattern = DamagePattern.RadiusAroundTarget,
        OnHitSlowDuration = 2.5f,
        OnHitSlowMagnitude = 0.35f,
        SpawnNovaDamage = 91f,
        SpawnNovaSlowDuration = 1.5f,
        SpawnNovaSlowMagnitude = 0.35f
      },

      CardId.WallBreakers => new CardStats
      {
        MaxHP = 331,
        MoveSpeed = 120f, // Rất nhanh
        AttackDamage = 0f, // Sát thương đòn đánh thường bằng 0 vì chúng tự sát nổ diện rộng
        CollisionRadius = 0.35f,
        PushWeight = 0.8f,
        AttackRange = 0.5f,
        AttackCooldown = 0f,
        DamagePattern = DamagePattern.SingleTarget,
        DeathNovaRadius = 2.0f,
        DeathNovaDamage = 392f // Sát thương phá công trình khi nổ cảm tử
      },

      CardId.IceGolem => new CardStats
      {
        MaxHP = 1197,
        MoveSpeed = 45f, // Chậm
        AttackDamage = 84f,
        CollisionRadius = 0.55f,
        PushWeight = 4f,
        AttackRange = 1.2f,
        AttackCooldown = 2.5f,
        DamagePattern = DamagePattern.SingleTarget,
        DeathNovaRadius = 2.5f,
        DeathNovaDamage = 84,
        DeathNovaSlowDuration = 3f,
        DeathNovaSlowMagnitude = 0.35f
      },

      CardId.Skeletons => new CardStats
      {
        MaxHP = 81f,
        MoveSpeed = 90f, // Nhanh
        AttackDamage = 81f,
        CollisionRadius = 0.25f,
        PushWeight = 0.3f,
        AttackRange = 0.5f,
        AttackCooldown = 1.0f,
        DamagePattern = DamagePattern.SingleTarget,
      },

      CardId.SpearGoblins => new CardStats
      {
        MaxHP = 133f,
        MoveSpeed = 120f, // Rất nhanh
        AttackDamage = 81f,
        CollisionRadius = 0.3f,
        PushWeight = 0.5f,
        AttackRange = 5.0f,
        AttackCooldown = 1.7f,
        DamagePattern = DamagePattern.SingleTarget,
      },

      CardId.Minions => new CardStats
      {
        MaxHP = 230f,
        MoveSpeed = 90f, // Nhanh
        AttackDamage = 102f,
        CollisionRadius = 0.3f,
        PushWeight = 0.4f,
        AttackRange = 1.6f, // Tầm đánh tầm xa dạng lính bay cận chiến ngắn
        AttackCooldown = 1.0f,
        DamagePattern = DamagePattern.SingleTarget,
      },

      CardId.SkeletonBarrel => new CardStats
      {
        MaxHP = 540f,
        MoveSpeed = 90f, // Trung bình
        AttackDamage = 0f,
        CollisionRadius = 0.45f,
        PushWeight = 1.5f,
        AttackRange = 0.5f,
        AttackCooldown = 0f,
        DamagePattern = DamagePattern.SingleTarget,
        DeathNovaRadius = 2.0f,
        DeathNovaDamage = 133f, // Sát thương khi rơi đất trúng mục tiêu bên dưới
      },

      _ => default
    };
  }
}
