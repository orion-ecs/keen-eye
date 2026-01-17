namespace KeenEyes.TestBridge.AI;

/// <summary>
/// Statistics about AI usage in the world.
/// </summary>
public sealed record AIStatisticsSnapshot
{
    /// <summary>
    /// Gets the total number of entities with state machine components.
    /// </summary>
    public int StateMachineCount { get; init; }

    /// <summary>
    /// Gets the number of enabled state machines.
    /// </summary>
    public int ActiveStateMachineCount { get; init; }

    /// <summary>
    /// Gets the total number of entities with behavior tree components.
    /// </summary>
    public int BehaviorTreeCount { get; init; }

    /// <summary>
    /// Gets the number of enabled behavior trees.
    /// </summary>
    public int ActiveBehaviorTreeCount { get; init; }

    /// <summary>
    /// Gets the total number of entities with utility AI components.
    /// </summary>
    public int UtilityAICount { get; init; }

    /// <summary>
    /// Gets the number of enabled utility AI systems.
    /// </summary>
    public int ActiveUtilityAICount { get; init; }

    /// <summary>
    /// Gets the total number of AI components.
    /// </summary>
    public int TotalCount => StateMachineCount + BehaviorTreeCount + UtilityAICount;

    /// <summary>
    /// Gets the total number of active AI components.
    /// </summary>
    public int TotalActiveCount => ActiveStateMachineCount + ActiveBehaviorTreeCount + ActiveUtilityAICount;
}
