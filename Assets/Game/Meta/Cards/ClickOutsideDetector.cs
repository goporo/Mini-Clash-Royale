using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ClashMeta
{
  public class ClickOutsideDetector : MonoBehaviour
  {
    struct Entry
    {
      public GameObject panel;
      public GameObject safeZone; // clicks here are ignored (e.g. the button that opened the panel)
      public Action onClickOutside;
    }

    static ClickOutsideDetector _instance;
    readonly List<Entry> _entries = new();
    readonly List<RaycastResult> _hits = new();

    public static void Watch(GameObject panel, Action onClickOutside, GameObject safeZone = null)
    {
      EnsureInstance();
      _instance._entries.RemoveAll(e => e.panel == panel);
      _instance._entries.Add(new Entry { panel = panel, safeZone = safeZone, onClickOutside = onClickOutside });
    }

    public static void Unwatch(GameObject panel)
    {
      if (_instance == null) return;
      _instance._entries.RemoveAll(e => e.panel == panel);
    }

    void Update()
    {
      if (!Input.GetMouseButtonDown(0) || _entries.Count == 0) return;

      var pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
      EventSystem.current.RaycastAll(pointerData, _hits);

      for (int i = _entries.Count - 1; i >= 0; i--)
      {
        var entry = _entries[i];
        if (entry.panel == null || !entry.panel.activeSelf)
        {
          _entries.RemoveAt(i);
          continue;
        }

        bool hit = false;
        foreach (var result in _hits)
        {
          var t = result.gameObject.transform;
          if (t.IsChildOf(entry.panel.transform) || result.gameObject == entry.panel)
          { hit = true; break; }
          if (entry.safeZone != null && (t.IsChildOf(entry.safeZone.transform) || result.gameObject == entry.safeZone))
          { hit = true; break; }
        }

        if (!hit) entry.onClickOutside?.Invoke();
      }
    }

    static void EnsureInstance()
    {
      if (_instance != null) return;
      var go = new GameObject("[ClickOutsideDetector]");
      DontDestroyOnLoad(go);
      _instance = go.AddComponent<ClickOutsideDetector>();
    }
  }
}
