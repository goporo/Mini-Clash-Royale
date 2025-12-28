namespace ClashServer
{
  /// <summary>
  /// Logging abstraction for pure .NET server
  /// Implementations can use Console.WriteLine, Debug.Log, or any other logging system
  /// </summary>
  public interface ILogger
  {
    void Log(string message);
    void LogWarning(string message);
    void LogError(string message);
  }

  /// <summary>
  /// Default logger using Console
  /// </summary>
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

#if UNITY_5_3_OR_NEWER
  /// <summary>
  /// Unity logger using Debug.Log
  /// </summary>
  public class UnityLogger : ILogger
  {
    public void Log(string message)
    {
      UnityEngine.Debug.Log(message);
    }

    public void LogWarning(string message)
    {
      UnityEngine.Debug.LogWarning(message);
    }

    public void LogError(string message)
    {
      UnityEngine.Debug.LogError(message);
    }
  }
#endif
}
