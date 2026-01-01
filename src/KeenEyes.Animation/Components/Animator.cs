namespace KeenEyes.Animation.Components;

/// <summary>
/// Component for state machine-based animation with multiple clips and transitions.
/// </summary>
/// <remarks>
/// <para>
/// Animator provides a state machine for managing multiple animation states
/// with transitions and blending. Each state references an animation clip,
/// and transitions define how to move between states.
/// </para>
/// <para>
/// For simple single-clip playback, use <see cref="AnimationPlayer"/> instead.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var entity = world.Spawn()
///     .With(Transform3D.Identity)
///     .With(new Animator { ControllerId = humanoidControllerId })
///     .Build();
///
/// // Trigger a state change by setting TriggerStateHash directly
/// ref var animator = ref world.Get&lt;Animator&gt;(entity);
/// animator.TriggerStateHash = Animator.GetStateHash("Jump");
/// </code>
/// </example>
[Component]
public partial struct Animator
{
    /// <summary>
    /// The ID of the animator controller (state machine definition).
    /// </summary>
    public int ControllerId;

    /// <summary>
    /// The hash of the current state name.
    /// </summary>
    [BuilderIgnore]
    public int CurrentStateHash;

    /// <summary>
    /// The current playback time within the current state.
    /// </summary>
    [BuilderIgnore]
    public float StateTime;

    /// <summary>
    /// The hash of the state being transitioned to, or 0 if not transitioning.
    /// </summary>
    [BuilderIgnore]
    public int NextStateHash;

    /// <summary>
    /// The transition progress (0-1) when transitioning between states.
    /// </summary>
    [BuilderIgnore]
    public float TransitionProgress;

    /// <summary>
    /// The duration of the current transition.
    /// </summary>
    [BuilderIgnore]
    public float TransitionDuration;

    /// <summary>
    /// The playback time in the next state (for crossfade blending).
    /// </summary>
    [BuilderIgnore]
    public float NextStateTime;

    /// <summary>
    /// Whether the animator is currently enabled.
    /// </summary>
    public bool Enabled;

    /// <summary>
    /// The global speed multiplier for all animations.
    /// </summary>
    public float Speed;

    /// <summary>
    /// Hash of triggered state (set by user, cleared by system after processing).
    /// </summary>
    [BuilderIgnore]
    public int TriggerStateHash;

    /// <summary>
    /// Creates a default animator.
    /// </summary>
    public static Animator Default => new()
    {
        ControllerId = -1,
        CurrentStateHash = 0,
        StateTime = 0f,
        NextStateHash = 0,
        TransitionProgress = 0f,
        TransitionDuration = 0f,
        NextStateTime = 0f,
        Enabled = true,
        Speed = 1f,
        TriggerStateHash = 0
    };

    /// <summary>
    /// Creates an animator for the specified controller.
    /// </summary>
    /// <param name="controllerId">The controller ID.</param>
    /// <returns>A configured animator.</returns>
    public static Animator ForController(int controllerId) => Default with
    {
        ControllerId = controllerId
    };

    /// <summary>
    /// Gets a hash code for a state name.
    /// </summary>
    /// <remarks>
    /// Use this method to convert state names to hashes, then set <see cref="TriggerStateHash"/>
    /// directly to trigger state transitions. This follows ECS principles where components
    /// are pure data and systems handle all logic.
    /// </remarks>
    /// <param name="stateName">The state name.</param>
    /// <returns>A hash code for the state name.</returns>
    /// <example>
    /// <code>
    /// ref var animator = ref world.Get&lt;Animator&gt;(entity);
    /// animator.TriggerStateHash = Animator.GetStateHash("Jump");
    /// </code>
    /// </example>
    public static int GetStateHash(string stateName)
    {
        return stateName.GetHashCode(StringComparison.Ordinal);
    }
}
