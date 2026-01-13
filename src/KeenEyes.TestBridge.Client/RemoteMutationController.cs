using System.Text.Json;
using KeenEyes.TestBridge.Mutation;

namespace KeenEyes.TestBridge.Client;

/// <summary>
/// Remote implementation of <see cref="IMutationController"/> that communicates over IPC.
/// </summary>
internal sealed class RemoteMutationController(TestBridgeClient client) : IMutationController
{
    #region Entity Management

    /// <inheritdoc />
    public async Task<EntityResult> SpawnAsync(
        string? name = null,
        IReadOnlyList<ComponentData>? components = null,
        CancellationToken cancellationToken = default)
    {
        var result = await client.SendRequestAsync<EntityResult>(
            "mutation.spawn",
            new { name, components },
            cancellationToken);
        return result ?? EntityResult.Fail("No response from server");
    }

    /// <inheritdoc />
    public async Task<bool> DespawnAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "mutation.despawn",
            new { entityId },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EntityResult> CloneAsync(
        int entityId,
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        var result = await client.SendRequestAsync<EntityResult>(
            "mutation.clone",
            new { entityId, name },
            cancellationToken);
        return result ?? EntityResult.Fail("No response from server");
    }

    /// <inheritdoc />
    public async Task<bool> SetNameAsync(int entityId, string name, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "mutation.setName",
            new { entityId, name },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ClearNameAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "mutation.clearName",
            new { entityId },
            cancellationToken);
    }

    #endregion

    #region Hierarchy

    /// <inheritdoc />
    public async Task<bool> SetParentAsync(int entityId, int? parentId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "mutation.setParent",
            new { entityId, parentId },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> GetRootEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var result = await client.SendRequestAsync<int[]>(
            "mutation.getRootEntities",
            null,
            cancellationToken);
        return result ?? [];
    }

    #endregion

    #region Components

    /// <inheritdoc />
    public async Task<bool> AddComponentAsync(
        int entityId,
        string componentType,
        JsonElement? data = null,
        CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "mutation.addComponent",
            new { entityId, componentType, data },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> RemoveComponentAsync(
        int entityId,
        string componentType,
        CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "mutation.removeComponent",
            new { entityId, componentType },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> SetComponentAsync(
        int entityId,
        string componentType,
        JsonElement data,
        CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "mutation.setComponent",
            new { entityId, componentType, data },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> SetFieldAsync(
        int entityId,
        string componentType,
        string fieldName,
        JsonElement value,
        CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "mutation.setField",
            new { entityId, componentType, fieldName, value },
            cancellationToken);
    }

    #endregion

    #region Tags

    /// <inheritdoc />
    public async Task<bool> AddTagAsync(int entityId, string tag, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "mutation.addTag",
            new { entityId, tag },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> RemoveTagAsync(int entityId, string tag, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "mutation.removeTag",
            new { entityId, tag },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetAllTagsAsync(CancellationToken cancellationToken = default)
    {
        var result = await client.SendRequestAsync<string[]>(
            "mutation.getAllTags",
            null,
            cancellationToken);
        return result ?? [];
    }

    #endregion
}
