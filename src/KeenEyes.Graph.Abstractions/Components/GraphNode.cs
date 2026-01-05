using System.Numerics;

namespace KeenEyes.Graph.Abstractions;

/// <summary>
/// Component for a graph node entity.
/// </summary>
/// <remarks>
/// <para>
/// Graph nodes are positioned in canvas coordinates and rendered as rounded rectangles
/// with a title bar and ports. The node type determines available ports via the
/// port registry.
/// </para>
/// <para>
/// Node height is calculated dynamically based on the number of ports. Width is
/// fixed but can be customized per node type.
/// </para>
/// </remarks>
public struct GraphNode : IComponent
{
    /// <summary>
    /// Position in canvas coordinates (top-left corner).
    /// </summary>
    public Vector2 Position;

    /// <summary>
    /// Width of the node in canvas units.
    /// </summary>
    public float Width;

    /// <summary>
    /// Height of the node in canvas units (calculated by layout system).
    /// </summary>
    public float Height;

    /// <summary>
    /// The node type ID for port registry lookup.
    /// </summary>
    public int NodeTypeId;

    /// <summary>
    /// Reference to the parent canvas entity.
    /// </summary>
    public Entity Canvas;

    /// <summary>
    /// Display name for this node instance.
    /// </summary>
    /// <remarks>
    /// If null, the node type name from the registry is used.
    /// </remarks>
    public string? DisplayName;

    /// <summary>
    /// Creates a node with default settings at the specified position.
    /// </summary>
    /// <param name="position">The canvas position.</param>
    /// <param name="nodeTypeId">The node type ID.</param>
    /// <param name="canvas">The parent canvas entity.</param>
    /// <returns>A new graph node component.</returns>
    public static GraphNode Create(Vector2 position, int nodeTypeId, Entity canvas)
    {
        return new()
        {
            Position = position,
            Width = 200f,
            Height = 0f, // Calculated by layout
            NodeTypeId = nodeTypeId,
            Canvas = canvas,
            DisplayName = null
        };
    }
}
