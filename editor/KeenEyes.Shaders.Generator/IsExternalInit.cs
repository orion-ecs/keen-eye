// Polyfill for init-only setters when targeting netstandard2.0

using System.ComponentModel;

namespace System.Runtime.CompilerServices;

[EditorBrowsable(EditorBrowsableState.Never)]
internal static class IsExternalInit
{
}
