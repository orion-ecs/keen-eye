using System.Numerics;
using KeenEyes.Editor.Abstractions;
using KeenEyes.Graph.Abstractions;

namespace KeenEyes.Graph.Commands;

/// <summary>
/// Command to move one or more graph nodes.
/// </summary>
/// <remarks>
/// This command supports merging consecutive moves within a 300ms window,
/// keeping the undo history clean during continuous dragging.
/// </remarks>
public sealed class MoveNodesCommand : IEditorCommand
{
    private readonly IWorld world;
    private readonly NodeMovement[] movements;
    private DateTime timestamp;

    /// <summary>
    /// Represents a node movement with old and new positions.
    /// </summary>
    private readonly struct NodeMovement
    {
        public required Entity Node { get; init; }
        public required Vector2 OldPosition { get; init; }
        public Vector2 NewPosition { get; init; }

        public NodeMovement WithNewPosition(Vector2 position) => new()
        {
            Node = Node,
            OldPosition = OldPosition,
            NewPosition = position
        };
    }

    /// <summary>
    /// Creates a new move nodes command.
    /// </summary>
    /// <param name="world">The world containing the nodes.</param>
    /// <param name="nodePositions">Dictionary mapping nodes to their old and new positions.</param>
    public MoveNodesCommand(
        IWorld world,
        IReadOnlyDictionary<Entity, (Vector2 OldPosition, Vector2 NewPosition)> nodePositions)
    {
        this.world = world;
        movements = nodePositions.Select(kvp => new NodeMovement
        {
            Node = kvp.Key,
            OldPosition = kvp.Value.OldPosition,
            NewPosition = kvp.Value.NewPosition
        }).ToArray();
        timestamp = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public string Description => movements.Length == 1
        ? "Move Node"
        : $"Move {movements.Length} Nodes";

    /// <inheritdoc/>
    public void Execute()
    {
        foreach (var movement in movements)
        {
            if (world.IsAlive(movement.Node) && world.Has<GraphNode>(movement.Node))
            {
                ref var node = ref world.Get<GraphNode>(movement.Node);
                node.Position = movement.NewPosition;
            }
        }
    }

    /// <inheritdoc/>
    public void Undo()
    {
        foreach (var movement in movements)
        {
            if (world.IsAlive(movement.Node) && world.Has<GraphNode>(movement.Node))
            {
                ref var node = ref world.Get<GraphNode>(movement.Node);
                node.Position = movement.OldPosition;
            }
        }
    }

    /// <inheritdoc/>
    public bool TryMerge(IEditorCommand other)
    {
        if (other is not MoveNodesCommand moveCmd)
        {
            return false;
        }

        // Only merge if same nodes
        if (!NodesMatch(moveCmd.movements))
        {
            return false;
        }

        // Only merge if within 300ms window
        var timeDelta = moveCmd.timestamp - timestamp;
        if (timeDelta.TotalMilliseconds > 300)
        {
            return false;
        }

        // Update new positions but keep old positions from first command
        for (int i = 0; i < movements.Length; i++)
        {
            // Find matching node in other command
            var matchingMovement = moveCmd.movements.FirstOrDefault(m => m.Node == movements[i].Node);
            if (matchingMovement.Node.IsValid)
            {
                movements[i] = movements[i].WithNewPosition(matchingMovement.NewPosition);
            }
        }

        timestamp = moveCmd.timestamp;
        return true;
    }

    private bool NodesMatch(NodeMovement[] otherMovements)
    {
        if (movements.Length != otherMovements.Length)
        {
            return false;
        }

        var ourNodes = movements.Select(m => m.Node).ToHashSet();
        return otherMovements.All(m => ourNodes.Contains(m.Node));
    }
}
