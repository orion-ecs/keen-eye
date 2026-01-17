using System.ComponentModel;
using KeenEyes.Mcp.TestBridge.Connection;
using KeenEyes.TestBridge.Physics;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Tools;

/// <summary>
/// MCP tools for physics debugging: raycasting, queries, body state, and force application.
/// </summary>
/// <remarks>
/// <para>
/// These tools expose the physics simulation debugging infrastructure via MCP, allowing
/// inspection and manipulation of physics bodies in running games.
/// </para>
/// <para>
/// Note: These tools require the PhysicsPlugin to be installed in the target world.
/// Entities must have the appropriate physics components (RigidBody, PhysicsShape, Transform3D)
/// for the operations to work.
/// </para>
/// </remarks>
[McpServerToolType]
public sealed class PhysicsTools(BridgeConnectionManager connection)
{
    #region Statistics

    [McpServerTool(Name = "physics_get_statistics")]
    [Description("Get overall physics statistics including body counts and interpolation state.")]
    public async Task<PhysicsStatisticsResult> GetStatistics()
    {
        var bridge = connection.GetBridge();
        var stats = await bridge.Physics.GetStatisticsAsync();
        return PhysicsStatisticsResult.FromSnapshot(stats);
    }

    #endregion

    #region Raycasting

    [McpServerTool(Name = "physics_raycast")]
    [Description("Cast a ray and return the first hit. Returns null if nothing was hit.")]
    public async Task<RaycastResult> Raycast(
        [Description("Ray origin X coordinate")]
        float originX,
        [Description("Ray origin Y coordinate")]
        float originY,
        [Description("Ray origin Z coordinate")]
        float originZ,
        [Description("Ray direction X (normalized)")]
        float directionX,
        [Description("Ray direction Y (normalized)")]
        float directionY,
        [Description("Ray direction Z (normalized)")]
        float directionZ,
        [Description("Maximum distance to check for hits")]
        float maxDistance)
    {
        var bridge = connection.GetBridge();
        var origin = new Vector3Snapshot { X = originX, Y = originY, Z = originZ };
        var direction = new Vector3Snapshot { X = directionX, Y = directionY, Z = directionZ };
        var hit = await bridge.Physics.RaycastAsync(origin, direction, maxDistance);

        if (hit == null)
        {
            return new RaycastResult
            {
                Success = true,
                Hit = false
            };
        }

        return new RaycastResult
        {
            Success = true,
            Hit = true,
            EntityId = hit.EntityId,
            PositionX = hit.Position.X,
            PositionY = hit.Position.Y,
            PositionZ = hit.Position.Z,
            NormalX = hit.Normal.X,
            NormalY = hit.Normal.Y,
            NormalZ = hit.Normal.Z,
            Distance = hit.Distance
        };
    }

    #endregion

    #region Overlap Queries

    [McpServerTool(Name = "physics_overlap_sphere")]
    [Description("Find all entities within a sphere.")]
    public async Task<OverlapResult> OverlapSphere(
        [Description("Sphere center X coordinate")]
        float centerX,
        [Description("Sphere center Y coordinate")]
        float centerY,
        [Description("Sphere center Z coordinate")]
        float centerZ,
        [Description("Sphere radius")]
        float radius)
    {
        var bridge = connection.GetBridge();
        var center = new Vector3Snapshot { X = centerX, Y = centerY, Z = centerZ };
        var entities = await bridge.Physics.OverlapSphereAsync(center, radius);

        return new OverlapResult
        {
            Success = true,
            EntityIds = entities,
            Count = entities.Count
        };
    }

    [McpServerTool(Name = "physics_overlap_box")]
    [Description("Find all entities within an axis-aligned box.")]
    public async Task<OverlapResult> OverlapBox(
        [Description("Box center X coordinate")]
        float centerX,
        [Description("Box center Y coordinate")]
        float centerY,
        [Description("Box center Z coordinate")]
        float centerZ,
        [Description("Box half-extent X (half-width)")]
        float halfExtentX,
        [Description("Box half-extent Y (half-height)")]
        float halfExtentY,
        [Description("Box half-extent Z (half-depth)")]
        float halfExtentZ)
    {
        var bridge = connection.GetBridge();
        var center = new Vector3Snapshot { X = centerX, Y = centerY, Z = centerZ };
        var halfExtents = new Vector3Snapshot { X = halfExtentX, Y = halfExtentY, Z = halfExtentZ };
        var entities = await bridge.Physics.OverlapBoxAsync(center, halfExtents);

        return new OverlapResult
        {
            Success = true,
            EntityIds = entities,
            Count = entities.Count
        };
    }

    #endregion

    #region Body State

    [McpServerTool(Name = "physics_get_body")]
    [Description("Get the complete state of a physics body including position, rotation, velocities, and mass.")]
    public async Task<PhysicsBodyResult> GetBodyState(
        [Description("The entity ID to query")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var snapshot = await bridge.Physics.GetBodyStateAsync(entityId);

        if (snapshot == null)
        {
            return new PhysicsBodyResult
            {
                Success = false,
                Error = $"Entity {entityId} has no physics body"
            };
        }

        return PhysicsBodyResult.FromSnapshot(snapshot);
    }

    [McpServerTool(Name = "physics_get_velocity")]
    [Description("Get the linear velocity of a physics body.")]
    public async Task<Vector3Result> GetVelocity(
        [Description("The entity ID to query")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var velocity = await bridge.Physics.GetVelocityAsync(entityId);

        if (velocity == null)
        {
            return new Vector3Result
            {
                Success = false,
                Error = $"Entity {entityId} has no physics body"
            };
        }

        return new Vector3Result
        {
            Success = true,
            X = velocity.X,
            Y = velocity.Y,
            Z = velocity.Z
        };
    }

    [McpServerTool(Name = "physics_set_velocity")]
    [Description("Set the linear velocity of a physics body.")]
    public async Task<OperationResult> SetVelocity(
        [Description("The entity ID to modify")]
        int entityId,
        [Description("Velocity X component")]
        float x,
        [Description("Velocity Y component")]
        float y,
        [Description("Velocity Z component")]
        float z)
    {
        var bridge = connection.GetBridge();
        var velocity = new Vector3Snapshot { X = x, Y = y, Z = z };
        var success = await bridge.Physics.SetVelocityAsync(entityId, velocity);

        return new OperationResult
        {
            Success = success,
            Error = success ? null : $"Failed to set velocity on entity {entityId}"
        };
    }

    [McpServerTool(Name = "physics_is_awake")]
    [Description("Check if a physics body is awake (not sleeping).")]
    public async Task<BooleanResult> IsAwake(
        [Description("The entity ID to check")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var isAwake = await bridge.Physics.IsAwakeAsync(entityId);

        if (isAwake == null)
        {
            return new BooleanResult
            {
                Success = false,
                Error = $"Entity {entityId} has no physics body"
            };
        }

        return new BooleanResult
        {
            Success = true,
            Value = isAwake.Value
        };
    }

    [McpServerTool(Name = "physics_wake_up")]
    [Description("Wake up a sleeping physics body.")]
    public async Task<OperationResult> WakeUp(
        [Description("The entity ID to wake")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.Physics.WakeUpAsync(entityId);

        return new OperationResult
        {
            Success = success,
            Error = success ? null : $"Failed to wake up entity {entityId}"
        };
    }

    #endregion

    #region Forces and Impulses

    [McpServerTool(Name = "physics_apply_force")]
    [Description("Apply a force to a physics body at its center of mass.")]
    public async Task<OperationResult> ApplyForce(
        [Description("The entity ID to apply force to")]
        int entityId,
        [Description("Force X component")]
        float x,
        [Description("Force Y component")]
        float y,
        [Description("Force Z component")]
        float z)
    {
        var bridge = connection.GetBridge();
        var force = new Vector3Snapshot { X = x, Y = y, Z = z };
        var success = await bridge.Physics.ApplyForceAsync(entityId, force);

        return new OperationResult
        {
            Success = success,
            Error = success ? null : $"Failed to apply force to entity {entityId}"
        };
    }

    [McpServerTool(Name = "physics_apply_impulse")]
    [Description("Apply an impulse to a physics body at its center of mass.")]
    public async Task<OperationResult> ApplyImpulse(
        [Description("The entity ID to apply impulse to")]
        int entityId,
        [Description("Impulse X component")]
        float x,
        [Description("Impulse Y component")]
        float y,
        [Description("Impulse Z component")]
        float z)
    {
        var bridge = connection.GetBridge();
        var impulse = new Vector3Snapshot { X = x, Y = y, Z = z };
        var success = await bridge.Physics.ApplyImpulseAsync(entityId, impulse);

        return new OperationResult
        {
            Success = success,
            Error = success ? null : $"Failed to apply impulse to entity {entityId}"
        };
    }

    #endregion

    #region Gravity

    [McpServerTool(Name = "physics_get_gravity")]
    [Description("Get the current gravity vector for the simulation.")]
    public async Task<Vector3Result> GetGravity()
    {
        var bridge = connection.GetBridge();
        var gravity = await bridge.Physics.GetGravityAsync();

        return new Vector3Result
        {
            Success = true,
            X = gravity.X,
            Y = gravity.Y,
            Z = gravity.Z
        };
    }

    [McpServerTool(Name = "physics_set_gravity")]
    [Description("Set the gravity vector for the simulation.")]
    public async Task<OperationResult> SetGravity(
        [Description("Gravity X component")]
        float x,
        [Description("Gravity Y component")]
        float y,
        [Description("Gravity Z component")]
        float z)
    {
        var bridge = connection.GetBridge();
        var gravity = new Vector3Snapshot { X = x, Y = y, Z = z };
        var success = await bridge.Physics.SetGravityAsync(gravity);

        return new OperationResult
        {
            Success = success,
            Error = success ? null : "Failed to set gravity"
        };
    }

    #endregion
}

#region Result Types

/// <summary>
/// Result for physics statistics.
/// </summary>
public sealed record PhysicsStatisticsResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; } = true;

    /// <summary>
    /// Gets the total number of physics bodies (dynamic + kinematic).
    /// </summary>
    public int BodyCount { get; init; }

    /// <summary>
    /// Gets the total number of static bodies.
    /// </summary>
    public int StaticCount { get; init; }

    /// <summary>
    /// Gets the interpolation alpha for smooth rendering (0-1).
    /// </summary>
    public float InterpolationAlpha { get; init; }

    /// <summary>
    /// Gets the total count of all physics objects.
    /// </summary>
    public int TotalCount => BodyCount + StaticCount;

    internal static PhysicsStatisticsResult FromSnapshot(PhysicsStatisticsSnapshot snapshot)
    {
        return new PhysicsStatisticsResult
        {
            BodyCount = snapshot.BodyCount,
            StaticCount = snapshot.StaticCount,
            InterpolationAlpha = snapshot.InterpolationAlpha
        };
    }
}

/// <summary>
/// Result for raycast operations.
/// </summary>
public sealed record RaycastResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets whether something was hit.
    /// </summary>
    public bool Hit { get; init; }

    /// <summary>
    /// Gets the entity ID that was hit.
    /// </summary>
    public int? EntityId { get; init; }

    /// <summary>
    /// Gets the hit position X coordinate.
    /// </summary>
    public float? PositionX { get; init; }

    /// <summary>
    /// Gets the hit position Y coordinate.
    /// </summary>
    public float? PositionY { get; init; }

    /// <summary>
    /// Gets the hit position Z coordinate.
    /// </summary>
    public float? PositionZ { get; init; }

    /// <summary>
    /// Gets the surface normal X component.
    /// </summary>
    public float? NormalX { get; init; }

    /// <summary>
    /// Gets the surface normal Y component.
    /// </summary>
    public float? NormalY { get; init; }

    /// <summary>
    /// Gets the surface normal Z component.
    /// </summary>
    public float? NormalZ { get; init; }

    /// <summary>
    /// Gets the distance from the ray origin to the hit point.
    /// </summary>
    public float? Distance { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Result for overlap queries.
/// </summary>
public sealed record OverlapResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the list of entity IDs overlapping the query volume.
    /// </summary>
    public IReadOnlyList<int>? EntityIds { get; init; }

    /// <summary>
    /// Gets the count of entities.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Result for physics body queries.
/// </summary>
public sealed record PhysicsBodyResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public int EntityId { get; init; }

    /// <summary>
    /// Gets the position X coordinate.
    /// </summary>
    public float PositionX { get; init; }

    /// <summary>
    /// Gets the position Y coordinate.
    /// </summary>
    public float PositionY { get; init; }

    /// <summary>
    /// Gets the position Z coordinate.
    /// </summary>
    public float PositionZ { get; init; }

    /// <summary>
    /// Gets the rotation quaternion X component.
    /// </summary>
    public float RotationX { get; init; }

    /// <summary>
    /// Gets the rotation quaternion Y component.
    /// </summary>
    public float RotationY { get; init; }

    /// <summary>
    /// Gets the rotation quaternion Z component.
    /// </summary>
    public float RotationZ { get; init; }

    /// <summary>
    /// Gets the rotation quaternion W component.
    /// </summary>
    public float RotationW { get; init; }

    /// <summary>
    /// Gets the linear velocity X component.
    /// </summary>
    public float LinearVelocityX { get; init; }

    /// <summary>
    /// Gets the linear velocity Y component.
    /// </summary>
    public float LinearVelocityY { get; init; }

    /// <summary>
    /// Gets the linear velocity Z component.
    /// </summary>
    public float LinearVelocityZ { get; init; }

    /// <summary>
    /// Gets the angular velocity X component.
    /// </summary>
    public float AngularVelocityX { get; init; }

    /// <summary>
    /// Gets the angular velocity Y component.
    /// </summary>
    public float AngularVelocityY { get; init; }

    /// <summary>
    /// Gets the angular velocity Z component.
    /// </summary>
    public float AngularVelocityZ { get; init; }

    /// <summary>
    /// Gets the mass in kilograms.
    /// </summary>
    public float Mass { get; init; }

    /// <summary>
    /// Gets the body type (Dynamic, Kinematic, Static).
    /// </summary>
    public string? BodyType { get; init; }

    /// <summary>
    /// Gets whether the body is currently awake (not sleeping).
    /// </summary>
    public bool IsAwake { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    internal static PhysicsBodyResult FromSnapshot(PhysicsBodySnapshot snapshot)
    {
        return new PhysicsBodyResult
        {
            Success = true,
            EntityId = snapshot.EntityId,
            PositionX = snapshot.Position.X,
            PositionY = snapshot.Position.Y,
            PositionZ = snapshot.Position.Z,
            RotationX = snapshot.Rotation.X,
            RotationY = snapshot.Rotation.Y,
            RotationZ = snapshot.Rotation.Z,
            RotationW = snapshot.Rotation.W,
            LinearVelocityX = snapshot.LinearVelocity.X,
            LinearVelocityY = snapshot.LinearVelocity.Y,
            LinearVelocityZ = snapshot.LinearVelocity.Z,
            AngularVelocityX = snapshot.AngularVelocity.X,
            AngularVelocityY = snapshot.AngularVelocity.Y,
            AngularVelocityZ = snapshot.AngularVelocity.Z,
            Mass = snapshot.Mass,
            BodyType = snapshot.BodyType,
            IsAwake = snapshot.IsAwake
        };
    }
}

/// <summary>
/// Result for Vector3 queries.
/// </summary>
public sealed record Vector3Result
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the X component.
    /// </summary>
    public float X { get; init; }

    /// <summary>
    /// Gets the Y component.
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    /// Gets the Z component.
    /// </summary>
    public float Z { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Result for boolean queries.
/// </summary>
public sealed record BooleanResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the boolean value.
    /// </summary>
    public bool Value { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }
}

#endregion
