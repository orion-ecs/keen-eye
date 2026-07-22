using System.Text.Json;
using KeenEyes.TestBridge.Mutation;

namespace KeenEyes.Mcp.TestBridge.Tests.Mocks;

/// <summary>
/// Mock implementation of IMutationController for testing MCP tools.
/// </summary>
internal sealed class MockMutationController : IMutationController
{
    private int nextEntityId = 1;

    public Task<EntityResult> SpawnAsync(
        string? name = null,
        IReadOnlyList<ComponentData>? components = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(EntityResult.Ok(nextEntityId++, 1));

    public Task<bool> DespawnAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<EntityResult> CloneAsync(
        int entityId,
        string? name = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(EntityResult.Ok(nextEntityId++, 1));

    public Task<bool> SetNameAsync(int entityId, string name, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> ClearNameAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> SetParentAsync(int entityId, int? parentId, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> AddComponentAsync(
        int entityId,
        string componentType,
        JsonElement? data = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> RemoveComponentAsync(
        int entityId,
        string componentType,
        CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> SetComponentAsync(
        int entityId,
        string componentType,
        JsonElement data,
        CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> SetFieldAsync(
        int entityId,
        string componentType,
        string fieldName,
        JsonElement value,
        CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> AddTagAsync(int entityId, string tag, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> RemoveTagAsync(int entityId, string tag, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<IReadOnlyList<int>> GetRootEntitiesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<int>>([]);

    public Task<IReadOnlyList<string>> GetAllTagsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>([]);
}
