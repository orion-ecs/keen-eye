using System.Numerics;

using KeenEyes.Animation.Components;
using KeenEyes.Animation.IK;
using KeenEyes.Animation.IK.Solvers;
using KeenEyes.Common;

namespace KeenEyes.Animation.Systems;

/// <summary>
/// System that solves IK chains after FK animation has been applied.
/// </summary>
/// <remarks>
/// <para>
/// This system runs after <see cref="SkeletonPoseSystem"/> (order 55) at order 57 so that
/// IK adjustments are layered on top of the sampled FK pose. For each entity carrying an
/// <see cref="IKChainReference"/>, the system resolves the chain definition from the
/// <see cref="IKManager"/> world extension, collects the chain's bone entities by walking
/// the entity hierarchy up from the end effector, resolves the target from the referenced
/// <see cref="IKTarget"/> entity, and solves the chain with the solver registered for the
/// chain's <see cref="IKChainDefinition.SolverType"/> (falling back to FABRIK when that
/// solver cannot handle the chain's bone count).
/// </para>
/// <para>
/// The solved pose is blended against the FK pose using the effective weight
/// <c>IKRig.GlobalWeight × IKChainReference.Weight × IKTarget.Weight</c> (clamped to [0, 1]).
/// A weight of zero skips solving entirely, leaving the FK pose untouched; a weight of one
/// applies the full IK result; intermediate weights spherically interpolate each bone's
/// local rotation between the FK and IK poses.
/// </para>
/// <para>
/// Invalid configurations degrade gracefully: chains with missing or dead target entities,
/// unregistered chain IDs, incomplete bone hierarchies, or unavailable solvers are skipped
/// without modifying the FK pose.
/// </para>
/// </remarks>
public sealed class IKSolverSystem : SystemBase
{
    private IKManager? manager;

    // Per-frame caches keyed by entity ID (component IDs reference entities by ID).
    private readonly Dictionary<int, IKTarget> targetCache = [];
    private readonly Dictionary<int, IKRig> rigCache = [];
    private readonly Dictionary<int, Entity> poleEntityCache = [];
    private readonly HashSet<int> requiredPoleIds = [];

    // Reusable scratch buffers keyed by chain length to avoid per-frame allocation.
    private readonly Dictionary<int, Entity[]> boneBuffers = [];
    private readonly Dictionary<int, Quaternion[]> rotationBuffers = [];

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        World.TryGetExtension(out manager);
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        manager ??= World.TryGetExtension<IKManager>(out var m) ? m : null;
        if (manager == null)
        {
            return;
        }

        BuildFrameCaches();

        foreach (var entity in World.Query<IKChainReference, BoneReference, Transform3D>())
        {
            SolveChain(entity, deltaTime);
        }
    }

    private void BuildFrameCaches()
    {
        targetCache.Clear();
        rigCache.Clear();
        poleEntityCache.Clear();
        requiredPoleIds.Clear();

        foreach (var entity in World.Query<IKTarget>())
        {
            ref readonly var target = ref World.Get<IKTarget>(entity);
            targetCache[entity.Id] = target;

            if (target.PoleTargetEntityId >= 0)
            {
                requiredPoleIds.Add(target.PoleTargetEntityId);
            }
        }

        foreach (var entity in World.Query<IKRig>())
        {
            rigCache[entity.Id] = World.Get<IKRig>(entity);
        }

        // Pole targets are referenced by entity ID; resolve the referenced entity
        // handles in a single pass only when at least one target uses a pole.
        if (requiredPoleIds.Count > 0)
        {
            foreach (var entity in World.Query<Transform3D>())
            {
                if (requiredPoleIds.Contains(entity.Id))
                {
                    poleEntityCache[entity.Id] = entity;
                }
            }
        }
    }

    private void SolveChain(Entity endEffector, float deltaTime)
    {
        ref readonly var chainRef = ref World.Get<IKChainReference>(endEffector);
        if (!chainRef.Enabled || chainRef.ChainId < 0)
        {
            return;
        }

        if (!manager!.TryGetChain(chainRef.ChainId, out var chain) || chain is null)
        {
            return;
        }

        // The rig (if any) lives on the skeleton root referenced by the end effector's bone.
        ref readonly var boneRef = ref World.Get<BoneReference>(endEffector);
        var globalWeight = 1f;
        if (rigCache.TryGetValue(boneRef.SkeletonRootId, out var rig))
        {
            if (!rig.Enabled)
            {
                return;
            }

            globalWeight = rig.GlobalWeight;
        }

        if (chainRef.TargetEntityId < 0 ||
            !targetCache.TryGetValue(chainRef.TargetEntityId, out var target))
        {
            return;
        }

        var weight = Math.Clamp(globalWeight * chainRef.Weight * target.Weight, 0f, 1f);
        if (weight.IsApproximatelyZero())
        {
            return;
        }

        if (!TryCollectBones(endEffector, chain, out var bones))
        {
            return;
        }

        var solver = ResolveSolver(chain.SolverType, bones.Length);
        if (solver is null)
        {
            return;
        }

        Vector3? polePosition = null;
        if (target.PoleTargetEntityId >= 0 &&
            poleEntityCache.TryGetValue(target.PoleTargetEntityId, out var poleEntity))
        {
            polePosition = IKSolverMath.GetWorldTransform(World, poleEntity).Position;
        }

        // Snapshot the FK rotations so the solved pose can be blended back toward them.
        // Solvers only modify local rotations, so rotations are all that need saving.
        var fkRotations = GetRotationBuffer(bones.Length);
        for (var i = 0; i < bones.Length; i++)
        {
            fkRotations[i] = World.Get<Transform3D>(bones[i]).Rotation;
        }

        var context = new IKSolverContext
        {
            World = World,
            Chain = chain,
            BoneEntities = bones,
            TargetPosition = target.Position,
            TargetRotation = target.UseRotation ? target.Rotation : null,
            PolePosition = polePosition,
            DeltaTime = deltaTime,
        };

        solver.Solve(in context);

        if (weight < 1f && !weight.ApproximatelyEquals(1f))
        {
            for (var i = 0; i < bones.Length; i++)
            {
                ref var transform = ref World.Get<Transform3D>(bones[i]);
                transform.Rotation = Quaternion.Slerp(fkRotations[i], transform.Rotation, weight);
            }
        }
    }

    /// <summary>
    /// Collects the chain's bone entities by walking the hierarchy up from the end
    /// effector, validating each bone against the chain definition's bone names.
    /// </summary>
    private bool TryCollectBones(Entity endEffector, IKChainDefinition chain, out Entity[] bones)
    {
        if (chain.BoneCount < 2)
        {
            bones = [];
            return false;
        }

        bones = GetBoneBuffer(chain.BoneCount);

        var current = endEffector;
        for (var i = chain.BoneCount - 1; i >= 0; i--)
        {
            if (!current.IsValid ||
                !World.IsAlive(current) ||
                !World.Has<Transform3D>(current) ||
                !World.Has<BoneReference>(current))
            {
                return false;
            }

            ref readonly var boneRef = ref World.Get<BoneReference>(current);
            if (!string.Equals(boneRef.BoneName, chain.BoneNames[i], StringComparison.Ordinal))
            {
                return false;
            }

            bones[i] = current;
            current = World.GetParent(current);
        }

        return true;
    }

    /// <summary>
    /// Resolves the solver for a chain, falling back to FABRIK when the configured
    /// solver is unavailable or cannot handle the chain's bone count.
    /// </summary>
    private IIKSolver? ResolveSolver(IKSolverType solverType, int boneCount)
    {
        if (manager!.TryGetSolver(solverType, out var solver) &&
            solver is not null &&
            solver.CanHandle(boneCount))
        {
            return solver;
        }

        if (solverType != IKSolverType.FABRIK &&
            manager.TryGetSolver(IKSolverType.FABRIK, out var fallback) &&
            fallback is not null &&
            fallback.CanHandle(boneCount))
        {
            return fallback;
        }

        return null;
    }

    private Entity[] GetBoneBuffer(int length)
    {
        if (!boneBuffers.TryGetValue(length, out var buffer))
        {
            buffer = new Entity[length];
            boneBuffers[length] = buffer;
        }

        return buffer;
    }

    private Quaternion[] GetRotationBuffer(int length)
    {
        if (!rotationBuffers.TryGetValue(length, out var buffer))
        {
            buffer = new Quaternion[length];
            rotationBuffers[length] = buffer;
        }

        return buffer;
    }
}
