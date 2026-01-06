namespace KeenEyes.AI.BehaviorTree;

/// <summary>
/// Component that enables behavior tree AI for an entity.
/// </summary>
/// <remarks>
/// <para>
/// Attach this component to entities that should have behavior tree-based AI.
/// The <see cref="BehaviorTreeSystem"/> will evaluate the tree each tick.
/// </para>
/// <para>
/// The component supports both asset-based definitions (stored externally and reused)
/// and inline definitions (created per-entity).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Using a predefined behavior tree asset
/// var enemy = world.Spawn()
///     .With(new BehaviorTreeComponent { Definition = enemyBT })
///     .Build();
///
/// // Or using the Create helper
/// var enemy = world.Spawn()
///     .With(BehaviorTreeComponent.Create(enemyBT))
///     .Build();
/// </code>
/// </example>
[Component]
public partial struct BehaviorTreeComponent : IComponent
{
    /// <summary>
    /// The behavior tree definition (asset reference or inline).
    /// </summary>
    public BehaviorTree? Definition;

    /// <summary>
    /// Whether the behavior tree is currently enabled.
    /// </summary>
    /// <remarks>
    /// Set to false to pause behavior tree evaluation.
    /// </remarks>
    public bool Enabled;

    /// <summary>
    /// Whether the behavior tree has been initialized.
    /// </summary>
    [BuilderIgnore]
    public bool IsInitialized;

    /// <summary>
    /// Per-entity blackboard for sharing state between nodes.
    /// </summary>
    [BuilderIgnore]
    public Blackboard? Blackboard;

    /// <summary>
    /// The node that is currently returning Running (for resumption).
    /// </summary>
    /// <remarks>
    /// When a node returns Running, execution resumes from that point
    /// on the next tick rather than starting from the root.
    /// </remarks>
    [BuilderIgnore]
    public BTNode? RunningNode;

    /// <summary>
    /// The last result from tree execution.
    /// </summary>
    [BuilderIgnore]
    public BTNodeState LastResult;

    /// <summary>
    /// Creates a BehaviorTreeComponent with the specified definition.
    /// </summary>
    /// <param name="definition">The behavior tree definition.</param>
    /// <returns>A configured BehaviorTreeComponent.</returns>
    public static BehaviorTreeComponent Create(BehaviorTree definition) => new()
    {
        Definition = definition,
        Enabled = true,
        IsInitialized = false,
        Blackboard = null,
        RunningNode = null,
        LastResult = BTNodeState.Running
    };

    /// <summary>
    /// Gets or creates the blackboard for this component.
    /// </summary>
    /// <returns>The blackboard instance.</returns>
    public Blackboard GetOrCreateBlackboard()
    {
        Blackboard ??= new Blackboard();
        return Blackboard;
    }
}
