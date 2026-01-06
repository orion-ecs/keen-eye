namespace KeenEyes.AI.FSM;

/// <summary>
/// Defines a finite state machine for AI behavior.
/// </summary>
/// <remarks>
/// <para>
/// A state machine consists of a set of states and transitions between them.
/// At any given time, an entity is in exactly one state. Transitions are
/// evaluated each tick to determine if the entity should change states.
/// </para>
/// <para>
/// State machines are ideal for simple AI behaviors with clear, discrete modes
/// of operation, such as:
/// </para>
/// <list type="bullet">
/// <item><description>Door (Open/Closed)</description></item>
/// <item><description>Simple enemy (Patrol/Chase/Attack)</description></item>
/// <item><description>NPC conversation states</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var enemyFSM = new StateMachine
/// {
///     Name = "EnemyAI",
///     States = [
///         new State { Name = "Patrol", OnUpdateActions = [new PatrolAction()] },
///         new State { Name = "Chase", OnUpdateActions = [new ChaseAction()] },
///         new State { Name = "Attack", OnUpdateActions = [new AttackAction()] }
///     ],
///     Transitions = [
///         new StateTransition { FromStateIndex = 0, ToStateIndex = 1, Condition = new SeePlayerCondition() },
///         new StateTransition { FromStateIndex = 1, ToStateIndex = 2, Condition = new InRangeCondition() },
///         new StateTransition { FromStateIndex = 1, ToStateIndex = 0, Condition = new LostPlayerCondition() },
///         new StateTransition { FromStateIndex = 2, ToStateIndex = 1, Condition = new OutOfRangeCondition() }
///     ],
///     InitialStateIndex = 0
/// };
/// </code>
/// </example>
public sealed class StateMachine
{
    /// <summary>
    /// Gets or sets the name of this state machine.
    /// </summary>
    /// <remarks>
    /// Used for debugging and visualization.
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the states in this state machine.
    /// </summary>
    public List<State> States { get; set; } = [];

    /// <summary>
    /// Gets or sets the transitions between states.
    /// </summary>
    public List<StateTransition> Transitions { get; set; } = [];

    /// <summary>
    /// Gets or sets the index of the initial state.
    /// </summary>
    public int InitialStateIndex { get; set; }

    /// <summary>
    /// Validates the state machine definition.
    /// </summary>
    /// <returns>An error message if validation fails; otherwise, null.</returns>
    public string? Validate()
    {
        if (States.Count == 0)
        {
            return "State machine must have at least one state.";
        }

        if (InitialStateIndex < 0 || InitialStateIndex >= States.Count)
        {
            return $"Initial state index {InitialStateIndex} is out of range [0, {States.Count - 1}].";
        }

        foreach (var transition in Transitions)
        {
            if (transition.FromStateIndex < 0 || transition.FromStateIndex >= States.Count)
            {
                return $"Transition from state index {transition.FromStateIndex} is out of range.";
            }

            if (transition.ToStateIndex < 0 || transition.ToStateIndex >= States.Count)
            {
                return $"Transition to state index {transition.ToStateIndex} is out of range.";
            }
        }

        return null;
    }

    /// <summary>
    /// Gets transitions from a specific state, ordered by priority (descending).
    /// </summary>
    /// <param name="stateIndex">The state index to get transitions from.</param>
    /// <returns>Transitions from the specified state, ordered by priority.</returns>
    internal IEnumerable<StateTransition> GetTransitionsFrom(int stateIndex)
    {
        return Transitions
            .Where(t => t.FromStateIndex == stateIndex)
            .OrderByDescending(t => t.Priority);
    }

    /// <summary>
    /// Gets a state by index.
    /// </summary>
    /// <param name="index">The state index.</param>
    /// <returns>The state at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if index is out of range.</exception>
    internal State GetState(int index)
    {
        if (index < 0 || index >= States.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"State index {index} is out of range [0, {States.Count - 1}].");
        }

        return States[index];
    }
}
