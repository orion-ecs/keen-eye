using KeenEyes.TestBridge.Snapshot;

namespace KeenEyes.Mcp.TestBridge.Tests.Mocks;

/// <summary>
/// Mock implementation of ISnapshotController for testing MCP tools.
/// </summary>
internal sealed class MockSnapshotController : ISnapshotController
{
    public Task<SnapshotResult> CreateAsync(string name) => Task.FromResult(SnapshotResult.Ok(name));

    public Task<SnapshotResult> RestoreAsync(string name) => Task.FromResult(SnapshotResult.Ok(name));

    public Task<bool> DeleteAsync(string name) => Task.FromResult(true);

    public Task<IReadOnlyList<SnapshotInfo>> ListAsync()
        => Task.FromResult<IReadOnlyList<SnapshotInfo>>([]);

    public Task<SnapshotInfo?> GetInfoAsync(string name) => Task.FromResult<SnapshotInfo?>(null);

    public Task<SnapshotDiff> DiffAsync(string name1, string name2)
        => Task.FromResult(EmptyDiff(name1, name2));

    public Task<SnapshotDiff> DiffCurrentAsync(string name)
        => Task.FromResult(EmptyDiff("current", name));

    public Task<SnapshotResult> SaveToFileAsync(string name, string path)
        => Task.FromResult(SnapshotResult.Ok(name));

    public Task<SnapshotResult> LoadFromFileAsync(string path, string? name = null)
        => Task.FromResult(SnapshotResult.Ok(name ?? "loaded"));

    public Task<string> ExportJsonAsync(string name) => Task.FromResult("{}");

    public Task<SnapshotResult> ImportJsonAsync(string json, string name)
        => Task.FromResult(SnapshotResult.Ok(name));

    public Task<SnapshotResult> QuickSaveAsync() => Task.FromResult(SnapshotResult.Ok("quicksave"));

    public Task<SnapshotResult> QuickLoadAsync() => Task.FromResult(SnapshotResult.Ok("quicksave"));

    private static SnapshotDiff EmptyDiff(string name1, string name2) => new()
    {
        Snapshot1 = name1,
        Snapshot2 = name2,
        AddedEntities = [],
        RemovedEntities = [],
        ModifiedEntities = [],
        TotalChanges = 0
    };
}
