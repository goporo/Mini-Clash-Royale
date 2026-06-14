using UnityEngine;

public class ServerSceneBootstrap : MonoBehaviour
{
#if UNITY_SERVER
  [SerializeField] private GameObject[] clientOnlyRoots;

  void Awake()
  {
    foreach (var go in clientOnlyRoots)
    {
      if (go != null)
        go.SetActive(false);
    }
  }
#endif
}
