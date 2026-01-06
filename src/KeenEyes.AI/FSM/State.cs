namespace KeenEyes.AI.FSM;

/// <summary>
/// Represents a state in a finite state machine.
/// </summary>
/// <remarks>
/// <para>
/// States define behavior for when an entity is in a particular mode of operation.
/// Each state can have actions that execute at different lifecycle points:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="OnEnterActions"/> - Execute once when entering the state</description></item>
/// <item><description><see cref="OnUpdateActions"/> - Execute every tick while in the state</description></item>
/// <item><description><see cref="OnExitActions"/> - Execute once when leaving the state</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var patrolState = new State
/// {
///     Name = "Patrol",
///     OnUpdateActions = [new PatrolAction { WaypointTag = "PatrolPoint" }]
/// };
///
/// var chaseState = new State
/// {
///     Name = "Chase",
///     OnEnterActions = [new PlaySoundAction { Sound = "alert" }],
///     OnUpdateActions = [new ChaseAction { Speed = 5f }]
/// };
/// </code>
/// </example>
public sealed class State
{
    /// <summary>
    /// Gets or sets the name of the state.
    /// </summary>
    /// <remarks>
    /// The name is used for debugging and visualization.
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the actions executed once when entering this state.
    /// </summary>
    public List<IAIAction> OnEnterActions { get; set; } = [];

    /// <summary>
    /// Gets or sets the actions executed every tick while in this state.
    /// </summary>
    public List<IAIAction> OnUpdateActions { get; set; } = [];

    /// <summary>
    /// Gets or sets the actions executed once when exiting this state.
    /// </summary>
    public List<IAIAction> OnExitActions { get; set; } = [];

    /// <summary>
    /// Executes all OnEnter actions for the given entity.
    /// </summary>
    /// <param name="entity">The entity entering this state.</param>
    /// <param name="blackboard">The blackboard for shared state.</param>
    /// <param name="world">The ECS world.</param>
    internal void ExecuteEnterActions(Entity entity, Blackboard blackboard, IWorld world)
    {
        foreach (var action in OnEnterActions)
        {
            action.Execute(entity, blackboard, world);
        }
    }

    /// <summary>
    /// Executes all OnUpdate actions for the given entity.
    /// </summary>
    /// <param name="entity">The entity in this state.</param>
    /// <param name="blackboard">The blackboard for shared state.</param>
    /// <param name="world">The ECS world.</param>
    /// <returns>The combined state of all actions (Running if any are Running).</returns>
    internal BTNodeState ExecuteUpdateActions(Entity entity, Blackboard blackboard, IWorld world)
    {
        var result = BTNodeState.Success;

        foreach (var action in OnUpdateActions)
        {
            var state = action.Execute(entity, blackboard, world);
            if (state == BTNodeState.Running)
            {
                result = BTNodeState.Running;
            }
        }

        return result;
    }

    /// <summary>
    /// Executes all OnExit actions for the given entity.
    /// </summary>
    /// <param name="entity">The entity leaving this state.</param>
    /// <param name="blackboard">The blackboard for shared state.</param>
    /// <param name="world">The ECS world.</param>
    internal void ExecuteExitActions(Entity entity, Blackboard blackboard, IWorld world)
    {
        foreach (var action in OnExitActions)
        {
            action.Execute(entity, blackboard, world);
        }
    }

    /// <summary>
    /// Resets all actions in this state.
    /// </summary>
    internal void ResetActions()
    {
        foreach (var action in OnEnterActions)
        {
            action.Reset();
        }

        foreach (var action in OnUpdateActions)
        {
            action.Reset();
        }

        foreach (var action in OnExitActions)
        {
            action.Reset();
        }
    }
}
