using Mirror;
using UnityEngine;
using ClashBattle;
using ClashServer;
using ClashShared;

public class MyNetworkManager : NetworkManager, IClientBattleTransport
{
  public bool IsConnected => NetworkClient.isConnected;

  [HideInInspector] public MatchMode MatchMode = MatchMode.PvE;

  public override void Awake()
  {
    System.Console.WriteLine("[Server] Awake() called");
    base.Awake();
#if UNITY_SERVER
    System.Console.WriteLine("[Server] UNITY_SERVER defined — configuring headless mode");
    MatchMode = MatchMode.PvP;
    headlessStartMode = HeadlessStartOptions.AutoStartServer;

    string[] args = System.Environment.GetCommandLineArgs();
    for (int i = 0; i < args.Length - 1; i++)
    {
      if (args[i] == "-port" && ushort.TryParse(args[i + 1], out ushort port))
      {
        if (Transport.active is PortTransport pt)
          pt.Port = port;
        Debug.Log($"[Server] Port set to {port} from command line");
      }
    }

    Debug.Log("[Server] ========================================");
    Debug.Log("[Server] Dedicated Server Build starting...");
    ushort activePort = Transport.active is PortTransport p ? p.Port : (ushort)7777;
    Debug.Log($"[Server] Port: {activePort}");
    Debug.Log("[Server] ========================================");
#endif
  }

  public override void OnStartServer()
  {
    base.OnStartServer();

    NetworkServer.RegisterHandler<PlayCardMessage>(OnPlayCardMessage);
    NetworkServer.RegisterHandler<ClientReadyMessage>(OnClientReadyMessage);

    var transport = Transport.active;
    string address = networkAddress;
    ushort port = (transport is PortTransport pt) ? pt.Port : (ushort)0;

    Debug.Log("[Server] ========================================");
    Debug.Log($"[Server] Server ONLINE — listening on port {port}");
    Debug.Log($"[Server] Match mode: {MatchMode}");
    Debug.Log("[Server] Waiting for players...");
    Debug.Log("[Server] ========================================");
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
    int playerCount = NetworkServer.connections.Count;
    Debug.Log($"[Server] >> Player connected | connId={conn.connectionId} | address={conn.address} | total={playerCount}");
    base.OnServerConnect(conn);
  }

  public override void OnServerDisconnect(NetworkConnectionToClient conn)
  {
    int playerCount = NetworkServer.connections.Count - 1;
    Debug.Log($"[Server] << Player disconnected | connId={conn.connectionId} | remaining={playerCount}");
    base.OnServerDisconnect(conn);
  }

  public override void OnServerAddPlayer(NetworkConnectionToClient conn)
  {
    // Do not spawn a player prefab — server tracks players via ServerMatchController
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
