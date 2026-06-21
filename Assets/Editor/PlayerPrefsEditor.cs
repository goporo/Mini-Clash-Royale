using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerPrefsEditor : EditorWindow
{
  enum PrefType { String, Int, Float }

  class PrefRow
  {
    public string key;
    public string rawValue;
    public PrefType type;
    public bool dirty;
  }

  static readonly Color ColRow     = new Color(0.20f, 0.20f, 0.20f);
  static readonly Color ColRowAlt  = new Color(0.23f, 0.23f, 0.23f);
  static readonly Color ColHeader  = new Color(0.14f, 0.14f, 0.14f);
  static readonly Color ColAccent  = new Color(0.31f, 0.79f, 0.69f);
  static readonly Color ColDanger  = new Color(0.81f, 0.57f, 0.47f);
  static readonly Color ColDirty   = new Color(0.95f, 0.78f, 0.30f);
  static readonly Color ColTypStr  = new Color(0.40f, 0.70f, 1.00f);
  static readonly Color ColTypInt  = new Color(0.75f, 0.60f, 1.00f);
  static readonly Color ColTypFlt  = new Color(0.55f, 0.90f, 0.55f);

  readonly List<PrefRow> rows = new();
  Vector2 scroll;
  string addKey   = "";
  string addValue = "";
  PrefType addType = PrefType.String;
  string filter    = "";
  string statusMsg = "";
  double statusUntil;

  GUIStyle styleMono;
  GUIStyle styleTypeBadge;
  GUIStyle styleColHeader;

  [MenuItem("Tools/PlayerPrefs Editor")]
  static void Open()
  {
    var w = GetWindow<PlayerPrefsEditor>();
    w.titleContent = new GUIContent("PlayerPrefs Editor");
    w.minSize = new Vector2(560, 380);
  }

  void OnEnable() => Reload();

  void BuildStyles()
  {
    if (styleMono != null) return;

    styleMono = new GUIStyle(EditorStyles.textField)
    {
      font     = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Courier New", "Courier" }, 11),
      fontSize = 11,
      padding  = new RectOffset(4, 4, 2, 2),
    };

    styleTypeBadge = new GUIStyle(EditorStyles.miniLabel)
    {
      fontStyle = FontStyle.Bold,
      fontSize  = 9,
      alignment = TextAnchor.MiddleCenter,
    };

    styleColHeader = new GUIStyle(EditorStyles.boldLabel)
    {
      fontSize  = 10,
      alignment = TextAnchor.MiddleLeft,
      padding   = new RectOffset(4, 0, 0, 0),
      normal    = { textColor = new Color(0.55f, 0.55f, 0.55f) },
    };
  }

  void Reload()
  {
    rows.Clear();
    foreach (var k in ReadAllKeys())
    {
      var row = new PrefRow { key = k };
      SniffType(row);
      rows.Add(row);
    }
  }

  static void SniffType(PrefRow row)
  {
    // Try string first (covers almost all cases in practice)
    string sv = PlayerPrefs.GetString(row.key, "\0MISS\0");
    if (sv != "\0MISS\0") { row.type = PrefType.String; row.rawValue = sv; return; }

    float fv = PlayerPrefs.GetFloat(row.key, float.NaN);
    if (!float.IsNaN(fv)) { row.type = PrefType.Float; row.rawValue = fv.ToString("G"); return; }

    row.type     = PrefType.Int;
    row.rawValue = PlayerPrefs.GetInt(row.key, 0).ToString();
  }

  // ── Registry enumeration (Windows editor only) ──────────────────────────────
  static List<string> ReadAllKeys()
  {
    var result = new List<string>();
#if UNITY_EDITOR_WIN
    try
    {
      string path = $@"Software\Unity\UnityEditor\{PlayerSettings.companyName}\{PlayerSettings.productName}";
      var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(path);
      if (key != null)
      {
        foreach (var name in key.GetValueNames())
        {
          int h = name.LastIndexOf('_');
          if (h > 0) result.Add(name[..h]);
        }
        key.Close();
      }
    }
    catch { }
#endif
    return result;
  }

  // ── GUI ──────────────────────────────────────────────────────────────────────
  void OnGUI()
  {
    BuildStyles();
    Toolbar();
    ColumnHeaders();
    RowList();
    AddBar();
    StatusBar();
  }

  void Toolbar()
  {
    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

    filter = EditorGUILayout.TextField(filter, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
    GUILayout.Space(4);

    if (GUILayout.Button("Reload",   EditorStyles.toolbarButton, GUILayout.Width(54))) Reload();
    if (GUILayout.Button("Save All", EditorStyles.toolbarButton, GUILayout.Width(60))) SaveAll();

    GUI.color = ColDanger;
    if (GUILayout.Button("Delete All", EditorStyles.toolbarButton, GUILayout.Width(68)))
    {
      if (EditorUtility.DisplayDialog("Delete All PlayerPrefs",
            "Wipe every PlayerPrefs key? This cannot be undone.", "Delete", "Cancel"))
      {
        PlayerPrefs.DeleteAll(); PlayerPrefs.Save(); Reload(); Status("All prefs deleted.");
      }
    }
    GUI.color = Color.white;

    EditorGUILayout.EndHorizontal();
  }

  void ColumnHeaders()
  {
    var r = EditorGUILayout.GetControlRect(false, 18);
    EditorGUI.DrawRect(r, ColHeader);
    Layout(r, out var rT, out var rK, out var rV, out _);
    GUI.Label(rT, "TYPE",  styleColHeader);
    GUI.Label(rK, "KEY",   styleColHeader);
    GUI.Label(rV, "VALUE", styleColHeader);
  }

  void RowList()
  {
    scroll = EditorGUILayout.BeginScrollView(scroll, GUIStyle.none, GUI.skin.verticalScrollbar);

    string lo = filter?.ToLower() ?? "";
    int n = 0;
    for (int i = 0; i < rows.Count; i++)
    {
      var row = rows[i];
      if (lo.Length > 0 && !row.key.ToLower().Contains(lo)) continue;

      var rect = EditorGUILayout.GetControlRect(false, 22);
      EditorGUI.DrawRect(rect, n % 2 == 0 ? ColRow : ColRowAlt);
      n++;

      Layout(rect, out var rT, out var rK, out var rV, out var rD);

      // Type badge
      Color tc = row.type == PrefType.String ? ColTypStr : row.type == PrefType.Int ? ColTypInt : ColTypFlt;
      EditorGUI.DrawRect(Inset(rT, 2, 3), tc * 0.20f + ColRow * 0.80f);
      GUI.contentColor = tc;
      GUI.Label(Inset(rT, 2, 3), row.type == PrefType.String ? "STR" : row.type == PrefType.Int ? "INT" : "FLT", styleTypeBadge);
      GUI.contentColor = Color.white;

      // Dirty indicator
      if (row.dirty)
        EditorGUI.DrawRect(new Rect(rT.xMax - 5, rT.y + 3, 4, 4), ColDirty);

      // Key
      GUI.contentColor = new Color(0.72f, 0.72f, 0.72f);
      EditorGUI.SelectableLabel(Inset(rK, 2, 2), row.key, styleMono);
      GUI.contentColor = Color.white;

      // Value
      EditorGUI.BeginChangeCheck();
      string nv = EditorGUI.TextField(Inset(rV, 2, 2), row.rawValue, styleMono);
      if (EditorGUI.EndChangeCheck()) { row.rawValue = nv; row.dirty = true; }

      // Delete
      GUI.contentColor = ColDanger;
      if (GUI.Button(Inset(rD, 2, 3), "✕", EditorStyles.miniButton))
      {
        PlayerPrefs.DeleteKey(row.key); PlayerPrefs.Save();
        rows.RemoveAt(i); i--;
        Status($"Deleted \"{row.key}\"");
      }
      GUI.contentColor = Color.white;
    }

    if (n == 0)
    {
      EditorGUILayout.Space(24);
      GUI.contentColor = new Color(0.45f, 0.45f, 0.45f);
      GUILayout.Label(rows.Count == 0 ? "No PlayerPrefs found." : $"No keys match \"{filter}\"",
        new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 11 });
      GUI.contentColor = Color.white;
    }

    EditorGUILayout.EndScrollView();
  }

  void AddBar()
  {
    EditorGUILayout.Space(2);
    var bar = EditorGUILayout.GetControlRect(false, 26);
    EditorGUI.DrawRect(bar, ColHeader);

    float p = 4, typeW = 62, btnW = 60;
    float rest = bar.width - typeW - btnW - p * 4;
    float keyW = rest * 0.38f, valW = rest - keyW;
    float y = bar.y + 4, h = 18;

    var rT = new Rect(bar.x + p,                     y, typeW, h);
    var rK = new Rect(rT.xMax + p,                   y, keyW,  h);
    var rV = new Rect(rK.xMax + p,                   y, valW,  h);
    var rB = new Rect(bar.xMax - btnW - p,            y, btnW,  h);

    addType  = (PrefType)EditorGUI.EnumPopup(rT, addType, EditorStyles.toolbarDropDown);
    addKey   = EditorGUI.TextField(rK, addKey,   styleMono);
    addValue = EditorGUI.TextField(rV, addValue, styleMono);

    bool ok = !string.IsNullOrWhiteSpace(addKey);
    GUI.contentColor = ok ? ColAccent : new Color(0.45f, 0.45f, 0.45f);
    using (new EditorGUI.DisabledScope(!ok))
      if (GUI.Button(rB, "+ Add", EditorStyles.miniButton) && ok) CommitAdd();
    GUI.contentColor = Color.white;
  }

  void StatusBar()
  {
    if (string.IsNullOrEmpty(statusMsg)) return;
    if (EditorApplication.timeSinceStartup > statusUntil) { statusMsg = ""; return; }

    var r = EditorGUILayout.GetControlRect(false, 16);
    GUI.contentColor = ColAccent;
    GUI.Label(r, statusMsg, new GUIStyle(EditorStyles.centeredGreyMiniLabel) { normal = { textColor = ColAccent } });
    GUI.contentColor = Color.white;
    Repaint();
  }

  // ── Actions ──────────────────────────────────────────────────────────────────
  void CommitAdd()
  {
    string k = addKey.Trim();
    switch (addType)
    {
      case PrefType.String: PlayerPrefs.SetString(k, addValue); break;
      case PrefType.Int:    PlayerPrefs.SetInt(k,    int.TryParse(addValue,   out int   iv) ? iv : 0);    break;
      case PrefType.Float:  PlayerPrefs.SetFloat(k,  float.TryParse(addValue, out float fv) ? fv : 0f);   break;
    }
    PlayerPrefs.Save();

    int idx = rows.FindIndex(r => r.key == k);
    var row = new PrefRow { key = k, rawValue = addValue, type = addType };
    if (idx >= 0) rows[idx] = row; else rows.Add(row);

    Status($"Saved \"{k}\"");
    addKey = ""; addValue = "";
  }

  void SaveAll()
  {
    int count = 0;
    foreach (var row in rows)
    {
      if (!row.dirty) continue;
      switch (row.type)
      {
        case PrefType.String: PlayerPrefs.SetString(row.key, row.rawValue); break;
        case PrefType.Int:    PlayerPrefs.SetInt(row.key,    int.TryParse(row.rawValue,   out int   iv) ? iv : 0);   break;
        case PrefType.Float:  PlayerPrefs.SetFloat(row.key,  float.TryParse(row.rawValue, out float fv) ? fv : 0f); break;
      }
      row.dirty = false;
      count++;
    }
    PlayerPrefs.Save();
    Status(count > 0 ? $"Saved {count} change{(count == 1 ? "" : "s")}." : "Nothing to save.");
  }

  void Status(string msg) { statusMsg = msg; statusUntil = EditorApplication.timeSinceStartup + 3.0; Repaint(); }

  // ── Layout helpers ───────────────────────────────────────────────────────────
  static void Layout(Rect r, out Rect type, out Rect key, out Rect val, out Rect del)
  {
    float typeW = 38, delW = 22;
    float rest  = r.width - typeW - delW;
    float keyW  = rest * 0.38f, valW = rest - keyW;
    type = new Rect(r.x,                    r.y, typeW, r.height);
    key  = new Rect(r.x + typeW,            r.y, keyW,  r.height);
    val  = new Rect(r.x + typeW + keyW,     r.y, valW,  r.height);
    del  = new Rect(r.x + typeW + keyW + valW, r.y, delW, r.height);
  }

  static Rect Inset(Rect r, float h, float v) =>
    new Rect(r.x + h, r.y + v, r.width - h * 2, r.height - v * 2);
}
