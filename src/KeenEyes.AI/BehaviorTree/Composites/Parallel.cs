namespace KeenEyes.AI.BehaviorTree.Composites;

/// <summary>
/// Defines when a parallel node succeeds or fails based on child results.
/// </summary>
public enum ParallelPolicy
{
    /// <summary>
    /// Requires only one child to match the condition.
    /// </summary>
    RequireOne,

    /// <summary>
    /// Requires all children to match the condition.
    /// </summary>
    RequireAll
}

/// <summary>
/// Composite node that executes all children simultaneously.
/// </summary>
/// <remarks>
/// <para>
/// Parallel executes all children each tick and determines its result based on policies:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="SuccessPolicy"/> - When to return Success (default: RequireAll)</description></item>
/// <item><description><see cref="FailurePolicy"/> - When to return Failure (default: RequireOne)</description></item>
/// </list>
/// <para>
/// Use parallel for concurrent behaviors: "Aim at target while moving toward cover."
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Move to target while playing animation
/// var parallel = new Parallel
/// {
///     SuccessPolicy = ParallelPolicy.RequireAll,
///     FailurePolicy = ParallelPolicy.RequireOne,
///     Children = [
///         new ActionNode { Action = new MoveToAction() },
///         new ActionNode { Action = new PlayAnimationAction() }
///     ]
/// };
/// </code>
/// </example>
public sealed class Parallel : CompositeNode
{
    /// <summary>
    /// Gets or sets the policy for determining success.
    /// </summary>
    public ParallelPolicy SuccessPolicy { get; set; } = ParallelPolicy.RequireAll;

    /// <summary>
    /// Gets or sets the policy for determining failure.
    /// </summary>
    public ParallelPolicy FailurePolicy { get; set; } = ParallelPolicy.RequireOne;

    /// <inheritdoc/>
    public override BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        if (Children.Count == 0)
        {
            return SetState(BTNodeState.Success);
        }

        var successCount = 0;
        var failureCount = 0;
        var runningCount = 0;

        foreach (var child in Children)
        {
            var state = child.Execute(entity, blackboard, world);

            switch (state)
            {
                case BTNodeState.Success:
                    successCount++;
                    break;
                case BTNodeState.Failure:
                    failureCount++;
                    break;
                case BTNodeState.Running:
                    runningCount++;
                    break;
            }
        }

        // Check failure policy first
        var shouldFail = FailurePolicy switch
        {
            ParallelPolicy.RequireOne => failureCount > 0,
            ParallelPolicy.RequireAll => failureCount == Children.Count,
            _ => false
        };

        if (shouldFail)
        {
            return SetState(BTNodeState.Failure);
        }

        // Check success policy
        var shouldSucceed = SuccessPolicy switch
        {
            ParallelPolicy.RequireOne => successCount > 0,
            ParallelPolicy.RequireAll => successCount == Children.Count,
            _ => false
        };

        if (shouldSucceed)
        {
            return SetState(BTNodeState.Success);
        }

        // Still running if neither success nor failure conditions are met
        return SetState(BTNodeState.Running);
    }
}
