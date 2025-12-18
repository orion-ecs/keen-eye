namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Component for a resizable splitter container that divides space between two panes.
/// </summary>
/// <remarks>
/// <para>
/// A splitter divides its area into two panes separated by a draggable handle.
/// The <see cref="Orientation"/> determines the split direction:
/// <list type="bullet">
/// <item><see cref="LayoutDirection.Horizontal"/> - Panes are side by side (vertical divider)</item>
/// <item><see cref="LayoutDirection.Vertical"/> - Panes are stacked (horizontal divider)</item>
/// </list>
/// </para>
/// <para>
/// The split position is controlled by <see cref="SplitRatio"/> (0.0-1.0), where 0.0 means
/// all space goes to the second pane and 1.0 means all space goes to the first pane.
/// </para>
/// </remarks>
/// <param name="orientation">The split direction.</param>
/// <param name="splitRatio">Initial ratio of space allocated to the first pane (0.0-1.0).</param>
public struct UISplitter(LayoutDirection orientation, float splitRatio = 0.5f) : IComponent
{
    /// <summary>
    /// The split direction. Horizontal means panes are side by side, Vertical means stacked.
    /// </summary>
    public LayoutDirection Orientation = orientation;

    /// <summary>
    /// The ratio of space allocated to the first pane (0.0-1.0).
    /// </summary>
    public float SplitRatio = splitRatio;

    /// <summary>
    /// Minimum size in pixels for the first pane.
    /// </summary>
    public float MinFirstPane = 50f;

    /// <summary>
    /// Minimum size in pixels for the second pane.
    /// </summary>
    public float MinSecondPane = 50f;

    /// <summary>
    /// Size of the draggable handle in pixels.
    /// </summary>
    public float HandleSize = 4f;
}

/// <summary>
/// Component identifying the draggable handle of a splitter.
/// </summary>
/// <remarks>
/// The splitter system listens for drag events on entities with this component
/// to adjust the parent splitter's <see cref="UISplitter.SplitRatio"/>.
/// </remarks>
/// <param name="splitterContainer">The parent splitter container entity.</param>
public struct UISplitterHandle(Entity splitterContainer) : IComponent
{
    /// <summary>
    /// Reference to the parent splitter container entity.
    /// </summary>
    public Entity SplitterContainer = splitterContainer;
}

/// <summary>
/// Component identifying the first pane of a splitter (left or top depending on orientation).
/// </summary>
/// <param name="splitterContainer">The parent splitter container entity.</param>
public struct UISplitterFirstPane(Entity splitterContainer) : IComponent
{
    /// <summary>
    /// Reference to the parent splitter container entity.
    /// </summary>
    public Entity SplitterContainer = splitterContainer;
}

/// <summary>
/// Component identifying the second pane of a splitter (right or bottom depending on orientation).
/// </summary>
/// <param name="splitterContainer">The parent splitter container entity.</param>
public struct UISplitterSecondPane(Entity splitterContainer) : IComponent
{
    /// <summary>
    /// Reference to the parent splitter container entity.
    /// </summary>
    public Entity SplitterContainer = splitterContainer;
}
