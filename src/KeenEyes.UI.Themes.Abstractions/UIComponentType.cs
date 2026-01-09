namespace KeenEyes.UI.Themes.Abstractions;

/// <summary>
/// Identifies the type of UI component for theme styling purposes.
/// </summary>
public enum UIComponentType
{
    /// <summary>
    /// No specific component type; uses default styling.
    /// </summary>
    None = 0,

    /// <summary>
    /// A button component.
    /// </summary>
    Button,

    /// <summary>
    /// A panel or container component.
    /// </summary>
    Panel,

    /// <summary>
    /// A text input field component.
    /// </summary>
    Input,

    /// <summary>
    /// A menu component (dropdown, context menu).
    /// </summary>
    Menu,

    /// <summary>
    /// A menu item within a menu.
    /// </summary>
    MenuItem,

    /// <summary>
    /// A modal or dialog component.
    /// </summary>
    Modal,

    /// <summary>
    /// A scrollbar track component.
    /// </summary>
    ScrollbarTrack,

    /// <summary>
    /// A scrollbar thumb component.
    /// </summary>
    ScrollbarThumb,

    /// <summary>
    /// A tooltip component.
    /// </summary>
    Tooltip
}
