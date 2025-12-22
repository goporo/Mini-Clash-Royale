using Mirror;
using UnityEngine;

public class MyNetworkManager : NetworkManager
{
  // Session manager shared between server and client
  public override void OnServerConnect(NetworkConnectionToClient conn)
  {
    Debug.Log($"[Server] Player {conn.connectionId} connected");
    base.OnServerConnect(conn);
  }

  public override void OnServerDisconnect(NetworkConnectionToClient conn)
  {
    Debug.Log($"[Server] Player {conn.connectionId} disconnected");
    base.OnServerDisconnect(conn);
  }

  public override void OnClientConnect()
  {
    Debug.Log("[Client] Connected to server");
    base.OnClientConnect();
  }

  public override void OnClientDisconnect()
  {
    Debug.Log("[Client] Disconnected from server");
    base.OnClientDisconnect();
  }


}
