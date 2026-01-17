using KeenEyes.TestBridge.Navigation;

namespace KeenEyes.TestBridge.Client;

/// <summary>
/// Remote implementation of <see cref="INavigationController"/> that communicates over IPC.
/// </summary>
internal sealed class RemoteNavigationController(TestBridgeClient client) : INavigationController
{
    #region Statistics

    /// <inheritdoc />
    public async Task<NavigationStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<NavigationStatisticsSnapshot>(
            "navigation.getStatistics",
            null,
            cancellationToken) ?? new NavigationStatisticsSnapshot
            {
                IsReady = false,
                Strategy = "None",
                ActiveAgentCount = 0,
                PendingRequestCount = 0
            };
    }

    /// <inheritdoc />
    public async Task<bool> IsReadyAsync(CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "navigation.isReady",
            null,
            cancellationToken);
    }

    #endregion

    #region Agent Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> GetNavigationEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var result = await client.SendRequestAsync<int[]>(
            "navigation.getNavigationEntities",
            null,
            cancellationToken);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<NavAgentSnapshot?> GetAgentStateAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<NavAgentSnapshot?>(
            "navigation.getAgentState",
            new { entityId },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<NavPathSnapshot?> GetPathAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<NavPathSnapshot?>(
            "navigation.getPath",
            new { entityId },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> SetDestinationAsync(int entityId, float x, float y, float z, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "navigation.setDestination",
            new { entityId, x, y, z },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> StopAgentAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "navigation.stopAgent",
            new { entityId },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ResumeAgentAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "navigation.resumeAgent",
            new { entityId },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> WarpAgentAsync(int entityId, float x, float y, float z, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "navigation.warpAgent",
            new { entityId, x, y, z },
            cancellationToken);
    }

    #endregion

    #region Path Queries

    /// <inheritdoc />
    public async Task<NavPathSnapshot?> FindPathAsync(float startX, float startY, float startZ, float endX, float endY, float endZ, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<NavPathSnapshot?>(
            "navigation.findPath",
            new { startX, startY, startZ, endX, endY, endZ },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsNavigableAsync(float x, float y, float z, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "navigation.isNavigable",
            new { x, y, z },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<NavPointSnapshot?> FindNearestPointAsync(float x, float y, float z, float searchRadius, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<NavPointSnapshot?>(
            "navigation.findNearestPoint",
            new { x, y, z, searchRadius },
            cancellationToken);
    }

    #endregion
}
