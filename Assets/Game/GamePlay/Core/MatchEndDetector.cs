public class MatchEndDetector
{
  private GameplayDirector director;

  public bool IsMatchOver { get; private set; }
  public EntityTeam Winner { get; private set; }

  public MatchEndDetector(GameplayDirector director)
  {
    this.director = director;
  }

  public void Tick()
  {
    // if (IsMatchOver) return;

    // bool t1 = false;
    // bool t2 = false;

    // foreach (var e in director.Entities.Entities)
    // {
    //   if (!e.IsAlive) continue;

    //   if (e.Team == EntityTeam.Team1) t1 = true;
    //   if (e.Team == EntityTeam.Team2) t2 = true;
    // }

    // if (!t1) { Winner = EntityTeam.Team2; IsMatchOver = true; }
    // if (!t2) { Winner = EntityTeam.Team1; IsMatchOver = true; }
  }
}
