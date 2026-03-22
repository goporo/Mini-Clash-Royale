using Mirror;
using UnityEngine;
using ClashServer;
using ClashShared;

public class MyNetworkManager : NetworkManager, IClientGameTransport
{
  public bool IsConnected => NetworkClient.isConnected;

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
    ClientNetworkBridge.Transport = this;

    NetworkClient.RegisterHandler<FullSnapshotMessage>(OnFullSnapshotMessage);
    NetworkClient.RegisterHandler<DeltaSnapshotMessage>(OnDeltaSnapshotMessage);
    NetworkClient.RegisterHandler<PlayCardFailedMessage>(OnPlayCardFailedMessage);
    NetworkClient.RegisterHandler<MatchEndedMessage>(OnMatchEndedMessage);
    NetworkClient.RegisterHandler<ElixirUpdateMessage>(OnElixirUpdateMessage);
    NetworkClient.RegisterHandler<HandStateMessage>(OnHandStateMessage);
    NetworkClient.RegisterHandler<CardDrawnMessage>(OnCardDrawnMessage);

    Debug.Log("[Client] Message handlers registered");
  }

  public override void OnStopServer()
  {
    NetworkServer.UnregisterHandler<PlayCardMessage>();
    NetworkServer.UnregisterHandler<ClientReadyMessage>();
    base.OnStopServer();
  }

  public override void OnStopClient()
  {
    if (ReferenceEquals(ClientNetworkBridge.Transport, this))
      ClientNetworkBridge.Transport = null;

    base.OnStopClient();
  }

  public void SendPlayCard(CardId cardId, Vector2Data position)
  {
    NetworkClient.Send(new PlayCardMessage
    {
      CardId = cardId,
      Position = position
    });
  }

  void OnPlayCardMessage(NetworkConnectionToClient conn, PlayCardMessage msg)
  {
    if (ServerMatchController.Instance != null)
    {
      System.Numerics.Vector2 position = msg.Position.ToVector2();
      ServerMatchController.Instance.Server_PlayCard(conn, msg.CardId, position);
    }
  }

  void OnClientReadyMessage(NetworkConnectionToClient conn, ClientReadyMessage msg)
  {
    ServerMatchController.Instance?.HandleClientReady(conn);
  }

  void OnFullSnapshotMessage(FullSnapshotMessage msg)
  {
    ClientNetworkBridge.PublishFullSnapshot(msg.Snapshot);
  }

  void OnDeltaSnapshotMessage(DeltaSnapshotMessage msg)
  {
    ClientNetworkBridge.PublishDeltaSnapshot(msg.Delta);
  }

  void OnPlayCardFailedMessage(PlayCardFailedMessage msg)
  {
    ClientNetworkBridge.PublishPlayCardFailed(msg.Reason);
  }

  void OnMatchEndedMessage(MatchEndedMessage msg)
  {
    ClientNetworkBridge.PublishMatchEnded(msg.Winner);
  }

  void OnElixirUpdateMessage(ElixirUpdateMessage msg)
  {
    ClientNetworkBridge.PublishElixirUpdated(msg.MilliElixir);
  }

  void OnHandStateMessage(HandStateMessage msg)
  {
    ClientNetworkBridge.PublishHandState(msg);
  }

  void OnCardDrawnMessage(CardDrawnMessage msg)
  {
    ClientNetworkBridge.PublishCardDrawn(msg);
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
