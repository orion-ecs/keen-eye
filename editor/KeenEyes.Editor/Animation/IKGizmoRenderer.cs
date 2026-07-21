using System.Numerics;

using KeenEyes.Animation;
using KeenEyes.Animation.Components;
using KeenEyes.Animation.IK;
using KeenEyes.Common;
using KeenEyes.Editor.Abstractions.Capabilities;

namespace KeenEyes.Editor.Animation;

/// <summary>
/// Gizmo renderer that visualizes IK chains, targets, and pole vectors in the viewport.
/// </summary>
/// <remarks>
/// <para>
/// For every end effector entity carrying an <see cref="IKChainReference"/>, this renderer
/// resolves the chain definition from the <see cref="IKManager"/> world extension, collects
/// the chain's bone entities by walking the entity hierarchy up from the end effector, and
/// draws:
/// <list type="bullet">
///   <item><description>Chain bones as blue lines with a sphere at each joint.</description></item>
///   <item><description>The target position as a green sphere (red when unreachable).</description></item>
///   <item><description>The target rotation as RGB axis lines when the target uses rotation.</description></item>
///   <item><description>The pole vector as a yellow line from the chain's mid joint.</description></item>
/// </list>
/// </para>
/// <para>
/// The effective blend weight (<c>IKRig.GlobalWeight × IKChainReference.Weight ×
/// IKTarget.Weight</c>, clamped to [0, 1]) modulates the opacity of every element, and a target
/// whose distance from the chain root exceeds the total chain length is highlighted in red.
/// </para>
/// <para>
/// The renderer degrades gracefully: worlds without an <see cref="IKManager"/> extension, dead
/// or missing targets, unregistered chain IDs, and incomplete bone hierarchies are skipped
/// without drawing anything and without throwing.
/// </para>
/// </remarks>
public sealed class IKGizmoRenderer : IGizmoRenderer
{
    private static readonly Vector4 ChainLineColor = new(0.2f, 0.4f, 1.0f, 1.0f);   // Blue
    private static readonly Vector4 JointColor = new(0.3f, 0.6f, 1.0f, 1.0f);       // Blue
    private static readonly Vector4 TargetColor = new(0.2f, 0.9f, 0.3f, 1.0f);      // Green
    private static readonly Vector4 UnreachableColor = new(1.0f, 0.2f, 0.2f, 1.0f); // Red
    private static readonly Vector4 PoleColor = new(1.0f, 0.9f, 0.2f, 1.0f);        // Yellow
    private static readonly Vector4 AxisXColor = new(1.0f, 0.2f, 0.2f, 1.0f);       // Red
    private static readonly Vector4 AxisYColor = new(0.2f, 1.0f, 0.2f, 1.0f);       // Green
    private static readonly Vector4 AxisZColor = new(0.2f, 0.4f, 1.0f, 1.0f);       // Blue

    private const float JointSphereRadius = 0.03f;
    private const float TargetSphereRadius = 0.05f;
    private const float LineWidth = 2.0f;
    private const float AxisLength = 0.15f;
    private const float JointPointSize = 6.0f;

    /// <inheritdoc />
    public string Id => "ik-gizmo";

    /// <inheritdoc />
    public string DisplayName => "IK Chains";

    /// <inheritdoc />
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc />
    public int Order => 101; // Render just after the skeleton gizmo (100)

    /// <inheritdoc />
    public void Render(GizmoRenderContext context)
    {
        if (!IsEnabled)
        {
            return;
        }

        var world = context.SceneWorld;

        // IK is optional; worlds without the manager extension simply draw nothing.
        if (!world.TryGetExtension<IKManager>(out var manager) || manager is null)
        {
            return;
        }

        // Component IDs reference entities by ID, so build per-frame lookups keyed by ID.
        var targets = new Dictionary<int, IKTarget>();
        var poleNeeded = false;
        foreach (var entity in world.Query<IKTarget>())
        {
            ref readonly var target = ref world.Get<IKTarget>(entity);
            targets[entity.Id] = target;
            if (target.PoleTargetEntityId >= 0)
            {
                poleNeeded = true;
            }
        }

        var rigs = new Dictionary<int, IKRig>();
        foreach (var entity in world.Query<IKRig>())
        {
            rigs[entity.Id] = world.Get<IKRig>(entity);
        }

        // Pole targets are referenced by entity ID; only resolve entity handles when needed.
        Dictionary<int, Entity>? transformsById = null;
        if (poleNeeded)
        {
            transformsById = [];
            foreach (var entity in world.Query<Transform3D>())
            {
                transformsById[entity.Id] = entity;
            }
        }

        foreach (var endEffector in world.Query<IKChainReference, BoneReference, Transform3D>())
        {
            DrawChain(context, manager, endEffector, targets, rigs, transformsById);
        }
    }

    /// <inheritdoc />
    public bool ShouldRender(Entity entity, IWorld sceneWorld)
    {
        // Render for entities participating in IK: chain end effectors, rigs, or targets.
        return sceneWorld.Has<IKChainReference>(entity) ||
               sceneWorld.Has<IKRig>(entity) ||
               sceneWorld.Has<IKTarget>(entity);
    }

    private static void DrawChain(
        GizmoRenderContext context,
        IKManager manager,
        Entity endEffector,
        Dictionary<int, IKTarget> targets,
        Dictionary<int, IKRig> rigs,
        Dictionary<int, Entity>? transformsById)
    {
        var world = context.SceneWorld;

        ref readonly var chainRef = ref world.Get<IKChainReference>(endEffector);
        if (!chainRef.Enabled || chainRef.ChainId < 0)
        {
            return;
        }

        if (!manager.TryGetChain(chainRef.ChainId, out var chain) || chain is null)
        {
            return;
        }

        // The rig (if any) lives on the skeleton root referenced by the end effector's bone.
        var globalWeight = 1f;
        ref readonly var boneRef = ref world.Get<BoneReference>(endEffector);
        if (rigs.TryGetValue(boneRef.SkeletonRootId, out var rig))
        {
            if (!rig.Enabled)
            {
                return;
            }

            globalWeight = rig.GlobalWeight;
        }

        if (chainRef.TargetEntityId < 0 ||
            !targets.TryGetValue(chainRef.TargetEntityId, out var target))
        {
            return;
        }

        var weight = ComputeEffectiveWeight(globalWeight, chainRef.Weight, target.Weight);
        if (weight.IsApproximatelyZero())
        {
            return;
        }

        if (!TryCollectChainBones(world, endEffector, chain, out var bones))
        {
            return;
        }

        var joints = new Vector3[bones.Length];
        for (var i = 0; i < bones.Length; i++)
        {
            joints[i] = GetWorldPosition(world, bones[i]);
        }

        var chainLength = ComputeChainLength(joints);
        var targetDistance = Vector3.Distance(joints[0], target.Position);
        var unreachable = IsUnreachable(chainLength, targetDistance);

        // Chain bones: blue lines with a joint sphere at each bone.
        var jointColor = WithAlpha(JointColor, weight);
        var lineColor = WithAlpha(ChainLineColor, weight);
        for (var i = 0; i < joints.Length; i++)
        {
            context.DrawPoint(joints[i], jointColor, JointPointSize);
            context.Drawer.DrawWireSphere(joints[i], JointSphereRadius, jointColor);

            if (i > 0)
            {
                context.DrawLine(joints[i - 1], joints[i], lineColor, LineWidth);
            }
        }

        // Target position: green sphere, or red when the target is out of reach.
        var targetColor = WithAlpha(unreachable ? UnreachableColor : TargetColor, weight);
        context.Drawer.DrawWireSphere(target.Position, TargetSphereRadius, targetColor);

        // Target rotation: RGB axis lines from the target position.
        if (target.UseRotation)
        {
            DrawRotationAxes(context, target.Position, target.Rotation, weight);
        }

        // Pole vector: yellow line from the chain's mid joint toward the pole target.
        if (target.PoleTargetEntityId >= 0 &&
            transformsById is not null &&
            transformsById.TryGetValue(target.PoleTargetEntityId, out var poleEntity) &&
            world.IsAlive(poleEntity))
        {
            var polePosition = GetWorldPosition(world, poleEntity);
            var midJoint = joints[joints.Length / 2];
            context.DrawLine(midJoint, polePosition, WithAlpha(PoleColor, weight), LineWidth);
        }
    }

    private static void DrawRotationAxes(
        GizmoRenderContext context, Vector3 origin, Quaternion rotation, float weight)
    {
        var axisX = Vector3.Transform(Vector3.UnitX, rotation);
        var axisY = Vector3.Transform(Vector3.UnitY, rotation);
        var axisZ = Vector3.Transform(Vector3.UnitZ, rotation);

        context.DrawLine(origin, origin + (axisX * AxisLength), WithAlpha(AxisXColor, weight), LineWidth);
        context.DrawLine(origin, origin + (axisY * AxisLength), WithAlpha(AxisYColor, weight), LineWidth);
        context.DrawLine(origin, origin + (axisZ * AxisLength), WithAlpha(AxisZColor, weight), LineWidth);
    }

    /// <summary>
    /// Computes the effective IK blend weight, clamped to the range [0, 1].
    /// </summary>
    /// <param name="globalWeight">The rig's global weight.</param>
    /// <param name="chainWeight">The per-chain weight multiplier.</param>
    /// <param name="targetWeight">The target's blend weight.</param>
    /// <returns>The clamped product of the three weights.</returns>
    internal static float ComputeEffectiveWeight(float globalWeight, float chainWeight, float targetWeight)
        => Math.Clamp(globalWeight * chainWeight * targetWeight, 0f, 1f);

    /// <summary>
    /// Sums the distances between consecutive chain joints to obtain the total chain length.
    /// </summary>
    /// <param name="joints">World-space joint positions ordered root to tip.</param>
    /// <returns>The total length of the chain, or zero for fewer than two joints.</returns>
    internal static float ComputeChainLength(IReadOnlyList<Vector3> joints)
    {
        var length = 0f;
        for (var i = 1; i < joints.Count; i++)
        {
            length += Vector3.Distance(joints[i - 1], joints[i]);
        }

        return length;
    }

    /// <summary>
    /// Determines whether a target is unreachable, i.e. farther from the chain root than the
    /// chain can extend.
    /// </summary>
    /// <param name="chainLength">The total length of the chain.</param>
    /// <param name="targetDistance">The distance from the chain root to the target.</param>
    /// <returns><see langword="true"/> when the target lies beyond the chain's reach.</returns>
    internal static bool IsUnreachable(float chainLength, float targetDistance)
        => targetDistance > chainLength;

    /// <summary>
    /// Collects the chain's bone entities by walking the hierarchy up from the end effector,
    /// validating each bone against the chain definition's bone names.
    /// </summary>
    /// <param name="world">The world containing the bones.</param>
    /// <param name="endEffector">The chain's tip (end effector) entity.</param>
    /// <param name="chain">The chain definition describing the expected bones.</param>
    /// <param name="bones">The collected bone entities ordered root to tip, on success.</param>
    /// <returns><see langword="true"/> when a complete, matching bone chain was collected.</returns>
    internal static bool TryCollectChainBones(
        IWorld world, Entity endEffector, IKChainDefinition chain, out Entity[] bones)
    {
        bones = [];
        if (chain.BoneCount < 2)
        {
            return false;
        }

        var collected = new Entity[chain.BoneCount];
        var current = endEffector;
        for (var i = chain.BoneCount - 1; i >= 0; i--)
        {
            if (!current.IsValid ||
                !world.IsAlive(current) ||
                !world.Has<Transform3D>(current) ||
                !world.Has<BoneReference>(current))
            {
                return false;
            }

            ref readonly var boneRef = ref world.Get<BoneReference>(current);
            if (!string.Equals(boneRef.BoneName, chain.BoneNames[i], StringComparison.Ordinal))
            {
                return false;
            }

            collected[i] = current;
            current = world.GetParent(current);
        }

        bones = collected;
        return true;
    }

    /// <summary>
    /// Computes the world-space position of an entity by composing local transforms up the
    /// entity hierarchy. Bone <see cref="Transform3D"/> components store parent-relative values.
    /// </summary>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="entity">The entity to locate.</param>
    /// <returns>The world-space position, or <see cref="Vector3.Zero"/> when unavailable.</returns>
    internal static Vector3 GetWorldPosition(IWorld world, Entity entity)
    {
        if (!entity.IsValid || !world.IsAlive(entity) || !world.Has<Transform3D>(entity))
        {
            return Vector3.Zero;
        }

        return GetWorldTransform(world, entity).Position;
    }

    private static (Vector3 Position, Quaternion Rotation, Vector3 Scale) GetWorldTransform(
        IWorld world, Entity entity)
    {
        ref readonly var local = ref world.Get<Transform3D>(entity);
        var parent = world.GetParent(entity);

        if (!parent.IsValid || !world.IsAlive(parent) || !world.Has<Transform3D>(parent))
        {
            return (local.Position, local.Rotation, local.Scale);
        }

        var (parentPos, parentRot, parentScale) = GetWorldTransform(world, parent);

        return (
            parentPos + Vector3.Transform(parentScale * local.Position, parentRot),
            Quaternion.Normalize(parentRot * local.Rotation),
            parentScale * local.Scale);
    }

    private static Vector4 WithAlpha(Vector4 color, float alpha)
        => new(color.X, color.Y, color.Z, color.W * alpha);
}
