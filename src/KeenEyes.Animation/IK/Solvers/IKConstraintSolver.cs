using System.Numerics;

using KeenEyes.Animation.Components;
using KeenEyes.Common;

namespace KeenEyes.Animation.IK.Solvers;

/// <summary>
/// Clamps bone rotations to <see cref="IKConstraint"/> joint limits during IK solving.
/// </summary>
/// <remarks>
/// <para>
/// Constraints operate on LOCAL (parent-relative) bone rotations, the same space
/// <see cref="IKSolverMath.ApplyWorldPositions"/> writes solved poses into. All angle
/// limits are expressed in degrees, matching the <see cref="IKConstraint"/> field
/// conventions.
/// </para>
/// <para>
/// Hinge constraints keep only the rotation component about <see cref="IKConstraint.Axis"/>
/// (any swing off the hinge axis is removed) and clamp the signed angle to
/// <c>[MinAngles.X, MaxAngles.X]</c>. Ball-socket constraints use swing-twist decomposition
/// about <see cref="IKConstraint.Axis"/>: the swing angle is clamped to
/// <see cref="IKConstraint.ConeAngle"/> and the twist angle to
/// ±<see cref="IKConstraint.TwistLimit"/>. Euler constraints clamp the pitch/yaw/roll
/// angles (rotations about X/Y/Z respectively) independently against
/// <see cref="IKConstraint.MinAngles"/> and <see cref="IKConstraint.MaxAngles"/>.
/// </para>
/// </remarks>
internal static class IKConstraintSolver
{
    private const float DegToRad = MathF.PI / 180f;
    private const float RadToDeg = 180f / MathF.PI;

    /// <summary>
    /// Clamps a bone's parent-relative rotation to the limits of its constraint.
    /// </summary>
    /// <param name="localRotation">The bone's local (parent-relative) rotation.</param>
    /// <param name="constraint">The joint constraint to enforce.</param>
    /// <returns>The rotation clamped to the constraint limits.</returns>
    internal static Quaternion Apply(Quaternion localRotation, in IKConstraint constraint)
        => constraint.ConstraintType switch
        {
            IKConstraintType.Hinge => ApplyHinge(localRotation, constraint),
            IKConstraintType.BallSocket => ApplyBallSocket(localRotation, constraint),
            IKConstraintType.Euler => ApplyEuler(localRotation, constraint),
            _ => localRotation,
        };

    /// <summary>
    /// Collects the per-bone constraints for a chain, indexed to match <paramref name="bones"/>.
    /// </summary>
    /// <param name="world">The world containing the bones.</param>
    /// <param name="bones">Bone entities ordered root to tip.</param>
    /// <returns>
    /// An array with one entry per bone (unconstrained bones hold
    /// <see cref="IKConstraintType.None"/>), or <see langword="null"/> when no bone in the
    /// chain carries an active constraint so solvers can skip constraint handling entirely.
    /// </returns>
    internal static IKConstraint[]? GetChainConstraints(IWorld world, Entity[] bones)
    {
        IKConstraint[]? constraints = null;

        for (var i = 0; i < bones.Length; i++)
        {
            if (!world.Has<IKConstraint>(bones[i]))
            {
                continue;
            }

            ref readonly var constraint = ref world.Get<IKConstraint>(bones[i]);
            if (constraint.ConstraintType == IKConstraintType.None)
            {
                continue;
            }

            constraints ??= new IKConstraint[bones.Length];
            constraints[i] = constraint;
        }

        return constraints;
    }

    /// <summary>
    /// Clamps a hinge joint: only rotation about the hinge axis is kept, with the signed
    /// angle limited to <c>[MinAngles.X, MaxAngles.X]</c> degrees.
    /// </summary>
    private static Quaternion ApplyHinge(Quaternion rotation, in IKConstraint constraint)
    {
        if (constraint.Axis.LengthSquared().IsApproximatelyZero())
        {
            return rotation;
        }

        var axis = Vector3.Normalize(constraint.Axis);
        DecomposeSwingTwist(Canonicalize(rotation), axis, out _, out var twist);

        var angle = SignedTwistAngle(twist, axis) * RadToDeg;
        var clamped = Math.Clamp(angle, constraint.MinAngles.X, constraint.MaxAngles.X);

        return Quaternion.CreateFromAxisAngle(axis, clamped * DegToRad);
    }

    /// <summary>
    /// Clamps a ball-socket joint: the swing away from the socket axis is limited to the
    /// cone angle and the twist about the axis to ±<see cref="IKConstraint.TwistLimit"/>.
    /// </summary>
    private static Quaternion ApplyBallSocket(Quaternion rotation, in IKConstraint constraint)
    {
        if (constraint.Axis.LengthSquared().IsApproximatelyZero())
        {
            return rotation;
        }

        var axis = Vector3.Normalize(constraint.Axis);
        DecomposeSwingTwist(Canonicalize(rotation), axis, out var swing, out var twist);

        swing = Canonicalize(swing);
        var swingAngle = 2f * MathF.Acos(Math.Clamp(swing.W, -1f, 1f));
        var coneAngle = constraint.ConeAngle * DegToRad;

        if (swingAngle > coneAngle)
        {
            var swingAxis = new Vector3(swing.X, swing.Y, swing.Z);
            if (!swingAxis.LengthSquared().IsApproximatelyZero())
            {
                swing = Quaternion.CreateFromAxisAngle(Vector3.Normalize(swingAxis), coneAngle);
            }
        }

        var twistAngle = SignedTwistAngle(twist, axis) * RadToDeg;
        var clampedTwist = Math.Clamp(twistAngle, -constraint.TwistLimit, constraint.TwistLimit);
        var twistRotation = Quaternion.CreateFromAxisAngle(axis, clampedTwist * DegToRad);

        return Quaternion.Normalize(swing * twistRotation);
    }

    /// <summary>
    /// Clamps an Euler joint: pitch (X), yaw (Y), and roll (Z) angles are extracted in the
    /// <see cref="Quaternion.CreateFromYawPitchRoll"/> convention and clamped independently.
    /// </summary>
    private static Quaternion ApplyEuler(Quaternion rotation, in IKConstraint constraint)
    {
        var q = Canonicalize(rotation);

        var pitch = MathF.Asin(Math.Clamp(2f * ((q.W * q.X) - (q.Y * q.Z)), -1f, 1f));
        var yaw = MathF.Atan2(
            2f * ((q.X * q.Z) + (q.W * q.Y)),
            1f - (2f * ((q.X * q.X) + (q.Y * q.Y))));
        var roll = MathF.Atan2(
            2f * ((q.X * q.Y) + (q.W * q.Z)),
            1f - (2f * ((q.X * q.X) + (q.Z * q.Z))));

        var clampedPitch = Math.Clamp(pitch * RadToDeg, constraint.MinAngles.X, constraint.MaxAngles.X);
        var clampedYaw = Math.Clamp(yaw * RadToDeg, constraint.MinAngles.Y, constraint.MaxAngles.Y);
        var clampedRoll = Math.Clamp(roll * RadToDeg, constraint.MinAngles.Z, constraint.MaxAngles.Z);

        return Quaternion.CreateFromYawPitchRoll(
            clampedYaw * DegToRad, clampedPitch * DegToRad, clampedRoll * DegToRad);
    }

    /// <summary>
    /// Decomposes a rotation into swing (perpendicular to the axis) and twist (about the
    /// axis) components such that <c>rotation = swing * twist</c>.
    /// </summary>
    private static void DecomposeSwingTwist(
        Quaternion rotation, Vector3 axis, out Quaternion swing, out Quaternion twist)
    {
        var projection = Vector3.Dot(new Vector3(rotation.X, rotation.Y, rotation.Z), axis);
        var candidate = new Quaternion(axis * projection, rotation.W);

        if (candidate.LengthSquared().IsApproximatelyZero())
        {
            // 180-degree rotation about an axis perpendicular to the twist axis: pure swing.
            twist = Quaternion.Identity;
        }
        else
        {
            twist = Quaternion.Normalize(candidate);
        }

        swing = Quaternion.Normalize(rotation * Quaternion.Inverse(twist));
    }

    /// <summary>
    /// Returns the signed rotation angle (radians) of a twist quaternion about the axis.
    /// </summary>
    private static float SignedTwistAngle(Quaternion twist, Vector3 axis)
    {
        var canonical = Canonicalize(twist);
        var projection = Vector3.Dot(new Vector3(canonical.X, canonical.Y, canonical.Z), axis);
        return 2f * MathF.Atan2(projection, canonical.W);
    }

    /// <summary>
    /// Negates a quaternion when its scalar part is negative so angle extraction stays in
    /// the shortest-arc range.
    /// </summary>
    private static Quaternion Canonicalize(Quaternion rotation)
        => rotation.W < 0f ? Quaternion.Negate(rotation) : rotation;
}
