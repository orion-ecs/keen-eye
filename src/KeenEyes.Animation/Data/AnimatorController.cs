using KeenEyes.Animation.Tweening;

namespace KeenEyes.Animation.Data;

/// <summary>
/// Defines a transition between animation states.
/// </summary>
/// <param name="TargetStateHash">The hash of the target state.</param>
/// <param name="Duration">The crossfade duration in seconds.</param>
/// <param name="ExitTime">Normalized time (0-1) when transition can occur, or null for immediate.</param>
/// <param name="EaseType">The easing function for the transition blend.</param>
public readonly record struct AnimatorTransition(
    int TargetStateHash,
    float Duration,
    float? ExitTime = null,
    EaseType EaseType = EaseType.Linear);

/// <summary>
/// Defines an animation state within an animator controller.
/// </summary>
public sealed class AnimatorState
{
    /// <summary>
    /// Gets the name of this state.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the hash of the state name for fast comparison.
    /// </summary>
    public int Hash => Name.GetHashCode(StringComparison.Ordinal);

    /// <summary>
    /// Gets or sets the animation clip ID for this state.
    /// </summary>
    public int ClipId { get; set; } = -1;

    /// <summary>
    /// Gets or sets the playback speed for this state.
    /// </summary>
    public float Speed { get; set; } = 1f;

    /// <summary>
    /// Gets the transitions from this state.
    /// </summary>
    public List<AnimatorTransition> Transitions { get; } = [];

    /// <summary>
    /// Adds a transition to another state.
    /// </summary>
    /// <param name="targetState">The target state name.</param>
    /// <param name="duration">The crossfade duration.</param>
    /// <param name="exitTime">The exit time (0-1), or null for immediate.</param>
    /// <param name="easeType">The easing function.</param>
    public void AddTransition(string targetState, float duration, float? exitTime = null, EaseType easeType = EaseType.Linear)
    {
        Transitions.Add(new AnimatorTransition(
            targetState.GetHashCode(StringComparison.Ordinal),
            duration,
            exitTime,
            easeType));
    }
}

/// <summary>
/// A state machine controller for managing animation states and transitions.
/// </summary>
/// <remarks>
/// AnimatorController defines the structure of an animation state machine,
/// including states, transitions, and the default entry state.
/// </remarks>
public sealed class AnimatorController
{
    private readonly Dictionary<int, AnimatorState> states = [];

    /// <summary>
    /// Gets or sets the name of this controller.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the hash of the default entry state.
    /// </summary>
    public int DefaultStateHash { get; set; }

    /// <summary>
    /// Gets the states in this controller.
    /// </summary>
    public IReadOnlyDictionary<int, AnimatorState> States => states;

    /// <summary>
    /// Adds a state to the controller.
    /// </summary>
    /// <param name="state">The state to add.</param>
    /// <param name="isDefault">Whether this is the default entry state.</param>
    public void AddState(AnimatorState state, bool isDefault = false)
    {
        states[state.Hash] = state;
        if (isDefault || DefaultStateHash == 0)
        {
            DefaultStateHash = state.Hash;
        }
    }

    /// <summary>
    /// Gets a state by name.
    /// </summary>
    /// <param name="name">The state name.</param>
    /// <param name="state">The state, if found.</param>
    /// <returns>True if the state was found.</returns>
    public bool TryGetState(string name, out AnimatorState? state)
    {
        return states.TryGetValue(name.GetHashCode(StringComparison.Ordinal), out state);
    }

    /// <summary>
    /// Gets a state by hash.
    /// </summary>
    /// <param name="hash">The state hash.</param>
    /// <param name="state">The state, if found.</param>
    /// <returns>True if the state was found.</returns>
    public bool TryGetState(int hash, out AnimatorState? state)
    {
        return states.TryGetValue(hash, out state);
    }
}
