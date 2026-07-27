namespace KeenEyes.AI;

/// <summary>
/// Base class for the per-entity execution memory of a behavior tree node.
/// </summary>
/// <remarks>
/// <para>
/// Behavior tree definitions are shared between every entity that runs them, so node
/// instances must stay immutable while executing. All state that changes during a run
/// (last result, child cursors, timers) lives in a memory object stored on the entity's
/// <see cref="Blackboard"/> via <see cref="Blackboard.GetMemory{TMemory}"/> (#1281).
/// </para>
/// <para>
/// Nodes that track more than their last state derive a memory type from this class and
/// override <see cref="BehaviorTree.BTNode.GetMemory"/> so the shared entry always holds
/// the most-derived memory.
/// </para>
/// </remarks>
public class BTNodeMemory
{
    /// <summary>
    /// Gets or sets the last execution state this node returned for the owning entity.
    /// </summary>
    public BTNodeState LastState { get; set; } = BTNodeState.Running;
}
