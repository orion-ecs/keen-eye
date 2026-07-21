using System.Numerics;

using KeenEyes.Common;

namespace KeenEyes.Animation.IK.Solvers;

/// <summary>
/// Analytical two-bone IK solver using the law of cosines.
/// </summary>
/// <remarks>
/// <para>
/// Solves three-bone chains (root, mid, tip — e.g. shoulder/elbow/hand or hip/knee/foot)
/// in O(1) constant time. The mid joint is placed on the bend plane defined by the
/// root-to-target axis and the pole reference (<see cref="IKSolverContext.PolePosition"/>
/// if provided, otherwise <see cref="IKChainDefinition.PoleVector"/>).
/// </para>
/// <para>
/// Targets beyond reach stretch the chain straight toward the target (bone lengths are
/// preserved); targets closer than the minimum reach fold the limb naturally toward the
/// pole. When <see cref="IKSolverContext.TargetRotation"/> is set, the end effector's
/// world rotation is matched to it.
/// </para>
/// <para>
/// Bone <see cref="Transform3D"/> components hold LOCAL (parent-relative) transforms; the
/// solver composes world positions by walking the entity hierarchy and writes results back
/// as local rotations (see <see cref="IKSolverMath"/> for the space determination).
/// </para>
/// <para>
/// Bones carrying an <see cref="Components.IKConstraint"/> component have their computed
/// local rotations clamped to the joint limits when the analytic pose is written back
/// (root and mid rotations, plus the end effector when a target rotation is requested).
/// Constrained chains may not reach the target; the reported error always measures the
/// actual end effector position. Bones without a constraint are unaffected.
/// </para>
/// </remarks>
public sealed class TwoBoneSolver : IIKSolver
{
    /// <inheritdoc />
    public string Name => "TwoBone";

    /// <inheritdoc />
    public IKSolverType SolverType => IKSolverType.TwoBone;

    /// <inheritdoc />
    public bool CanHandle(int boneCount) => boneCount == 3;

    /// <inheritdoc />
    public IKSolverResult Solve(in IKSolverContext context)
    {
        var world = context.World;
        var bones = context.BoneEntities;

        var rootPos = IKSolverMath.GetWorldTransform(world, bones[0]).Position;
        var midPos = IKSolverMath.GetWorldTransform(world, bones[1]).Position;
        var tipPos = IKSolverMath.GetWorldTransform(world, bones[2]).Position;

        var upperLength = Vector3.Distance(rootPos, midPos);
        var lowerLength = Vector3.Distance(midPos, tipPos);
        var totalLength = upperLength + lowerLength;

        if (totalLength.IsApproximatelyZero())
        {
            // Degenerate chain with zero-length bones: nothing to rotate.
            return IKSolverResult.Partial(0, Vector3.Distance(tipPos, context.TargetPosition));
        }

        var toTarget = context.TargetPosition - rootPos;
        var targetDistance = toTarget.Length();

        // Direction from root toward the target; fall back to the current tip direction
        // (then an arbitrary axis) when the target sits on the root.
        Vector3 axis;
        if (targetDistance.IsApproximatelyZero())
        {
            var currentReach = tipPos - rootPos;
            axis = currentReach.LengthSquared().IsApproximatelyZero()
                ? Vector3.UnitX
                : Vector3.Normalize(currentReach);
        }
        else
        {
            axis = toTarget / targetDistance;
        }

        // Clamp the reach: beyond-reach targets stretch the chain straight, too-close
        // targets fold the limb to its minimum reach.
        var minReach = MathF.Abs(upperLength - lowerLength);
        var reach = Math.Clamp(targetDistance, minReach, totalLength);

        // Bend direction: pole reference projected onto the plane perpendicular to the axis.
        var poleReference = context.PolePosition.HasValue
            ? context.PolePosition.Value - rootPos
            : context.Chain.PoleVector;
        var bendDir = poleReference - (axis * Vector3.Dot(poleReference, axis));

        if (bendDir.LengthSquared().IsApproximatelyZero())
        {
            // Pole is degenerate (zero or parallel to the axis): preserve the current
            // bend plane, or pick any perpendicular direction for a straight chain.
            var currentBend = (midPos - rootPos) - (axis * Vector3.Dot(midPos - rootPos, axis));
            bendDir = currentBend.LengthSquared().IsApproximatelyZero()
                ? IKSolverMath.AnyPerpendicular(axis)
                : currentBend;
        }

        bendDir = Vector3.Normalize(bendDir);

        // Law of cosines: angle at the root between the target axis and the upper bone.
        float cosRoot;
        var denominator = 2f * upperLength * reach;
        if (denominator.IsApproximatelyZero())
        {
            // Zero-length upper bone or fully folded chain (reach ~ 0): bend fully
            // toward the pole.
            cosRoot = 0f;
        }
        else
        {
            cosRoot = ((upperLength * upperLength) + (reach * reach) - (lowerLength * lowerLength))
                / denominator;
        }

        cosRoot = Math.Clamp(cosRoot, -1f, 1f);
        var sinRoot = MathF.Sqrt(MathF.Max(0f, 1f - (cosRoot * cosRoot)));

        var solvedMid = rootPos + (((axis * cosRoot) + (bendDir * sinRoot)) * upperLength);
        var solvedTip = rootPos + (axis * reach);

        Span<Vector3> solved = stackalloc Vector3[3];
        solved[0] = rootPos;
        solved[1] = solvedMid;
        solved[2] = solvedTip;

        var constraints = IKConstraintSolver.GetChainConstraints(world, bones);
        IKSolverMath.ApplyWorldPositions(world, bones, solved, context.TargetRotation, constraints);

        // Constraint clamping can pull the end effector off the analytic solution, so
        // measure the error from the actual tip position when constraints are active.
        var error = constraints is null
            ? Vector3.Distance(solvedTip, context.TargetPosition)
            : Vector3.Distance(
                IKSolverMath.GetWorldTransform(world, bones[2]).Position,
                context.TargetPosition);

        return error <= context.Chain.Tolerance
            ? IKSolverResult.Success(0, error)
            : IKSolverResult.Partial(0, error);
    }
}
