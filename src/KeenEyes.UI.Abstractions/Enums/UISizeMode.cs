namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Specifies how a UI element's dimension (width or height) is calculated.
/// </summary>
public enum UISizeMode : byte
{
    /// <summary>
    /// Size is fixed to the specified pixel value.
    /// </summary>
    Fixed = 0,

    /// <summary>
    /// Size expands to fit the content (children or text).
    /// </summary>
    FitContent = 1,

    /// <summary>
    /// Size fills all available space in the parent container.
    /// </summary>
    Fill = 2,

    /// <summary>
    /// Size is a percentage of the parent container's size.
    /// </summary>
    Percentage = 3
}
