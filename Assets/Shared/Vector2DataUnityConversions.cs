using UnityEngine;

namespace ClashShared
{
  public static class Vector2DataUnityConversions
  {
    public static Vector2Data FromUnityVector2(Vector2 value)
    {
      return new Vector2Data(value.x, value.y);
    }

    public static Vector2 ToUnityVector2(this Vector2Data value)
    {
      return new Vector2(value.X, value.Y);
    }
  }
}
