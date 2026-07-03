using Mirror;
using UnityEngine;
using System.Collections.Generic;
using ClashShared;

namespace ClashServer
{
  public class ServerMatchController : MonoBehaviour
  {
    public static ServerMatchController Instance;

    [SerializeField] bool enableReplay = true;
    [SerializeField] bool enableDriftDetection = true;
    [SerializeField] string replayFolderPath = "Replays";

    void OnEnable()
    {
      Instance = this;
      NetworkServer.OnConnectedEvent += HandlePlayerConnected;
      NetworkServer.OnDisconnectedEvent += HandlePlayerDisconnected;
      Debug.Log("[Server] ServerMatchController ready");
    }

    void OnDisable()
    {
      NetworkServer.OnConnectedEvent -= HandlePlayerConnected;
      NetworkServer.OnDisconnectedEvent -= HandlePlayerDisconnected;
      if (Instance == this) Instance = null;
    }

    void Update()
    {
      if (!NetworkServer.active) return;
      var tickedRooms = new HashSet<MatchRoom>();
      foreach (var conn in NetworkServer.connections.Values)
      {
        if (MatchRegistry.TryGetRoomByConn(conn.connectionId, out var room) && tickedRooms.Add(room))
          room.Tick(Time.deltaTime);
      }
    }

    public async void HandleClientReady(NetworkConnectionToClient conn, string matchId, string playerToken, ushort[] deckIds = null)
    {
      MatchRoom room;

      if (string.IsNullOrEmpty(matchId))
      {
        string pveMatchId = $"pve-{conn.connectionId}";
        room = MatchRegistry.GetOrCreatePveRoom(pveMatchId, enableReplay, enableDriftDetection, replayFolderPath);
        if (deckIds == null || deckIds.Length != 8)
        {
          Debug.LogWarning($"[Server] PvE rejected — invalid deck (len={deckIds?.Length})");
          conn.Disconnect();
          return;
        }
        var deck = new List<CardId>(8);
        foreach (var id in deckIds) deck.Add((CardId)id);
        if (!room.TryAddPlayer(conn, deck, EntityTeam.Team1))
        {
          conn.Disconnect();
          return;
        }
        MatchRegistry.RegisterConn(conn.connectionId, room);
      }
      else
      {
        var (ok, r, _) = await MatchRegistry.TryJoinAsync(
          matchId, playerToken, conn,
          enableReplay, enableDriftDetection, replayFolderPath);

        if (!ok)
        {
          Debug.LogWarning($"[Server] Client {conn.connectionId} — invalid matchId/token, disconnecting");
          conn.Disconnect();
          return;
        }
        room = r;
      }

      room.HandleClientReady(conn);
    }

    public void Server_PlayCard(NetworkConnectionToClient sender, uint requestId, CardId cardId, System.Numerics.Vector2 position)
    {
      if (!MatchRegistry.TryGetRoomByConn(sender.connectionId, out var room))
      {
        sender.Send(new PlayCardFailedMessage { Reason = "Not in any match" });
        return;
      }

      room.HandlePlayCard(sender, requestId, cardId, position);
    }

    void HandlePlayerConnected(NetworkConnectionToClient conn)
    {
      Debug.Log($"[Server] Player {conn.connectionId} connected — waiting for ClientReadyMessage");
    }

    void HandlePlayerDisconnected(NetworkConnectionToClient conn)
    {
      if (MatchRegistry.TryGetRoomByConn(conn.connectionId, out var room))
      {
        bool roomEmpty = room.HandlePlayerDisconnected(conn);
        MatchRegistry.RemoveConn(conn.connectionId);
        if (roomEmpty)
        {
          MatchRegistry.RemoveActive(room.MatchId);
          Debug.Log($"[Server] Room {room.MatchId} empty — removed from registry");
        }
      }
    }
  }
}
