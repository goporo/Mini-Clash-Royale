using System.Collections.Generic;
using UnityEngine;
using ClashServer;

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
    public string type;
    public EntityTeam team;
    public bool isBuilding;
  }

  private Dictionary<int, EntityViewData> entityViews = new Dictionary<int, EntityViewData>();

  [Header("Visual Prefabs")]
  public GameObject knightPrefab;
  public GameObject archerPrefab;
  public GameObject giantPrefab;
  public GameObject towerPrefab;
  public GameObject kingTowerPrefab;

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

      UnityEngine.Vector2 pos = entityData.Position.ToUnityVector2();
      Vector3 worldPos = new Vector3(pos.x, 0, pos.y);

      GameObject prefab = GetPrefabForType(entityData.Type);
      GameObject go = Instantiate(prefab, worldPos, Quaternion.identity);
      go.name = $"Entity_{entityData.Id}_{entityData.Type}";

      // Color by team
      Renderer renderer = go.GetComponent<Renderer>();
      if (renderer != null)
      {
        if (entityData.IsBuilding)
          renderer.material.color = Color.yellow;
        else
          renderer.material.color = entityData.Team == EntityTeam.Team1 ? Color.blue : Color.red;
      }

      entityViews[entityData.Id] = new EntityViewData
      {
        go = go,
        targetPosition = worldPos,
        currentHP = entityData.CurrentHP,
        maxHP = entityData.MaxHP,
        type = entityData.Type,
        team = entityData.Team,
        isBuilding = entityData.IsBuilding
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

      UnityEngine.Vector2 pos = entityData.Position.ToUnityVector2();
      Vector3 worldPos = new Vector3(pos.x, 0, pos.y);

      GameObject prefab = GetPrefabForType(entityData.Type);
      GameObject go = Instantiate(prefab, worldPos, Quaternion.identity);
      go.name = $"Entity_{entityData.Id}_{entityData.Type}";

      // Color by team
      Renderer renderer = go.GetComponent<Renderer>();
      if (renderer != null)
      {
        if (entityData.IsBuilding)
          renderer.material.color = Color.yellow;
        else
          renderer.material.color = entityData.Team == EntityTeam.Team1 ? Color.blue : Color.red;
      }

      entityViews[entityData.Id] = new EntityViewData
      {
        go = go,
        targetPosition = worldPos,
        currentHP = entityData.CurrentHP,
        maxHP = entityData.MaxHP,
        type = entityData.Type,
        team = entityData.Team,
        isBuilding = entityData.IsBuilding
      };

      EntityView entityView = go.GetComponent<EntityView>();
      if (entityView != null)
      {
        entityView.SetEntityId(entityData.Id);
        entityView.SetTargetPosition(worldPos);
      }

      UpdateHealthBar(entityViews[entityData.Id]);
      Debug.Log($"[Client] Spawned {entityData.Type} (id={entityData.Id}) from delta");
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
      Debug.Log($"[EntityView] Removed entity {entityId}");
    }
  }

  public GameObject GetPrefabForType(string type)
  {
    switch (type.ToLower())
    {
      case "knight":
        return knightPrefab != null ? knightPrefab : GetDefaultCube();
      case "archer":
        return archerPrefab != null ? archerPrefab : GetDefaultCube();
      case "giant":
        return giantPrefab != null ? giantPrefab : GetDefaultCube();
      case "tower":
        return towerPrefab != null ? towerPrefab : GetDefaultCube();
      case "kingtower":
        return kingTowerPrefab != null ? kingTowerPrefab : GetDefaultCube();
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
    float hpPercent = data.currentHP / data.maxHP;

    if (data.go != null)
    {
      EntityView entityView = data.go.GetComponent<EntityView>();
      if (entityView != null)
      {
        entityView.SetHealth((int)data.currentHP, (int)data.maxHP);
      }

      Renderer renderer = data.go.GetComponent<Renderer>();
      if (renderer != null && hpPercent < 0.5f)
      {
        Color baseColor = data.team == EntityTeam.Team1 ? Color.blue : Color.red;
        if (data.isBuilding)
          baseColor = Color.yellow;

        renderer.material.color = Color.Lerp(Color.black, baseColor, hpPercent + 0.5f);
      }
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
