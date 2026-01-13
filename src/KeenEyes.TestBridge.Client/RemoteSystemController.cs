using KeenEyes.TestBridge.Systems;

namespace KeenEyes.TestBridge.Client;

/// <summary>
/// Remote implementation of <see cref="ISystemController"/> that communicates over IPC.
/// </summary>
internal sealed class RemoteSystemController(TestBridgeClient client) : ISystemController
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SystemSnapshot>> GetSystemsAsync()
    {
        var result = await client.SendRequestAsync<SystemSnapshot[]>("system.list", null, CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<int> GetCountAsync()
    {
        return await client.SendRequestAsync<int>("system.getCount", null, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<SystemSnapshot?> GetSystemAsync(string name)
    {
        return await client.SendRequestAsync<SystemSnapshot?>(
            "system.get",
            new { name },
            CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<SystemSnapshot> EnableSystemAsync(string name)
    {
        var result = await client.SendRequestAsync<SystemSnapshot>(
            "system.enable",
            new { name },
            CancellationToken.None);

        return result ?? throw new KeyNotFoundException($"System not found: {name}");
    }

    /// <inheritdoc />
    public async Task<SystemSnapshot> DisableSystemAsync(string name)
    {
        var result = await client.SendRequestAsync<SystemSnapshot>(
            "system.disable",
            new { name },
            CancellationToken.None);

        return result ?? throw new KeyNotFoundException($"System not found: {name}");
    }

    /// <inheritdoc />
    public async Task<SystemSnapshot> ToggleSystemAsync(string name)
    {
        var result = await client.SendRequestAsync<SystemSnapshot>(
            "system.toggle",
            new { name },
            CancellationToken.None);

        return result ?? throw new KeyNotFoundException($"System not found: {name}");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SystemSnapshot>> GetSystemsByPhaseAsync(string phase)
    {
        var result = await client.SendRequestAsync<SystemSnapshot[]>(
            "system.getByPhase",
            new { phase },
            CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SystemSnapshot>> GetEnabledSystemsAsync()
    {
        var result = await client.SendRequestAsync<SystemSnapshot[]>("system.getEnabled", null, CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SystemSnapshot>> GetDisabledSystemsAsync()
    {
        var result = await client.SendRequestAsync<SystemSnapshot[]>("system.getDisabled", null, CancellationToken.None);
        return result ?? [];
    }
}
