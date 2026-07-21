using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using KeenEyes.Common;

namespace KeenEyes.Animation.IK.Solvers;

/// <summary>
/// Iterative FABRIK (Forward And Backward Reaching Inverse Kinematics) solver
/// for multi-bone chains such as spines, tails, and tentacles.
/// </summary>
/// <remarks>
/// <para>
/// Each iteration performs a forward pass (moving the end effector to the target and
/// repositioning joints toward the root) followed by a backward pass (re-anchoring the
/// root and repositioning joints toward the tip), preserving bone lengths throughout.
/// Iteration stops when the end effector is within <see cref="IKChainDefinition.Tolerance"/>
/// of the target or <see cref="IKChainDefinition.MaxIterations"/> is reached.
/// </para>
/// <para>
/// Unreachable targets stretch the chain straight toward the target with bone lengths
/// preserved. Bone rotations are reconstructed from the solved joint positions.
/// </para>
/// <para>
/// Bone <see cref="Transform3D"/> components hold LOCAL (parent-relative) transforms; the
/// solver composes world positions by walking the entity hierarchy and writes results back
/// as local rotations (see <see cref="IKSolverMath"/> for the space determination).
/// </para>
/// </remarks>
[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase",
    Justification = "FABRIK is an established algorithm acronym, consistent with IKSolverType.FABRIK.")]
public sealed class FABRIKSolver : IIKSolver
{
    /// <inheritdoc />
    public string Name => "FABRIK";

    /// <inheritdoc />
    public IKSolverType SolverType => IKSolverType.FABRIK;

    /// <inheritdoc />
    public bool CanHandle(int boneCount) => boneCount >= 2;

    /// <inheritdoc />
    public IKSolverResult Solve(in IKSolverContext context)
    {
        var world = context.World;
        var bones = context.BoneEntities;
        var boneCount = bones.Length;
        var target = context.TargetPosition;

        var positions = new Vector3[boneCount];
        IKSolverMath.GetWorldPositions(world, bones, positions);

        var lengths = new float[boneCount - 1];
        var totalLength = 0f;
        for (var i = 0; i < lengths.Length; i++)
        {
            lengths[i] = Vector3.Distance(positions[i], positions[i + 1]);
            totalLength += lengths[i];
        }

        if (totalLength.IsApproximatelyZero())
        {
            // Degenerate chain with zero-length bones: nothing to rotate.
            return IKSolverResult.Partial(0, Vector3.Distance(positions[^1], target));
        }

        var rootPos = positions[0];
        var tolerance = MathF.Max(context.Chain.Tolerance, 0f);
        var rootToTarget = target - rootPos;

        if (rootToTarget.Length() > totalLength)
        {
            // Unreachable: stretch the chain straight toward the target.
            var direction = Vector3.Normalize(rootToTarget);
            for (var i = 0; i < lengths.Length; i++)
            {
                positions[i + 1] = positions[i] + (direction * lengths[i]);
            }

            IKSolverMath.ApplyWorldPositions(world, bones, positions, tipWorldRotation: null);
            return IKSolverResult.Partial(0, Vector3.Distance(positions[^1], target));
        }

        var error = Vector3.Distance(positions[^1], target);
        var iterations = 0;
        var maxIterations = Math.Max(1, context.Chain.MaxIterations);

        while (error > tolerance && iterations < maxIterations)
        {
            iterations++;

            // Forward pass: move the end effector to the target, reposition toward the root.
            positions[^1] = target;
            for (var i = boneCount - 2; i >= 0; i--)
            {
                var direction = SafeDirection(positions[i] - positions[i + 1], -rootToTarget);
                positions[i] = positions[i + 1] + (direction * lengths[i]);
            }

            // Backward pass: re-anchor the root, reposition toward the tip.
            positions[0] = rootPos;
            for (var i = 0; i < boneCount - 1; i++)
            {
                var direction = SafeDirection(positions[i + 1] - positions[i], rootToTarget);
                positions[i + 1] = positions[i] + (direction * lengths[i]);
            }

            error = Vector3.Distance(positions[^1], target);
        }

        IKSolverMath.ApplyWorldPositions(world, bones, positions, tipWorldRotation: null);

        return error <= tolerance
            ? IKSolverResult.Success(iterations, error)
            : IKSolverResult.Partial(iterations, error);
    }

    /// <summary>
    /// Normalizes a direction, falling back when joints are coincident.
    /// </summary>
    private static Vector3 SafeDirection(Vector3 direction, Vector3 fallback)
    {
        if (!direction.LengthSquared().IsApproximatelyZero())
        {
            return Vector3.Normalize(direction);
        }

        return fallback.LengthSquared().IsApproximatelyZero()
            ? Vector3.UnitY
            : Vector3.Normalize(fallback);
    }
}
