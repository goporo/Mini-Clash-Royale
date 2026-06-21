using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using Mirror;
using ClashShared;

namespace ClashServer
{
  public class VerifyResponse
  {
    [JsonProperty("valid")]    public bool Valid;
    [JsonProperty("team")]     public int Team;
    [JsonProperty("deck")]     public ushort[] Deck;
    [JsonProperty("playerId")] public string PlayerId;
    [JsonProperty("username")] public string Username;
  }

  public static class MatchRegistry
  {
    static readonly HttpClient http = new();

    // Active rooms: matchId → room
    static readonly ConcurrentDictionary<string, MatchRoom> active = new();

    // Lookup nhanh: connId → room
    static readonly ConcurrentDictionary<int, MatchRoom> connToRoom = new();

    public static string MetaBaseUrl = "http://localhost:5000";
    public static string InternalKey  = "";

    public static async Task<(bool ok, MatchRoom room, List<CardId> deck)> TryJoinAsync(
      string matchId, string token, NetworkConnectionToClient conn,
      bool enableReplay, bool enableDrift, string replayPath)
    {
      // Gọi Meta verify
      var request = new HttpRequestMessage(
        HttpMethod.Get,
        $"{MetaBaseUrl}/internal/match/{matchId}/verify?token={token}");

      if (!string.IsNullOrEmpty(InternalKey))
        request.Headers.Add("X-Internal-Key", InternalKey);

      VerifyResponse verify;
      try
      {
        var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return (false, null, null);

        string json = await response.Content.ReadAsStringAsync();
        verify = JsonConvert.DeserializeObject<VerifyResponse>(json);
      }
      catch (System.Exception e)
      {
        Debug.LogError($"[MatchRegistry] Meta verify failed: {e.Message}");
        return (false, null, null);
      }

      if (!verify.Valid || verify.Deck == null || verify.Deck.Length != 8)
        return (false, null, null);

      // Parse deck
      var deck = new List<CardId>(verify.Deck.Length);
      foreach (var id in verify.Deck)
        deck.Add((CardId)id);

      // Mode từ Meta — PvP vì đây là matched game
      if (!active.TryGetValue(matchId, out var room))
      {
        room = new MatchRoom(matchId, MatchMode.PvP, enableReplay, enableDrift, replayPath);
        active[matchId] = room;
      }

      // Team từ Meta (1-based → 0-based)
      EntityTeam team = verify.Team == 1 ? EntityTeam.Team1 : EntityTeam.Team2;
      if (!room.TryAddPlayer(conn, deck, team)) return (false, null, null);

      connToRoom[conn.connectionId] = room;

      return (true, room, deck);
    }

    public static bool TryGetRoomByConn(int connectionId, out MatchRoom room)
      => connToRoom.TryGetValue(connectionId, out room);

    public static void RemoveConn(int connectionId) => connToRoom.TryRemove(connectionId, out _);

    public static void RegisterConn(int connectionId, MatchRoom room) => connToRoom[connectionId] = room;

    public static void RemoveActive(string matchId) => active.TryRemove(matchId, out _);

    public static MatchRoom GetOrCreatePveRoom(string matchId, bool enableReplay, bool enableDrift, string replayPath)
    {
      if (!active.TryGetValue(matchId, out var room))
      {
        room = new MatchRoom(matchId, MatchMode.PvE, enableReplay, enableDrift, replayPath);
        active[matchId] = room;
      }
      return room;
    }

    public static List<CardId> RandomDeck()
    {
      var all = CardIdHelper.GetAllPlayableCards();
      var pool = (CardId[])all.Clone();
      var rng = new System.Random();
      for (int i = pool.Length - 1; i > 0; i--)
      {
        int j = rng.Next(i + 1);
        (pool[i], pool[j]) = (pool[j], pool[i]);
      }
      return new List<CardId>(pool[..8]);
    }
  }
}
