namespace KeenEyes;

/// <summary>
/// Fluent builder for configuring a spawned entity in a <see cref="CommandBuffer"/>.
/// </summary>
/// <remarks>
/// <para>
/// EntityCommands is returned by <see cref="CommandBuffer.Spawn()"/> and allows
/// chaining component additions before the entity is actually created.
/// </para>
/// <para>
/// The entity is not created until <see cref="CommandBuffer.Flush"/> is called.
/// This enables safe entity creation during system iteration.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var buffer = new CommandBuffer();
/// buffer.Spawn()
///     .With(new Position { X = 0, Y = 0 })
///     .With(new Velocity { X = 1, Y = 0 })
///     .WithTag&lt;ActiveTag&gt;();
/// buffer.Flush(world);
/// </code>
/// </example>
public sealed class EntityCommands : IEntityBuilder<EntityCommands>
{
    private readonly SpawnCommand spawnCommand;

    internal EntityCommands(SpawnCommand spawnCommand)
    {
        this.spawnCommand = spawnCommand;
    }

    /// <summary>
    /// Gets the placeholder ID for this entity.
    /// This can be used to reference the entity in subsequent commands before it is created.
    /// </summary>
    /// <remarks>
    /// The placeholder ID is a negative value that will be mapped to the real entity
    /// after <see cref="CommandBuffer.Flush"/> is called.
    /// </remarks>
    public int PlaceholderId => spawnCommand.PlaceholderId;

    /// <summary>
    /// Adds a component to the entity being built.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="component">The component data.</param>
    /// <returns>This builder for chaining.</returns>
    public EntityCommands With<T>(T component) where T : struct, IComponent
    {
        spawnCommand.Components.Add((typeof(T), component, false));
        return this;
    }

    /// <summary>
    /// Adds a tag component to the entity being built.
    /// </summary>
    /// <typeparam name="T">The tag component type.</typeparam>
    /// <returns>This builder for chaining.</returns>
    public EntityCommands WithTag<T>() where T : struct, ITagComponent
    {
        spawnCommand.Components.Add((typeof(T), default(T)!, true));
        return this;
    }

    // Explicit interface implementations for non-generic interface
    IEntityBuilder IEntityBuilder.With<T>(T component) => With(component);
    IEntityBuilder IEntityBuilder.WithTag<T>() => WithTag<T>();
}
