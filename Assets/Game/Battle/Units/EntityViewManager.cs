using System.Collections.Generic;
using ClashShared;
using UnityEngine;

// Pure client-side entity view manager
public class EntityViewManager : MonoBehaviour
{
  public static EntityViewManager Instance { get; private set; }

  private sealed class EntityViewData
  {
    public GameObject go;
    public EntityView view;
    public Vector3 targetPosition;
    public float currentHP;
    public float maxHP;
    public CardId type;
    public EntityTeam team;
    public bool isBuilding;
    public float footprintRadius;
    public int targetId;
    public float lastAttackVisualTime;
    public bool isSlowed;
  }

  private readonly struct DamageCue
  {
    public readonly int targetId;

    public DamageCue(int targetId)
    {
      this.targetId = targetId;
    }
  }

  private readonly Dictionary<int, EntityViewData> entityViews = new();

  [Header("Card Library")]
  public CardLibrary cardLibrary;

  [Header("Fallback Visual")]
  public GameObject cubePrefab;

  private static CardStats Stats(CardId type) => CardStatsTable.Get(type);

  // A decaying building bleeds MaxHP/lifetime per tick. We suppress the take-damage flash for
  // those routine ticks, but a real hit drops far more HP — so only treat the drop as decay when
  // it stays under a small multiple of the expected per-tick loss. That keeps real hits flashing
  // even while the building is decaying.
  private static bool IsDecayTick(EntitySnapshot entityData, float hpDrop)
  {
    if (!entityData.IsDecaying)
      return false;

    float lifetimeSeconds = Stats(entityData.Type).LifetimeSeconds;
    if (lifetimeSeconds <= 0f)
      return false;

    // Expected decay per server tick, with headroom for a few batched ticks of latency.
    float lifetimeTicks = lifetimeSeconds / ClashServer.ServerTickSettings.FixedDeltaTime;
    float decayPerTick = entityData.MaxHP / lifetimeTicks;
    return hpDrop <= decayPerTick * 4f;
  }

  private static Color GetBaseColor(EntityViewData data)
  {
    if (data.isSlowed)
      return Color.cyan;
    return data.team == EntityTeam.Team1
      ? new Color(0.55f, 0.78f, 1f)
      : new Color(1f, 0.62f, 0.62f);
  }

  private static Color GetProjectileColor(EntityViewData attacker)
  {
    Color baseColor = attacker.team == EntityTeam.Team1
      ? new Color(0.7f, 0.9f, 1f)
      : new Color(1f, 0.78f, 0.55f);

    if (attacker.type == CardId.Cannon)
      return new Color(1f, 0.82f, 0.35f);

    return baseColor;
  }

  private static Vector3 GetProjectileSpawnPosition(EntityViewData attacker)
  {
    if (attacker.go == null)
      return attacker.targetPosition + Vector3.up * 0.75f;

    float height = attacker.isBuilding ? 1.2f : 0.85f;
    return attacker.go.transform.position + Vector3.up * height;
  }

  private static Vector3 GetProjectileTargetPosition(EntityViewData target)
  {
    if (target.go == null)
      return target.targetPosition + Vector3.up * 0.6f;

    return target.go.transform.position + Vector3.up * 0.55f;
  }

  private static void ApplyEntityColor(EntityViewData data)
  {
    if (data.view != null)
      data.view.SetColor(GetBaseColor(data));
  }

  private void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }

    Instance = this;
  }

  public void ApplyFullSnapshot(FullSnapshot snapshot)
  {
    Debug.Log($"[Client] Applying full snapshot: {snapshot.Entities.Count} entities");

    ClearAllEntities();

    foreach (EntitySnapshot entityData in snapshot.Entities)
    {
      if (!entityData.IsAlive)
        continue;

      CreateEntity(entityData);
    }

    RefreshCombatTargets();
  }

  public void ApplyDeltaSnapshot(DeltaSnapshot delta)
  {
    List<DamageCue> damageCues = new();

    foreach (EntitySnapshot entityData in delta.SpawnedEntities)
    {
      if (entityViews.ContainsKey(entityData.Id))
        continue;

      CreateEntity(entityData);
    }

    foreach (EntitySnapshot entityData in delta.UpdatedEntities)
    {
      if (!entityViews.TryGetValue(entityData.Id, out EntityViewData data))
        continue;

      Vector2 pos = entityData.Position.ToUnityVector2();
      data.targetPosition = LocalPlayerContext.ToVisual(new Vector3(pos.x, 0f, pos.y));

      float hpDrop = data.currentHP - entityData.CurrentHP;
      if (hpDrop > 0.05f && !IsDecayTick(entityData, hpDrop))
        damageCues.Add(new DamageCue(entityData.Id));

      data.currentHP = entityData.CurrentHP;
      data.maxHP = entityData.MaxHP;
      data.targetId = entityData.TargetId;

      bool slowChanged = data.isSlowed != entityData.IsSlowed;
      data.isSlowed = entityData.IsSlowed;

      if (data.view != null)
        data.view.SetTargetPosition(data.targetPosition);

      UpdateHealthBar(data);

      if (slowChanged)
        ApplyEntityColor(data);
    }

    RefreshCombatTargets();

    foreach (DamageCue damageCue in damageCues)
      PlayDamageCue(damageCue.targetId);

    foreach (int entityId in delta.DestroyedEntityIds)
    {
      PlayDamageCue(entityId);
      RemoveEntity(entityId);
    }

    RefreshCombatTargets();
  }

  public void SetSpellHighlight(Vector3 worldCenter, float spellRadius)
  {
    foreach (KeyValuePair<int, EntityViewData> kvp in entityViews)
    {
      EntityViewData data = kvp.Value;
      float dist = Vector2.Distance(
        new Vector2(worldCenter.x, worldCenter.z),
        new Vector2(data.targetPosition.x, data.targetPosition.z));

      if (data.view != null)
      {
        data.view.SetColor(dist <= spellRadius + data.footprintRadius
          ? Color.white
          : GetBaseColor(data));
      }
    }
  }

  public void ClearSpellHighlight()
  {
    foreach (KeyValuePair<int, EntityViewData> kvp in entityViews)
      ApplyEntityColor(kvp.Value);
  }

  public void PlaySpellCast(CardId cardId, Vector3 worldPosition, EntityTeam team)
  {
    switch (cardId)
    {
      case CardId.Fireball:
        CombatFx.PlayFireball(worldPosition, team);
        break;
    }
  }

  public void RemoveEntity(int entityId)
  {
    if (!entityViews.TryGetValue(entityId, out EntityViewData data))
      return;

    entityViews.Remove(entityId);

    float novaRadius = Stats(data.type).DeathNovaRadius;
    if (novaRadius > 0f)
      DrawDebugNovaCircle(data.targetPosition, novaRadius);

    if (data.go == null)
      return;

    if (data.view != null)
    {
      data.view.PlayDeath(() =>
      {
        if (data.go != null)
          Destroy(data.go);
      });
      return;
    }

    Destroy(data.go);
  }

  public GameObject GetPrefabForType(CardId type)
  {
    GameObject prefab = cardLibrary?.Get(type)?.EntityPrefab;
    return prefab != null ? prefab : GetDefaultCube();
  }

  public void ClearAllEntities()
  {
    foreach (KeyValuePair<int, EntityViewData> kvp in entityViews)
    {
      if (kvp.Value.go != null)
        Destroy(kvp.Value.go);
    }

    entityViews.Clear();
  }

  private void CreateEntity(EntitySnapshot entityData)
  {
    Vector2 pos = entityData.Position.ToUnityVector2();
    Vector3 worldPos = LocalPlayerContext.ToVisual(new Vector3(pos.x, 0f, pos.y));

    GameObject prefab = GetPrefabForType(entityData.Type);
    GameObject go = Instantiate(prefab, worldPos, Quaternion.identity);
    go.name = $"Entity_{entityData.Id}_{entityData.Type}";

    EntityView view = go.GetComponent<EntityView>();
    if (view != null)
    {
      view.SetEntityId(entityData.Id);
      view.SetTargetPosition(worldPos);
    }

    EntityViewData data = new()
    {
      go = go,
      view = view,
      targetPosition = worldPos,
      currentHP = entityData.CurrentHP,
      maxHP = entityData.MaxHP,
      type = entityData.Type,
      team = entityData.Team,
      isBuilding = entityData.IsBuilding,
      footprintRadius = Stats(entityData.Type).FootprintRadius,
      targetId = entityData.TargetId,
      lastAttackVisualTime = -999f,
      isSlowed = entityData.IsSlowed
    };

    entityViews[entityData.Id] = data;
    UpdateHealthBar(data);
  }

  private GameObject GetDefaultCube()
  {
    if (cubePrefab != null)
      return cubePrefab;

    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
    cube.transform.localScale = Vector3.one;
    return cube;
  }

  private void UpdateHealthBar(EntityViewData data)
  {
    if (data.view != null)
      data.view.SetHealth((int)data.currentHP, (int)data.maxHP);

    ApplyEntityColor(data);
  }

  private void RefreshCombatTargets()
  {
    foreach (KeyValuePair<int, EntityViewData> kvp in entityViews)
    {
      EntityViewData attacker = kvp.Value;
      if (attacker.view == null)
        continue;

      if (attacker.isBuilding)
      {
        attacker.view.ClearCombatTarget();
      }
      else if (attacker.targetId >= 0 && entityViews.TryGetValue(attacker.targetId, out EntityViewData target))
        attacker.view.SetCombatTarget(target.targetPosition);
      else
        attacker.view.ClearCombatTarget();
    }
  }

  private void PlayDamageCue(int targetId)
  {
    if (!entityViews.TryGetValue(targetId, out EntityViewData target))
      return;

    if (target.view != null)
      target.view.PlayHitFlash();

    if (!TryGetBestAttackCueAttacker(targetId, target, out EntityViewData attacker))
      return;

    CardStats attackerStats = Stats(attacker.type);
    attacker.lastAttackVisualTime = Time.time;

    bool isMelee = attackerStats.AttackRange <= 1.5f;
    if (attackerStats.DamagePattern == DamagePattern.RadiusAroundSelf)
      attacker.view.PlaySpinAttack(attackerStats.SplashRadius);
    else
      attacker.view.PlayAttack(target.targetPosition, isMelee);

    if (!isMelee)
    {
      bool largeProjectile = attackerStats.AttackRange > 5f;
      CombatFx.PlayProjectile(
        GetProjectileSpawnPosition(attacker),
        GetProjectileTargetPosition(target),
        GetProjectileColor(attacker),
        largeProjectile ? 0.22f : 0.14f,
        largeProjectile ? 0.30f : 0.18f);
    }
  }

  private bool IsAttackerInVisualRange(EntityViewData attacker, EntityViewData target)
  {
    float range = Stats(attacker.type).AttackRange + target.footprintRadius + 0.75f;
    Vector2 attackerPos = new(attacker.targetPosition.x, attacker.targetPosition.z);
    Vector2 targetPos = new(target.targetPosition.x, target.targetPosition.z);
    return (attackerPos - targetPos).sqrMagnitude <= range * range;
  }

  private static void DrawDebugNovaCircle(Vector3 center, float radius)
  {
    const int segments = 24;
    const float duration = 0.8f;
    float step = 2f * UnityEngine.Mathf.PI / segments;
    for (int i = 0; i < segments; i++)
    {
      float a0 = i * step;
      float a1 = (i + 1) * step;
      Vector3 p0 = center + new Vector3(UnityEngine.Mathf.Cos(a0) * radius, 0.02f, UnityEngine.Mathf.Sin(a0) * radius);
      Vector3 p1 = center + new Vector3(UnityEngine.Mathf.Cos(a1) * radius, 0.02f, UnityEngine.Mathf.Sin(a1) * radius);
      Debug.DrawLine(p0, p1, Color.white, duration);
    }
  }

  private bool TryGetBestAttackCueAttacker(int targetId, EntityViewData target, out EntityViewData bestAttacker)
  {
    bestAttacker = null;
    float bestScore = float.MinValue;
    float now = Time.time;

    foreach (KeyValuePair<int, EntityViewData> kvp in entityViews)
    {
      EntityViewData attacker = kvp.Value;
      if (attacker.targetId != targetId || attacker.team == target.team || attacker.view == null)
        continue;

      if (!IsAttackerInVisualRange(attacker, target))
        continue;

      float visualCooldown = Stats(attacker.type).AttackCooldown * 0.6f;
      float timeSinceLastAttackCue = now - attacker.lastAttackVisualTime;
      if (timeSinceLastAttackCue < visualCooldown)
        continue;

      Vector2 attackerPos = new(attacker.targetPosition.x, attacker.targetPosition.z);
      Vector2 targetPos = new(target.targetPosition.x, target.targetPosition.z);
      float distanceScore = -(attackerPos - targetPos).sqrMagnitude;
      float readinessScore = Mathf.Min(timeSinceLastAttackCue, visualCooldown * 2f);
      float score = readinessScore * 10f + distanceScore;

      if (score > bestScore)
      {
        bestScore = score;
        bestAttacker = attacker;
      }
    }

    return bestAttacker != null;
  }
}
