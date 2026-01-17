using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Physics.Components;
using KeenEyes.Physics.Core;
using KeenEyes.TestBridge.Physics;

namespace KeenEyes.TestBridge.PhysicsImpl;

/// <summary>
/// In-process implementation of <see cref="IPhysicsController"/>.
/// </summary>
internal sealed class PhysicsControllerImpl(World world) : IPhysicsController
{
    #region Statistics

    /// <inheritdoc />
    public Task<PhysicsStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<PhysicsWorld>(out var physics))
        {
            return Task.FromResult(new PhysicsStatisticsSnapshot
            {
                BodyCount = 0,
                StaticCount = 0,
                InterpolationAlpha = 0f
            });
        }

        return Task.FromResult(new PhysicsStatisticsSnapshot
        {
            BodyCount = physics.BodyCount,
            StaticCount = physics.StaticCount,
            InterpolationAlpha = physics.InterpolationAlpha
        });
    }

    #endregion

    #region Raycasting and Queries

    /// <inheritdoc />
    public Task<RayHitSnapshot?> RaycastAsync(Vector3Snapshot origin, Vector3Snapshot direction, float maxDistance, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<PhysicsWorld>(out var physics))
        {
            return Task.FromResult<RayHitSnapshot?>(null);
        }

        var originVec = new Vector3(origin.X, origin.Y, origin.Z);
        var directionVec = new Vector3(direction.X, direction.Y, direction.Z);

        if (physics.Raycast(originVec, directionVec, maxDistance, out var hit))
        {
            return Task.FromResult<RayHitSnapshot?>(new RayHitSnapshot
            {
                EntityId = hit.Entity.Id,
                Position = new Vector3Snapshot { X = hit.Position.X, Y = hit.Position.Y, Z = hit.Position.Z },
                Normal = new Vector3Snapshot { X = hit.Normal.X, Y = hit.Normal.Y, Z = hit.Normal.Z },
                Distance = hit.Distance
            });
        }

        return Task.FromResult<RayHitSnapshot?>(null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<int>> OverlapSphereAsync(Vector3Snapshot center, float radius, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<PhysicsWorld>(out var physics))
        {
            return Task.FromResult<IReadOnlyList<int>>([]);
        }

        var centerVec = new Vector3(center.X, center.Y, center.Z);
        var entities = physics.OverlapSphere(centerVec, radius).Select(e => e.Id).ToList();
        return Task.FromResult<IReadOnlyList<int>>(entities);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<int>> OverlapBoxAsync(Vector3Snapshot center, Vector3Snapshot halfExtents, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<PhysicsWorld>(out var physics))
        {
            return Task.FromResult<IReadOnlyList<int>>([]);
        }

        var centerVec = new Vector3(center.X, center.Y, center.Z);
        var halfExtentsVec = new Vector3(halfExtents.X, halfExtents.Y, halfExtents.Z);
        var entities = physics.OverlapBox(centerVec, halfExtentsVec).Select(e => e.Id).ToList();
        return Task.FromResult<IReadOnlyList<int>>(entities);
    }

    #endregion

    #region Body State

    /// <inheritdoc />
    public Task<PhysicsBodySnapshot?> GetBodyStateAsync(int entityId, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<PhysicsWorld>(out var physics))
        {
            return Task.FromResult<PhysicsBodySnapshot?>(null);
        }

        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !physics.HasPhysicsBody(entity))
        {
            return Task.FromResult<PhysicsBodySnapshot?>(null);
        }

        // Get transform
        Vector3 position = Vector3.Zero;
        Quaternion rotation = Quaternion.Identity;
        if (world.Has<Transform3D>(entity))
        {
            ref readonly var transform = ref world.Get<Transform3D>(entity);
            position = transform.Position;
            rotation = transform.Rotation;
        }

        // Get velocities
        Vector3 linearVelocity = Vector3.Zero;
        Vector3 angularVelocity = Vector3.Zero;
        try
        {
            linearVelocity = physics.GetVelocity(entity);
            angularVelocity = physics.GetAngularVelocity(entity);
        }
        catch
        {
            // Entity might not have dynamic body
        }

        // Get body properties
        float mass = 0f;
        string bodyType = "Unknown";
        if (world.Has<RigidBody>(entity))
        {
            ref readonly var rigidBody = ref world.Get<RigidBody>(entity);
            mass = rigidBody.Mass;
            bodyType = rigidBody.BodyType.ToString();
        }

        bool isAwake = false;
        try
        {
            isAwake = physics.IsAwake(entity);
        }
        catch
        {
            // Entity might not support awake state (static bodies)
        }

        return Task.FromResult<PhysicsBodySnapshot?>(new PhysicsBodySnapshot
        {
            EntityId = entityId,
            Position = new Vector3Snapshot { X = position.X, Y = position.Y, Z = position.Z },
            Rotation = new QuaternionSnapshot { X = rotation.X, Y = rotation.Y, Z = rotation.Z, W = rotation.W },
            LinearVelocity = new Vector3Snapshot { X = linearVelocity.X, Y = linearVelocity.Y, Z = linearVelocity.Z },
            AngularVelocity = new Vector3Snapshot { X = angularVelocity.X, Y = angularVelocity.Y, Z = angularVelocity.Z },
            Mass = mass,
            BodyType = bodyType,
            IsAwake = isAwake
        });
    }

    /// <inheritdoc />
    public Task<Vector3Snapshot?> GetVelocityAsync(int entityId, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<PhysicsWorld>(out var physics))
        {
            return Task.FromResult<Vector3Snapshot?>(null);
        }

        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !physics.HasPhysicsBody(entity))
        {
            return Task.FromResult<Vector3Snapshot?>(null);
        }

        try
        {
            var velocity = physics.GetVelocity(entity);
            return Task.FromResult<Vector3Snapshot?>(new Vector3Snapshot
            {
                X = velocity.X,
                Y = velocity.Y,
                Z = velocity.Z
            });
        }
        catch
        {
            return Task.FromResult<Vector3Snapshot?>(null);
        }
    }

    /// <inheritdoc />
    public Task<bool> SetVelocityAsync(int entityId, Vector3Snapshot velocity, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<PhysicsWorld>(out var physics))
        {
            return Task.FromResult(false);
        }

        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity))
        {
            return Task.FromResult(false);
        }

        try
        {
            var velocityVec = new Vector3(velocity.X, velocity.Y, velocity.Z);
            physics.SetVelocity(entity, velocityVec);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public Task<bool?> IsAwakeAsync(int entityId, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<PhysicsWorld>(out var physics))
        {
            return Task.FromResult<bool?>(null);
        }

        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !physics.HasPhysicsBody(entity))
        {
            return Task.FromResult<bool?>(null);
        }

        try
        {
            return Task.FromResult<bool?>(physics.IsAwake(entity));
        }
        catch
        {
            return Task.FromResult<bool?>(null);
        }
    }

    /// <inheritdoc />
    public Task<bool> WakeUpAsync(int entityId, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<PhysicsWorld>(out var physics))
        {
            return Task.FromResult(false);
        }

        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity))
        {
            return Task.FromResult(false);
        }

        try
        {
            physics.WakeUp(entity);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    #endregion

    #region Forces and Impulses

    /// <inheritdoc />
    public Task<bool> ApplyForceAsync(int entityId, Vector3Snapshot force, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<PhysicsWorld>(out var physics))
        {
            return Task.FromResult(false);
        }

        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity))
        {
            return Task.FromResult(false);
        }

        try
        {
            var forceVec = new Vector3(force.X, force.Y, force.Z);
            physics.ApplyForce(entity, forceVec);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public Task<bool> ApplyImpulseAsync(int entityId, Vector3Snapshot impulse, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<PhysicsWorld>(out var physics))
        {
            return Task.FromResult(false);
        }

        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity))
        {
            return Task.FromResult(false);
        }

        try
        {
            var impulseVec = new Vector3(impulse.X, impulse.Y, impulse.Z);
            physics.ApplyImpulse(entity, impulseVec);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    #endregion

    #region Gravity

    /// <inheritdoc />
    public Task<Vector3Snapshot> GetGravityAsync(CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<PhysicsWorld>(out var physics))
        {
            return Task.FromResult(new Vector3Snapshot { X = 0f, Y = 0f, Z = 0f });
        }

        var gravity = physics.Gravity;
        return Task.FromResult(new Vector3Snapshot
        {
            X = gravity.X,
            Y = gravity.Y,
            Z = gravity.Z
        });
    }

    /// <inheritdoc />
    public Task<bool> SetGravityAsync(Vector3Snapshot gravity, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<PhysicsWorld>(out var physics))
        {
            return Task.FromResult(false);
        }

        var gravityVec = new Vector3(gravity.X, gravity.Y, gravity.Z);
        physics.SetGravity(gravityVec);
        return Task.FromResult(true);
    }

    #endregion
}
