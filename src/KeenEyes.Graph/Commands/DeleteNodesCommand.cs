using System.Numerics;
using KeenEyes.Editor.Abstractions;
using KeenEyes.Graph.Abstractions;

namespace KeenEyes.Graph.Commands;

/// <summary>
/// Command to delete one or more graph nodes and their connections.
/// </summary>
/// <remarks>
/// This command captures full node and connection state for restoration on undo.
/// </remarks>
public sealed class DeleteNodesCommand : IEditorCommand
{
    private readonly IWorld world;
    private readonly GraphContext graphContext;
    private readonly NodeSnapshot[] nodeSnapshots;
    private readonly ConnectionSnapshot[] connectionSnapshots;

    /// <summary>
    /// Snapshot of a node's state for restoration.
    /// </summary>
    private readonly struct NodeSnapshot
    {
        public required GraphNode NodeData { get; init; }
        public required Entity Canvas { get; init; }
        public Entity RestoredEntity { get; init; }

        public NodeSnapshot WithRestoredEntity(Entity entity) => new()
        {
            NodeData = NodeData,
            Canvas = Canvas,
            RestoredEntity = entity
        };
    }

    /// <summary>
    /// Snapshot of a connection's state for restoration.
    /// </summary>
    private readonly struct ConnectionSnapshot
    {
        public required Entity SourceNode { get; init; }
        public required int SourcePortIndex { get; init; }
        public required Entity TargetNode { get; init; }
        public required int TargetPortIndex { get; init; }
        public required Entity Canvas { get; init; }
    }

    /// <summary>
    /// Creates a new delete nodes command.
    /// </summary>
    /// <param name="world">The world containing the nodes.</param>
    /// <param name="graphContext">The graph context for node operations.</param>
    /// <param name="nodes">The nodes to delete.</param>
    public DeleteNodesCommand(IWorld world, GraphContext graphContext, IEnumerable<Entity> nodes)
    {
        this.world = world;
        this.graphContext = graphContext;

        var nodeList = nodes.ToList();
        var nodeSet = nodeList.ToHashSet();

        // Capture node snapshots
        nodeSnapshots = nodeList
            .Where(n => world.IsAlive(n) && world.Has<GraphNode>(n))
            .Select(n => new NodeSnapshot
            {
                NodeData = world.Get<GraphNode>(n),
                Canvas = world.Get<GraphNode>(n).Canvas,
                RestoredEntity = Entity.Null
            })
            .ToArray();

        // Capture connections that involve any of the nodes
        var connections = new List<ConnectionSnapshot>();
        foreach (var connectionEntity in world.Query<GraphConnection>())
        {
            ref readonly var conn = ref world.Get<GraphConnection>(connectionEntity);
            if (nodeSet.Contains(conn.SourceNode) || nodeSet.Contains(conn.TargetNode))
            {
                connections.Add(new ConnectionSnapshot
                {
                    SourceNode = conn.SourceNode,
                    SourcePortIndex = conn.SourcePortIndex,
                    TargetNode = conn.TargetNode,
                    TargetPortIndex = conn.TargetPortIndex,
                    Canvas = conn.Canvas
                });
            }
        }

        connectionSnapshots = [.. connections];
    }

    /// <inheritdoc/>
    public string Description => nodeSnapshots.Length == 1
        ? "Delete Node"
        : $"Delete {nodeSnapshots.Length} Nodes";

    /// <inheritdoc/>
    public void Execute()
    {
        // Delete connections first (they reference nodes)
        foreach (var connSnapshot in connectionSnapshots)
        {
            // Find and delete the connection entity
            foreach (var connectionEntity in world.Query<GraphConnection>())
            {
                ref readonly var conn = ref world.Get<GraphConnection>(connectionEntity);
                if (conn.SourceNode == connSnapshot.SourceNode &&
                    conn.SourcePortIndex == connSnapshot.SourcePortIndex &&
                    conn.TargetNode == connSnapshot.TargetNode &&
                    conn.TargetPortIndex == connSnapshot.TargetPortIndex)
                {
                    world.Despawn(connectionEntity);
                    break;
                }
            }
        }

        // Delete nodes
        foreach (var snapshot in nodeSnapshots)
        {
            // Find the node by matching position and type (since we don't store original entity)
            foreach (var nodeEntity in world.Query<GraphNode>())
            {
                ref readonly var node = ref world.Get<GraphNode>(nodeEntity);
                if (node.Position == snapshot.NodeData.Position &&
                    node.NodeTypeId == snapshot.NodeData.NodeTypeId &&
                    node.Canvas == snapshot.Canvas)
                {
                    world.Despawn(nodeEntity);
                    break;
                }
            }
        }
    }

    /// <inheritdoc/>
    public void Undo()
    {
        // Restore nodes first
        var restoredNodes = new Dictionary<(Vector2 Position, int TypeId), Entity>();

        for (int i = 0; i < nodeSnapshots.Length; i++)
        {
            var snapshot = nodeSnapshots[i];

            // Recreate the node (DisplayName is already in NodeData)
            var restoredEntity = graphContext.CreateNode(
                snapshot.Canvas,
                snapshot.NodeData.NodeTypeId,
                snapshot.NodeData.Position,
                snapshot.NodeData.DisplayName);

            // Track for connection restoration
            restoredNodes[(snapshot.NodeData.Position, snapshot.NodeData.NodeTypeId)] = restoredEntity;
            nodeSnapshots[i] = snapshot.WithRestoredEntity(restoredEntity);
        }

        // Restore connections
        foreach (var connSnapshot in connectionSnapshots)
        {
            // Map old entities to restored entities
            var sourceEntity = FindRestoredNode(connSnapshot.SourceNode, restoredNodes);
            var targetEntity = FindRestoredNode(connSnapshot.TargetNode, restoredNodes);

            if (sourceEntity.IsValid && targetEntity.IsValid)
            {
                graphContext.Connect(
                    sourceEntity,
                    connSnapshot.SourcePortIndex,
                    targetEntity,
                    connSnapshot.TargetPortIndex);
            }
        }
    }

    private Entity FindRestoredNode(
        Entity originalEntity,
        Dictionary<(Vector2 Position, int TypeId), Entity> restoredNodes)
    {
        // First check if the original entity still exists (wasn't deleted)
        if (world.IsAlive(originalEntity) && world.Has<GraphNode>(originalEntity))
        {
            return originalEntity;
        }

        // Find the restored entity by matching the snapshot
        foreach (var snapshot in nodeSnapshots)
        {
            // Match by position and type since original entities are gone
            if (snapshot.RestoredEntity.IsValid)
            {
                var key = (snapshot.NodeData.Position, snapshot.NodeData.NodeTypeId);
                if (restoredNodes.TryGetValue(key, out var restored))
                {
                    // Check if this matches the original entity's data
                    // We need to find the right restored node
                    return restored;
                }
            }
        }

        return Entity.Null;
    }

    /// <inheritdoc/>
    public bool TryMerge(IEditorCommand other) => false;
}
