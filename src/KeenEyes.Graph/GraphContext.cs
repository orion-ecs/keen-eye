using System.Numerics;
using KeenEyes.Graph.Abstractions;

namespace KeenEyes.Graph;

/// <summary>
/// Provides graph node editor state and manipulation methods.
/// </summary>
/// <remarks>
/// <para>
/// GraphContext is registered as a world extension by the GraphPlugin. It provides
/// methods for creating and manipulating graph canvases, nodes, and connections.
/// </para>
/// <para>
/// Access the context via <c>world.GetExtension&lt;GraphContext&gt;()</c> after installing
/// the Graph plugin.
/// </para>
/// </remarks>
[PluginExtension("Graph")]
public sealed class GraphContext
{
    private readonly IWorld world;
    private readonly PortRegistry portRegistry;

    internal GraphContext(IWorld world, PortRegistry portRegistry)
    {
        this.world = world;
        this.portRegistry = portRegistry;
    }

    /// <summary>
    /// Gets the port registry for node type definitions.
    /// </summary>
    public PortRegistry PortRegistry => portRegistry;

    /// <summary>
    /// Creates a new graph canvas.
    /// </summary>
    /// <param name="name">Optional name for the canvas entity.</param>
    /// <returns>The canvas entity.</returns>
    public Entity CreateCanvas(string? name = null)
    {
        var builder = name is not null ? world.Spawn(name) : world.Spawn();

        return builder
            .With(GraphCanvas.Default)
            .With(new GraphCanvasTag())
            .Build();
    }

    /// <summary>
    /// Creates a new node on the specified canvas.
    /// </summary>
    /// <param name="canvas">The parent canvas entity.</param>
    /// <param name="nodeTypeId">The node type ID from the port registry.</param>
    /// <param name="position">The initial position in canvas coordinates.</param>
    /// <param name="displayName">Optional display name override.</param>
    /// <returns>The node entity.</returns>
    /// <exception cref="ArgumentException">Thrown if canvas is invalid or nodeTypeId is not registered.</exception>
    public Entity CreateNode(Entity canvas, int nodeTypeId, Vector2 position, string? displayName = null)
    {
        if (!world.IsAlive(canvas))
        {
            throw new ArgumentException("Canvas entity is not valid.", nameof(canvas));
        }

        if (!world.Has<GraphCanvas>(canvas))
        {
            throw new ArgumentException("Entity is not a graph canvas.", nameof(canvas));
        }

        if (!portRegistry.IsRegistered(nodeTypeId))
        {
            throw new ArgumentException($"Node type {nodeTypeId} is not registered.", nameof(nodeTypeId));
        }

        var node = world.Spawn()
            .With(new GraphNode
            {
                Position = position,
                Width = 200f,
                Height = 0f, // Calculated by layout
                NodeTypeId = nodeTypeId,
                Canvas = canvas,
                DisplayName = displayName
            })
            .Build();

        world.SetParent(node, canvas);
        return node;
    }

    /// <summary>
    /// Creates a connection between two nodes.
    /// </summary>
    /// <param name="sourceNode">The source node entity.</param>
    /// <param name="sourcePortIndex">The output port index on the source.</param>
    /// <param name="targetNode">The target node entity.</param>
    /// <param name="targetPortIndex">The input port index on the target.</param>
    /// <returns>The connection entity, or Entity.Null if connection is invalid.</returns>
    public Entity Connect(Entity sourceNode, int sourcePortIndex, Entity targetNode, int targetPortIndex)
    {
        // Validate entities
        if (!world.IsAlive(sourceNode) || !world.Has<GraphNode>(sourceNode))
        {
            return Entity.Null;
        }

        if (!world.IsAlive(targetNode) || !world.Has<GraphNode>(targetNode))
        {
            return Entity.Null;
        }

        ref readonly var source = ref world.Get<GraphNode>(sourceNode);
        ref readonly var target = ref world.Get<GraphNode>(targetNode);

        // Ensure same canvas
        if (source.Canvas != target.Canvas)
        {
            return Entity.Null;
        }

        // Validate ports exist
        if (!portRegistry.TryGetNodeType(source.NodeTypeId, out var sourceType) ||
            !portRegistry.TryGetNodeType(target.NodeTypeId, out var targetType))
        {
            return Entity.Null;
        }

        if (sourcePortIndex < 0 || sourcePortIndex >= sourceType.OutputPorts.Length)
        {
            return Entity.Null;
        }

        if (targetPortIndex < 0 || targetPortIndex >= targetType.InputPorts.Length)
        {
            return Entity.Null;
        }

        // Check type compatibility
        var sourcePort = sourceType.OutputPorts[sourcePortIndex];
        var targetPort = targetType.InputPorts[targetPortIndex];

        if (!PortTypeCompatibility.CanConnect(sourcePort.TypeId, targetPort.TypeId))
        {
            return Entity.Null;
        }

        // Create connection
        var connection = world.Spawn()
            .With(GraphConnection.Create(sourceNode, sourcePortIndex, targetNode, targetPortIndex, source.Canvas))
            .Build();

        world.SetParent(connection, source.Canvas);
        return connection;
    }

    /// <summary>
    /// Deletes a node and all its connections.
    /// </summary>
    /// <param name="node">The node entity to delete.</param>
    public void DeleteNode(Entity node)
    {
        if (!world.IsAlive(node) || !world.Has<GraphNode>(node))
        {
            return;
        }

        // Find and delete all connections involving this node
        foreach (var connectionEntity in world.Query<GraphConnection>())
        {
            ref readonly var conn = ref world.Get<GraphConnection>(connectionEntity);
            if (conn.SourceNode == node || conn.TargetNode == node)
            {
                world.Despawn(connectionEntity);
            }
        }

        // Delete the node
        world.Despawn(node);
    }

    /// <summary>
    /// Deletes a connection.
    /// </summary>
    /// <param name="connection">The connection entity to delete.</param>
    public void DeleteConnection(Entity connection)
    {
        if (!world.IsAlive(connection) || !world.Has<GraphConnection>(connection))
        {
            return;
        }

        world.Despawn(connection);
    }

    /// <summary>
    /// Selects a node.
    /// </summary>
    /// <param name="node">The node to select.</param>
    /// <param name="addToSelection">If true, adds to selection; if false, replaces selection.</param>
    public void SelectNode(Entity node, bool addToSelection = false)
    {
        if (!world.IsAlive(node) || !world.Has<GraphNode>(node))
        {
            return;
        }

        if (!addToSelection)
        {
            ClearSelection();
        }

        if (!world.Has<GraphNodeSelectedTag>(node))
        {
            world.Add(node, new GraphNodeSelectedTag());
        }
    }

    /// <summary>
    /// Deselects a node.
    /// </summary>
    /// <param name="node">The node to deselect.</param>
    public void DeselectNode(Entity node)
    {
        if (!world.IsAlive(node))
        {
            return;
        }

        if (world.Has<GraphNodeSelectedTag>(node))
        {
            world.Remove<GraphNodeSelectedTag>(node);
        }
    }

    /// <summary>
    /// Clears all selections on all canvases.
    /// </summary>
    public void ClearSelection()
    {
        // Collect entities first to avoid modifying during iteration
        var selectedNodes = world.Query<GraphNodeSelectedTag>().ToList();
        foreach (var entity in selectedNodes)
        {
            world.Remove<GraphNodeSelectedTag>(entity);
        }

        var selectedConnections = world.Query<GraphConnectionSelectedTag>().ToList();
        foreach (var entity in selectedConnections)
        {
            world.Remove<GraphConnectionSelectedTag>(entity);
        }
    }

    /// <summary>
    /// Gets all selected nodes.
    /// </summary>
    /// <returns>An enumerable of selected node entities.</returns>
    public IEnumerable<Entity> GetSelectedNodes()
    {
        foreach (var entity in world.Query<GraphNode, GraphNodeSelectedTag>())
        {
            yield return entity;
        }
    }

    /// <summary>
    /// Converts screen coordinates to canvas coordinates.
    /// </summary>
    /// <param name="canvas">The canvas entity.</param>
    /// <param name="screenPos">The screen position.</param>
    /// <param name="canvasOrigin">The screen position of the canvas origin.</param>
    /// <returns>The canvas position.</returns>
    public Vector2 ScreenToCanvas(Entity canvas, Vector2 screenPos, Vector2 canvasOrigin)
    {
        if (!world.IsAlive(canvas) || !world.Has<GraphCanvas>(canvas))
        {
            return screenPos;
        }

        ref readonly var canvasData = ref world.Get<GraphCanvas>(canvas);
        return GraphTransform.ScreenToCanvas(screenPos, canvasData.Pan, canvasData.Zoom, canvasOrigin);
    }

    /// <summary>
    /// Converts canvas coordinates to screen coordinates.
    /// </summary>
    /// <param name="canvas">The canvas entity.</param>
    /// <param name="canvasPos">The canvas position.</param>
    /// <param name="canvasOrigin">The screen position of the canvas origin.</param>
    /// <returns>The screen position.</returns>
    public Vector2 CanvasToScreen(Entity canvas, Vector2 canvasPos, Vector2 canvasOrigin)
    {
        if (!world.IsAlive(canvas) || !world.Has<GraphCanvas>(canvas))
        {
            return canvasPos;
        }

        ref readonly var canvasData = ref world.Get<GraphCanvas>(canvas);
        return GraphTransform.CanvasToScreen(canvasPos, canvasData.Pan, canvasData.Zoom, canvasOrigin);
    }
}
