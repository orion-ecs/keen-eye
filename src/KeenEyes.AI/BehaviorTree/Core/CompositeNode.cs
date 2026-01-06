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

    /// <summary>
    /// The index of the currently executing child (for resumable composites).
    /// </summary>
    protected int currentChildIndex;

    /// <inheritdoc/>
    public override void Reset()
    {
        base.Reset();
        currentChildIndex = 0;

        foreach (var child in Children)
        {
            child.Reset();
        }
    }

    /// <inheritdoc/>
    public override void OnInterrupted(Entity entity, Blackboard blackboard, IWorld world)
    {
        base.OnInterrupted(entity, blackboard, world);

        // Interrupt all running children
        foreach (var child in Children)
        {
            if (child.LastState == BTNodeState.Running)
            {
                child.OnInterrupted(entity, blackboard, world);
            }
        }
    }
}
