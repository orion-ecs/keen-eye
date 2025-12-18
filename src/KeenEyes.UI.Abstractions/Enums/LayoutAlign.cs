namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Specifies alignment of children along the main or cross axis in a layout container.
/// </summary>
public enum LayoutAlign : byte
{
    /// <summary>
    /// Children are aligned to the start of the axis.
    /// </summary>
    Start = 0,

    /// <summary>
    /// Children are centered along the axis.
    /// </summary>
    Center = 1,

    /// <summary>
    /// Children are aligned to the end of the axis.
    /// </summary>
    End = 2,

    /// <summary>
    /// Children are distributed with equal space between them (no space at edges).
    /// </summary>
    SpaceBetween = 3,

    /// <summary>
    /// Children are distributed with equal space around them (half space at edges).
    /// </summary>
    SpaceAround = 4,

    /// <summary>
    /// Children are distributed with equal space between and around them.
    /// </summary>
    SpaceEvenly = 5
}
