using System.Numerics;
using KeenEyes.Common;

namespace KeenEyes.AI.Utility;

/// <summary>
/// Consideration input that calculates normalized distance to a target.
/// </summary>
/// <remarks>
/// <para>
/// This input calculates the distance between the entity and its target,
/// normalized by a maximum distance. Requires <see cref="Transform3D"/> on the entity
/// and a target position or entity in the blackboard.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var distanceInput = new DistanceInput
/// {
///     MaxDistance = 20f,
///     TargetKey = BBKeys.TargetPosition
/// };
/// </code>
/// </example>
public sealed class DistanceInput : IConsiderationInput
{
    /// <summary>
    /// Gets or sets the maximum distance for normalization.
    /// </summary>
    /// <remarks>
    /// Distances beyond this are clamped to 1.
    /// </remarks>
    public float MaxDistance { get; set; } = 10f;

    /// <summary>
    /// Gets or sets the blackboard key for the target position.
    /// </summary>
    /// <remarks>
    /// The value at this key should be a <see cref="Vector3"/>.
    /// </remarks>
    public string TargetKey { get; set; } = BBKeys.TargetPosition;

    /// <summary>
    /// Gets or sets the value to return when no target exists.
    /// </summary>
    public float NoTargetValue { get; set; } = 1f;

    /// <inheritdoc/>
    public float GetValue(Entity entity, Blackboard blackboard, IWorld world)
    {
        // Get entity position
        if (!world.Has<Transform3D>(entity))
        {
            return NoTargetValue;
        }

        ref readonly var transform = ref world.Get<Transform3D>(entity);
        var myPosition = transform.Position;

        // Get target position from blackboard
        if (!blackboard.TryGet<Vector3>(TargetKey, out var targetPosition))
        {
            return NoTargetValue;
        }

        var distance = Vector3.Distance(myPosition, targetPosition);
        return Math.Clamp(distance / MaxDistance, 0f, 1f);
    }
}
