using KeenEyes.Editor.Abstractions;
using KeenEyes.Graph.Abstractions;

namespace KeenEyes.Graph.Commands;

/// <summary>
/// Command to delete a connection between two graph nodes.
/// </summary>
public sealed class DeleteConnectionCommand : IEditorCommand
{
    private readonly IWorld world;
    private readonly GraphContext graphContext;
    private readonly Entity sourceNode;
    private readonly int sourcePortIndex;
    private readonly Entity targetNode;
    private readonly int targetPortIndex;
    private Entity connectionToDelete;

    /// <summary>
    /// Creates a new delete connection command.
    /// </summary>
    /// <param name="world">The world containing the graph.</param>
    /// <param name="graphContext">The graph context for connection operations.</param>
    /// <param name="connection">The connection entity to delete.</param>
    public DeleteConnectionCommand(IWorld world, GraphContext graphContext, Entity connection)
    {
        this.world = world;
        this.graphContext = graphContext;
        connectionToDelete = connection;

        // Capture connection data for restoration
        if (world.IsAlive(connection) && world.Has<GraphConnection>(connection))
        {
            ref readonly var conn = ref world.Get<GraphConnection>(connection);
            sourceNode = conn.SourceNode;
            sourcePortIndex = conn.SourcePortIndex;
            targetNode = conn.TargetNode;
            targetPortIndex = conn.TargetPortIndex;
        }
    }

    /// <inheritdoc/>
    public string Description => "Delete Connection";

    /// <inheritdoc/>
    public void Execute()
    {
        if (connectionToDelete.IsValid && world.IsAlive(connectionToDelete))
        {
            graphContext.DeleteConnection(connectionToDelete);
        }
    }

    /// <inheritdoc/>
    public void Undo()
    {
        // Restore the connection if both nodes still exist
        if (world.IsAlive(sourceNode) && world.IsAlive(targetNode))
        {
            connectionToDelete = graphContext.Connect(sourceNode, sourcePortIndex, targetNode, targetPortIndex);
        }
    }

    /// <inheritdoc/>
    public bool TryMerge(IEditorCommand other) => false;
}
