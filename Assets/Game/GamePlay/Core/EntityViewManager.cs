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
    public ClashServer.EntityTeam team;
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

  /// <summary>
  /// Called from MatchController.RpcSpawnUnit
  /// </summary>
  public void SpawnEntity(int entityId, int cardId, Vector2 position, ClashServer.EntityTeam team, string type, bool isBuilding = false)
  {
    if (entityViews.ContainsKey(entityId))
    {
      Debug.LogWarning($"[EntityView] Entity {entityId} already exists, skipping spawn");
      return;
    }

    Vector3 worldPos = new Vector3(position.x, 0, position.y);
    GameObject prefab = GetPrefabForType(type);
    GameObject go = Instantiate(prefab, worldPos, Quaternion.identity);
    go.name = $"Entity_{entityId}_{type}";

    // Color by team
    Renderer renderer = go.GetComponent<Renderer>();
    if (renderer != null)
    {
      if (isBuilding)
        renderer.material.color = Color.yellow;
      else
        renderer.material.color = team == ClashServer.EntityTeam.Team1 ? Color.blue : Color.red;
    }

    entityViews[entityId] = new EntityViewData
    {
      go = go,
      targetPosition = worldPos,
      currentHP = 100, // Will be updated by RpcUpdateEntity
      maxHP = 100,
      type = type,
      team = team,
      isBuilding = isBuilding
    };

    Debug.Log($"[EntityView] Spawned {type} (id={entityId}) at {position}");
  }

  /// <summary>
  /// Apply batched snapshot - called from MatchController.RpcApplySnapshot
  /// </summary>
  public void ApplySnapshot(EntitySnapshot[] snapshots)
  {
    foreach (var snapshot in snapshots)
    {
      if (!snapshot.isAlive)
      {
        RemoveEntity(snapshot.id);
        continue;
      }

      if (!entityViews.ContainsKey(snapshot.id))
      {
        // Entity exists on server but not spawned on client yet
        // This can happen if spawn RPC arrives after first snapshot
        continue;
      }

      EntityViewData data = entityViews[snapshot.id];
      data.targetPosition = snapshot.ToWorldPosition();
      data.currentHP = snapshot.GetNormalizedHP() * data.maxHP;

      // Update HP bar or visual feedback here
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

  void Update()
  {
    // Smoothly interpolate entities to their target positions
    foreach (var kvp in entityViews)
    {
      var data = kvp.Value;
      if (data.go != null)
      {
        data.go.transform.position = Vector3.Lerp(
          data.go.transform.position,
          data.targetPosition,
          Time.deltaTime * 10f
        );
      }
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

    // Create a default cube
    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
    cube.transform.localScale = Vector3.one;
    return cube;
  }

  private string GetTypeFromCardId(int cardId)
  {
    switch (cardId)
    {
      case 0: return "knight";
      case 1: return "archer";
      case 2: return "giant";
      default: return "knight";
    }
  }

  private void UpdateHealthBar(EntityViewData data)
  {
    // TODO: Implement health bar visualization
    // You can add a UI canvas above the entity or change material color based on HP
    float hpPercent = data.currentHP / data.maxHP;

    if (data.go != null)
    {
      Renderer renderer = data.go.GetComponent<Renderer>();
      if (renderer != null && hpPercent < 0.5f)
      {
        // Darken color when HP is low
        Color baseColor = data.team == ClashServer.EntityTeam.Team1 ? Color.blue : Color.red;
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
