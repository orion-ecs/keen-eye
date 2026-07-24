using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Physics.Components;
using KeenEyes.Physics.Core;

namespace KeenEyes.Physics.Systems;

/// <summary>
/// System that synchronizes physics state to ECS components with interpolation for rendering.
/// </summary>
/// <remarks>
/// <para>
/// This system runs in the <see cref="SystemPhase.LateUpdate"/> phase after all
/// gameplay logic has executed. It interpolates between the previous and current
/// physics states to provide smooth rendering even when physics runs at a lower
/// frequency than the render rate.
/// </para>
/// <para>
/// Interpolation only affects Transform3D for rendering purposes. The actual
/// physics state (used for gameplay logic) is updated by <see cref="PhysicsStepSystem"/>.
/// </para>
/// </remarks>
public sealed class PhysicsSyncSystem : SystemBase
{
    private PhysicsWorld? physicsWorld;

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        if (World.TryGetExtension<PhysicsWorld>(out var pw))
        {
            physicsWorld = pw;
        }
    }

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        var pw = physicsWorld;
        if (pw == null)
        {
            if (!World.TryGetExtension<PhysicsWorld>(out pw))
            {
                return;
            }

            physicsWorld = pw;
        }

        if (!pw!.Config.EnableInterpolation)
        {
            return;
        }

        var alpha = pw.InterpolationAlpha;

        foreach (var entity in World.Query<Transform3D, RigidBody>())
        {
            ref readonly var rigidBody = ref World.Get<RigidBody>(entity);

            // Only interpolate dynamic bodies
            if (rigidBody.BodyType != RigidBodyType.Dynamic)
            {
                continue;
            }

            // Interpolate between the last two raw physics poses. Reading the raw poses from
            // the physics world (rather than the current Transform3D) avoids feeding the
            // previously displayed, already-interpolated value back in as input.
            if (!pw.TryGetInterpolationPoses(entity, out var prevPos, out var prevRot, out var currentPos, out var currentRot))
            {
                continue;
            }

            ref var transform = ref World.Get<Transform3D>(entity);
            transform.Position = Vector3.Lerp(prevPos, currentPos, alpha);
            transform.Rotation = Quaternion.Slerp(prevRot, currentRot, alpha);
        }
    }
}
