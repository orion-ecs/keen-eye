namespace KeenEyes.AI.Utility;

/// <summary>
/// Component that enables utility AI decision-making for an entity.
/// </summary>
/// <remarks>
/// <para>
/// Attach this component to entities that should use utility-based AI.
/// The <see cref="UtilitySystem"/> will evaluate actions and execute
/// the selected one each tick.
/// </para>
/// <para>
/// The component supports both asset-based definitions (stored externally and reused)
/// and inline definitions (created per-entity).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Using a predefined utility AI
/// var guard = world.Spawn()
///     .With(new UtilityComponent { Definition = guardAI })
///     .Build();
///
/// // Or using the Create helper
/// var guard = world.Spawn()
///     .With(UtilityComponent.Create(guardAI))
///     .Build();
/// </code>
/// </example>
[Component]
public partial struct UtilityComponent : IComponent
{
    /// <summary>
    /// The utility AI definition (asset reference or inline).
    /// </summary>
    public UtilityAI? Definition;

    /// <summary>
    /// Whether the utility AI is currently enabled.
    /// </summary>
    public bool Enabled;

    /// <summary>
    /// How often to re-evaluate actions (in seconds).
    /// </summary>
    /// <remarks>
    /// Set to 0 to evaluate every tick. Higher values reduce CPU usage but
    /// make the AI less responsive.
    /// </remarks>
    public float EvaluationInterval;

    /// <summary>
    /// Whether the utility AI has been initialized.
    /// </summary>
    [BuilderIgnore]
    public bool IsInitialized;

    /// <summary>
    /// Per-entity blackboard for sharing state.
    /// </summary>
    [BuilderIgnore]
    public Blackboard? Blackboard;

    /// <summary>
    /// The currently executing action.
    /// </summary>
    [BuilderIgnore]
    public UtilityAction? CurrentAction;

    /// <summary>
    /// Time since last evaluation.
    /// </summary>
    [BuilderIgnore]
    public float TimeSinceEvaluation;

    /// <summary>
    /// Creates a UtilityComponent with the specified definition.
    /// </summary>
    /// <param name="definition">The utility AI definition.</param>
    /// <returns>A configured UtilityComponent.</returns>
    public static UtilityComponent Create(UtilityAI definition) => new()
    {
        Definition = definition,
        Enabled = true,
        EvaluationInterval = 0f,
        IsInitialized = false,
        Blackboard = null,
        CurrentAction = null,
        TimeSinceEvaluation = 0f
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
