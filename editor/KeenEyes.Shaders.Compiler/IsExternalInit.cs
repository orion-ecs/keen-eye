// Polyfill for init-only setters when targeting netstandard2.0

#if NETSTANDARD2_0

using System.ComponentModel;

namespace System.Runtime.CompilerServices;

[EditorBrowsable(EditorBrowsableState.Never)]
internal static class IsExternalInit
{
}

#endif
