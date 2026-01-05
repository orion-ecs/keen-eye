using System.Numerics;
using KeenEyes.Editor.Abstractions;
using KeenEyes.Graph.Abstractions;

namespace KeenEyes.Graph.Commands;

/// <summary>
/// Command to create a new graph node.
/// </summary>
/// <param name="world">The world containing the graph.</param>
/// <param name="graphContext">The graph context for node operations.</param>
/// <param name="canvas">The canvas to add the node to.</param>
/// <param name="nodeTypeId">The node type ID from the port registry.</param>
/// <param name="position">The initial position in canvas coordinates.</param>
/// <param name="displayName">Optional display name for the node.</param>
public sealed class CreateNodeCommand(
    IWorld world,
    GraphContext graphContext,
    Entity canvas,
    int nodeTypeId,
    Vector2 position,
    string? displayName = null) : IEditorCommand
{
    private Entity createdNode;

    /// <inheritdoc/>
    public string Description => "Create Node";

    /// <summary>
    /// Gets the entity that was created by this command.
    /// </summary>
    public Entity CreatedNode => createdNode;

    /// <inheritdoc/>
    public void Execute()
    {
        createdNode = graphContext.CreateNode(canvas, nodeTypeId, position, displayName);
    }

    /// <inheritdoc/>
    public void Undo()
    {
        if (createdNode.IsValid && world.IsAlive(createdNode))
        {
            graphContext.DeleteNode(createdNode);
            createdNode = Entity.Null;
        }
    }

    /// <inheritdoc/>
    public bool TryMerge(IEditorCommand other) => false;
}
