using Mirror;
using UnityEngine;
using ClashBattle;
using ClashServer;
using ClashShared;

public class MyNetworkManager : NetworkManager, IClientBattleTransport
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
    ClientBattleGateway.Transport = this;

    NetworkClient.RegisterHandler<FullSnapshotMessage>(OnFullSnapshotMessage);
    NetworkClient.RegisterHandler<DeltaSnapshotMessage>(OnDeltaSnapshotMessage);
    NetworkClient.RegisterHandler<SpellCastMessage>(OnSpellCastMessage);
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
    if (ReferenceEquals(ClientBattleGateway.Transport, this))
      ClientBattleGateway.Transport = null;

    base.OnStopClient();
  }

  public void SendPlayCard(uint requestId, CardId cardId, Vector2Data position)
  {
    NetworkClient.Send(new PlayCardMessage
    {
      RequestId = requestId,
      CardId = cardId,
      Position = position
    });
  }

  void OnPlayCardMessage(NetworkConnectionToClient conn, PlayCardMessage msg)
  {
    if (ServerMatchController.Instance != null)
    {
      System.Numerics.Vector2 position = msg.Position.ToVector2();
      ServerMatchController.Instance.Server_PlayCard(conn, msg.RequestId, msg.CardId, position);
    }
  }

  void OnClientReadyMessage(NetworkConnectionToClient conn, ClientReadyMessage msg)
  {
    ServerMatchController.Instance?.HandleClientReady(conn);
  }

  void OnFullSnapshotMessage(FullSnapshotMessage msg)
  {
    ClientBattleGateway.PublishFullSnapshot(msg.Snapshot);
  }

  void OnDeltaSnapshotMessage(DeltaSnapshotMessage msg)
  {
    ClientBattleGateway.PublishDeltaSnapshot(msg.Delta);
  }

  void OnSpellCastMessage(SpellCastMessage msg)
  {
    ClientBattleGateway.PublishSpellCast(msg);
  }

  void OnPlayCardFailedMessage(PlayCardFailedMessage msg)
  {
    ClientBattleGateway.PublishPlayCardFailed(msg.Reason);
  }

  void OnMatchEndedMessage(MatchEndedMessage msg)
  {
    ClientBattleGateway.PublishMatchEnded(msg.Winner);
  }

  void OnElixirUpdateMessage(ElixirUpdateMessage msg)
  {
    ClientBattleGateway.PublishElixirUpdated(msg.MilliElixir);
  }

  void OnHandStateMessage(HandStateMessage msg)
  {
    ClientBattleGateway.PublishHandState(msg);
  }

  void OnCardDrawnMessage(CardDrawnMessage msg)
  {
    ClientBattleGateway.PublishCardDrawn(msg);
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
