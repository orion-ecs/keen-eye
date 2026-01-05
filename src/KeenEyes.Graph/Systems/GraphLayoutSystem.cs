using System.Numerics;
using KeenEyes.Graph.Abstractions;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph;

/// <summary>
/// System that calculates node dimensions and port positions.
/// </summary>
/// <remarks>
/// <para>
/// This system updates the <see cref="GraphNode.Height"/> based on the number of
/// ports, and calculates world positions for each port for use in rendering and
/// hit testing.
/// </para>
/// <para>
/// Port positions are cached in the <see cref="PortPositionCache"/> for efficient
/// lookup during rendering and connection creation.
/// </para>
/// </remarks>
public sealed class GraphLayoutSystem : SystemBase
{
    private PortRegistry? portRegistry;

    /// <summary>
    /// Height of the node header/title bar in pixels.
    /// </summary>
    public const float HeaderHeight = 30f;

    /// <summary>
    /// Height per port row in pixels.
    /// </summary>
    public const float PortRowHeight = 25f;

    /// <summary>
    /// Padding below the last port in pixels.
    /// </summary>
    public const float BottomPadding = 10f;

    /// <summary>
    /// Radius of port circles in pixels.
    /// </summary>
    public const float PortRadius = 6f;

    /// <summary>
    /// Minimum node height in pixels.
    /// </summary>
    public const float MinNodeHeight = 60f;

    /// <summary>
    /// Gets the port position cache, updated each frame.
    /// </summary>
    public PortPositionCache PortCache { get; } = new();

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Lazy initialization
        if (portRegistry is null && !World.TryGetExtension(out portRegistry))
        {
            return;
        }

        // Clear cache from previous frame
        PortCache.Clear();

        // Process each node
        foreach (var node in World.Query<GraphNode>())
        {
            CalculateNodeLayout(node);
        }
    }

    private void CalculateNodeLayout(Entity node)
    {
        ref var nodeData = ref World.Get<GraphNode>(node);

        if (!portRegistry!.TryGetNodeType(nodeData.NodeTypeId, out var nodeType))
        {
            // Unknown node type - use minimum size
            nodeData.Height = MinNodeHeight;
            return;
        }

        // Calculate height based on port count
        var inputCount = nodeType.InputPorts.Length;
        var outputCount = nodeType.OutputPorts.Length;
        var maxPortCount = Math.Max(inputCount, outputCount);

        nodeData.Height = HeaderHeight + (maxPortCount * PortRowHeight) + BottomPadding;
        nodeData.Height = Math.Max(nodeData.Height, MinNodeHeight);

        // Calculate port positions
        CalculatePortPositions(node, ref nodeData, nodeType);
    }

    private void CalculatePortPositions(Entity node, ref GraphNode nodeData, PortRegistry.NodeTypeInfo nodeType)
    {
        var nodeOrigin = nodeData.Position;

        // Input ports (left edge)
        for (int i = 0; i < nodeType.InputPorts.Length; i++)
        {
            var yOffset = HeaderHeight + (i * PortRowHeight) + (PortRowHeight / 2);
            var position = nodeOrigin + new Vector2(0, yOffset);
            PortCache.SetPortPosition(node, PortDirection.Input, i, position);
        }

        // Output ports (right edge)
        for (int i = 0; i < nodeType.OutputPorts.Length; i++)
        {
            var yOffset = HeaderHeight + (i * PortRowHeight) + (PortRowHeight / 2);
            var position = nodeOrigin + new Vector2(nodeData.Width, yOffset);
            PortCache.SetPortPosition(node, PortDirection.Output, i, position);
        }
    }
}

/// <summary>
/// Caches calculated port world positions for efficient lookup.
/// </summary>
public sealed class PortPositionCache
{
    private readonly Dictionary<PortKey, Vector2> positions = [];

    /// <summary>
    /// Key for identifying a specific port.
    /// </summary>
    public readonly record struct PortKey(Entity Node, PortDirection Direction, int PortIndex);

    /// <summary>
    /// Sets the world position for a port.
    /// </summary>
    /// <param name="node">The node entity.</param>
    /// <param name="direction">The port direction.</param>
    /// <param name="portIndex">The port index.</param>
    /// <param name="position">The world position.</param>
    public void SetPortPosition(Entity node, PortDirection direction, int portIndex, Vector2 position)
    {
        positions[new PortKey(node, direction, portIndex)] = position;
    }

    /// <summary>
    /// Gets the world position for a port.
    /// </summary>
    /// <param name="node">The node entity.</param>
    /// <param name="direction">The port direction.</param>
    /// <param name="portIndex">The port index.</param>
    /// <returns>The world position, or Vector2.Zero if not found.</returns>
    public Vector2 GetPortPosition(Entity node, PortDirection direction, int portIndex)
    {
        return positions.TryGetValue(new PortKey(node, direction, portIndex), out var pos) ? pos : Vector2.Zero;
    }

    /// <summary>
    /// Tries to get the world position for a port.
    /// </summary>
    /// <param name="node">The node entity.</param>
    /// <param name="direction">The port direction.</param>
    /// <param name="portIndex">The port index.</param>
    /// <param name="position">The world position if found.</param>
    /// <returns>True if the position was found.</returns>
    public bool TryGetPortPosition(Entity node, PortDirection direction, int portIndex, out Vector2 position)
    {
        return positions.TryGetValue(new PortKey(node, direction, portIndex), out position);
    }

    /// <summary>
    /// Clears all cached positions.
    /// </summary>
    public void Clear()
    {
        positions.Clear();
    }

    /// <summary>
    /// Gets the number of cached port positions.
    /// </summary>
    public int Count => positions.Count;
}
