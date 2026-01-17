namespace KeenEyes.TestBridge.AI;

/// <summary>
/// Snapshot of a state machine's current state.
/// </summary>
public sealed record StateMachineSnapshot
{
    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public required int EntityId { get; init; }

    /// <summary>
    /// Gets whether the state machine is enabled.
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Gets whether the state machine is initialized.
    /// </summary>
    public required bool IsInitialized { get; init; }

    /// <summary>
    /// Gets the name of the state machine definition.
    /// </summary>
    public string? MachineName { get; init; }

    /// <summary>
    /// Gets the current state index.
    /// </summary>
    public required int CurrentStateIndex { get; init; }

    /// <summary>
    /// Gets the current state name.
    /// </summary>
    public string? CurrentStateName { get; init; }

    /// <summary>
    /// Gets the previous state index.
    /// </summary>
    public int? PreviousStateIndex { get; init; }

    /// <summary>
    /// Gets the previous state name.
    /// </summary>
    public string? PreviousStateName { get; init; }

    /// <summary>
    /// Gets the time spent in the current state in seconds.
    /// </summary>
    public required float TimeInState { get; init; }

    /// <summary>
    /// Gets whether the state was just entered this frame.
    /// </summary>
    public required bool StateJustEntered { get; init; }

    /// <summary>
    /// Gets the list of available states.
    /// </summary>
    public IReadOnlyList<StateInfoSnapshot>? States { get; init; }

    /// <summary>
    /// Gets the number of entries in the blackboard.
    /// </summary>
    public int BlackboardEntryCount { get; init; }
}

/// <summary>
/// Information about a state in a state machine.
/// </summary>
public sealed record StateInfoSnapshot
{
    /// <summary>
    /// Gets the state index.
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// Gets the state name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets whether this is the current state.
    /// </summary>
    public required bool IsCurrent { get; init; }

    /// <summary>
    /// Gets the number of enter actions.
    /// </summary>
    public int EnterActionCount { get; init; }

    /// <summary>
    /// Gets the number of update actions.
    /// </summary>
    public int UpdateActionCount { get; init; }

    /// <summary>
    /// Gets the number of exit actions.
    /// </summary>
    public int ExitActionCount { get; init; }

    /// <summary>
    /// Gets the number of transitions from this state.
    /// </summary>
    public int TransitionCount { get; init; }
}
