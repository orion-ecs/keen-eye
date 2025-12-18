using System.Numerics;

namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Component that identifies a floating window container.
/// </summary>
/// <remarks>
/// <para>
/// Windows are floating panels that can be dragged by their title bar and exist
/// outside the normal layout hierarchy. They are positioned absolutely using their
/// <see cref="UIRect"/> position.
/// </para>
/// <para>
/// The window system handles drag events on the title bar to move
/// the window, and click events on the close button to hide or destroy it.
/// </para>
/// </remarks>
/// <param name="title">The window title displayed in the title bar.</param>
public struct UIWindow(string title) : IComponent
{
    /// <summary>
    /// The window title displayed in the title bar.
    /// </summary>
    public string Title = title;

    /// <summary>
    /// Whether the window can be dragged by its title bar.
    /// </summary>
    public bool CanDrag = true;

    /// <summary>
    /// Whether the window can be resized.
    /// </summary>
    public bool CanResize = false;

    /// <summary>
    /// Whether the window has a close button.
    /// </summary>
    public bool CanClose = true;

    /// <summary>
    /// The current z-order of this window. Higher values are drawn on top.
    /// </summary>
    public int ZOrder = 0;

    /// <summary>
    /// The minimum size the window can be resized to.
    /// </summary>
    public Vector2 MinSize = new(100, 50);

    /// <summary>
    /// The maximum size the window can be resized to. Zero means no limit.
    /// </summary>
    public Vector2 MaxSize = Vector2.Zero;
}

/// <summary>
/// Component that identifies a window's title bar for drag handling.
/// </summary>
/// <param name="window">The window entity this title bar belongs to.</param>
public struct UIWindowTitleBar(Entity window) : IComponent
{
    /// <summary>
    /// Reference to the window entity.
    /// </summary>
    public Entity Window = window;
}

/// <summary>
/// Component that identifies a window's close button.
/// </summary>
/// <param name="window">The window entity this close button belongs to.</param>
public struct UIWindowCloseButton(Entity window) : IComponent
{
    /// <summary>
    /// Reference to the window entity.
    /// </summary>
    public Entity Window = window;
}

/// <summary>
/// Component that identifies a window's resize handle.
/// </summary>
/// <param name="window">The window entity this resize handle belongs to.</param>
/// <param name="edge">Which edge/corner this handle controls.</param>
public struct UIWindowResizeHandle(Entity window, ResizeEdge edge) : IComponent
{
    /// <summary>
    /// Reference to the window entity.
    /// </summary>
    public Entity Window = window;

    /// <summary>
    /// Which edge or corner this handle controls.
    /// </summary>
    public ResizeEdge Edge = edge;
}

/// <summary>
/// Specifies which edge or corner a resize handle controls.
/// </summary>
[Flags]
public enum ResizeEdge
{
    /// <summary>No edge.</summary>
    None = 0,

    /// <summary>Top edge.</summary>
    Top = 1,

    /// <summary>Bottom edge.</summary>
    Bottom = 2,

    /// <summary>Left edge.</summary>
    Left = 4,

    /// <summary>Right edge.</summary>
    Right = 8,

    /// <summary>Top-left corner.</summary>
    TopLeft = Top | Left,

    /// <summary>Top-right corner.</summary>
    TopRight = Top | Right,

    /// <summary>Bottom-left corner.</summary>
    BottomLeft = Bottom | Left,

    /// <summary>Bottom-right corner.</summary>
    BottomRight = Bottom | Right
}
