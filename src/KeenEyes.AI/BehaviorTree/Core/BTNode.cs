namespace KeenEyes.AI.BehaviorTree;

/// <summary>
/// Abstract base class for all behavior tree nodes.
/// </summary>
/// <remarks>
/// <para>
/// Behavior tree nodes form a hierarchical structure where each node returns
/// one of three states: <see cref="BTNodeState.Running"/>, <see cref="BTNodeState.Success"/>,
/// or <see cref="BTNodeState.Failure"/>.
/// </para>
/// <para>
/// Node types include:
/// </para>
/// <list type="bullet">
/// <item><description><b>Composites</b> - Nodes with multiple children (Selector, Sequence, Parallel)</description></item>
/// <item><description><b>Decorators</b> - Nodes with a single child that modify behavior (Inverter, Repeater)</description></item>
/// <item><description><b>Leaves</b> - Terminal nodes that perform actions or check conditions</description></item>
/// </list>
/// </remarks>
public abstract class BTNode
{
    /// <summary>
    /// Gets or sets the name of this node.
    /// </summary>
    /// <remarks>
    /// Used for debugging and visualization.
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the last execution state of this node.
    /// </summary>
    /// <remarks>
    /// Useful for debugging and visualization to see the state of each node
    /// after tree evaluation.
    /// </remarks>
    public BTNodeState LastState { get; protected set; } = BTNodeState.Running;

    /// <summary>
    /// Executes this node for the given entity.
    /// </summary>
    /// <param name="entity">The entity executing this behavior.</param>
    /// <param name="blackboard">The blackboard for reading/writing shared state.</param>
    /// <param name="world">The ECS world containing the entity.</param>
    /// <returns>The execution state of this node.</returns>
    public abstract BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world);

    /// <summary>
    /// Resets the node state for a new execution.
    /// </summary>
    /// <remarks>
    /// Called when the behavior tree restarts or when this node needs to be
    /// re-evaluated from scratch. Override to reset internal state.
    /// </remarks>
    public virtual void Reset()
    {
        LastState = BTNodeState.Running;
    }

    /// <summary>
    /// Called when this node is interrupted before completion.
    /// </summary>
    /// <param name="entity">The entity that was executing this behavior.</param>
    /// <param name="blackboard">The blackboard with shared state.</param>
    /// <param name="world">The ECS world containing the entity.</param>
    /// <remarks>
    /// Override to clean up resources or revert partial changes when the node
    /// is aborted (e.g., by a higher-priority behavior).
    /// </remarks>
    public virtual void OnInterrupted(Entity entity, Blackboard blackboard, IWorld world)
    {
    }

    /// <summary>
    /// Updates the last state and returns it.
    /// </summary>
    /// <param name="state">The new state.</param>
    /// <returns>The state (for chaining).</returns>
    protected BTNodeState SetState(BTNodeState state)
    {
        LastState = state;
        return state;
    }
}
