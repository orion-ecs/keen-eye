using System.Numerics;

using KeenEyes.Common;

namespace KeenEyes.Animation.IK.Solvers;

/// <summary>
/// Pure-math solver for single-bone look-at/aim constraints (head tracking, eye
/// following, weapon aiming).
/// </summary>
/// <remarks>
/// <para>
/// The solver computes the shortest-arc rotation that swings the bone's FK forward
/// direction (the component's forward axis rotated by the FK world rotation) onto the
/// direction toward the target. That offset rotation is clamped so its angle never
/// exceeds the maximum angle, meaning the bone can never aim further than the limit
/// away from its FK forward direction.
/// </para>
/// <para>
/// Smoothing is a frame-rate-independent exponential approach toward the clamped goal
/// offset: each step interpolates the stored offset by
/// <c>alpha = 1 - exp(-deltaTime / smoothing)</c>, so the remaining angular error after a
/// total elapsed time <c>T</c> is <c>exp(-T / smoothing)</c> of the initial error
/// regardless of how many frames that time is divided into. A smoothing value of zero
/// (or less) snaps instantly to the goal.
/// </para>
/// <para>
/// The smoothed offset is stored in offset space (relative to the current frame's FK
/// forward direction), so the constraint layers naturally on top of an animating FK
/// pose and <see cref="Quaternion.Identity"/> represents "no deviation from FK".
/// </para>
/// </remarks>
internal static class LookAtSolver
{
    /// <summary>
    /// Solves the look-at constraint for a single bone, returning the clamped,
    /// smoothed world rotation.
    /// </summary>
    /// <param name="bonePosition">The bone's world-space position.</param>
    /// <param name="fkWorldRotation">The bone's FK world-space rotation before the constraint.</param>
    /// <param name="targetPosition">The world-space position to look at.</param>
    /// <param name="forwardAxis">The bone's local forward axis (need not be normalized).</param>
    /// <param name="maxAngleDegrees">Maximum allowed angle, in degrees, between the FK forward direction and the aimed direction.</param>
    /// <param name="smoothing">Smoothing time constant in seconds; zero or less snaps instantly.</param>
    /// <param name="deltaTime">Elapsed frame time in seconds.</param>
    /// <param name="smoothedOffset">
    /// The persisted smoothed offset rotation relative to the FK forward direction.
    /// Updated in place; pass <see cref="Quaternion.Identity"/> (or a zero quaternion)
    /// to start from the unmodified FK pose.
    /// </param>
    /// <returns>
    /// The bone's new world rotation, or <paramref name="fkWorldRotation"/> unchanged when
    /// the target coincides with the bone position or the forward axis is degenerate.
    /// </returns>
    internal static Quaternion Solve(
        Vector3 bonePosition,
        Quaternion fkWorldRotation,
        Vector3 targetPosition,
        Vector3 forwardAxis,
        float maxAngleDegrees,
        float smoothing,
        float deltaTime,
        ref Quaternion smoothedOffset)
    {
        var toTarget = targetPosition - bonePosition;
        if (toTarget.LengthSquared().IsApproximatelyZero() ||
            forwardAxis.LengthSquared().IsApproximatelyZero())
        {
            return fkWorldRotation;
        }

        var desiredDirection = Vector3.Normalize(toTarget);
        var fkForward = Vector3.Normalize(
            Vector3.Transform(Vector3.Normalize(forwardAxis), fkWorldRotation));

        var goalOffset = ClampAngle(
            IKSolverMath.RotationBetween(fkForward, desiredDirection),
            maxAngleDegrees);

        // A zero quaternion means the offset state was never initialized (default
        // struct value); treat it as "no deviation from FK".
        var previousOffset = smoothedOffset.LengthSquared().IsApproximatelyZero()
            ? Quaternion.Identity
            : Quaternion.Normalize(smoothedOffset);

        // Frame-rate-independent exponential smoothing: alpha = 1 - exp(-dt / smoothing).
        var alpha = smoothing <= FloatExtensions.DefaultEpsilon
            ? 1f
            : 1f - MathF.Exp(-deltaTime / smoothing);

        smoothedOffset = Quaternion.Normalize(Quaternion.Slerp(previousOffset, goalOffset, alpha));

        return Quaternion.Normalize(smoothedOffset * fkWorldRotation);
    }

    /// <summary>
    /// Clamps a rotation so its angle does not exceed the given limit, preserving
    /// the rotation axis.
    /// </summary>
    /// <param name="rotation">The rotation to clamp.</param>
    /// <param name="maxAngleDegrees">The maximum allowed rotation angle in degrees.</param>
    /// <returns>The rotation, shortened along its axis if it exceeded the limit.</returns>
    private static Quaternion ClampAngle(Quaternion rotation, float maxAngleDegrees)
    {
        var maxRadians = MathF.Max(maxAngleDegrees, 0f) * (MathF.PI / 180f);

        var shortest = Quaternion.Normalize(rotation);
        if (shortest.W < 0f)
        {
            shortest = -shortest;
        }

        var angle = 2f * MathF.Acos(Math.Clamp(shortest.W, -1f, 1f));
        if (angle <= maxRadians || angle.IsApproximatelyZero())
        {
            return shortest;
        }

        return Quaternion.Slerp(Quaternion.Identity, shortest, maxRadians / angle);
    }
}
