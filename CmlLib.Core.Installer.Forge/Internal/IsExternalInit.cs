#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices;

// Enables record and init-only properties when targeting .NET Standard 2.0.
internal static class IsExternalInit
{
}
#endif
