namespace KeenEyes.TestBridge.AI;

/// <summary>
/// Snapshot of a behavior tree's current state.
/// </summary>
public sealed record BehaviorTreeSnapshot
{
    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public required int EntityId { get; init; }

    /// <summary>
    /// Gets whether the behavior tree is enabled.
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Gets whether the behavior tree is initialized.
    /// </summary>
    public required bool IsInitialized { get; init; }

    /// <summary>
    /// Gets the name of the behavior tree definition.
    /// </summary>
    public string? TreeName { get; init; }

    /// <summary>
    /// Gets the last execution result.
    /// </summary>
    public required string LastResult { get; init; }

    /// <summary>
    /// Gets the name of the currently running node, if any.
    /// </summary>
    public string? RunningNodeName { get; init; }

    /// <summary>
    /// Gets the type of the currently running node, if any.
    /// </summary>
    public string? RunningNodeType { get; init; }

    /// <summary>
    /// Gets the number of entries in the blackboard.
    /// </summary>
    public int BlackboardEntryCount { get; init; }
}

/// <summary>
/// Detailed snapshot of a behavior tree node.
/// </summary>
public sealed record BehaviorTreeNodeSnapshot
{
    /// <summary>
    /// Gets the node name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the node type (e.g., "Selector", "Sequence", "ActionNode").
    /// </summary>
    public required string NodeType { get; init; }

    /// <summary>
    /// Gets the last execution state.
    /// </summary>
    public required string LastState { get; init; }

    /// <summary>
    /// Gets the child nodes, if this is a composite or decorator node.
    /// </summary>
    public IReadOnlyList<BehaviorTreeNodeSnapshot>? Children { get; init; }
}
