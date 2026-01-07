using System.Numerics;
using KeenEyes.Editor.Abstractions;
using KeenEyes.Graph.Abstractions;

namespace KeenEyes.Graph.Commands;

/// <summary>
/// Command to duplicate one or more graph nodes.
/// </summary>
/// <remarks>
/// Duplicated nodes are offset from the originals and automatically selected.
/// Connections between duplicated nodes are also duplicated.
/// </remarks>
/// <param name="world">The world containing the nodes.</param>
/// <param name="graphContext">The graph context for node operations.</param>
/// <param name="nodes">The nodes to duplicate.</param>
/// <param name="duplicateOffset">Optional offset for duplicated nodes.</param>
public sealed class DuplicateNodesCommand(
    IWorld world,
    GraphContext graphContext,
    IEnumerable<Entity> nodes,
    Vector2? duplicateOffset = null) : IEditorCommand
{
    /// <summary>
    /// Default offset for duplicated nodes.
    /// </summary>
    public static readonly Vector2 DefaultOffset = new(20, 20);

    private readonly Entity[] originalNodes = nodes.Where(n => world.IsAlive(n) && world.Has<GraphNode>(n)).ToArray();
    private readonly Vector2 offset = duplicateOffset ?? DefaultOffset;
    private readonly List<Entity> duplicatedNodes = [];
    private readonly List<Entity> duplicatedConnections = [];

    /// <inheritdoc/>
    public string Description => originalNodes.Length == 1
        ? "Duplicate Node"
        : $"Duplicate {originalNodes.Length} Nodes";

    /// <summary>
    /// Gets the entities that were created by this command.
    /// </summary>
    public IReadOnlyList<Entity> DuplicatedNodes => duplicatedNodes;

    /// <inheritdoc/>
    public void Execute()
    {
        duplicatedNodes.Clear();
        duplicatedConnections.Clear();

        var originalToNew = new Dictionary<Entity, Entity>();

        // Duplicate each node
        foreach (var original in originalNodes)
        {
            if (!world.IsAlive(original))
            {
                continue;
            }

            ref readonly var nodeData = ref world.Get<GraphNode>(original);

            var newNode = graphContext.CreateNode(
                nodeData.Canvas,
                nodeData.NodeTypeId,
                nodeData.Position + offset,
                nodeData.DisplayName);

            originalToNew[original] = newNode;
            duplicatedNodes.Add(newNode);

            // Select the new node
            graphContext.SelectNode(newNode, addToSelection: true);
        }

        // Duplicate connections between duplicated nodes
        var originalSet = originalNodes.ToHashSet();
        foreach (var connectionEntity in world.Query<GraphConnection>())
        {
            ref readonly var conn = ref world.Get<GraphConnection>(connectionEntity);

            // Only duplicate connections where both endpoints were duplicated
            if (originalSet.Contains(conn.SourceNode) && originalSet.Contains(conn.TargetNode) &&
                originalToNew.TryGetValue(conn.SourceNode, out var newSource) &&
                originalToNew.TryGetValue(conn.TargetNode, out var newTarget))
            {
                var newConnection = graphContext.Connect(
                    newSource,
                    conn.SourcePortIndex,
                    newTarget,
                    conn.TargetPortIndex);

                if (newConnection.IsValid)
                {
                    duplicatedConnections.Add(newConnection);
                }
            }
        }

        // Deselect original nodes
        foreach (var original in originalNodes)
        {
            if (world.IsAlive(original))
            {
                graphContext.DeselectNode(original);
            }
        }
    }

    /// <inheritdoc/>
    public void Undo()
    {
        // Delete duplicated connections first
        foreach (var connection in duplicatedConnections)
        {
            if (world.IsAlive(connection))
            {
                graphContext.DeleteConnection(connection);
            }
        }

        duplicatedConnections.Clear();

        // Delete duplicated nodes
        foreach (var node in duplicatedNodes)
        {
            if (world.IsAlive(node))
            {
                graphContext.DeleteNode(node);
            }
        }

        duplicatedNodes.Clear();

        // Re-select original nodes
        foreach (var original in originalNodes)
        {
            if (world.IsAlive(original))
            {
                graphContext.SelectNode(original, addToSelection: true);
            }
        }
    }

    /// <inheritdoc/>
    public bool TryMerge(IEditorCommand other) => false;
}
