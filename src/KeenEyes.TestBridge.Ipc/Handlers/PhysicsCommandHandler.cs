using System.Text.Json;
using KeenEyes.TestBridge.Physics;

namespace KeenEyes.TestBridge.Ipc.Handlers;

/// <summary>
/// Handles physics debugging commands for raycasting, queries, and body manipulation.
/// </summary>
internal sealed class PhysicsCommandHandler(IPhysicsController physicsController) : ICommandHandler
{
    public string Prefix => "physics";

    public async ValueTask<object?> HandleAsync(string command, JsonElement? args, CancellationToken cancellationToken)
    {
        return command switch
        {
            // Statistics
            "getStatistics" => await physicsController.GetStatisticsAsync(cancellationToken),

            // Raycasting and Queries
            "raycast" => await HandleRaycastAsync(args, cancellationToken),
            "overlapSphere" => await HandleOverlapSphereAsync(args, cancellationToken),
            "overlapBox" => await HandleOverlapBoxAsync(args, cancellationToken),

            // Body State
            "getBodyState" => await HandleGetBodyStateAsync(args, cancellationToken),
            "getVelocity" => await HandleGetVelocityAsync(args, cancellationToken),
            "setVelocity" => await HandleSetVelocityAsync(args, cancellationToken),
            "isAwake" => await HandleIsAwakeAsync(args, cancellationToken),
            "wakeUp" => await HandleWakeUpAsync(args, cancellationToken),

            // Forces and Impulses
            "applyForce" => await HandleApplyForceAsync(args, cancellationToken),
            "applyImpulse" => await HandleApplyImpulseAsync(args, cancellationToken),

            // Gravity
            "getGravity" => await physicsController.GetGravityAsync(cancellationToken),
            "setGravity" => await HandleSetGravityAsync(args, cancellationToken),

            _ => throw new InvalidOperationException($"Unknown physics command: {command}")
        };
    }

    #region Raycast and Query Handlers

    private async Task<RayHitSnapshot?> HandleRaycastAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var origin = GetRequiredVector3(args, "origin");
        var direction = GetRequiredVector3(args, "direction");
        var maxDistance = GetRequiredFloat(args, "maxDistance");
        return await physicsController.RaycastAsync(origin, direction, maxDistance, cancellationToken);
    }

    private async Task<IReadOnlyList<int>> HandleOverlapSphereAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var center = GetRequiredVector3(args, "center");
        var radius = GetRequiredFloat(args, "radius");
        return await physicsController.OverlapSphereAsync(center, radius, cancellationToken);
    }

    private async Task<IReadOnlyList<int>> HandleOverlapBoxAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var center = GetRequiredVector3(args, "center");
        var halfExtents = GetRequiredVector3(args, "halfExtents");
        return await physicsController.OverlapBoxAsync(center, halfExtents, cancellationToken);
    }

    #endregion

    #region Body State Handlers

    private async Task<PhysicsBodySnapshot?> HandleGetBodyStateAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await physicsController.GetBodyStateAsync(entityId, cancellationToken);
    }

    private async Task<Vector3Snapshot?> HandleGetVelocityAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await physicsController.GetVelocityAsync(entityId, cancellationToken);
    }

    private async Task<bool> HandleSetVelocityAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var velocity = GetRequiredVector3(args, "velocity");
        return await physicsController.SetVelocityAsync(entityId, velocity, cancellationToken);
    }

    private async Task<bool?> HandleIsAwakeAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await physicsController.IsAwakeAsync(entityId, cancellationToken);
    }

    private async Task<bool> HandleWakeUpAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await physicsController.WakeUpAsync(entityId, cancellationToken);
    }

    #endregion

    #region Force and Impulse Handlers

    private async Task<bool> HandleApplyForceAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var force = GetRequiredVector3(args, "force");
        return await physicsController.ApplyForceAsync(entityId, force, cancellationToken);
    }

    private async Task<bool> HandleApplyImpulseAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var impulse = GetRequiredVector3(args, "impulse");
        return await physicsController.ApplyImpulseAsync(entityId, impulse, cancellationToken);
    }

    #endregion

    #region Gravity Handlers

    private async Task<bool> HandleSetGravityAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var gravity = GetRequiredVector3(args, "gravity");
        return await physicsController.SetGravityAsync(gravity, cancellationToken);
    }

    #endregion

    #region Typed Argument Helpers (AOT-compatible)

    private static int GetRequiredInt(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetInt32();
    }

    private static float GetRequiredFloat(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetSingle();
    }

    private static Vector3Snapshot GetRequiredVector3(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return new Vector3Snapshot
        {
            X = prop.GetProperty("x").GetSingle(),
            Y = prop.GetProperty("y").GetSingle(),
            Z = prop.GetProperty("z").GetSingle()
        };
    }

    #endregion
}
