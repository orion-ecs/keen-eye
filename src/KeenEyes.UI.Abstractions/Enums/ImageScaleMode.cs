namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Specifies how an image is scaled within a UI element.
/// </summary>
public enum ImageScaleMode : byte
{
    /// <summary>
    /// Image is stretched to fill the entire element bounds.
    /// </summary>
    Stretch = 0,

    /// <summary>
    /// Image is scaled uniformly to fit within the element while preserving aspect ratio.
    /// May leave empty space.
    /// </summary>
    ScaleToFit = 1,

    /// <summary>
    /// Image is scaled uniformly to fill the element while preserving aspect ratio.
    /// May crop the image.
    /// </summary>
    ScaleToFill = 2,

    /// <summary>
    /// Image is tiled to fill the element bounds.
    /// </summary>
    Tile = 3,

    /// <summary>
    /// Image uses nine-slice scaling, preserving corners and stretching edges.
    /// </summary>
    NineSlice = 4
}
