namespace KeenEyes;

/// <summary>
/// Fluent builder for configuring a spawned entity in a command buffer.
/// </summary>
/// <remarks>
/// <para>
/// EntityCommands is returned by <see cref="ICommandBuffer.Spawn()"/> and allows
/// chaining component additions before the entity is actually created.
/// </para>
/// <para>
/// The entity is not created until <see cref="ICommandBuffer.Flush"/> is called.
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
    /// <summary>
    /// Gets the placeholder ID for this entity.
    /// This can be used to reference the entity in subsequent commands before it is created.
    /// </summary>
    /// <remarks>
    /// The placeholder ID is a negative value that will be mapped to the real entity
    /// after command buffer flush is called.
    /// </remarks>
    public int PlaceholderId { get; }

    /// <summary>
    /// Gets the optional name for this entity.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the list of component addition delegates.
    /// Each delegate takes an IEntityBuilder and returns the modified builder.
    /// </summary>
    internal List<Func<IEntityBuilder, IEntityBuilder>> ComponentAdders { get; } = [];

    internal EntityCommands(int placeholderId, string? name = null)
    {
        PlaceholderId = placeholderId;
        Name = name;
    }

    /// <summary>
    /// Adds a component to the entity being built.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="component">The component data.</param>
    /// <returns>This builder for chaining.</returns>
    public EntityCommands With<T>(T component) where T : struct, IComponent
    {
        ComponentAdders.Add(builder => builder.With(component));
        return this;
    }

    /// <summary>
    /// Adds a tag component to the entity being built.
    /// </summary>
    /// <typeparam name="T">The tag component type.</typeparam>
    /// <returns>This builder for chaining.</returns>
    public EntityCommands WithTag<T>() where T : struct, ITagComponent
    {
        ComponentAdders.Add(builder => builder.WithTag<T>());
        return this;
    }

    /// <summary>
    /// Sets the parent entity for the entity being built.
    /// </summary>
    /// <param name="parent">The parent entity. Must be alive when CommandBuffer.Flush() is called.</param>
    /// <returns>This builder for chaining.</returns>
    /// <remarks>
    /// The parent-child relationship will be established when the command buffer is flushed.
    /// The parent entity must exist and be alive at that time.
    /// </remarks>
    public EntityCommands WithParent(Entity parent)
    {
        ComponentAdders.Add(builder => builder.WithParent(parent));
        return this;
    }

    /// <summary>
    /// Not supported on EntityCommands. Use CommandBuffer.Flush() instead.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown - entities are created during Flush().</exception>
    public Entity Build()
    {
        throw new NotSupportedException(
            "EntityCommands cannot build entities directly. " +
            "Call CommandBuffer.Flush(world) to execute all queued commands and create entities.");
    }

    // Explicit interface implementations for non-generic interface
    IEntityBuilder IEntityBuilder.With<T>(T component) => With(component);
    IEntityBuilder IEntityBuilder.WithTag<T>() => WithTag<T>();
    IEntityBuilder IEntityBuilder.WithParent(Entity parent) => WithParent(parent);
}
