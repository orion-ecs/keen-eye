using System.Numerics;

using KeenEyes.Animation.Components;
using KeenEyes.Animation.IK.Solvers;
using KeenEyes.Common;

namespace KeenEyes.Animation.Systems;

/// <summary>
/// System that applies <see cref="LookAtTarget"/> constraints so bones aim at
/// entity or world-position targets.
/// </summary>
/// <remarks>
/// <para>
/// This system runs at order 58, after <see cref="IKSolverSystem"/> (order 57) and
/// before <see cref="TweenSystem"/> (order 60), so look-at adjustments (head tracking,
/// eye following, weapon aiming) are layered on top of both the sampled FK pose and any
/// solved IK chains.
/// </para>
/// <para>
/// For each entity carrying a <see cref="LookAtTarget"/> and a <see cref="Transform3D"/>,
/// the system resolves the target position (a tracked entity's world position when
/// <see cref="LookAtTarget.TargetEntityId"/> is set, otherwise
/// <see cref="LookAtTarget.WorldTarget"/>), computes the clamped, smoothed world rotation
/// via <see cref="LookAtSolver"/>, blends it against the FK rotation using
/// <see cref="LookAtTarget.Weight"/>, and writes the result back as a LOCAL
/// (parent-relative) rotation. The smoothed offset state persists in
/// <see cref="LookAtTarget.CurrentRotation"/>.
/// </para>
/// <para>
/// The max-angle clamp and weight blend are always measured against the bone's FK
/// rotation. When animation re-poses the bone every frame the current transform IS the
/// FK pose; for bones nothing re-poses, the system remembers the world rotation it wrote
/// and the FK rotation that produced it, so the constraint never compounds its own
/// output from previous frames.
/// </para>
/// <para>
/// Invalid configurations degrade gracefully: constraints with dead or missing target
/// entities, zero weight, degenerate forward axes, or targets coinciding with the bone
/// position are skipped without modifying the FK pose.
/// </para>
/// </remarks>
public sealed class LookAtSystem : SystemBase
{
    // Per-frame cache of target world positions keyed by entity ID
    // (LookAtTarget references target entities by ID).
    private readonly Dictionary<int, Vector3> targetPositionCache = [];
    private readonly HashSet<int> requiredTargetIds = [];

    // Persistent per-entity record of the world rotation this system wrote and the FK
    // world rotation it was derived from. When a bone's current rotation still matches
    // the written value (nothing re-posed it since), the stored FK rotation is reused as
    // the clamp/blend reference instead of treating our own output as the FK pose.
    private readonly Dictionary<int, AppliedRotation> appliedRotations = [];
    private readonly HashSet<int> activeEntityIds = [];
    private readonly List<int> staleEntityIds = [];

    private readonly record struct AppliedRotation(Quaternion Written, Quaternion Fk);

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        BuildTargetCache();

        activeEntityIds.Clear();
        foreach (var entity in World.Query<LookAtTarget, Transform3D>())
        {
            activeEntityIds.Add(entity.Id);
            ApplyLookAt(entity, deltaTime);
        }

        PruneStaleState();
    }

    private void BuildTargetCache()
    {
        targetPositionCache.Clear();
        requiredTargetIds.Clear();

        foreach (var entity in World.Query<LookAtTarget>())
        {
            ref readonly var lookAt = ref World.Get<LookAtTarget>(entity);
            if (lookAt.TargetEntityId >= 0)
            {
                requiredTargetIds.Add(lookAt.TargetEntityId);
            }
        }

        // Targets are referenced by entity ID; resolve their world positions in a
        // single pass only when at least one constraint tracks an entity.
        if (requiredTargetIds.Count > 0)
        {
            foreach (var entity in World.Query<Transform3D>())
            {
                if (requiredTargetIds.Contains(entity.Id))
                {
                    targetPositionCache[entity.Id] =
                        IKSolverMath.GetWorldTransform(World, entity).Position;
                }
            }
        }
    }

    private void ApplyLookAt(Entity entity, float deltaTime)
    {
        ref var lookAt = ref World.Get<LookAtTarget>(entity);

        var weight = Math.Clamp(lookAt.Weight, 0f, 1f);
        if (weight.IsApproximatelyZero())
        {
            return;
        }

        Vector3 targetPosition;
        if (lookAt.TargetEntityId >= 0)
        {
            // Dead or missing target entities leave the FK pose untouched.
            if (!targetPositionCache.TryGetValue(lookAt.TargetEntityId, out targetPosition))
            {
                return;
            }
        }
        else
        {
            targetPosition = lookAt.WorldTarget;
        }

        // Bone Transform3D components are LOCAL (parent-relative); compose the parent's
        // world transform so the solved world rotation can be written back locally.
        var parentPosition = Vector3.Zero;
        var parentRotation = Quaternion.Identity;
        var parentScale = Vector3.One;

        var parent = World.GetParent(entity);
        if (parent.IsValid && World.Has<Transform3D>(parent))
        {
            (parentPosition, parentRotation, parentScale) =
                IKSolverMath.GetWorldTransform(World, parent);
        }

        ref var transform = ref World.Get<Transform3D>(entity);
        var currentWorldRotation = Quaternion.Normalize(parentRotation * transform.Rotation);
        var bonePosition = parentPosition +
            Vector3.Transform(parentScale * transform.Position, parentRotation);

        // If the bone still holds exactly what this system wrote last frame, nothing has
        // re-posed it since, so the stored FK rotation remains the constraint reference.
        var fkWorldRotation = currentWorldRotation;
        if (appliedRotations.TryGetValue(entity.Id, out var applied) &&
            MathF.Abs(Quaternion.Dot(currentWorldRotation, applied.Written)) > 1f - FloatExtensions.DefaultEpsilon)
        {
            fkWorldRotation = applied.Fk;
        }

        var smoothedOffset = lookAt.CurrentRotation;
        var solvedWorldRotation = LookAtSolver.Solve(
            bonePosition,
            fkWorldRotation,
            targetPosition,
            lookAt.ForwardAxis,
            lookAt.MaxAngle,
            lookAt.Smoothing,
            deltaTime,
            ref smoothedOffset);
        lookAt.CurrentRotation = smoothedOffset;

        var finalWorldRotation = solvedWorldRotation;
        if (weight < 1f && !weight.ApproximatelyEquals(1f))
        {
            finalWorldRotation = Quaternion.Slerp(fkWorldRotation, solvedWorldRotation, weight);
        }

        finalWorldRotation = Quaternion.Normalize(finalWorldRotation);
        transform.Rotation = Quaternion.Normalize(
            Quaternion.Inverse(parentRotation) * finalWorldRotation);
        appliedRotations[entity.Id] = new AppliedRotation(finalWorldRotation, fkWorldRotation);
    }

    private void PruneStaleState()
    {
        if (appliedRotations.Count == activeEntityIds.Count)
        {
            return;
        }

        staleEntityIds.Clear();
        foreach (var id in appliedRotations.Keys)
        {
            if (!activeEntityIds.Contains(id))
            {
                staleEntityIds.Add(id);
            }
        }

        foreach (var id in staleEntityIds)
        {
            appliedRotations.Remove(id);
        }
    }
}
