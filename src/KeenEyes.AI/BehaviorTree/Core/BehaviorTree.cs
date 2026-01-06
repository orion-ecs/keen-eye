namespace KeenEyes.AI.BehaviorTree;

/// <summary>
/// Container for a behavior tree definition.
/// </summary>
/// <remarks>
/// <para>
/// A behavior tree consists of a hierarchical structure of nodes with a single
/// root node. The tree is evaluated from the root down each tick, with nodes
/// returning Success, Failure, or Running to control execution flow.
/// </para>
/// <para>
/// Behavior trees are ideal for complex AI behaviors that need modular,
/// reusable patterns, such as:
/// </para>
/// <list type="bullet">
/// <item><description>Complex enemy AI with multiple behavior options</description></item>
/// <item><description>Boss fight patterns</description></item>
/// <item><description>Companion NPCs</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var enemyBT = new BehaviorTree
/// {
///     Name = "EnemyBT",
///     Root = new Selector
///     {
///         Children = [
///             new Sequence { Children = [
///                 new ConditionNode { Condition = new InRangeCondition() },
///                 new ActionNode { Action = new AttackAction() }
///             ]},
///             new Sequence { Children = [
///                 new ConditionNode { Condition = new CanSeePlayerCondition() },
///                 new ActionNode { Action = new ChaseAction() }
///             ]},
///             new ActionNode { Action = new PatrolAction() }
///         ]
///     }
/// };
/// </code>
/// </example>
public sealed class BehaviorTree
{
    /// <summary>
    /// Gets or sets the name of this behavior tree.
    /// </summary>
    /// <remarks>
    /// Used for debugging and visualization.
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the root node of the tree.
    /// </summary>
    public BTNode? Root { get; set; }

    /// <summary>
    /// Validates the behavior tree definition.
    /// </summary>
    /// <returns>An error message if validation fails; otherwise, null.</returns>
    public string? Validate()
    {
        if (Root == null)
        {
            return "Behavior tree must have a root node.";
        }

        return null;
    }

    /// <summary>
    /// Executes the behavior tree for the given entity.
    /// </summary>
    /// <param name="entity">The entity executing this behavior.</param>
    /// <param name="blackboard">The blackboard for shared state.</param>
    /// <param name="world">The ECS world.</param>
    /// <returns>The execution state of the root node.</returns>
    public BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        if (Root == null)
        {
            return BTNodeState.Failure;
        }

        return Root.Execute(entity, blackboard, world);
    }

    /// <summary>
    /// Resets the entire behavior tree.
    /// </summary>
    public void Reset()
    {
        ResetNode(Root);
    }

    private static void ResetNode(BTNode? node)
    {
        if (node == null)
        {
            return;
        }

        node.Reset();

        // Recursively reset children based on node type
        if (node is CompositeNode composite)
        {
            foreach (var child in composite.Children)
            {
                ResetNode(child);
            }
        }
        else if (node is DecoratorNode decorator)
        {
            ResetNode(decorator.Child);
        }
    }
}
