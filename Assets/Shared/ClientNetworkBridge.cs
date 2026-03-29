namespace ClashShared
{
  public interface IClientGameTransport
  {
    bool IsConnected { get; }
    void SendPlayCard(uint requestId, CardId cardId, Vector2Data position);
  }

  public interface IClientMatchEventSink
  {
    void OnFullSnapshotReceived(FullSnapshot snapshot);
    void OnDeltaSnapshotReceived(DeltaSnapshot delta);
    void OnSpellCastReceived(SpellCastMessage msg);
    void OnPlayCardFailed(string reason);
    void OnMatchEnded(EntityTeam winner);
    void OnElixirUpdated(int milliElixir);
    void OnHandStateReceived(HandStateMessage msg);
    void OnCardDrawn(CardDrawnMessage msg);
  }

  public static class ClientNetworkBridge
  {
    private static uint nextPlayRequestId = 1;

    public static IClientGameTransport Transport { get; set; }
    public static IClientMatchEventSink EventSink { get; set; }

    public static bool TrySendPlayCard(CardId cardId, Vector2Data position)
    {
      if (Transport == null || !Transport.IsConnected)
        return false;

      Transport.SendPlayCard(nextPlayRequestId++, cardId, position);
      return true;
    }

    public static void PublishFullSnapshot(FullSnapshot snapshot)
    {
      EventSink?.OnFullSnapshotReceived(snapshot);
    }

    public static void PublishDeltaSnapshot(DeltaSnapshot delta)
    {
      EventSink?.OnDeltaSnapshotReceived(delta);
    }

    public static void PublishSpellCast(SpellCastMessage msg)
    {
      EventSink?.OnSpellCastReceived(msg);
    }

    public static void PublishPlayCardFailed(string reason)
    {
      EventSink?.OnPlayCardFailed(reason);
    }

    public static void PublishMatchEnded(EntityTeam winner)
    {
      EventSink?.OnMatchEnded(winner);
    }

    public static void PublishElixirUpdated(int milliElixir)
    {
      EventSink?.OnElixirUpdated(milliElixir);
    }

    public static void PublishHandState(HandStateMessage msg)
    {
      EventSink?.OnHandStateReceived(msg);
    }

    public static void PublishCardDrawn(CardDrawnMessage msg)
    {
      EventSink?.OnCardDrawn(msg);
    }
  }
}
