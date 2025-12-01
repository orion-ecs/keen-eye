namespace KeenEyes;

/// <summary>
/// Buffers entity modifications for deferred execution.
/// Use this to safely modify entities during iteration.
/// </summary>
/// <remarks>
/// <para>
/// CommandBuffer allows you to queue entity creation, destruction, and component
/// modifications during system iteration without invalidating the iterator.
/// Call <see cref="Execute"/> to apply all buffered commands.
/// </para>
/// <example>
/// <code>
/// var buffer = new CommandBuffer(world);
/// foreach (var entity in world.Query&lt;Health&gt;())
/// {
///     ref var health = ref world.Get&lt;Health&gt;(entity);
///     if (health.Current &lt;= 0)
///     {
///         buffer.QueueDespawn(entity);
///     }
/// }
/// buffer.Execute();
/// </code>
/// </example>
/// </remarks>
public sealed class CommandBuffer
{
    private readonly World world;
    private readonly List<ICommand> commands = [];

    /// <summary>
    /// Creates a new command buffer for the specified world.
    /// </summary>
    /// <param name="world">The world to execute commands on.</param>
    public CommandBuffer(World world)
    {
        this.world = world;
    }

    /// <summary>
    /// The world this command buffer operates on.
    /// </summary>
    public World World => world;

    /// <summary>
    /// The number of commands currently buffered.
    /// </summary>
    public int Count => commands.Count;

    /// <summary>
    /// Queues a new entity for creation.
    /// </summary>
    /// <returns>A builder for adding components to the deferred entity.</returns>
    public DeferredEntityBuilder QueueSpawn()
    {
        var builder = new DeferredEntityBuilder(this, null);
        return builder;
    }

    /// <summary>
    /// Queues a new entity for creation with the specified name.
    /// </summary>
    /// <param name="name">The name to assign to the entity.</param>
    /// <returns>A builder for adding components to the deferred entity.</returns>
    public DeferredEntityBuilder QueueSpawn(string name)
    {
        var builder = new DeferredEntityBuilder(this, name);
        return builder;
    }

    /// <summary>
    /// Queues an entity for destruction.
    /// </summary>
    /// <param name="entity">The entity to destroy.</param>
    public void QueueDespawn(Entity entity)
    {
        commands.Add(new DespawnCommand(entity));
    }

    /// <summary>
    /// Queues a component to be added to an entity.
    /// </summary>
    /// <typeparam name="T">The component type to add.</typeparam>
    /// <param name="entity">The entity to add the component to.</param>
    /// <param name="component">The component data to add.</param>
    public void QueueAdd<T>(Entity entity, T component) where T : struct, IComponent
    {
        commands.Add(new AddComponentCommand<T>(entity, component));
    }

    /// <summary>
    /// Queues a tag component to be added to an entity.
    /// </summary>
    /// <typeparam name="T">The tag component type to add.</typeparam>
    /// <param name="entity">The entity to add the tag to.</param>
    public void QueueAddTag<T>(Entity entity) where T : struct, ITagComponent
    {
        commands.Add(new AddTagCommand<T>(entity));
    }

    /// <summary>
    /// Queues a component to be set on an entity.
    /// </summary>
    /// <typeparam name="T">The component type to set.</typeparam>
    /// <param name="entity">The entity to set the component on.</param>
    /// <param name="component">The component data to set.</param>
    public void QueueSet<T>(Entity entity, T component) where T : struct, IComponent
    {
        commands.Add(new SetComponentCommand<T>(entity, component));
    }

    /// <summary>
    /// Queues a component to be removed from an entity.
    /// </summary>
    /// <typeparam name="T">The component type to remove.</typeparam>
    /// <param name="entity">The entity to remove the component from.</param>
    public void QueueRemove<T>(Entity entity) where T : struct, IComponent
    {
        commands.Add(new RemoveComponentCommand<T>(entity));
    }

    /// <summary>
    /// Executes all buffered commands and clears the buffer.
    /// </summary>
    public void Execute()
    {
        foreach (var command in commands)
        {
            command.Execute(world);
        }
        commands.Clear();
    }

    /// <summary>
    /// Clears all buffered commands without executing them.
    /// </summary>
    public void Clear()
    {
        commands.Clear();
    }

    internal void AddSpawnCommand(SpawnCommand command)
    {
        commands.Add(command);
    }
}

/// <summary>
/// Builder for creating entities through a command buffer.
/// </summary>
public sealed class DeferredEntityBuilder
{
    private readonly CommandBuffer buffer;
    private readonly string? name;
    private readonly List<(ComponentInfo Info, object Data)> components = [];

    internal DeferredEntityBuilder(CommandBuffer buffer, string? name)
    {
        this.buffer = buffer;
        this.name = name;
    }

    /// <summary>
    /// Adds a component to the deferred entity.
    /// </summary>
    /// <typeparam name="T">The component type to add.</typeparam>
    /// <param name="component">The component data.</param>
    /// <returns>This builder for chaining.</returns>
    public DeferredEntityBuilder With<T>(T component) where T : struct, IComponent
    {
        var info = buffer.World.Components.GetOrRegister<T>();
        components.Add((info, component));
        return this;
    }

    /// <summary>
    /// Adds a tag component to the deferred entity.
    /// </summary>
    /// <typeparam name="T">The tag component type to add.</typeparam>
    /// <returns>This builder for chaining.</returns>
    public DeferredEntityBuilder WithTag<T>() where T : struct, ITagComponent
    {
        var info = buffer.World.Components.GetOrRegister<T>(isTag: true);
        components.Add((info, default(T)!));
        return this;
    }

    /// <summary>
    /// Queues the entity for creation when the buffer is executed.
    /// </summary>
    public void Build()
    {
        buffer.AddSpawnCommand(new SpawnCommand(components, name));
    }
}

/// <summary>
/// Base interface for deferred commands.
/// </summary>
internal interface ICommand
{
    void Execute(World world);
}

/// <summary>
/// Command to spawn a new entity.
/// </summary>
internal sealed class SpawnCommand : ICommand
{
    private readonly List<(ComponentInfo Info, object Data)> components;
    private readonly string? name;

    public SpawnCommand(List<(ComponentInfo Info, object Data)> components, string? name)
    {
        // Copy the list to avoid issues if the builder is reused
        this.components = [.. components];
        this.name = name;
    }

    public void Execute(World world)
    {
        world.CreateEntity(components, name);
    }
}

/// <summary>
/// Command to despawn an entity.
/// </summary>
internal sealed class DespawnCommand : ICommand
{
    private readonly Entity entity;

    public DespawnCommand(Entity entity)
    {
        this.entity = entity;
    }

    public void Execute(World world)
    {
        world.Despawn(entity);
    }
}

/// <summary>
/// Command to add a component to an entity.
/// </summary>
internal sealed class AddComponentCommand<T> : ICommand where T : struct, IComponent
{
    private readonly Entity entity;
    private readonly T component;

    public AddComponentCommand(Entity entity, T component)
    {
        this.entity = entity;
        this.component = component;
    }

    public void Execute(World world)
    {
        if (world.IsAlive(entity) && !world.Has<T>(entity))
        {
            world.Add(entity, component);
        }
    }
}

/// <summary>
/// Command to add a tag component to an entity.
/// </summary>
internal sealed class AddTagCommand<T> : ICommand where T : struct, ITagComponent
{
    private readonly Entity entity;

    public AddTagCommand(Entity entity)
    {
        this.entity = entity;
    }

    public void Execute(World world)
    {
        if (world.IsAlive(entity) && !world.Has<T>(entity))
        {
            world.AddTag<T>(entity);
        }
    }
}

/// <summary>
/// Command to set a component on an entity.
/// </summary>
internal sealed class SetComponentCommand<T> : ICommand where T : struct, IComponent
{
    private readonly Entity entity;
    private readonly T component;

    public SetComponentCommand(Entity entity, T component)
    {
        this.entity = entity;
        this.component = component;
    }

    public void Execute(World world)
    {
        if (world.IsAlive(entity) && world.Has<T>(entity))
        {
            world.Set(entity, component);
        }
    }
}

/// <summary>
/// Command to remove a component from an entity.
/// </summary>
internal sealed class RemoveComponentCommand<T> : ICommand where T : struct, IComponent
{
    private readonly Entity entity;

    public RemoveComponentCommand(Entity entity)
    {
        this.entity = entity;
    }

    public void Execute(World world)
    {
        if (world.IsAlive(entity))
        {
            world.Remove<T>(entity);
        }
    }
}
