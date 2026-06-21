using System.Collections;
using UnityEngine;

namespace ClashMeta
{
  public class AppBootstrap : MonoBehaviour
  {
    [SerializeField] GuestLoginPopup guestLoginPopup;
    [SerializeField] TopBarView topBarView;

    IEnumerator Start()
    {
      yield return AuthService.RestoreSession(ok =>
      {
        if (ok)
          OnSessionReady();
        else
          guestLoginPopup.Show(OnSessionReady);
      });
    }

    void OnSessionReady()
    {
      if (topBarView != null) topBarView.Refresh();
    }
  }
}
