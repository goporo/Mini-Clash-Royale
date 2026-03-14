using System.Collections.Generic;
using UnityEngine;
using ClashServer;
using ClashShared;

// Pure client-side entity view manager
public class EntityViewManager : MonoBehaviour
{
  public static EntityViewManager Instance { get; private set; }


  private class EntityViewData
  {
    public GameObject go;
    public Vector3 targetPosition;
    public float currentHP;
    public float maxHP;
    public CardId type;
    public EntityTeam team;
    public bool isBuilding;
    public float footprintRadius;
  }

  private static float GetFootprintRadius(CardId type) => type switch
  {
    CardId.PrincessTower => 1.5f,
    CardId.KingTower => 2.0f,
    CardId.Cannon => 1.5f,
    _ => 0f,
  };

  private static Color GetBaseColor(EntityViewData data)
  {
    if (data.isBuilding) return Color.yellow;
    return data.team == EntityTeam.Team1 ? Color.blue : Color.red;
  }

  private static void ApplyEntityColor(EntityViewData data)
  {
    Renderer r = data.go?.GetComponent<Renderer>();
    if (r == null) return;
    float hp = data.currentHP / data.maxHP;
    Color baseColor = GetBaseColor(data);
    r.material.color = hp < 0.5f ? Color.Lerp(Color.black, baseColor, hp + 0.5f) : baseColor;
  }

  public void SetSpellHighlight(Vector3 worldCenter, float spellRadius)
  {
    foreach (var kvp in entityViews)
    {
      EntityViewData data = kvp.Value;
      Renderer r = data.go?.GetComponent<Renderer>();
      if (r == null) continue;

      float dist = Vector2.Distance(
        new Vector2(worldCenter.x, worldCenter.z),
        new Vector2(data.targetPosition.x, data.targetPosition.z));

      if (dist <= spellRadius + data.footprintRadius)
        r.material.color = Color.white;
      else
        ApplyEntityColor(data);
    }
  }

  public void ClearSpellHighlight()
  {
    foreach (var kvp in entityViews)
      ApplyEntityColor(kvp.Value);
  }

  private Dictionary<int, EntityViewData> entityViews = new Dictionary<int, EntityViewData>();

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

  void Awake()
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

    foreach (var entityData in snapshot.Entities)
    {
      if (!entityData.IsAlive) continue;

      Vector2 pos = entityData.Position.ToUnityVector2();
      Vector3 worldPos = new(pos.x, 0, pos.y);

      GameObject prefab = GetPrefabForType(entityData.Type);
      GameObject go = Instantiate(prefab, worldPos, Quaternion.identity);
      go.name = $"Entity_{entityData.Id}_{entityData.Type}";

      entityViews[entityData.Id] = new EntityViewData
      {
        go = go,
        targetPosition = worldPos,
        currentHP = entityData.CurrentHP,
        maxHP = entityData.MaxHP,
        type = entityData.Type,
        team = entityData.Team,
        isBuilding = entityData.IsBuilding,
        footprintRadius = GetFootprintRadius(entityData.Type)
      };

      EntityView entityView = go.GetComponent<EntityView>();
      if (entityView != null)
      {
        entityView.SetEntityId(entityData.Id);
        entityView.SetTargetPosition(worldPos);
      }

      UpdateHealthBar(entityViews[entityData.Id]);
    }
  }

  public void ApplyDeltaSnapshot(DeltaSnapshot delta)
  {
    // Spawn new entities
    foreach (var entityData in delta.SpawnedEntities)
    {
      if (entityViews.ContainsKey(entityData.Id))
        continue;

      Vector2 pos = entityData.Position.ToUnityVector2();
      Vector3 worldPos = new(pos.x, 0, pos.y);

      GameObject prefab = GetPrefabForType(entityData.Type);
      GameObject go = Instantiate(prefab, worldPos, Quaternion.identity);
      go.name = $"Entity_{entityData.Id}_{entityData.Type}";

      entityViews[entityData.Id] = new EntityViewData
      {
        go = go,
        targetPosition = worldPos,
        currentHP = entityData.CurrentHP,
        maxHP = entityData.MaxHP,
        type = entityData.Type,
        team = entityData.Team,
        isBuilding = entityData.IsBuilding,
        footprintRadius = GetFootprintRadius(entityData.Type)
      };

      EntityView entityView = go.GetComponent<EntityView>();
      if (entityView != null)
      {
        entityView.SetEntityId(entityData.Id);
        entityView.SetTargetPosition(worldPos);
      }

      UpdateHealthBar(entityViews[entityData.Id]);
    }

    foreach (var entityId in delta.DestroyedEntityIds)
    {
      RemoveEntity(entityId);
    }

    foreach (var entityData in delta.UpdatedEntities)
    {
      if (!entityViews.ContainsKey(entityData.Id))
        continue;

      EntityViewData data = entityViews[entityData.Id];
      UnityEngine.Vector2 pos = entityData.Position.ToUnityVector2();
      data.targetPosition = new Vector3(pos.x, 0, pos.y);
      data.currentHP = entityData.CurrentHP;

      EntityView entityView = data.go.GetComponent<EntityView>();
      if (entityView != null)
      {
        entityView.SetTargetPosition(data.targetPosition);
      }

      UpdateHealthBar(data);
    }
  }

  public void RemoveEntity(int entityId)
  {
    if (entityViews.ContainsKey(entityId))
    {
      Destroy(entityViews[entityId].go);
      entityViews.Remove(entityId);
    }
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
    if (data.go != null)
    {
      EntityView entityView = data.go.GetComponent<EntityView>();
      if (entityView != null)
        entityView.SetHealth((int)data.currentHP, (int)data.maxHP);

      ApplyEntityColor(data);
    }
  }

  public void ClearAllEntities()
  {
    foreach (var kvp in entityViews)
    {
      if (kvp.Value.go != null)
        Destroy(kvp.Value.go);
    }
    entityViews.Clear();
  }
}
