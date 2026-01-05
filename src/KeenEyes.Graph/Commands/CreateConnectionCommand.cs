using KeenEyes.Editor.Abstractions;
using KeenEyes.Graph.Abstractions;

namespace KeenEyes.Graph.Commands;

/// <summary>
/// Command to create a connection between two graph nodes.
/// </summary>
/// <param name="world">The world containing the graph.</param>
/// <param name="graphContext">The graph context for connection operations.</param>
/// <param name="sourceNode">The source node entity.</param>
/// <param name="sourcePortIndex">The output port index on the source.</param>
/// <param name="targetNode">The target node entity.</param>
/// <param name="targetPortIndex">The input port index on the target.</param>
public sealed class CreateConnectionCommand(
    IWorld world,
    GraphContext graphContext,
    Entity sourceNode,
    int sourcePortIndex,
    Entity targetNode,
    int targetPortIndex) : IEditorCommand
{
    private Entity createdConnection;

    /// <inheritdoc/>
    public string Description => "Create Connection";

    /// <summary>
    /// Gets the entity that was created by this command.
    /// </summary>
    public Entity CreatedConnection => createdConnection;

    /// <inheritdoc/>
    public void Execute()
    {
        createdConnection = graphContext.Connect(sourceNode, sourcePortIndex, targetNode, targetPortIndex);
    }

    /// <inheritdoc/>
    public void Undo()
    {
        if (createdConnection.IsValid && world.IsAlive(createdConnection))
        {
            graphContext.DeleteConnection(createdConnection);
            createdConnection = Entity.Null;
        }
    }

    /// <inheritdoc/>
    public bool TryMerge(IEditorCommand other) => false;
}
