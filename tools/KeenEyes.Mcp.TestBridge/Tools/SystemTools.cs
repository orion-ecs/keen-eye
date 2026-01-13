using System.ComponentModel;
using KeenEyes.Mcp.TestBridge.Connection;
using KeenEyes.TestBridge.Systems;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Tools;

/// <summary>
/// MCP tools for ECS system management.
/// </summary>
/// <remarks>
/// <para>
/// These tools allow querying and controlling the ECS systems registered
/// with the game world. Systems can be enabled/disabled for debugging
/// purposes, and their metadata can be inspected.
/// </para>
/// </remarks>
[McpServerToolType]
public sealed class SystemTools(BridgeConnectionManager connection)
{
    #region Listing

    [McpServerTool(Name = "system_list")]
    [Description("List all registered ECS systems with their state and metadata.")]
    public async Task<SystemListResult> List()
    {
        var bridge = connection.GetBridge();
        var systems = await bridge.Systems.GetSystemsAsync();
        return new SystemListResult
        {
            Success = true,
            Systems = systems,
            Count = systems.Count
        };
    }

    [McpServerTool(Name = "system_get_count")]
    [Description("Get the total number of registered systems.")]
    public async Task<SystemCountResult> GetCount()
    {
        var bridge = connection.GetBridge();
        var count = await bridge.Systems.GetCountAsync();
        return new SystemCountResult { Count = count };
    }

    [McpServerTool(Name = "system_get")]
    [Description("Get detailed information about a specific system by name.")]
    public async Task<SystemResult> Get(
        [Description("The system name (e.g., 'MovementSystem', 'PhysicsSystem')")]
        string name)
    {
        var bridge = connection.GetBridge();
        var system = await bridge.Systems.GetSystemAsync(name);

        if (system == null)
        {
            return new SystemResult
            {
                Success = false,
                Error = $"System not found: {name}"
            };
        }

        return SystemResult.FromSnapshot(system);
    }

    #endregion

    #region Enable/Disable

    [McpServerTool(Name = "system_enable")]
    [Description("Enable a system so it will be executed during world updates.")]
    public async Task<SystemResult> Enable(
        [Description("The system name to enable")]
        string name)
    {
        try
        {
            var bridge = connection.GetBridge();
            var system = await bridge.Systems.EnableSystemAsync(name);
            return SystemResult.FromSnapshot(system);
        }
        catch (KeyNotFoundException)
        {
            return new SystemResult
            {
                Success = false,
                Error = $"System not found: {name}"
            };
        }
    }

    [McpServerTool(Name = "system_disable")]
    [Description("Disable a system so it will be skipped during world updates.")]
    public async Task<SystemResult> Disable(
        [Description("The system name to disable")]
        string name)
    {
        try
        {
            var bridge = connection.GetBridge();
            var system = await bridge.Systems.DisableSystemAsync(name);
            return SystemResult.FromSnapshot(system);
        }
        catch (KeyNotFoundException)
        {
            return new SystemResult
            {
                Success = false,
                Error = $"System not found: {name}"
            };
        }
    }

    [McpServerTool(Name = "system_toggle")]
    [Description("Toggle a system's enabled state.")]
    public async Task<SystemResult> Toggle(
        [Description("The system name to toggle")]
        string name)
    {
        try
        {
            var bridge = connection.GetBridge();
            var system = await bridge.Systems.ToggleSystemAsync(name);
            return SystemResult.FromSnapshot(system);
        }
        catch (KeyNotFoundException)
        {
            return new SystemResult
            {
                Success = false,
                Error = $"System not found: {name}"
            };
        }
    }

    #endregion

    #region Filtering

    [McpServerTool(Name = "system_get_by_phase")]
    [Description("Get all systems in a specific execution phase.")]
    public async Task<SystemListResult> GetByPhase(
        [Description("The phase name (e.g., 'Update', 'FixedUpdate', 'LateUpdate', 'Render')")]
        string phase)
    {
        var bridge = connection.GetBridge();
        var systems = await bridge.Systems.GetSystemsByPhaseAsync(phase);
        return new SystemListResult
        {
            Success = true,
            Systems = systems,
            Count = systems.Count
        };
    }

    [McpServerTool(Name = "system_get_enabled")]
    [Description("Get all currently enabled systems.")]
    public async Task<SystemListResult> GetEnabled()
    {
        var bridge = connection.GetBridge();
        var systems = await bridge.Systems.GetEnabledSystemsAsync();
        return new SystemListResult
        {
            Success = true,
            Systems = systems,
            Count = systems.Count
        };
    }

    [McpServerTool(Name = "system_get_disabled")]
    [Description("Get all currently disabled systems.")]
    public async Task<SystemListResult> GetDisabled()
    {
        var bridge = connection.GetBridge();
        var systems = await bridge.Systems.GetDisabledSystemsAsync();
        return new SystemListResult
        {
            Success = true,
            Systems = systems,
            Count = systems.Count
        };
    }

    #endregion
}

#region Result Records

/// <summary>
/// Result containing a list of systems.
/// </summary>
public sealed record SystemListResult
{
    /// <summary>
    /// Gets whether the operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the list of system snapshots.
    /// </summary>
    public IReadOnlyList<SystemSnapshot> Systems { get; init; } = [];

    /// <summary>
    /// Gets the count of systems.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Gets an error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Result of a system count query.
/// </summary>
public sealed record SystemCountResult
{
    /// <summary>
    /// Gets the count of registered systems.
    /// </summary>
    public required int Count { get; init; }
}

/// <summary>
/// Result of a single system query or operation.
/// </summary>
public sealed record SystemResult
{
    /// <summary>
    /// Gets whether the operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the system name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the fully qualified type name.
    /// </summary>
    public string? TypeName { get; init; }

    /// <summary>
    /// Gets whether the system is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Gets the execution phase.
    /// </summary>
    public string? Phase { get; init; }

    /// <summary>
    /// Gets the execution order within the phase.
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// Gets the names of systems this system runs before.
    /// </summary>
    public IReadOnlyList<string>? RunsBefore { get; init; }

    /// <summary>
    /// Gets the names of systems this system runs after.
    /// </summary>
    public IReadOnlyList<string>? RunsAfter { get; init; }

    /// <summary>
    /// Gets whether this is a system group.
    /// </summary>
    public bool IsGroup { get; init; }

    /// <summary>
    /// Gets the child system names if this is a group.
    /// </summary>
    public IReadOnlyList<string>? ChildSystems { get; init; }

    /// <summary>
    /// Gets an error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Creates a successful result from a SystemSnapshot.
    /// </summary>
    public static SystemResult FromSnapshot(SystemSnapshot snapshot)
    {
        return new SystemResult
        {
            Success = true,
            Name = snapshot.Name,
            TypeName = snapshot.TypeName,
            Enabled = snapshot.Enabled,
            Phase = snapshot.Phase,
            Order = snapshot.Order,
            RunsBefore = snapshot.RunsBefore,
            RunsAfter = snapshot.RunsAfter,
            IsGroup = snapshot.IsGroup,
            ChildSystems = snapshot.ChildSystems
        };
    }
}

#endregion
