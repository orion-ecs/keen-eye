namespace KeenEyes.AI.BehaviorTree;

/// <summary>
/// Base class for composite nodes that have multiple children.
/// </summary>
/// <remarks>
/// Composite nodes execute their children according to specific rules:
/// <list type="bullet">
/// <item><description><see cref="Composites.Selector"/> - Returns Success on first child success (OR logic)</description></item>
/// <item><description><see cref="Composites.Sequence"/> - Returns Failure on first child failure (AND logic)</description></item>
/// <item><description><see cref="Composites.Parallel"/> - Runs all children simultaneously</description></item>
/// </list>
/// </remarks>
public abstract class CompositeNode : BTNode
{
    /// <summary>
    /// Gets or sets the child nodes.
    /// </summary>
    public List<BTNode> Children { get; set; } = [];

    /// <inheritdoc/>
    public override void Reset(Blackboard blackboard)
    {
        base.Reset(blackboard);
        GetCompositeMemory(blackboard).CurrentChildIndex = 0;

        foreach (var child in Children)
        {
            child.Reset(blackboard);
        }
    }

    /// <inheritdoc/>
    public override void OnInterrupted(Entity entity, Blackboard blackboard, IWorld world)
    {
        base.OnInterrupted(entity, blackboard, world);

        // Interrupt all running children
        foreach (var child in Children)
        {
            if (child.GetLastState(blackboard) == BTNodeState.Running)
            {
                child.OnInterrupted(entity, blackboard, world);
            }
        }
    }

    /// <inheritdoc/>
    protected internal override BTNodeMemory GetMemory(Blackboard blackboard)
        => blackboard.GetMemory<CompositeMemory>(this);

    /// <summary>
    /// Gets this composite's per-entity memory, including the resumable child cursor.
    /// </summary>
    /// <param name="blackboard">The blackboard of the entity executing this node.</param>
    /// <returns>The composite memory for that entity.</returns>
    protected CompositeMemory GetCompositeMemory(Blackboard blackboard)
        => (CompositeMemory)GetMemory(blackboard);

    /// <summary>
    /// Per-entity execution memory for composite nodes.
    /// </summary>
    public class CompositeMemory : BTNodeMemory
    {
        /// <summary>
        /// Gets or sets the index of the currently executing child (for resumable composites).
        /// </summary>
        public int CurrentChildIndex { get; set; }
    }
}
