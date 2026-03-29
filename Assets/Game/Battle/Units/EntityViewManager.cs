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

  [Header("Visual Prefabs")]
  public GameObject princessTowerPrefab;
  public GameObject kingTowerPrefab;
  public GameObject knightPrefab;
  public GameObject archerPrefab;
  public GameObject giantPrefab;
  public GameObject cannonPrefab;
  public GameObject goblinPrefab;
  public GameObject fireballPrefab;
  public GameObject musketeerPrefab;
  public GameObject miniPekkaPrefab;

  [Header("Fallback Visual")]
  public GameObject cubePrefab;

  private static float GetFootprintRadius(CardId type) => type switch
  {
    CardId.PrincessTower => 1.5f,
    CardId.KingTower => 2.0f,
    CardId.Cannon => 1.5f,
    _ => 0f,
  };

  private static float GetAttackRange(CardId type) => type switch
  {
    CardId.PrincessTower => 8f,
    CardId.KingTower => 8f,
    CardId.Knight => 1.5f,
    CardId.Archer => 5f,
    CardId.Giant => 1.5f,
    CardId.Cannon => 8f,
    CardId.Goblin => 1f,
    CardId.Musketeer => 4f,
    CardId.MiniPekka => 1.5f,
    _ => 0f,
  };

  private static float GetAttackCooldown(CardId type) => type switch
  {
    CardId.PrincessTower => 1f,
    CardId.KingTower => 1.2f,
    CardId.Knight => 1f,
    CardId.Archer => 1f,
    CardId.Giant => 1f,
    CardId.Cannon => 1.5f,
    CardId.Goblin => 0.8f,
    CardId.Musketeer => 1f,
    CardId.MiniPekka => 0.5f,
    _ => 0.75f,
  };

  private static bool IsRanged(CardId type) => type switch
  {
    CardId.Archer => true,
    CardId.Musketeer => true,
    CardId.Cannon => true,
    CardId.PrincessTower => true,
    CardId.KingTower => true,
    _ => false,
  };

  private static Color GetBaseColor(EntityViewData data)
  {
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
      data.targetPosition = new Vector3(pos.x, 0f, pos.y);

      if (data.currentHP - entityData.CurrentHP > 0.05f)
        damageCues.Add(new DamageCue(entityData.Id));

      data.currentHP = entityData.CurrentHP;
      data.maxHP = entityData.MaxHP;
      data.targetId = entityData.TargetId;

      if (data.view != null)
        data.view.SetTargetPosition(data.targetPosition);

      UpdateHealthBar(data);
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
    switch (type)
    {
      case CardId.Knight:
        return knightPrefab;
      case CardId.Archer:
        return archerPrefab;
      case CardId.Giant:
        return giantPrefab;
      case CardId.Cannon:
        return cannonPrefab;
      case CardId.PrincessTower:
        return princessTowerPrefab;
      case CardId.KingTower:
        return kingTowerPrefab;
      case CardId.Goblin:
        return goblinPrefab;
      case CardId.Fireball:
        return fireballPrefab;
      case CardId.Musketeer:
        return musketeerPrefab;
      case CardId.MiniPekka:
        return miniPekkaPrefab;
      default:
        return GetDefaultCube();
    }
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
    Vector3 worldPos = new(pos.x, 0f, pos.y);

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
      footprintRadius = GetFootprintRadius(entityData.Type),
      targetId = entityData.TargetId,
      lastAttackVisualTime = -999f
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

    bool ranged = IsRanged(attacker.type);
    attacker.lastAttackVisualTime = Time.time;
    attacker.view.PlayAttack(target.targetPosition, !ranged);

    if (ranged)
    {
      CombatFx.PlayProjectile(
        GetProjectileSpawnPosition(attacker),
        GetProjectileTargetPosition(target),
        GetProjectileColor(attacker),
        attacker.type == CardId.Cannon ? 0.22f : 0.14f,
        attacker.type == CardId.Cannon ? 0.3f : 0.18f);
    }
  }

  private bool IsAttackerInVisualRange(EntityViewData attacker, EntityViewData target)
  {
    float range = GetAttackRange(attacker.type) + target.footprintRadius + 0.75f;
    Vector2 attackerPos = new(attacker.targetPosition.x, attacker.targetPosition.z);
    Vector2 targetPos = new(target.targetPosition.x, target.targetPosition.z);
    return (attackerPos - targetPos).sqrMagnitude <= range * range;
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

      float visualCooldown = GetAttackCooldown(attacker.type) * 0.6f;
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
