namespace KeenEyes.TestBridge.AI;

/// <summary>
/// Snapshot of a utility AI's current state.
/// </summary>
public sealed record UtilityAISnapshot
{
    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public required int EntityId { get; init; }

    /// <summary>
    /// Gets whether the utility AI is enabled.
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Gets whether the utility AI is initialized.
    /// </summary>
    public required bool IsInitialized { get; init; }

    /// <summary>
    /// Gets the name of the utility AI definition.
    /// </summary>
    public string? AIName { get; init; }

    /// <summary>
    /// Gets the name of the currently executing action.
    /// </summary>
    public string? CurrentActionName { get; init; }

    /// <summary>
    /// Gets the selection mode (HighestScore, WeightedRandom, TopN).
    /// </summary>
    public required string SelectionMode { get; init; }

    /// <summary>
    /// Gets the evaluation interval in seconds.
    /// </summary>
    public required float EvaluationInterval { get; init; }

    /// <summary>
    /// Gets the time since last evaluation in seconds.
    /// </summary>
    public required float TimeSinceEvaluation { get; init; }

    /// <summary>
    /// Gets the selection threshold (actions below this score are ignored).
    /// </summary>
    public required float SelectionThreshold { get; init; }

    /// <summary>
    /// Gets the total number of actions available.
    /// </summary>
    public required int ActionCount { get; init; }

    /// <summary>
    /// Gets the number of entries in the blackboard.
    /// </summary>
    public int BlackboardEntryCount { get; init; }
}

/// <summary>
/// Score information for a utility AI action.
/// </summary>
public sealed record UtilityScoreSnapshot
{
    /// <summary>
    /// Gets the action name.
    /// </summary>
    public required string ActionName { get; init; }

    /// <summary>
    /// Gets the calculated score (0-1 typically, but can be weighted higher).
    /// </summary>
    public required float Score { get; init; }

    /// <summary>
    /// Gets the action weight.
    /// </summary>
    public required float Weight { get; init; }

    /// <summary>
    /// Gets whether this action is currently selected.
    /// </summary>
    public required bool IsSelected { get; init; }

    /// <summary>
    /// Gets the number of considerations for this action.
    /// </summary>
    public required int ConsiderationCount { get; init; }
}
