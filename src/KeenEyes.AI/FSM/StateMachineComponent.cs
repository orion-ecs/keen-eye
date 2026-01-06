namespace KeenEyes.AI.FSM;

/// <summary>
/// Component that enables finite state machine behavior for an entity.
/// </summary>
/// <remarks>
/// <para>
/// Attach this component to entities that should have FSM-based AI behavior.
/// The <see cref="StateMachineSystem"/> will evaluate transitions and execute
/// state actions each tick.
/// </para>
/// <para>
/// The component supports both asset-based definitions (stored externally and reused)
/// and inline definitions (created per-entity).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Using a predefined state machine asset
/// var enemy = world.Spawn()
///     .With(new StateMachineComponent { Definition = enemyFSM })
///     .Build();
///
/// // Or using the Create helper
/// var enemy = world.Spawn()
///     .With(StateMachineComponent.Create(enemyFSM))
///     .Build();
/// </code>
/// </example>
[Component]
public partial struct StateMachineComponent : IComponent
{
    /// <summary>
    /// The state machine definition (asset reference or inline).
    /// </summary>
    public StateMachine? Definition;

    /// <summary>
    /// Whether the state machine is currently enabled.
    /// </summary>
    public bool Enabled;

    /// <summary>
    /// The index of the current state.
    /// </summary>
    [BuilderIgnore]
    public int CurrentStateIndex;

    /// <summary>
    /// The index of the previous state (for debugging).
    /// </summary>
    [BuilderIgnore]
    public int PreviousStateIndex;

    /// <summary>
    /// Time spent in the current state in seconds.
    /// </summary>
    [BuilderIgnore]
    public float TimeInState;

    /// <summary>
    /// Whether the entity just entered the current state this tick.
    /// </summary>
    /// <remarks>
    /// This flag is set to true when entering a new state and cleared after
    /// the first update. Useful for state entry logic.
    /// </remarks>
    [BuilderIgnore]
    public bool StateJustEntered;

    /// <summary>
    /// Whether the state machine has been initialized.
    /// </summary>
    [BuilderIgnore]
    public bool IsInitialized;

    /// <summary>
    /// Per-entity blackboard for sharing state between actions.
    /// </summary>
    /// <remarks>
    /// The blackboard is created lazily on first access. Use this to store
    /// entity-specific AI state like targets, waypoints, and counters.
    /// </remarks>
    [BuilderIgnore]
    public Blackboard? Blackboard;

    /// <summary>
    /// Gets the name of the current state, if available.
    /// </summary>
    public readonly string? CurrentStateName => Definition?.States.Count > CurrentStateIndex
        ? Definition.States[CurrentStateIndex].Name
        : null;

    /// <summary>
    /// Creates a StateMachineComponent with the specified definition.
    /// </summary>
    /// <param name="definition">The state machine definition.</param>
    /// <returns>A configured StateMachineComponent.</returns>
    public static StateMachineComponent Create(StateMachine definition) => new()
    {
        Definition = definition,
        Enabled = true,
        CurrentStateIndex = definition.InitialStateIndex,
        PreviousStateIndex = -1,
        TimeInState = 0f,
        StateJustEntered = true,
        IsInitialized = false,
        Blackboard = null
    };

    /// <summary>
    /// Gets or creates the blackboard for this component.
    /// </summary>
    /// <returns>The blackboard instance.</returns>
    public Blackboard GetOrCreateBlackboard()
    {
        Blackboard ??= new Blackboard();
        return Blackboard;
    }
}
