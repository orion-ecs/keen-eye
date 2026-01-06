using System.Numerics;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Component that defines the position and size of a UI element using anchor-based layout.
/// </summary>
/// <remarks>
/// <para>
/// UIRect uses an anchor-based positioning system where anchors define attachment points
/// to the parent element's bounds. This allows for flexible, responsive layouts.
/// </para>
/// <para>
/// The <see cref="ComputedBounds"/> field is automatically calculated by the layout system
/// and should not be set directly. It represents the final screen-space bounds of the element.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Centered element with fixed size
/// var rect = new UIRect
/// {
///     AnchorMin = new Vector2(0.5f, 0.5f),  // Center anchor
///     AnchorMax = new Vector2(0.5f, 0.5f),
///     Pivot = new Vector2(0.5f, 0.5f),       // Center pivot
///     Size = new Vector2(200, 100),
///     WidthMode = UISizeMode.Fixed,
///     HeightMode = UISizeMode.Fixed
/// };
///
/// // Full-width element at top with fixed height
/// var headerRect = new UIRect
/// {
///     AnchorMin = new Vector2(0, 0),         // Top-left
///     AnchorMax = new Vector2(1, 0),         // Top-right
///     Size = new Vector2(0, 50),             // Height only matters
///     HeightMode = UISizeMode.Fixed
/// };
/// </code>
/// </example>
public struct UIRect : IComponent
{
    /// <summary>
    /// The minimum anchor point (0,0 = top-left of parent, 1,1 = bottom-right).
    /// </summary>
    public Vector2 AnchorMin;

    /// <summary>
    /// The maximum anchor point (0,0 = top-left of parent, 1,1 = bottom-right).
    /// </summary>
    public Vector2 AnchorMax;

    /// <summary>
    /// The pivot point for positioning and rotation (0,0 = top-left, 1,1 = bottom-right).
    /// </summary>
    public Vector2 Pivot;

    /// <summary>
    /// Pixel offset from the anchored position.
    /// </summary>
    public UIEdges Offset;

    /// <summary>
    /// The size of the element (used when anchors match or with Fixed size mode).
    /// </summary>
    public Vector2 Size;

    /// <summary>
    /// How the width is calculated.
    /// </summary>
    public UISizeMode WidthMode;

    /// <summary>
    /// How the height is calculated.
    /// </summary>
    public UISizeMode HeightMode;

    /// <summary>
    /// The computed screen-space bounds, calculated by the layout system.
    /// </summary>
    /// <remarks>
    /// This field is set automatically during the layout phase. Do not modify directly.
    /// </remarks>
    public Rectangle ComputedBounds;

    /// <summary>
    /// Local Z-index for render ordering within the same hierarchy level.
    /// Higher values render on top of lower values.
    /// </summary>
    public short LocalZIndex;

    /// <summary>
    /// When true, this element's horizontal layout will be mirrored for right-to-left (RTL) locales.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the layout system will automatically mirror the horizontal positioning
    /// of this element when the active locale uses RTL text direction (Arabic, Hebrew, etc.).
    /// </para>
    /// <para>
    /// This affects:
    /// <list type="bullet">
    ///   <item><description>Anchor point interpretation (left becomes right)</description></item>
    ///   <item><description>Flexbox layout child ordering for horizontal layouts</description></item>
    ///   <item><description>Offset interpretation</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Set to <c>false</c> for elements that should maintain their position regardless of
    /// text direction (e.g., logos, images that are direction-independent).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In LTR: [Back] [Home] [Forward]
    /// // In RTL: [Forward] [Home] [Back]
    /// var navBar = new UIRect
    /// {
    ///     MirrorForRtl = true
    /// };
    /// </code>
    /// </example>
    public bool MirrorForRtl;

    /// <summary>
    /// Creates a UI rect that stretches to fill its parent.
    /// </summary>
    public static UIRect Stretch() => new()
    {
        AnchorMin = Vector2.Zero,
        AnchorMax = Vector2.One,
        Pivot = new Vector2(0.5f, 0.5f),
        Offset = UIEdges.Zero,
        WidthMode = UISizeMode.Fill,
        HeightMode = UISizeMode.Fill
    };

    /// <summary>
    /// Creates a UI rect with a fixed size centered in its parent.
    /// </summary>
    /// <param name="width">The fixed width in pixels.</param>
    /// <param name="height">The fixed height in pixels.</param>
    public static UIRect Centered(float width, float height) => new()
    {
        AnchorMin = new Vector2(0.5f, 0.5f),
        AnchorMax = new Vector2(0.5f, 0.5f),
        Pivot = new Vector2(0.5f, 0.5f),
        Size = new Vector2(width, height),
        WidthMode = UISizeMode.Fixed,
        HeightMode = UISizeMode.Fixed
    };

    /// <summary>
    /// Creates a UI rect positioned at a specific location with fixed size.
    /// </summary>
    /// <param name="x">The X position in pixels from top-left.</param>
    /// <param name="y">The Y position in pixels from top-left.</param>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    public static UIRect Fixed(float x, float y, float width, float height) => new()
    {
        AnchorMin = Vector2.Zero,
        AnchorMax = Vector2.Zero,
        Pivot = Vector2.Zero,
        Offset = new UIEdges(x, y, 0, 0),
        Size = new Vector2(width, height),
        WidthMode = UISizeMode.Fixed,
        HeightMode = UISizeMode.Fixed
    };
}
