using System.Numerics;
using ClashShared;

namespace ClashServer
{
  public static class MatchSetup
  {
    public static void InitializeStandardArena(GameplayDirector director, BoardManager boardManager)
    {
      PlaceTower(director, boardManager, CardId.KingTower, new Vector2(0f, -13f), EntityTeam.Team1);
      PlaceTower(director, boardManager, CardId.PrincessTower, new Vector2(-5.5f, -10.5f), EntityTeam.Team1);
      PlaceTower(director, boardManager, CardId.PrincessTower, new Vector2(5.5f, -10.5f), EntityTeam.Team1);

      PlaceTower(director, boardManager, CardId.KingTower, new Vector2(0f, 13f), EntityTeam.Team2);
      PlaceTower(director, boardManager, CardId.PrincessTower, new Vector2(-5.5f, 10.5f), EntityTeam.Team2);
      PlaceTower(director, boardManager, CardId.PrincessTower, new Vector2(5.5f, 10.5f), EntityTeam.Team2);
    }

    private static void PlaceTower(
      GameplayDirector director,
      BoardManager boardManager,
      CardId cardId,
      Vector2 position,
      EntityTeam team)
    {
      ServerEntity building = director.SpawnEntity(cardId, position, team, isBuilding: true);
      boardManager.PlaceBuilding(building);
    }
  }
}
