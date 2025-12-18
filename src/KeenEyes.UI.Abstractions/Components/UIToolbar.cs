namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Component for a toolbar container that holds toolbar buttons.
/// </summary>
/// <remarks>
/// <para>
/// A toolbar is a horizontal or vertical strip of icon buttons typically used
/// for quick access to common actions. Toolbars can contain regular buttons,
/// toggle buttons, and separators.
/// </para>
/// </remarks>
public struct UIToolbar(LayoutDirection orientation) : IComponent
{
    /// <summary>
    /// The layout orientation of the toolbar (horizontal or vertical).
    /// </summary>
    public LayoutDirection Orientation = orientation;

    /// <summary>
    /// Number of buttons in this toolbar (excluding separators).
    /// </summary>
    public int ButtonCount = 0;
}

/// <summary>
/// Component for a button within a toolbar.
/// </summary>
/// <param name="toolbar">The parent toolbar entity.</param>
/// <remarks>
/// <para>
/// Toolbar buttons can be regular push buttons or toggle buttons.
/// Toggle buttons maintain their pressed state when clicked.
/// </para>
/// </remarks>
public struct UIToolbarButton(Entity toolbar) : IComponent
{
    /// <summary>
    /// The parent toolbar entity.
    /// </summary>
    public Entity Toolbar = toolbar;

    /// <summary>
    /// Whether this button acts as a toggle (maintains pressed state).
    /// </summary>
    public bool IsToggle = false;

    /// <summary>
    /// Whether the button is currently pressed (for toggle buttons).
    /// </summary>
    public bool IsPressed = false;

    /// <summary>
    /// Tooltip text to display when hovering over the button.
    /// </summary>
    public string TooltipText = string.Empty;

    /// <summary>
    /// Index of this button within the toolbar.
    /// </summary>
    public int Index = 0;
}

/// <summary>
/// Component for a separator within a toolbar.
/// </summary>
/// <param name="toolbar">The parent toolbar entity.</param>
/// <remarks>
/// <para>
/// Separators provide visual grouping between toolbar buttons.
/// They appear as a thin line or space between button groups.
/// </para>
/// </remarks>
public struct UIToolbarSeparator(Entity toolbar) : IComponent
{
    /// <summary>
    /// The parent toolbar entity.
    /// </summary>
    public Entity Toolbar = toolbar;

    /// <summary>
    /// Index of this separator within the toolbar.
    /// </summary>
    public int Index = 0;
}

/// <summary>
/// Component for a status bar container at the bottom of a window or panel.
/// </summary>
/// <remarks>
/// <para>
/// A status bar displays information about the current state of an application
/// or document. It is typically divided into sections, some fixed-width and
/// some flexible.
/// </para>
/// </remarks>
public struct UIStatusBar : IComponent
{
    /// <summary>
    /// Number of sections in this status bar.
    /// </summary>
    public int SectionCount;
}

/// <summary>
/// Component for a section within a status bar.
/// </summary>
/// <param name="statusBar">The parent status bar entity.</param>
/// <param name="index">The index of this section.</param>
/// <remarks>
/// <para>
/// Each section can have a fixed width or be flexible to fill available space.
/// Sections are arranged left-to-right in the status bar.
/// </para>
/// </remarks>
public struct UIStatusBarSection(Entity statusBar, int index) : IComponent
{
    /// <summary>
    /// The parent status bar entity.
    /// </summary>
    public Entity StatusBar = statusBar;

    /// <summary>
    /// Index of this section in the status bar.
    /// </summary>
    public int Index = index;

    /// <summary>
    /// Fixed width of the section in pixels. Set to 0 for flexible width.
    /// </summary>
    public float Width = 0f;

    /// <summary>
    /// Whether this section flexes to fill available space.
    /// </summary>
    public bool IsFlexible = true;

    /// <summary>
    /// Minimum width for flexible sections.
    /// </summary>
    public float MinWidth = 50f;
}

/// <summary>
/// Tag for toolbar button groups (buttons that act as a radio group).
/// </summary>
public struct UIToolbarButtonGroupTag : ITagComponent;
