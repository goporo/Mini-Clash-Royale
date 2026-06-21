using System;
using Mirror;
using UnityEngine;
using ClashBattle;
using ClashServer;
using ClashShared;

public class MyNetworkManager : NetworkManager, IClientBattleTransport
{
  public bool IsConnected => NetworkClient.isConnected;

  public override void Awake()
  {
    base.Awake();

    string[] args = System.Environment.GetCommandLineArgs();
    bool isHeadless = Array.IndexOf(args, "-batchmode") >= 0;

    if (isHeadless)
    {
      headlessStartMode = HeadlessStartOptions.AutoStartServer;

      for (int i = 0; i < args.Length - 1; i++)
      {
        if (args[i] == "-port" && ushort.TryParse(args[i + 1], out ushort port))
        {
          if (Transport.active is PortTransport pt)
            pt.Port = port;
        }
      }

      ushort activePort = Transport.active is PortTransport p ? p.Port : (ushort)7777;
      Debug.Log($"[Server] Dedicated server starting on port {activePort}");
    }
  }

  [Header("Dev")]
  [SerializeField] bool useLocalServer = false;

  [Header("Meta Server")]
  public string MetaBaseUrl = "http://localhost:5000";
  public string MetaInternalKey = "";

  [HideInInspector] public string PendingMatchId;
  [HideInInspector] public string PendingPlayerToken;
  [HideInInspector] public ushort[] PendingDeck;

  public override void Start()
  {
    base.Start();
    if (useLocalServer)
    {
      onlineScene = "";
      StartServer();
    }
  }

  public override void OnStartServer()
  {
    base.OnStartServer();

    MatchRegistry.MetaBaseUrl = MetaBaseUrl;
    MatchRegistry.InternalKey = MetaInternalKey;

    NetworkServer.RegisterHandler<PlayCardMessage>(OnPlayCardMessage);
    NetworkServer.RegisterHandler<ClientReadyMessage>(OnClientReadyMessage);

    ushort port = Transport.active is PortTransport pt ? pt.Port : (ushort)0;
    Debug.Log("[Server] ========================================");
    Debug.Log($"[Server] Server ONLINE — listening on port {port}");
    Debug.Log("[Server] Waiting for players...");
    Debug.Log("[Server] ========================================");
  }

  public override void OnStartClient()
  {
    base.OnStartClient();
    ClientBattleGateway.Transport = this;

    NetworkClient.RegisterHandler<MatchReadyMessage>(OnMatchReadyMessage);
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

  public override void OnClientConnect()
  {
    base.OnClientConnect();
    Debug.Log("[Client] Connected to server");
    UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnBattleSceneLoaded;
    UnityEngine.SceneManagement.SceneManager.LoadScene("Battle Scene");
  }

  void OnBattleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
  {
    if (scene.name != "Battle Scene") return;
    UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnBattleSceneLoaded;
    Debug.Log("[Client] Battle Scene loaded — sending ClientReadyMessage");
    NetworkClient.Send(new ClientReadyMessage { MatchId = PendingMatchId, PlayerToken = PendingPlayerToken, Deck = PendingDeck });
  }

  void OnMatchReadyMessage(MatchReadyMessage msg) { }

  public override void OnClientDisconnect()
  {
    Debug.Log("[Client] Disconnected from server");
    base.OnClientDisconnect();
  }

  public void SendPlayCard(uint requestId, CardId cardId, Vector2Data position)
  {
    NetworkClient.Send(new PlayCardMessage { RequestId = requestId, CardId = cardId, Position = position });
  }

  void OnPlayCardMessage(NetworkConnectionToClient conn, PlayCardMessage msg)
  {
    if (ServerMatchController.Instance != null)
      ServerMatchController.Instance.Server_PlayCard(conn, msg.RequestId, msg.CardId, msg.Position.ToVector2());
  }

  void OnClientReadyMessage(NetworkConnectionToClient conn, ClientReadyMessage msg)
  {
    if (ServerMatchController.Instance != null)
      ServerMatchController.Instance.HandleClientReady(conn, msg.MatchId, msg.PlayerToken, msg.Deck);
  }

  void OnFullSnapshotMessage(FullSnapshotMessage msg) => ClientBattleGateway.PublishFullSnapshot(msg.Snapshot);
  void OnDeltaSnapshotMessage(DeltaSnapshotMessage msg) => ClientBattleGateway.PublishDeltaSnapshot(msg.Delta);
  void OnSpellCastMessage(SpellCastMessage msg) => ClientBattleGateway.PublishSpellCast(msg);
  void OnPlayCardFailedMessage(PlayCardFailedMessage msg) => ClientBattleGateway.PublishPlayCardFailed(msg.Reason);
  void OnMatchEndedMessage(MatchEndedMessage msg) => ClientBattleGateway.PublishMatchEnded(msg.Winner);
  void OnElixirUpdateMessage(ElixirUpdateMessage msg) => ClientBattleGateway.PublishElixirUpdated(msg.MilliElixir);
  void OnHandStateMessage(HandStateMessage msg) => ClientBattleGateway.PublishHandState(msg);
  void OnCardDrawnMessage(CardDrawnMessage msg) => ClientBattleGateway.PublishCardDrawn(msg);

  public override void OnServerConnect(NetworkConnectionToClient conn)
  {
    Debug.Log($"[Server] >> Player connected | connId={conn.connectionId} | address={conn.address} | total={NetworkServer.connections.Count}");
    base.OnServerConnect(conn);
  }

  public override void OnServerDisconnect(NetworkConnectionToClient conn)
  {
    Debug.Log($"[Server] << Player disconnected | connId={conn.connectionId} | remaining={NetworkServer.connections.Count - 1}");
    base.OnServerDisconnect(conn);
  }

  public override void OnServerAddPlayer(NetworkConnectionToClient conn) { }
}
