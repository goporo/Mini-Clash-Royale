
using System.Threading.Tasks;
using UnityEngine;
using Colyseus;

public class NetworkClient : MonoBehaviour
{
  public static NetworkClient Instance { get; private set; }

  [Header("Server")]
  public string serverAddress = "ws://localhost:2567"; // default Colyseus port
  public string roomName = "my_room";                  // must match app.config.ts

  private ColyseusClient _client;
  public ColyseusRoom<Colyseus.Schema.Schema> Room { get; private set; }

  private async void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);

    _client = new ColyseusClient(serverAddress);

    try
    {
      Room = await _client.JoinOrCreate<Colyseus.Schema.Schema>(roomName);
      Debug.Log($"[Network] Connected to room: {Room.RoomId}");
      Room.OnMessage<object>("entity-update", OnEntityUpdate);
    }
    catch (System.Exception e)
    {
      Debug.LogError($"[Network] Failed to connect: {e.Message}");
    }
  }

  private float lastLogTime = 0f;
  private void OnEntityUpdate(object message)
  {
    // Try to parse the message as a list of dictionaries
    var entityList = message as System.Collections.IList;
    if (entityList == null)
    {
      Debug.LogWarning($"[Network] entity-update: message is not a list");
      return;
    }

    // Limit log to once per second
    if (Time.time - lastLogTime > 1f)
    {
      Debug.Log($"[Network] entity-update: received {entityList.Count} entities");
      lastLogTime = Time.time;
    }

    if (EntityViewManager.Instance != null)
    {
      EntityViewManager.Instance.UpdateEntities(entityList);
    }
  }

  private void OnApplicationQuit()
  {
    if (Room != null)
    {
      Room.Leave();
    }
  }

  private void OnDestroy()
  {
    if (Room != null)
    {
      Room.Leave();
    }
  }
}
