using KeenEyes.TestBridge.Systems;

namespace KeenEyes.TestBridge.SystemImpl;

/// <summary>
/// In-process implementation of <see cref="ISystemController"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation provides access to the world's system manager for
/// querying and controlling ECS systems via MCP tools.
/// </para>
/// </remarks>
/// <param name="world">The world whose systems to control.</param>
internal sealed class SystemControllerImpl(World world) : ISystemController
{
    /// <inheritdoc />
    public Task<IReadOnlyList<SystemSnapshot>> GetSystemsAsync()
    {
        var snapshots = new List<SystemSnapshot>();

        foreach (var entry in world.GetAllSystemEntries())
        {
            snapshots.Add(CreateSnapshot(entry.System, entry.Phase, entry.Order, entry.RunsBefore, entry.RunsAfter));
        }

        return Task.FromResult<IReadOnlyList<SystemSnapshot>>(snapshots);
    }

    /// <inheritdoc />
    public Task<int> GetCountAsync()
    {
        int count = 0;
        foreach (var _ in world.GetAllSystemEntries())
        {
            count++;
        }
        return Task.FromResult(count);
    }

    /// <inheritdoc />
    public Task<SystemSnapshot?> GetSystemAsync(string name)
    {
        foreach (var entry in world.GetAllSystemEntries())
        {
            var systemType = entry.System.GetType();
            if (systemType.Name == name || systemType.FullName == name)
            {
                return Task.FromResult<SystemSnapshot?>(
                    CreateSnapshot(entry.System, entry.Phase, entry.Order, entry.RunsBefore, entry.RunsAfter));
            }
        }

        return Task.FromResult<SystemSnapshot?>(null);
    }

    /// <inheritdoc />
    public async Task<SystemSnapshot> EnableSystemAsync(string name)
    {
        if (!world.EnableSystemByName(name))
        {
            throw new KeyNotFoundException($"System not found: {name}");
        }

        var snapshot = await GetSystemAsync(name);
        return snapshot!;
    }

    /// <inheritdoc />
    public async Task<SystemSnapshot> DisableSystemAsync(string name)
    {
        if (!world.DisableSystemByName(name))
        {
            throw new KeyNotFoundException($"System not found: {name}");
        }

        var snapshot = await GetSystemAsync(name);
        return snapshot!;
    }

    /// <inheritdoc />
    public async Task<SystemSnapshot> ToggleSystemAsync(string name)
    {
        var system = world.GetSystemByName(name);
        if (system is null)
        {
            throw new KeyNotFoundException($"System not found: {name}");
        }

        system.Enabled = !system.Enabled;

        var snapshot = await GetSystemAsync(name);
        return snapshot!;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SystemSnapshot>> GetSystemsByPhaseAsync(string phase)
    {
        var snapshots = new List<SystemSnapshot>();

        foreach (var entry in world.GetAllSystemEntries())
        {
            if (entry.Phase.ToString().Equals(phase, StringComparison.OrdinalIgnoreCase))
            {
                snapshots.Add(CreateSnapshot(entry.System, entry.Phase, entry.Order, entry.RunsBefore, entry.RunsAfter));
            }
        }

        return Task.FromResult<IReadOnlyList<SystemSnapshot>>(snapshots);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SystemSnapshot>> GetEnabledSystemsAsync()
    {
        var snapshots = new List<SystemSnapshot>();

        foreach (var entry in world.GetAllSystemEntries())
        {
            if (entry.System.Enabled)
            {
                snapshots.Add(CreateSnapshot(entry.System, entry.Phase, entry.Order, entry.RunsBefore, entry.RunsAfter));
            }
        }

        return Task.FromResult<IReadOnlyList<SystemSnapshot>>(snapshots);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SystemSnapshot>> GetDisabledSystemsAsync()
    {
        var snapshots = new List<SystemSnapshot>();

        foreach (var entry in world.GetAllSystemEntries())
        {
            if (!entry.System.Enabled)
            {
                snapshots.Add(CreateSnapshot(entry.System, entry.Phase, entry.Order, entry.RunsBefore, entry.RunsAfter));
            }
        }

        return Task.FromResult<IReadOnlyList<SystemSnapshot>>(snapshots);
    }

    private static SystemSnapshot CreateSnapshot(
        ISystem system,
        SystemPhase phase,
        int order,
        Type[] runsBefore,
        Type[] runsAfter)
    {
        var systemType = system.GetType();
        var isGroup = system is SystemGroup;
        List<string>? childSystems = null;

        if (system is SystemGroup group)
        {
            childSystems = [];
            foreach (var child in group.Systems)
            {
                childSystems.Add(child.GetType().Name);
            }
        }

        return new SystemSnapshot
        {
            Name = systemType.Name,
            TypeName = systemType.FullName ?? systemType.Name,
            Enabled = system.Enabled,
            Phase = phase.ToString(),
            Order = order,
            RunsBefore = runsBefore.Length > 0 ? runsBefore.Select(t => t.Name).ToList() : null,
            RunsAfter = runsAfter.Length > 0 ? runsAfter.Select(t => t.Name).ToList() : null,
            IsGroup = isGroup,
            ChildSystems = childSystems
        };
    }
}
