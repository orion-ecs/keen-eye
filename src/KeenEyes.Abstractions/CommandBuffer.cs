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

    /// <inheritdoc/>
    public void Clear()
    {
        commands.Clear();
        nextPlaceholderId = -1;
    }
}
