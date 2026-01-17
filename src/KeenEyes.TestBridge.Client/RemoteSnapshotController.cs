using KeenEyes.TestBridge.Snapshot;

namespace KeenEyes.TestBridge.Client;

/// <summary>
/// Remote implementation of <see cref="ISnapshotController"/> that communicates over IPC.
/// </summary>
internal sealed class RemoteSnapshotController(TestBridgeClient client) : ISnapshotController
{
    /// <inheritdoc />
    public async Task<SnapshotResult> CreateAsync(string name)
    {
        var result = await client.SendRequestAsync<SnapshotResult>(
            "snapshot.create",
            new { name },
            CancellationToken.None);
        return result ?? SnapshotResult.Fail("No response from server");
    }

    /// <inheritdoc />
    public async Task<SnapshotResult> RestoreAsync(string name)
    {
        var result = await client.SendRequestAsync<SnapshotResult>(
            "snapshot.restore",
            new { name },
            CancellationToken.None);
        return result ?? SnapshotResult.Fail("No response from server");
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string name)
    {
        var result = await client.SendRequestAsync<bool>(
            "snapshot.delete",
            new { name },
            CancellationToken.None);
        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SnapshotInfo>> ListAsync()
    {
        var result = await client.SendRequestAsync<List<SnapshotInfo>>(
            "snapshot.list",
            null,
            CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<SnapshotInfo?> GetInfoAsync(string name)
    {
        return await client.SendRequestAsync<SnapshotInfo>(
            "snapshot.getInfo",
            new { name },
            CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<SnapshotDiff> DiffAsync(string name1, string name2)
    {
        var result = await client.SendRequestAsync<SnapshotDiff>(
            "snapshot.diff",
            new { name1, name2 },
            CancellationToken.None);
        return result ?? CreateErrorDiff(name1, name2, "No response from server");
    }

    /// <inheritdoc />
    public async Task<SnapshotDiff> DiffCurrentAsync(string name)
    {
        var result = await client.SendRequestAsync<SnapshotDiff>(
            "snapshot.diffCurrent",
            new { name },
            CancellationToken.None);
        return result ?? CreateErrorDiff(name, "(current)", "No response from server");
    }

    /// <inheritdoc />
    public async Task<SnapshotResult> SaveToFileAsync(string name, string path)
    {
        var result = await client.SendRequestAsync<SnapshotResult>(
            "snapshot.saveToFile",
            new { name, path },
            CancellationToken.None);
        return result ?? SnapshotResult.Fail("No response from server");
    }

    /// <inheritdoc />
    public async Task<SnapshotResult> LoadFromFileAsync(string path, string? name = null)
    {
        var result = await client.SendRequestAsync<SnapshotResult>(
            "snapshot.loadFromFile",
            new { path, name },
            CancellationToken.None);
        return result ?? SnapshotResult.Fail("No response from server");
    }

    /// <inheritdoc />
    public async Task<string> ExportJsonAsync(string name)
    {
        var result = await client.SendRequestAsync<string>(
            "snapshot.exportJson",
            new { name },
            CancellationToken.None);
        return result ?? string.Empty;
    }

    /// <inheritdoc />
    public async Task<SnapshotResult> ImportJsonAsync(string json, string name)
    {
        var result = await client.SendRequestAsync<SnapshotResult>(
            "snapshot.importJson",
            new { json, name },
            CancellationToken.None);
        return result ?? SnapshotResult.Fail("No response from server");
    }

    /// <inheritdoc />
    public async Task<SnapshotResult> QuickSaveAsync()
    {
        var result = await client.SendRequestAsync<SnapshotResult>(
            "snapshot.quickSave",
            null,
            CancellationToken.None);
        return result ?? SnapshotResult.Fail("No response from server");
    }

    /// <inheritdoc />
    public async Task<SnapshotResult> QuickLoadAsync()
    {
        var result = await client.SendRequestAsync<SnapshotResult>(
            "snapshot.quickLoad",
            null,
            CancellationToken.None);
        return result ?? SnapshotResult.Fail("No response from server");
    }

    private static SnapshotDiff CreateErrorDiff(string name1, string name2, string error) => new()
    {
        Snapshot1 = name1,
        Snapshot2 = name2,
        AddedEntities = [],
        RemovedEntities = [],
        ModifiedEntities = [],
        TotalChanges = 0
    };
}
