using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all entity views on the client side.
/// Receives state updates from the server and updates visual representations.
/// </summary>
public class EntityViewManager : MonoBehaviour
{
  public static EntityViewManager Instance { get; private set; }

  private Dictionary<int, GameObject> entityViews = new Dictionary<int, GameObject>();
  private Dictionary<int, Vector3> targetPositions = new Dictionary<int, Vector3>();

  [Header("Cube Visuals")]
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

  public void UpdateEntities(System.Collections.IList entityList)
  {
    HashSet<int> seenIds = new HashSet<int>();
    foreach (var obj in entityList)
    {
      var entity = obj as System.Collections.IDictionary;
      if (entity == null) continue;
      int id = Convert.ToInt32(entity["id"]);
      float x = Convert.ToSingle(entity["x"]);
      float y = Convert.ToSingle(entity["y"]);
      string type = entity["type"].ToString();
      int team = Convert.ToInt32(entity["team"]);

      seenIds.Add(id);
      Vector3 pos = new Vector3(x, 0, y);
      targetPositions[id] = pos;

      if (!entityViews.ContainsKey(id))
      {
        var go = Instantiate(cubePrefab != null ? cubePrefab : GameObject.CreatePrimitive(PrimitiveType.Cube), pos, Quaternion.identity);
        go.name = $"Entity_{id}_{type}";
        go.GetComponent<Renderer>().material.color = team == 0 ? Color.blue : Color.red;
        entityViews[id] = go;
      }
    }
    // Remove entities not in the update
    var toRemove = new List<int>();
    foreach (var id in entityViews.Keys)
    {
      if (!seenIds.Contains(id))
      {
        Destroy(entityViews[id]);
        toRemove.Add(id);
      }
    }
    foreach (var id in toRemove)
    {
      entityViews.Remove(id);
      targetPositions.Remove(id);
    }
  }

  void Update()
  {
    // Smoothly move cubes to their target positions
    foreach (var kvp in entityViews)
    {
      int id = kvp.Key;
      GameObject go = kvp.Value;
      if (targetPositions.TryGetValue(id, out var target))
      {
        go.transform.position = Vector3.Lerp(go.transform.position, target, Time.deltaTime * 10f);
      }
    }
  }
}
