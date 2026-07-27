namespace KeenEyes.AI;

/// <summary>
/// Interface for AI actions that can be executed by behavior trees.
/// </summary>
/// <remarks>
/// <para>
/// Actions are the leaf nodes of behavior trees that perform actual work.
/// They return <see cref="BTNodeState.Running"/> if still in progress,
/// <see cref="BTNodeState.Success"/> when complete, or
/// <see cref="BTNodeState.Failure"/> if they cannot complete.
/// </para>
/// <para>
/// Navigation actions like <see cref="Actions.MoveToAction"/> implement this interface
/// to integrate pathfinding with behavior trees.
/// </para>
/// </remarks>
public interface IAIAction
{
    /// <summary>
    /// Executes the action for the given entity.
    /// </summary>
    /// <param name="entity">The entity executing this action.</param>
    /// <param name="blackboard">The blackboard for reading/writing shared state.</param>
    /// <param name="world">The ECS world containing the entity.</param>
    /// <returns>The current state of the action execution.</returns>
    BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world);

    /// <summary>
    /// Resets the action's per-entity execution state for a new run.
    /// </summary>
    /// <param name="blackboard">The blackboard of the entity whose state to reset.</param>
    /// <remarks>
    /// Called when the behavior tree restarts or when this action needs to be re-evaluated.
    /// Action instances are shared between every entity running the same behavior
    /// definition; store mutable state in a memory object obtained from
    /// <see cref="Blackboard.GetMemory{TMemory}"/>, never in instance fields (#1281).
    /// </remarks>
    void Reset(Blackboard blackboard)
    {
    }

    /// <summary>
    /// Called when the action is interrupted before completion.
    /// </summary>
    /// <param name="entity">The entity that was executing this action.</param>
    /// <param name="blackboard">The blackboard with shared state.</param>
    /// <param name="world">The ECS world containing the entity.</param>
    /// <remarks>
    /// Use this to clean up resources or revert partial changes when the action is aborted.
    /// </remarks>
    void OnInterrupted(Entity entity, Blackboard blackboard, IWorld world) { }
}
