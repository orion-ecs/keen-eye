namespace KeenEyes.Navigation.Abstractions.Components;

/// <summary>
/// Opt-in component that routes a <see cref="NavMeshAgent"/> through crowd
/// simulation with local avoidance.
/// </summary>
/// <remarks>
/// <para>
/// Attach this component alongside <see cref="NavMeshAgent"/> to have the agent
/// steered by the crowd simulation when the active navigation provider supports
/// it (see <see cref="ICrowdNavigationProvider"/>). Crowd-managed agents avoid
/// each other using velocity obstacles and optional separation forces.
/// </para>
/// <para>
/// When the provider does not support crowd simulation, entities with this
/// component fall back to plain waypoint steering.
/// </para>
/// <para>
/// Override fields set to zero fall back to the values on
/// <see cref="NavMeshAgent"/> (or provider defaults where noted).
/// </para>
/// </remarks>
public struct CrowdAgent : IComponent
{
    /// <summary>
    /// The avoidance radius override in world units.
    /// Zero uses <see cref="NavMeshAgent.Settings"/>' radius.
    /// </summary>
    public float Radius;

    /// <summary>
    /// The agent height override in world units.
    /// Zero uses <see cref="NavMeshAgent.Settings"/>' height.
    /// </summary>
    public float Height;

    /// <summary>
    /// The maximum acceleration override in units per second squared.
    /// Zero uses <see cref="NavMeshAgent.Acceleration"/>.
    /// </summary>
    public float MaxAcceleration;

    /// <summary>
    /// The maximum speed override in units per second.
    /// Zero uses <see cref="NavMeshAgent.Speed"/>.
    /// </summary>
    public float MaxSpeed;

    /// <summary>
    /// The obstacle avoidance quality preset, from 0 (fastest) to 3 (highest quality).
    /// </summary>
    public int AvoidanceQuality;

    /// <summary>
    /// The weight of the separation force pushing agents apart.
    /// Zero disables separation; typical values are 1-3.
    /// </summary>
    public float SeparationWeight;

    /// <summary>
    /// The range in world units within which neighbouring agents and walls are
    /// considered for avoidance. Zero uses a provider default derived from the
    /// effective radius.
    /// </summary>
    public float CollisionQueryRange;

    /// <summary>
    /// Creates a crowd agent with recommended default avoidance settings.
    /// </summary>
    /// <returns>A new CrowdAgent component.</returns>
    public static CrowdAgent Create()
        => new()
        {
            AvoidanceQuality = 2,
            SeparationWeight = 2f
        };
}
