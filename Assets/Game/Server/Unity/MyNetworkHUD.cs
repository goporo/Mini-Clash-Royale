using Mirror;
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Advanced Network HUD for Mini Clash Royale
/// Provides comprehensive server management and player-friendly UI
/// </summary>
public class MyNetworkHUD : MonoBehaviour
{
  NetworkManager manager;

  [Header("UI Settings")]
  public int offsetX = 10;
  public int offsetY = 10;
  public bool showGUI = true;
  public bool showPlayerList = true;
  public bool showServerStats = true;

  [Header("Styling")]
  private Color backgroundColor = new Color(0.05f, 0.05f, 0.1f, 0.95f);
  private Color panelColor = new Color(0.15f, 0.15f, 0.2f, 0.9f);
  private Color buttonColor = new Color(0.2f, 0.6f, 0.8f, 1f);
  private Color buttonHoverColor = new Color(0.3f, 0.7f, 0.9f, 1f);
  private Color dangerButtonColor = new Color(0.8f, 0.2f, 0.2f, 1f);
  private Color successButtonColor = new Color(0.2f, 0.8f, 0.4f, 1f);
  private Color textColor = Color.white;
  private Color accentColor = new Color(1f, 0.8f, 0.2f, 1f);

  [Header("Server Management")]
  public int maxPlayers = 8;

  private GUIStyle boxStyle;
  private GUIStyle panelStyle;
  private GUIStyle buttonStyle;
  private GUIStyle dangerButtonStyle;
  private GUIStyle successButtonStyle;
  private GUIStyle labelStyle;
  private GUIStyle boldLabelStyle;
  private GUIStyle textFieldStyle;
  private GUIStyle titleStyle;
  private GUIStyle sectionHeaderStyle;
  private GUIStyle scrollViewStyle;

  private Vector2 playerScrollPosition;
  private Vector2 consoleScrollPosition;

  private float serverStartTime;
  private bool isRestarting = false;
  private string statusMessage = "";
  private float statusMessageTime;
  private List<string> serverLogs = new List<string>();
  private const int maxLogs = 50;
  private bool showServerConsole = false;
  private bool isCollapsed = false;

  void Awake()
  {
    manager = GetComponent<NetworkManager>();
    Application.logMessageReceived += HandleLog;
  }

  void OnDestroy()
  {
    Application.logMessageReceived -= HandleLog;
  }

  void HandleLog(string logString, string stackTrace, LogType type)
  {
    if (type == LogType.Error || type == LogType.Warning || logString.Contains("Mirror"))
    {
      string timestamp = DateTime.Now.ToString("HH:mm:ss");
      string prefix = type == LogType.Error ? "❌" : type == LogType.Warning ? "⚠️" : "ℹ️";
      serverLogs.Add($"[{timestamp}] {prefix} {logString}");

      if (serverLogs.Count > maxLogs)
        serverLogs.RemoveAt(0);
    }
  }

  void InitializeStyles()
  {
    // Box style for main background
    boxStyle = new GUIStyle(GUI.skin.box);
    boxStyle.normal.background = MakeTex(2, 2, backgroundColor);
    boxStyle.padding = new RectOffset(20, 20, 20, 20);

    // Panel style for sections
    panelStyle = new GUIStyle(GUI.skin.box);
    panelStyle.normal.background = MakeTex(2, 2, panelColor);
    panelStyle.padding = new RectOffset(12, 12, 12, 12);
    panelStyle.margin = new RectOffset(0, 0, 5, 5);

    // Button styles
    buttonStyle = new GUIStyle(GUI.skin.button);
    buttonStyle.normal.background = MakeTex(2, 2, buttonColor);
    buttonStyle.hover.background = MakeTex(2, 2, buttonHoverColor);
    buttonStyle.active.background = MakeTex(2, 2, buttonHoverColor);
    buttonStyle.normal.textColor = textColor;
    buttonStyle.hover.textColor = textColor;
    buttonStyle.active.textColor = textColor;
    buttonStyle.fontSize = 13;
    buttonStyle.padding = new RectOffset(12, 12, 8, 8);
    buttonStyle.fontStyle = FontStyle.Bold;
    buttonStyle.alignment = TextAnchor.MiddleCenter;

    dangerButtonStyle = new GUIStyle(buttonStyle);
    dangerButtonStyle.normal.background = MakeTex(2, 2, dangerButtonColor);
    dangerButtonStyle.hover.background = MakeTex(2, 2, new Color(0.9f, 0.3f, 0.3f, 1f));

    successButtonStyle = new GUIStyle(buttonStyle);
    successButtonStyle.normal.background = MakeTex(2, 2, successButtonColor);
    successButtonStyle.hover.background = MakeTex(2, 2, new Color(0.3f, 0.9f, 0.5f, 1f));

    // Label styles
    labelStyle = new GUIStyle(GUI.skin.label);
    labelStyle.normal.textColor = textColor;
    labelStyle.fontSize = 12;
    labelStyle.wordWrap = true;
    labelStyle.hover.textColor = textColor;
    labelStyle.richText = true;

    boldLabelStyle = new GUIStyle(labelStyle);
    boldLabelStyle.fontStyle = FontStyle.Bold;
    boldLabelStyle.fontSize = 13;

    // Title style
    titleStyle = new GUIStyle(GUI.skin.label);
    titleStyle.normal.textColor = accentColor;
    titleStyle.fontSize = 20;
    titleStyle.fontStyle = FontStyle.Bold;
    titleStyle.alignment = TextAnchor.MiddleCenter;

    // Section header style
    sectionHeaderStyle = new GUIStyle(GUI.skin.label);
    sectionHeaderStyle.normal.textColor = accentColor;
    sectionHeaderStyle.fontSize = 14;
    sectionHeaderStyle.fontStyle = FontStyle.Bold;
    sectionHeaderStyle.padding = new RectOffset(0, 0, 5, 5);

    // Text field style
    textFieldStyle = new GUIStyle(GUI.skin.textField);
    textFieldStyle.normal.textColor = textColor;
    textFieldStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.2f, 0.25f, 1f));
    textFieldStyle.fontSize = 12;
    textFieldStyle.padding = new RectOffset(8, 8, 6, 6);
    textFieldStyle.margin = new RectOffset(0, 0, 2, 2);

    // Scroll view style
    scrollViewStyle = new GUIStyle();
    scrollViewStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.15f, 0.8f));
  }

  Texture2D MakeTex(int width, int height, Color col)
  {
    Color[] pix = new Color[width * height];
    for (int i = 0; i < pix.Length; i++)
      pix[i] = col;

    Texture2D result = new Texture2D(width, height);
    result.SetPixels(pix);
    result.Apply();
    return result;
  }

  void OnGUI()
  {
    if (!showGUI) return;

    InitializeStyles();

    if (isCollapsed)
    {
      // Collapsed state - show minimal expand button
      int collapsedWidth = 50;
      int collapsedHeight = 100;
      GUILayout.BeginArea(new Rect(offsetX, offsetY, collapsedWidth, collapsedHeight), boxStyle);

      if (GUILayout.Button("▶", buttonStyle, GUILayout.Height(80)))
      {
        isCollapsed = false;
      }

      GUILayout.EndArea();
    }
    else
    {
      // Expanded state - show full panel
      int width = 400;
      int maxHeight = Screen.height - (offsetY * 2);

      GUILayout.BeginArea(new Rect(offsetX, offsetY, width, maxHeight), boxStyle);

      // Title with collapse button
      GUILayout.BeginHorizontal();
      GUILayout.Label("⚔️ Mini Clash Royale", titleStyle);
      if (GUILayout.Button("◀", buttonStyle, GUILayout.Width(40), GUILayout.Height(30)))
      {
        isCollapsed = true;
      }
      GUILayout.EndHorizontal();

      DrawSeparator();

      // Show status message if any
      if (!string.IsNullOrEmpty(statusMessage) && Time.time - statusMessageTime < 3f)
      {
        GUILayout.Label($"📢 {statusMessage}", boldLabelStyle);
        GUILayout.Space(5);
      }

      if (!NetworkClient.isConnected && !NetworkServer.active && !isRestarting)
      {
        // Connection menu
        DrawConnectionMenu();
      }
      else if (isRestarting)
      {
        // Restarting state
        DrawRestartingScreen();
      }
      else
      {
        // Connected - show management interface
        DrawConnectedInterface();
      }

      GUILayout.EndArea();
    }
  }

  void DrawSeparator()
  {
    GUILayout.Space(5);
    GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
    GUILayout.Space(5);
  }

  void DrawConnectionMenu()
  {
    // Quick Status
    GUILayout.BeginVertical(panelStyle);
    GUILayout.Label("📡 CONNECTION STATUS", sectionHeaderStyle);
    GUILayout.Label("Not Connected", labelStyle);
    GUILayout.Label($"Transport: <color=#FFA500>{Transport.active.GetType().Name}</color>", labelStyle);
    GUILayout.EndVertical();

    GUILayout.Space(8);

    // Connection Options
    GUILayout.Label("🎮 SELECT MODE", sectionHeaderStyle);

    if (!NetworkClient.active)
    {
#if !UNITY_WEBGL
      // Host Game Button
      if (GUILayout.Button("🏠 HOST GAME", successButtonStyle, GUILayout.Height(45)))
      {
        StartServer(true);
        isCollapsed = true;
      }
      GUILayout.Space(3);
#endif

      // Join Game Section
      GUILayout.BeginVertical(panelStyle);
      GUILayout.Label("🔌 JOIN SERVER", sectionHeaderStyle);

      GUILayout.BeginHorizontal();
      GUILayout.Label("Address:", labelStyle, GUILayout.Width(70));
      manager.networkAddress = GUILayout.TextField(manager.networkAddress, textFieldStyle, GUILayout.Height(30));
      GUILayout.EndHorizontal();

      // Port field
      if (Transport.active is PortTransport portTransport)
      {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Port:", labelStyle, GUILayout.Width(70));
        string portString = GUILayout.TextField(portTransport.Port.ToString(), textFieldStyle, GUILayout.Height(30));
        if (ushort.TryParse(portString, out ushort port))
          portTransport.Port = port;
        GUILayout.EndHorizontal();
      }

      GUILayout.Space(5);
      if (GUILayout.Button("🎯 CONNECT", buttonStyle, GUILayout.Height(40)))
      {
        ShowStatus("Connecting to server...");
        manager.StartClient();
        isCollapsed = true;
      }
      GUILayout.EndVertical();

#if !UNITY_WEBGL
      GUILayout.Space(8);

      // Advanced Options
      GUILayout.BeginVertical(panelStyle);
      GUILayout.Label("⚙️ ADVANCED OPTIONS", sectionHeaderStyle);

      // Dedicated Server Button
      if (GUILayout.Button("🖥️ DEDICATED SERVER", buttonStyle, GUILayout.Height(40)))
      {
        StartServer(false);
        isCollapsed = true;
      }

      GUILayout.Space(5);

      // Server Settings
      GUILayout.BeginHorizontal();
      GUILayout.Label("Max Players:", labelStyle, GUILayout.Width(100));
      string maxPlayersStr = GUILayout.TextField(maxPlayers.ToString(), textFieldStyle, GUILayout.Width(60));
      if (int.TryParse(maxPlayersStr, out int newMax) && newMax > 0)
        maxPlayers = Mathf.Clamp(newMax, 2, 100);
      GUILayout.EndHorizontal();

      GUILayout.EndVertical();
#else
      GUILayout.Space(5);
      GUILayout.Label("⚠️ WebGL can only join as client", labelStyle);
#endif
    }
    else
    {
      // Connecting state
      GUILayout.BeginVertical(panelStyle);
      GUILayout.Label($"🔄 Connecting to {manager.networkAddress}...", boldLabelStyle);
      GUILayout.Space(5);
      if (GUILayout.Button("❌ CANCEL", dangerButtonStyle, GUILayout.Height(35)))
      {
        manager.StopClient();
        ShowStatus("Connection cancelled");
      }
      GUILayout.EndVertical();
    }
  }

  void DrawRestartingScreen()
  {
    GUILayout.BeginVertical(panelStyle);
    GUILayout.Label("🔄 RESTARTING SERVER", sectionHeaderStyle);
    GUILayout.Space(10);
    GUILayout.Label("Please wait...", labelStyle);
    GUILayout.Space(10);

    // Animated dots
    string dots = new string('.', (int)(Time.time * 2) % 4);
    GUILayout.Label($"Initializing{dots}", labelStyle);

    GUILayout.EndVertical();
  }

  void DrawConnectedInterface()
  {
    // Status Panel
    GUILayout.BeginVertical(panelStyle);
    GUILayout.Label("📊 SERVER STATUS", sectionHeaderStyle);

    if (NetworkServer.active && NetworkClient.active)
    {
      GUILayout.Label("<color=#90EE90>🏠 HOST MODE</color>", boldLabelStyle);
    }
    else if (NetworkServer.active)
    {
      GUILayout.Label("<color=#87CEEB>🖥️ DEDICATED SERVER</color>", boldLabelStyle);
    }
    else if (NetworkClient.isConnected)
    {
      GUILayout.Label("<color=#FFD700>🔌 CLIENT CONNECTED</color>", boldLabelStyle);
    }

    GUILayout.Space(3);

    if (NetworkServer.active)
    {
      int playerCount = NetworkServer.connections.Count;
      string uptime = GetServerUptime();

      GUILayout.Label($"👥 Players: <color=#FFA500>{playerCount}/{maxPlayers}</color>", labelStyle);
      GUILayout.Label($"⏱️ Uptime: <color=#FFA500>{uptime}</color>", labelStyle);
      GUILayout.Label($"🌐 Transport: <color=#FFA500>{Transport.active.GetType().Name}</color>", labelStyle);

      if (NetworkClient.active)
      {
        GUILayout.Label($"📍 Address: <color=#FFA500>{manager.networkAddress}</color>", labelStyle);
      }
    }
    else
    {
      GUILayout.Label($"📍 Server: <color=#FFA500>{manager.networkAddress}</color>", labelStyle);
      GUILayout.Label($"🌐 Transport: <color=#FFA500>{Transport.active.GetType().Name}</color>", labelStyle);
    }

    GUILayout.EndVertical();

    GUILayout.Space(5);

    // Client Ready Button
    if (NetworkClient.isConnected && !NetworkClient.ready)
    {
      if (GUILayout.Button("✅ READY UP!", successButtonStyle, GUILayout.Height(40)))
      {
        NetworkClient.Ready();
        if (NetworkClient.localPlayer == null)
          NetworkClient.AddPlayer();
        ShowStatus("Ready! Joining game...");
      }
      GUILayout.Space(5);
    }

    // Player List (for servers)
    if (NetworkServer.active && showPlayerList && NetworkServer.connections.Count > 0)
    {
      DrawPlayerList();
      GUILayout.Space(5);
    }

    // Server Console Toggle
    if (NetworkServer.active && showServerStats)
    {
      showServerConsole = GUILayout.Toggle(showServerConsole,
        showServerConsole ? "▼ Server Console" : "▶ Server Console",
        boldLabelStyle);

      if (showServerConsole)
      {
        DrawServerConsole();
        GUILayout.Space(5);
      }
    }

    // Control Buttons
    DrawControlButtons();
  }

  void DrawPlayerList()
  {
    GUILayout.BeginVertical(panelStyle);
    GUILayout.Label("👥 CONNECTED PLAYERS", sectionHeaderStyle);

    playerScrollPosition = GUILayout.BeginScrollView(playerScrollPosition, scrollViewStyle, GUILayout.Height(120));

    if (NetworkServer.connections.Count == 0)
    {
      GUILayout.Label("No players connected", labelStyle);
    }
    else
    {
      foreach (var conn in NetworkServer.connections.Values)
      {
        string playerName = conn.identity != null ? conn.identity.name : "Loading...";
        GUILayout.BeginHorizontal();
        GUILayout.Label($"🎮 Player #{conn.connectionId}", labelStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label($"{(conn.isReady ? "✅" : "⏳")}", labelStyle);
        GUILayout.EndHorizontal();
      }
    }

    GUILayout.EndScrollView();
    GUILayout.EndVertical();
  }

  void DrawServerConsole()
  {
    GUILayout.BeginVertical(panelStyle);

    consoleScrollPosition = GUILayout.BeginScrollView(consoleScrollPosition, scrollViewStyle, GUILayout.Height(150));

    if (serverLogs.Count == 0)
    {
      GUILayout.Label("No logs yet...", labelStyle);
    }
    else
    {
      for (int i = serverLogs.Count - 1; i >= 0; i--)
      {
        GUILayout.Label(serverLogs[i], labelStyle);
      }
    }

    GUILayout.EndScrollView();

    if (GUILayout.Button("🗑️ Clear Logs", buttonStyle, GUILayout.Height(25)))
    {
      serverLogs.Clear();
    }

    GUILayout.EndVertical();
  }

  void DrawControlButtons()
  {
    GUILayout.Label("⚙️ CONTROLS", sectionHeaderStyle);

    if (NetworkServer.active && NetworkClient.active)
    {
      // Host mode
      GUILayout.BeginHorizontal();

      if (GUILayout.Button("🔄 RESTART SERVER", buttonStyle, GUILayout.Height(40)))
      {
        RestartServer();
      }

      if (GUILayout.Button("🚪 LEAVE\n(Keep Server)", buttonStyle, GUILayout.Height(40)))
      {
        manager.StopClient();
        ShowStatus("Left game, server still running");
      }

      GUILayout.EndHorizontal();

      GUILayout.Space(3);

      if (GUILayout.Button("⏹️ STOP HOST", dangerButtonStyle, GUILayout.Height(40)))
      {
        manager.StopHost();
        ShowStatus("Host stopped");
        serverStartTime = 0;
      }
    }
    else if (NetworkClient.isConnected)
    {
      // Client only
      if (GUILayout.Button("🚪 DISCONNECT", dangerButtonStyle, GUILayout.Height(40)))
      {
        manager.StopClient();
        ShowStatus("Disconnected from server");
      }
    }
    else if (NetworkServer.active)
    {
      // Server only
      GUILayout.BeginHorizontal();

      if (GUILayout.Button("🔄 RESTART", buttonStyle, GUILayout.Height(40)))
      {
        RestartServer();
      }

      if (GUILayout.Button("⏹️ STOP", dangerButtonStyle, GUILayout.Height(40)))
      {
        manager.StopServer();
        ShowStatus("Server stopped");
        serverStartTime = 0;
      }

      GUILayout.EndHorizontal();
    }
  }

  void StartServer(bool asHost)
  {
    serverStartTime = Time.time;
    serverLogs.Clear();

    if (asHost)
    {
      manager.StartHost();
      ShowStatus("Host started successfully!");
      serverLogs.Add($"[{DateTime.Now:HH:mm:ss}] ✅ Host mode started");
    }
    else
    {
      manager.StartServer();
      ShowStatus("Dedicated server started!");
      serverLogs.Add($"[{DateTime.Now:HH:mm:ss}] ✅ Dedicated server started");
    }
  }

  // Public methods to call from external buttons
  public void HostGame()
  {
    StartServer(true);
    isCollapsed = true;
  }

  public void ConnectToServer()
  {
    ShowStatus("Connecting to server...");
    manager.StartClient();
    isCollapsed = true;
  }

  public void StartDedicatedServer()
  {
    StartServer(false);
    isCollapsed = true;
  }

  public void DisconnectFromServer()
  {
    if (NetworkServer.active && NetworkClient.active)
    {
      manager.StopHost();
      ShowStatus("Host stopped");
    }
    else if (NetworkClient.isConnected)
    {
      manager.StopClient();
      ShowStatus("Disconnected from server");
    }
    else if (NetworkServer.active)
    {
      manager.StopServer();
      ShowStatus("Server stopped");
    }
    serverStartTime = 0;
  }

  void RestartServer()
  {
    if (!NetworkServer.active) return;

    isRestarting = true;
    ShowStatus("Restarting server...");
    serverLogs.Add($"[{DateTime.Now:HH:mm:ss}] 🔄 Server restart initiated");

    bool wasHost = NetworkClient.active;

    // Stop current server
    if (wasHost)
      manager.StopHost();
    else
      manager.StopServer();

    // Restart after a short delay
    Invoke(nameof(CompleteRestart), 1.5f);
  }

  void CompleteRestart()
  {
    isRestarting = false;

    // Restart as host (you can make this configurable)
    StartServer(true);
    ShowStatus("Server restarted successfully!");
    serverLogs.Add($"[{DateTime.Now:HH:mm:ss}] ✅ Server restart complete");
  }

  void ShowStatus(string message)
  {
    statusMessage = message;
    statusMessageTime = Time.time;
  }

  string GetServerUptime()
  {
    if (serverStartTime == 0) return "00:00";

    float uptime = Time.time - serverStartTime;
    int minutes = (int)(uptime / 60);
    int seconds = (int)(uptime % 60);

    if (minutes >= 60)
    {
      int hours = minutes / 60;
      minutes = minutes % 60;
      return $"{hours:00}:{minutes:00}:{seconds:00}";
    }

    return $"{minutes:00}:{seconds:00}";
  }
}