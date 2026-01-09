namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Defines how 9-slice edges and center regions are filled.
/// </summary>
/// <remarks>
/// Used with <see cref="ImageScaleMode.NineSlice"/> to control how
/// the stretchable regions of a 9-slice image are rendered.
/// </remarks>
public enum SlicedFillMode : byte
{
    /// <summary>
    /// Stretch regions to fill available space.
    /// </summary>
    Stretch = 0,

    /// <summary>
    /// Tile regions using original pixel size.
    /// </summary>
    Tile = 1
}
