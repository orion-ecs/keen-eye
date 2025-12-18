namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Component that enables flexbox-style layout for child elements.
/// </summary>
/// <remarks>
/// <para>
/// When present on an entity, its child entities will be automatically arranged
/// according to the layout settings. Children can still have their own UIRect
/// for size preferences.
/// </para>
/// <para>
/// The layout system supports:
/// <list type="bullet">
///   <item><description>Horizontal and vertical directions</description></item>
///   <item><description>Main axis and cross axis alignment</description></item>
///   <item><description>Spacing between children</description></item>
///   <item><description>Wrapping (when <see cref="Wrap"/> is true)</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Horizontal layout with centered children
/// var layout = new UILayout
/// {
///     Direction = LayoutDirection.Horizontal,
///     MainAxisAlign = LayoutAlign.Center,
///     CrossAxisAlign = LayoutAlign.Center,
///     Spacing = 10f
/// };
///
/// // Vertical list with items at the start
/// var listLayout = new UILayout
/// {
///     Direction = LayoutDirection.Vertical,
///     MainAxisAlign = LayoutAlign.Start,
///     CrossAxisAlign = LayoutAlign.Start,
///     Spacing = 5f
/// };
/// </code>
/// </example>
public struct UILayout : IComponent
{
    /// <summary>
    /// The direction to arrange children (horizontal or vertical).
    /// </summary>
    public LayoutDirection Direction;

    /// <summary>
    /// How children are aligned along the main axis (direction of layout).
    /// </summary>
    public LayoutAlign MainAxisAlign;

    /// <summary>
    /// How children are aligned along the cross axis (perpendicular to direction).
    /// </summary>
    public LayoutAlign CrossAxisAlign;

    /// <summary>
    /// Space between children in pixels.
    /// </summary>
    public float Spacing;

    /// <summary>
    /// Whether children should wrap to the next line when they exceed bounds.
    /// </summary>
    public bool Wrap;

    /// <summary>
    /// Whether to reverse the order of children.
    /// </summary>
    public bool ReverseOrder;

    /// <summary>
    /// Creates a horizontal layout with default settings.
    /// </summary>
    /// <param name="spacing">Space between children.</param>
    public static UILayout Horizontal(float spacing = 0) => new()
    {
        Direction = LayoutDirection.Horizontal,
        MainAxisAlign = LayoutAlign.Start,
        CrossAxisAlign = LayoutAlign.Start,
        Spacing = spacing,
        Wrap = false,
        ReverseOrder = false
    };

    /// <summary>
    /// Creates a vertical layout with default settings.
    /// </summary>
    /// <param name="spacing">Space between children.</param>
    public static UILayout Vertical(float spacing = 0) => new()
    {
        Direction = LayoutDirection.Vertical,
        MainAxisAlign = LayoutAlign.Start,
        CrossAxisAlign = LayoutAlign.Start,
        Spacing = spacing,
        Wrap = false,
        ReverseOrder = false
    };

    /// <summary>
    /// Creates a horizontal layout that centers its children.
    /// </summary>
    /// <param name="spacing">Space between children.</param>
    public static UILayout HorizontalCentered(float spacing = 0) => new()
    {
        Direction = LayoutDirection.Horizontal,
        MainAxisAlign = LayoutAlign.Center,
        CrossAxisAlign = LayoutAlign.Center,
        Spacing = spacing,
        Wrap = false,
        ReverseOrder = false
    };

    /// <summary>
    /// Creates a vertical layout that centers its children.
    /// </summary>
    /// <param name="spacing">Space between children.</param>
    public static UILayout VerticalCentered(float spacing = 0) => new()
    {
        Direction = LayoutDirection.Vertical,
        MainAxisAlign = LayoutAlign.Center,
        CrossAxisAlign = LayoutAlign.Center,
        Spacing = spacing,
        Wrap = false,
        ReverseOrder = false
    };
}
