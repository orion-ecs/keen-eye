namespace KeenEyes.AI.BehaviorTree;

/// <summary>
/// Base class for decorator nodes that wrap a single child.
/// </summary>
/// <remarks>
/// Decorator nodes modify the behavior of their child node. Common decorators include:
/// <list type="bullet">
/// <item><description><see cref="Decorators.Inverter"/> - Inverts Success/Failure</description></item>
/// <item><description><see cref="Decorators.Repeater"/> - Repeats the child N times</description></item>
/// <item><description><see cref="Decorators.Cooldown"/> - Rate-limits child execution</description></item>
/// </list>
/// </remarks>
public abstract class DecoratorNode : BTNode
{
    /// <summary>
    /// Gets or sets the child node.
    /// </summary>
    public BTNode? Child { get; set; }

    /// <inheritdoc/>
    public override void Reset()
    {
        base.Reset();
        Child?.Reset();
    }

    /// <inheritdoc/>
    public override void OnInterrupted(Entity entity, Blackboard blackboard, IWorld world)
    {
        base.OnInterrupted(entity, blackboard, world);

        if (Child?.LastState == BTNodeState.Running)
        {
            Child.OnInterrupted(entity, blackboard, world);
        }
    }
}
