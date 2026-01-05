using System.Numerics;

namespace KeenEyes.Graph.Abstractions;

/// <summary>
/// Types of context menu that can be displayed.
/// </summary>
public enum ContextMenuType
{
    /// <summary>No menu displayed.</summary>
    None,
    /// <summary>Menu for creating nodes on the canvas.</summary>
    Canvas,
    /// <summary>Menu for node operations (delete, duplicate, etc.).</summary>
    Node,
    /// <summary>Menu for connection operations (delete).</summary>
    Connection
}

/// <summary>
/// Component that stores the state of a context menu on a graph canvas.
/// </summary>
public struct GraphContextMenu : IComponent
{
    /// <summary>The screen position where the menu was opened.</summary>
    public Vector2 ScreenPosition;

    /// <summary>The canvas position where the menu was opened.</summary>
    public Vector2 CanvasPosition;

    /// <summary>The type of menu being displayed.</summary>
    public ContextMenuType MenuType;

    /// <summary>The target entity (node or connection) if applicable.</summary>
    public Entity TargetEntity;

    /// <summary>The current search filter text.</summary>
    public string SearchFilter;

    /// <summary>The currently selected menu item index.</summary>
    public int SelectedIndex;

    /// <summary>
    /// Creates a default context menu component.
    /// </summary>
    public GraphContextMenu()
    {
        ScreenPosition = Vector2.Zero;
        CanvasPosition = Vector2.Zero;
        MenuType = ContextMenuType.None;
        TargetEntity = Entity.Null;
        SearchFilter = string.Empty;
        SelectedIndex = 0;
    }
}
