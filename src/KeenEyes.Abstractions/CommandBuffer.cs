namespace KeenEyes;

/// <inheritdoc/>
public sealed class CommandBuffer : ICommandBuffer
{
    private readonly List<ICommand> commands = [];
    private int nextPlaceholderId = -1;

    /// <inheritdoc/>
    public int Count => commands.Count;

    /// <inheritdoc/>
    public EntityCommands Spawn()
    {
        return Spawn(null);
    }

    /// <inheritdoc/>
    public EntityCommands Spawn(string? name)
    {
        var placeholderId = nextPlaceholderId--;
        var entityCommands = new EntityCommands(placeholderId, name);
        var spawnCommand = new SpawnCommand(entityCommands);
        commands.Add(spawnCommand);
        return entityCommands;
    }

    /// <inheritdoc/>
    public void Despawn(Entity entity)
    {
        commands.Add(new DespawnCommand(entity));
    }

    /// <inheritdoc/>
    public void Despawn(int placeholderId)
    {
        commands.Add(new DespawnCommand(placeholderId));
    }

    /// <inheritdoc/>
    public void AddComponent<T>(Entity entity, T component) where T : struct, IComponent
    {
        commands.Add(new AddComponentCommand(entity, (world, e) => world.Add(e, component)));
    }

    /// <inheritdoc/>
    public void AddComponent<T>(int placeholderId, T component) where T : struct, IComponent
    {
        commands.Add(new AddComponentCommand(placeholderId, (world, e) => world.Add(e, component)));
    }

    /// <inheritdoc/>
    public void RemoveComponent<T>(Entity entity) where T : struct, IComponent
    {
        commands.Add(new RemoveComponentCommand(entity, (world, e) => world.Remove<T>(e)));
    }

    /// <inheritdoc/>
    public void RemoveComponent<T>(int placeholderId) where T : struct, IComponent
    {
        commands.Add(new RemoveComponentCommand(placeholderId, (world, e) => world.Remove<T>(e)));
    }

    /// <inheritdoc/>
    public void SetComponent<T>(Entity entity, T component) where T : struct, IComponent
    {
        commands.Add(new SetComponentCommand(entity, (world, e) => world.Set(e, component)));
    }

    /// <inheritdoc/>
    public void SetComponent<T>(int placeholderId, T component) where T : struct, IComponent
    {
        commands.Add(new SetComponentCommand(placeholderId, (world, e) => world.Set(e, component)));
    }

    /// <inheritdoc/>
    public Dictionary<int, Entity> Flush(IWorld world)
    {
        var entityMap = new Dictionary<int, Entity>();

        try
        {
            foreach (var command in commands)
            {
                command.Execute(world, entityMap);
            }
        }
        finally
        {
            Clear();
        }

        return entityMap;
    }

    /// <summary>
    /// Executes all queued commands against a shared placeholder-to-entity map, enabling
    /// resolution of placeholders created by buffers flushed earlier in the same batch.
    /// </summary>
    /// <param name="world">The world to execute commands on.</param>
    /// <param name="sharedEntityMap">
    /// A placeholder-to-entity map shared across buffers. Spawn commands add this buffer's
    /// new entities to it (making them resolvable by later buffers), and placeholder
    /// references are resolved against it.
    /// </param>
    /// <returns>The entities spawned by this buffer, keyed by their local placeholder ID.</returns>
    /// <remarks>
    /// Used by the command buffer pool to thread cross-buffer placeholder resolution
    /// across a staged flush. Placeholder IDs are only unique within a single buffer, so when
    /// two buffers reuse the same local ID the most recently flushed spawn wins for that ID.
    /// </remarks>
    internal Dictionary<int, Entity> Flush(IWorld world, Dictionary<int, Entity> sharedEntityMap)
    {
        // This buffer allocates placeholder IDs contiguously starting at -1 (see Spawn), so at
        // flush time the IDs it created are exactly the range (nextPlaceholderId, -1]. Capture
        // the boundary before Clear() resets it so we can report this buffer's own spawns.
        var lowestPlaceholderId = nextPlaceholderId;

        try
        {
            foreach (var command in commands)
            {
                command.Execute(world, sharedEntityMap);
            }
        }
        finally
        {
            Clear();
        }

        var localEntityMap = new Dictionary<int, Entity>();
        for (var placeholderId = -1; placeholderId > lowestPlaceholderId; placeholderId--)
        {
            if (sharedEntityMap.TryGetValue(placeholderId, out var entity))
            {
                localEntityMap[placeholderId] = entity;
            }
        }

        return localEntityMap;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        commands.Clear();
        nextPlaceholderId = -1;
    }
}
