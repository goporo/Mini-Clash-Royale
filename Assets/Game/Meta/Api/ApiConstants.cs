namespace ClashMeta
{
  public static class ApiConstants
  {
    public static string BaseUrl = "http://localhost:5167";

    public static class Auth
    {
      public const string GuestLogin  = "/auth/guest-login";
      public const string RefreshToken = "/auth/refresh";
    }

    public static class Player
    {
      public const string Me = "/player/me";
    }

    public static class Collection
    {
      public const string Cards = "/collection/cards";
    }

    public static class Deck
    {
      public const string Get          = "/deck";
      public const string GetActive    = "/deck?active=true";
      public const string Update       = "/deck/update";
      public const string SetActive    = "/deck/active";
    }

    public static class Battle
    {
      public const string FindMatch   = "/battle/find";
      public const string MatchStatus = "/battle/find/status";
      public const string CancelMatch = "/battle/find/cancel";
    }
  }
}
