using KeenEyes.TestBridge.Physics;

namespace KeenEyes.TestBridge.Client;

/// <summary>
/// Remote implementation of <see cref="IPhysicsController"/> that communicates over IPC.
/// </summary>
internal sealed class RemotePhysicsController(TestBridgeClient client) : IPhysicsController
{
    #region Statistics

    /// <inheritdoc />
    public async Task<PhysicsStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<PhysicsStatisticsSnapshot>(
            "physics.getStatistics",
            null,
            cancellationToken) ?? new PhysicsStatisticsSnapshot { BodyCount = 0, StaticCount = 0, InterpolationAlpha = 0f };
    }

    #endregion

    #region Raycasting and Queries

    /// <inheritdoc />
    public async Task<RayHitSnapshot?> RaycastAsync(Vector3Snapshot origin, Vector3Snapshot direction, float maxDistance, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<RayHitSnapshot?>(
            "physics.raycast",
            new { origin, direction, maxDistance },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> OverlapSphereAsync(Vector3Snapshot center, float radius, CancellationToken cancellationToken = default)
    {
        var result = await client.SendRequestAsync<int[]>(
            "physics.overlapSphere",
            new { center, radius },
            cancellationToken);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> OverlapBoxAsync(Vector3Snapshot center, Vector3Snapshot halfExtents, CancellationToken cancellationToken = default)
    {
        var result = await client.SendRequestAsync<int[]>(
            "physics.overlapBox",
            new { center, halfExtents },
            cancellationToken);
        return result ?? [];
    }

    #endregion

    #region Body State

    /// <inheritdoc />
    public async Task<PhysicsBodySnapshot?> GetBodyStateAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<PhysicsBodySnapshot?>(
            "physics.getBodyState",
            new { entityId },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Vector3Snapshot?> GetVelocityAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<Vector3Snapshot?>(
            "physics.getVelocity",
            new { entityId },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> SetVelocityAsync(int entityId, Vector3Snapshot velocity, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "physics.setVelocity",
            new { entityId, velocity },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool?> IsAwakeAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool?>(
            "physics.isAwake",
            new { entityId },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> WakeUpAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "physics.wakeUp",
            new { entityId },
            cancellationToken);
    }

    #endregion

    #region Forces and Impulses

    /// <inheritdoc />
    public async Task<bool> ApplyForceAsync(int entityId, Vector3Snapshot force, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "physics.applyForce",
            new { entityId, force },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ApplyImpulseAsync(int entityId, Vector3Snapshot impulse, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "physics.applyImpulse",
            new { entityId, impulse },
            cancellationToken);
    }

    #endregion

    #region Gravity

    /// <inheritdoc />
    public async Task<Vector3Snapshot> GetGravityAsync(CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<Vector3Snapshot>(
            "physics.getGravity",
            null,
            cancellationToken) ?? new Vector3Snapshot { X = 0f, Y = 0f, Z = 0f };
    }

    /// <inheritdoc />
    public async Task<bool> SetGravityAsync(Vector3Snapshot gravity, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "physics.setGravity",
            new { gravity },
            cancellationToken);
    }

    #endregion
}
