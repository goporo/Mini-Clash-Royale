using Mirror;
using UnityEngine;
using ClashServer;

public class MyNetworkManager : NetworkManager
{
  public override void OnStartServer()
  {
    base.OnStartServer();

    NetworkServer.RegisterHandler<PlayCardMessage>(OnPlayCardMessage);
    NetworkServer.RegisterHandler<ClientReadyMessage>(OnClientReadyMessage);

    Debug.Log("[Server] Message handlers registered");
  }

  public override void OnStartClient()
  {
    base.OnStartClient();

    NetworkClient.RegisterHandler<FullSnapshotMessage>(OnFullSnapshotMessage);
    NetworkClient.RegisterHandler<DeltaSnapshotMessage>(OnDeltaSnapshotMessage);
    NetworkClient.RegisterHandler<PlayCardFailedMessage>(OnPlayCardFailedMessage);
    NetworkClient.RegisterHandler<MatchEndedMessage>(OnMatchEndedMessage);

    Debug.Log("[Client] Message handlers registered");
  }

  public override void OnStopServer()
  {
    NetworkServer.UnregisterHandler<PlayCardMessage>();
    NetworkServer.UnregisterHandler<ClientReadyMessage>();
    base.OnStopServer();
  }

  void OnPlayCardMessage(NetworkConnectionToClient conn, PlayCardMessage msg)
  {
    if (ServerMatchController.Instance != null)
    {
      System.Numerics.Vector2 position = msg.Position.ToVector2();
      ServerMatchController.Instance.Server_PlayCard(conn, msg.CardId, msg.Type, position);
    }
  }

  void OnClientReadyMessage(NetworkConnectionToClient conn, ClientReadyMessage msg)
  {
    if (ServerMatchController.Instance != null)
    {
      ServerMatchController.Instance.HandleClientReady(conn);
    }
  }

  void OnFullSnapshotMessage(FullSnapshotMessage msg)
  {
    if (ClientMatchController.Instance != null)
    {
      ClientMatchController.Instance.OnFullSnapshotReceived(msg.Snapshot);
    }
  }

  void OnDeltaSnapshotMessage(DeltaSnapshotMessage msg)
  {
    if (ClientMatchController.Instance != null)
    {
      ClientMatchController.Instance.OnDeltaSnapshotReceived(msg.Delta);
    }
  }

  void OnPlayCardFailedMessage(PlayCardFailedMessage msg)
  {
    if (ClientMatchController.Instance != null)
    {
      ClientMatchController.Instance.OnPlayCardFailed(msg.Reason);
    }
  }

  void OnMatchEndedMessage(MatchEndedMessage msg)
  {
    if (ClientMatchController.Instance != null)
    {
      ClientMatchController.Instance.OnMatchEnded(msg.Winner);
    }
  }

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

    NetworkClient.Send(new ClientReadyMessage());
  }

  public override void OnClientDisconnect()
  {
    Debug.Log("[Client] Disconnected from server");
    base.OnClientDisconnect();
  }
}
