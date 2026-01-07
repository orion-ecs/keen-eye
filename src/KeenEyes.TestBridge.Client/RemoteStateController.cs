using KeenEyes.TestBridge.Ipc.Protocol;
using KeenEyes.TestBridge.State;

namespace KeenEyes.TestBridge.Client;

/// <summary>
/// Remote implementation of <see cref="IStateController"/> that communicates over IPC.
/// </summary>
internal sealed class RemoteStateController(TestBridgeClient client) : IStateController
{
    /// <inheritdoc />
    public async Task<int> GetEntityCountAsync()
    {
        return await client.SendRequestAsync<int>("state.getEntityCount", null, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EntitySnapshot>> QueryEntitiesAsync(EntityQuery query)
    {
        var result = await client.SendRequestAsync<EntitySnapshot[]>("state.queryEntities", query, CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<EntitySnapshot?> GetEntityAsync(int entityId)
    {
        return await client.SendRequestAsync<EntitySnapshot?>("state.getEntity", new EntityIdArgs { EntityId = entityId }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<EntitySnapshot?> GetEntityByNameAsync(string name)
    {
        return await client.SendRequestAsync<EntitySnapshot?>("state.getEntityByName", new NameArgs { Name = name }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, object?>?> GetComponentAsync(int entityId, string componentTypeName)
    {
        return await client.SendRequestAsync<Dictionary<string, object?>?>(
            "state.getComponent",
            new ComponentArgs { EntityId = entityId, ComponentTypeName = componentTypeName },
            CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<WorldStats> GetWorldStatsAsync()
    {
        var result = await client.SendRequestAsync<WorldStats>("state.getWorldStats", null, CancellationToken.None);
        return result ?? throw new InvalidOperationException("Failed to get world stats");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SystemInfo>> GetSystemsAsync()
    {
        var result = await client.SendRequestAsync<SystemInfo[]>("state.getSystems", null, CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<object?> GetExtensionAsync(string typeName)
    {
        return await client.SendRequestAsync<object?>("state.getExtension", new TypeNameArgs { TypeName = typeName }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<bool> HasExtensionAsync(string typeName)
    {
        return await client.SendRequestAsync<bool>("state.hasExtension", new TypeNameArgs { TypeName = typeName }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<PerformanceMetrics> GetPerformanceMetricsAsync(int frameCount = 60)
    {
        var result = await client.SendRequestAsync<PerformanceMetrics>(
            "state.getPerformanceMetrics",
            new FrameCountArgs { FrameCount = frameCount },
            CancellationToken.None);
        return result ?? throw new InvalidOperationException("Failed to get performance metrics");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> GetEntitiesWithTagAsync(string tag)
    {
        var result = await client.SendRequestAsync<int[]>("state.getEntitiesWithTag", new TagArgs { Tag = tag }, CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> GetChildrenAsync(int parentId)
    {
        var result = await client.SendRequestAsync<int[]>("state.getChildren", new ParentIdArgs { ParentId = parentId }, CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<int?> GetParentAsync(int entityId)
    {
        return await client.SendRequestAsync<int?>("state.getParent", new EntityIdArgs { EntityId = entityId }, CancellationToken.None);
    }
}
