namespace ClashServer
{
  public interface ILogger
  {
    void Log(string message);
    void LogWarning(string message);
    void LogError(string message);
  }

  public class ConsoleLogger : ILogger
  {
    public void Log(string message)
    {
      System.Console.WriteLine(message);
    }

    public void LogWarning(string message)
    {
      System.Console.WriteLine($"WARNING: {message}");
    }

    public void LogError(string message)
    {
      System.Console.WriteLine($"ERROR: {message}");
    }
  }
}
