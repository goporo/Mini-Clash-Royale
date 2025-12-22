using Mirror;
using System;

namespace ClashServer
{
  // Pure server class representing a player's state
  public class PlayerState
  {
    public NetworkConnectionToClient Connection { get; set; }
    public EntityTeam Team { get; set; }
    public float Elixir { get; set; }
    public float ElixirPerSecond { get; set; }
    public float MaxElixir { get; set; }
    public int[] Deck { get; set; } // Card IDs in hand

    public PlayerState(NetworkConnectionToClient conn, EntityTeam team)
    {
      Connection = conn;
      Team = team;
      Elixir = 5f; // Starting elixir
      ElixirPerSecond = 1f;
      MaxElixir = 10f;
      Deck = new int[] { 0, 1, 2, 3 }; // Default deck
    }

    public void UpdateElixir(float deltaTime)
    {
      Elixir += ElixirPerSecond * deltaTime;
      if (Elixir > MaxElixir)
        Elixir = MaxElixir;
    }

    public bool CanAffordCard(int cardId)
    {
      return true;
      float cost = GetCardCost(cardId);
      return Elixir >= cost;
    }

    public void SpendElixir(int cardId)
    {
      float cost = GetCardCost(cardId);
      Elixir -= cost;
      if (Elixir < 0) Elixir = 0;
    }

    private float GetCardCost(int cardId)
    {
      // TODO: Load from card database
      switch (cardId)
      {
        case 0: return 3f; // Knight
        case 1: return 3f; // Archer
        case 2: return 5f; // Giant
        default: return 3f;
      }
    }
  }
}
