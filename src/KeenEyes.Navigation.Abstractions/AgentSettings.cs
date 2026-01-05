namespace KeenEyes.Navigation.Abstractions;

/// <summary>
/// Configuration settings for a navigation agent.
/// </summary>
/// <remarks>
/// <para>
/// Agent settings define the physical characteristics that affect pathfinding,
/// such as the agent's size and movement capabilities. These settings determine
/// which areas the agent can traverse and how paths are computed.
/// </para>
/// <para>
/// Use <see cref="Default"/> for typical humanoid characters or create custom
/// settings for vehicles, large creatures, or other agent types.
/// </para>
/// </remarks>
/// <param name="Radius">
/// The radius of the agent's collision cylinder in world units.
/// Used to determine navigable areas and obstacle avoidance.
/// </param>
/// <param name="Height">
/// The height of the agent's collision cylinder in world units.
/// Used for overhead clearance checks.
/// </param>
/// <param name="MaxSlopeAngle">
/// The maximum slope angle the agent can walk up, in degrees.
/// Surfaces steeper than this are considered unwalkable.
/// </param>
/// <param name="StepHeight">
/// The maximum step height the agent can climb without jumping.
/// Used for stair traversal and small obstacles.
/// </param>
public readonly record struct AgentSettings(
    float Radius,
    float Height,
    float MaxSlopeAngle,
    float StepHeight)
{
    /// <summary>
    /// Default settings for a typical humanoid character.
    /// </summary>
    /// <remarks>
    /// Radius: 0.5m, Height: 2.0m, MaxSlope: 45 degrees, StepHeight: 0.4m
    /// </remarks>
    public static AgentSettings Default => new(0.5f, 2.0f, 45f, 0.4f);

    /// <summary>
    /// Settings for a small agent (child, pet, etc.).
    /// </summary>
    /// <remarks>
    /// Radius: 0.3m, Height: 1.0m, MaxSlope: 50 degrees, StepHeight: 0.25m
    /// </remarks>
    public static AgentSettings Small => new(0.3f, 1.0f, 50f, 0.25f);

    /// <summary>
    /// Settings for a large agent (vehicle, large creature, etc.).
    /// </summary>
    /// <remarks>
    /// Radius: 1.5m, Height: 3.0m, MaxSlope: 30 degrees, StepHeight: 0.6m
    /// </remarks>
    public static AgentSettings Large => new(1.5f, 3.0f, 30f, 0.6f);

    /// <summary>
    /// Creates agent settings with the specified radius, using default values for other properties.
    /// </summary>
    /// <param name="radius">The agent radius.</param>
    /// <returns>New agent settings with the specified radius.</returns>
    public static AgentSettings WithRadius(float radius)
        => Default with { Radius = radius };

    /// <summary>
    /// Gets the diameter of the agent (twice the radius).
    /// </summary>
    public float Diameter => Radius * 2f;

    /// <summary>
    /// Validates that the settings are physically reasonable.
    /// </summary>
    /// <returns>True if settings are valid, false otherwise.</returns>
    public bool IsValid()
        => Radius > 0f
           && Height > 0f
           && MaxSlopeAngle > 0f && MaxSlopeAngle < 90f
           && StepHeight >= 0f && StepHeight < Height;
}
