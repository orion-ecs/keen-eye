using System.Numerics;

namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Component that enables scrolling of child content within an element.
/// </summary>
/// <remarks>
/// <para>
/// When present, the element's content can scroll beyond its visible bounds.
/// The <see cref="ScrollPosition"/> represents the current scroll offset.
/// </para>
/// <para>
/// The <see cref="ContentSize"/> is automatically calculated by the layout system
/// based on the total bounds of all children. Do not set this directly.
/// </para>
/// </remarks>
public struct UIScrollable : IComponent
{
    /// <summary>
    /// Whether horizontal scrolling is enabled.
    /// </summary>
    public bool HorizontalScroll;

    /// <summary>
    /// Whether vertical scrolling is enabled.
    /// </summary>
    public bool VerticalScroll;

    /// <summary>
    /// Current scroll position (offset from top-left of content).
    /// </summary>
    public Vector2 ScrollPosition;

    /// <summary>
    /// The total size of the scrollable content (calculated by layout system).
    /// </summary>
    public Vector2 ContentSize;

    /// <summary>
    /// Scroll speed multiplier for mouse wheel input.
    /// </summary>
    public float ScrollSensitivity;

    /// <summary>
    /// Creates a vertical scrolling container.
    /// </summary>
    /// <param name="sensitivity">Scroll speed multiplier.</param>
    public static UIScrollable Vertical(float sensitivity = 20f) => new()
    {
        HorizontalScroll = false,
        VerticalScroll = true,
        ScrollPosition = Vector2.Zero,
        ScrollSensitivity = sensitivity
    };

    /// <summary>
    /// Creates a horizontal scrolling container.
    /// </summary>
    /// <param name="sensitivity">Scroll speed multiplier.</param>
    public static UIScrollable Horizontal(float sensitivity = 20f) => new()
    {
        HorizontalScroll = true,
        VerticalScroll = false,
        ScrollPosition = Vector2.Zero,
        ScrollSensitivity = sensitivity
    };

    /// <summary>
    /// Creates a container that can scroll in both directions.
    /// </summary>
    /// <param name="sensitivity">Scroll speed multiplier.</param>
    public static UIScrollable Both(float sensitivity = 20f) => new()
    {
        HorizontalScroll = true,
        VerticalScroll = true,
        ScrollPosition = Vector2.Zero,
        ScrollSensitivity = sensitivity
    };

    /// <summary>
    /// Gets the maximum scroll position based on content and viewport sizes.
    /// </summary>
    /// <param name="viewportSize">The visible area size.</param>
    /// <returns>The maximum scroll position.</returns>
    public readonly Vector2 GetMaxScroll(Vector2 viewportSize)
    {
        return new Vector2(
            Math.Max(0, ContentSize.X - viewportSize.X),
            Math.Max(0, ContentSize.Y - viewportSize.Y)
        );
    }

    /// <summary>
    /// Clamps the scroll position to valid bounds.
    /// </summary>
    /// <param name="viewportSize">The visible area size.</param>
    public void ClampScrollPosition(Vector2 viewportSize)
    {
        var max = GetMaxScroll(viewportSize);
        ScrollPosition = new Vector2(
            Math.Clamp(ScrollPosition.X, 0, max.X),
            Math.Clamp(ScrollPosition.Y, 0, max.Y)
        );
    }
}

/// <summary>
/// Component that identifies a scrollbar thumb and links it to its parent ScrollView.
/// </summary>
/// <param name="scrollView">The parent ScrollView entity that contains the UIScrollable component.</param>
/// <param name="isVertical">True for vertical scrollbar, false for horizontal.</param>
public struct UIScrollbarThumb(Entity scrollView, bool isVertical) : IComponent
{
    /// <summary>
    /// The parent ScrollView entity.
    /// </summary>
    public Entity ScrollView = scrollView;

    /// <summary>
    /// Whether this is a vertical (true) or horizontal (false) scrollbar.
    /// </summary>
    public bool IsVertical = isVertical;
}
