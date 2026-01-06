namespace KeenEyes.AI.FSM;

/// <summary>
/// Defines a transition between states in a finite state machine.
/// </summary>
/// <remarks>
/// <para>
/// Transitions are evaluated in priority order (highest first).
/// When a transition's condition evaluates to true, the state machine
/// transitions from <see cref="FromStateIndex"/> to <see cref="ToStateIndex"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Transition from Patrol to Chase when player is seen
/// var patrolToChase = new StateTransition
/// {
///     FromStateIndex = 0, // Patrol
///     ToStateIndex = 1,   // Chase
///     Condition = new SeePlayerCondition { Range = 10f },
///     Priority = 1
/// };
///
/// // Transition from Chase back to Patrol when player is lost (lower priority)
/// var chaseToPatrol = new StateTransition
/// {
///     FromStateIndex = 1, // Chase
///     ToStateIndex = 0,   // Patrol
///     Condition = new LostPlayerCondition { Duration = 3f },
///     Priority = 0
/// };
/// </code>
/// </example>
public sealed class StateTransition
{
    /// <summary>
    /// Gets or sets the index of the source state.
    /// </summary>
    public int FromStateIndex { get; set; }

    /// <summary>
    /// Gets or sets the index of the destination state.
    /// </summary>
    public int ToStateIndex { get; set; }

    /// <summary>
    /// Gets or sets the condition that must be true for this transition to occur.
    /// </summary>
    /// <remarks>
    /// If null, the transition is considered unconditional and will always trigger
    /// (useful for fallback transitions with low priority).
    /// </remarks>
    public ICondition? Condition { get; set; }

    /// <summary>
    /// Gets or sets the priority of this transition.
    /// </summary>
    /// <remarks>
    /// Higher priority transitions are evaluated first.
    /// If multiple transitions have the same priority, they are evaluated
    /// in the order they appear in the state machine's transition list.
    /// </remarks>
    public int Priority { get; set; }

    /// <summary>
    /// Evaluates whether this transition should occur.
    /// </summary>
    /// <param name="entity">The entity to evaluate for.</param>
    /// <param name="blackboard">The blackboard with shared state.</param>
    /// <param name="world">The ECS world.</param>
    /// <returns>True if the transition condition is met; otherwise, false.</returns>
    internal bool Evaluate(Entity entity, Blackboard blackboard, IWorld world)
    {
        // Unconditional transition (always true)
        if (Condition == null)
        {
            return true;
        }

        return Condition.Evaluate(entity, blackboard, world);
    }
}
