// Polyfill required for C# 9 'init' properties on Unity's older .NET runtime.
namespace System.Runtime.CompilerServices
{
  internal static class IsExternalInit { }
}
