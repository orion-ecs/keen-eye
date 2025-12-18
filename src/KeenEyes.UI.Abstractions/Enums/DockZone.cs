namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Specifies the zone where a panel can be docked.
/// </summary>
[Flags]
public enum DockZone
{
    /// <summary>
    /// No docking.
    /// </summary>
    None = 0,

    /// <summary>
    /// Left edge of the container.
    /// </summary>
    Left = 1,

    /// <summary>
    /// Right edge of the container.
    /// </summary>
    Right = 2,

    /// <summary>
    /// Top edge of the container.
    /// </summary>
    Top = 4,

    /// <summary>
    /// Bottom edge of the container.
    /// </summary>
    Bottom = 8,

    /// <summary>
    /// Center area (tabbed with other panels).
    /// </summary>
    Center = 16,

    /// <summary>
    /// All dock zones allowed.
    /// </summary>
    All = Left | Right | Top | Bottom | Center
}

/// <summary>
/// Specifies the current state of a dockable panel.
/// </summary>
public enum DockState : byte
{
    /// <summary>
    /// Panel is floating as a separate window.
    /// </summary>
    Floating = 0,

    /// <summary>
    /// Panel is docked in a dock zone.
    /// </summary>
    Docked = 1,

    /// <summary>
    /// Panel is hidden but can be shown via tab/button.
    /// </summary>
    AutoHide = 2,

    /// <summary>
    /// Panel is tabbed with other panels in the same zone.
    /// </summary>
    Tabbed = 3
}
