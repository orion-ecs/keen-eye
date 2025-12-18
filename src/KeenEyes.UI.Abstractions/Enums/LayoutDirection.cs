namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Specifies the main axis direction for layout containers.
/// </summary>
public enum LayoutDirection : byte
{
    /// <summary>
    /// Children are arranged horizontally (left to right by default).
    /// </summary>
    Horizontal = 0,

    /// <summary>
    /// Children are arranged vertically (top to bottom by default).
    /// </summary>
    Vertical = 1
}
