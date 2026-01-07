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

    // Previous frame state for interpolation
    private readonly Dictionary<Entity, Vector3> previousPositions = [];
    private readonly Dictionary<Entity, Quaternion> previousRotations = [];

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
            if (!World.TryGetExtension(out pw))
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

            ref var transform = ref World.Get<Transform3D>(entity);

            // Store current as previous for next frame
            var currentPos = transform.Position;
            var currentRot = transform.Rotation;

            // Get previous values (or use current if first frame)
            if (!previousPositions.TryGetValue(entity, out var prevPos))
            {
                prevPos = currentPos;
            }

            if (!previousRotations.TryGetValue(entity, out var prevRot))
            {
                prevRot = currentRot;
            }

            // Interpolate for smooth rendering
            transform.Position = Vector3.Lerp(prevPos, currentPos, alpha);
            transform.Rotation = Quaternion.Slerp(prevRot, currentRot, alpha);

            // Update previous state tracking
            previousPositions[entity] = currentPos;
            previousRotations[entity] = currentRot;
        }

        // Clean up any entities that no longer exist
        CleanupStaleEntries();
    }

    private void CleanupStaleEntries()
    {
        var entitiesToRemove = new List<Entity>();

        foreach (var entity in previousPositions.Keys)
        {
            if (!World.IsAlive(entity))
            {
                entitiesToRemove.Add(entity);
            }
        }

        foreach (var entity in entitiesToRemove)
        {
            previousPositions.Remove(entity);
            previousRotations.Remove(entity);
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            previousPositions.Clear();
            previousRotations.Clear();
        }
        base.Dispose(disposing);
    }
}
