namespace KeenEyes.Graph.Abstractions;

/// <summary>
/// Current interaction mode for a graph canvas.
/// </summary>
public enum GraphInteractionMode
{
    /// <summary>No active interaction.</summary>
    None,

    /// <summary>User is panning the canvas.</summary>
    Panning,

    /// <summary>User is drawing a selection box.</summary>
    Selecting,

    /// <summary>User is dragging one or more nodes.</summary>
    DraggingNode,

    /// <summary>User is creating a connection by dragging from a port.</summary>
    ConnectingPort,

    /// <summary>Context menu is open.</summary>
    ContextMenu,

    /// <summary>User is duplicating nodes (Ctrl+D ghost preview).</summary>
    Duplicating
}
