using System.Numerics;

namespace KeenEyes.Graph.Abstractions;

/// <summary>
/// Container component for a graph editing canvas.
/// </summary>
/// <remarks>
/// <para>
/// A graph canvas is the root entity for a graph node editor. It stores the pan/zoom
/// state and interaction mode. All graph nodes and connections are children of a canvas.
/// </para>
/// <para>
/// The coordinate system uses canvas coordinates where (0,0) is the initial center.
/// Pan and zoom transform between canvas and screen coordinates.
/// </para>
/// </remarks>
public struct GraphCanvas : IComponent
{
    /// <summary>
    /// The pan offset in canvas coordinates.
    /// </summary>
    public Vector2 Pan;

    /// <summary>
    /// The zoom level (1.0 = 100%).
    /// </summary>
    public float Zoom;

    /// <summary>
    /// The minimum allowed zoom level.
    /// </summary>
    public float MinZoom;

    /// <summary>
    /// The maximum allowed zoom level.
    /// </summary>
    public float MaxZoom;

    /// <summary>
    /// The grid spacing in canvas units.
    /// </summary>
    public float GridSize;

    /// <summary>
    /// Whether to snap nodes to the grid when dragging.
    /// </summary>
    public bool SnapToGrid;

    /// <summary>
    /// The current interaction mode.
    /// </summary>
    public GraphInteractionMode Mode;

    /// <summary>
    /// Creates a canvas with default settings.
    /// </summary>
    public static GraphCanvas Default => new()
    {
        Pan = Vector2.Zero,
        Zoom = 1.0f,
        MinZoom = 0.25f,
        MaxZoom = 4.0f,
        GridSize = 20f,
        SnapToGrid = false,
        Mode = GraphInteractionMode.None
    };
}
