using UnityEngine;

/// <summary>
/// INSTRUCTIONS:
/// 1. Create an empty GameObject in your scene (name it "EntityViewManager")
/// 2. Add this component to that GameObject
/// 3. Assign your EntityViewFactory to the viewFactory field in the Inspector
/// 4. This manager will now handle all entity view creation/updates from the server
/// </summary>
public class EntityViewManagerSetup : MonoBehaviour
{
  [Header("Setup Instructions")]
  [Tooltip("Drag your EntityViewFactory prefab/component here")]
  [SerializeField] private EntityViewFactory viewFactory;

  [Header("Prefab Lookup (Option 1: Resources)")]
  [Tooltip("Path in Resources folder, e.g., 'Entities' for Resources/Entities/knight.prefab")]
  [SerializeField] private string resourcesPath = "Entities";

  [Header("Prefab Lookup (Option 2: Direct References)")]
  [Tooltip("Manually assign prefabs for each entity type")]
  [SerializeField] private EntityTypePrefab[] entityPrefabs;

  void Awake()
  {
    // Verify setup
    if (viewFactory == null)
    {
      Debug.LogError("[EntityViewManager] viewFactory not assigned! Please assign it in the Inspector.");
    }

    if (entityPrefabs == null || entityPrefabs.Length == 0)
    {
      Debug.LogWarning("[EntityViewManager] No entity prefabs assigned. Will try to load from Resources.");
    }
  }

  /// <summary>
  /// Create a view for the given entity type.
  /// Implement this based on your project structure.
  /// </summary>
  public EntityView CreateViewForType(string entityType, Vector3 position, EntityTeam team)
  {
    EntityView prefab = null;

    // Option 1: Load from manually assigned prefabs
    if (entityPrefabs != null)
    {
      foreach (var mapping in entityPrefabs)
      {
        if (mapping.typeName == entityType)
        {
          prefab = mapping.prefab;
          break;
        }
      }
    }

    // Option 2: Load from Resources folder
    if (prefab == null && !string.IsNullOrEmpty(resourcesPath))
    {
      prefab = Resources.Load<EntityView>($"{resourcesPath}/{entityType}");
    }

    // Fallback: Try loading directly by name
    if (prefab == null)
    {
      prefab = Resources.Load<EntityView>(entityType);
    }

    if (prefab == null)
    {
      Debug.LogError($"[EntityViewManager] Could not find prefab for entity type: {entityType}");
      return null;
    }

    // Instantiate the view
    var view = Instantiate(prefab, position, Quaternion.identity);
    view.name = $"{entityType}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
    return view;
  }
}

[System.Serializable]
public class EntityTypePrefab
{
  public string typeName;     // e.g., "knight", "tower", "archer"
  public EntityView prefab;   // The prefab to instantiate
}
