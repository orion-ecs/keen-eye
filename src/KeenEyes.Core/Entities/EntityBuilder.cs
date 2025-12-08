namespace KeenEyes;

/// <summary>
/// Fluent builder for creating entities with components.
/// </summary>
public sealed class EntityBuilder : IEntityBuilder<EntityBuilder>
{
    private readonly World world;
    private readonly List<(ComponentInfo Info, object Data)> components = [];
    private readonly List<string> stringTags = [];
    private string? name;

    internal EntityBuilder(World world)
    {
        this.world = world;
    }

    /// <summary>
    /// The world this builder creates entities in.
    /// </summary>
    public World World => world;

    /// <inheritdoc />
    public EntityBuilder With<T>(T component) where T : struct, IComponent
    {
        var info = world.Components.GetOrRegister<T>();

        // Remove existing component of same type to support overrides
        for (int i = components.Count - 1; i >= 0; i--)
        {
            if (components[i].Info.Type == typeof(T))
            {
                components.RemoveAt(i);
                break;
            }
        }

        components.Add((info, component));
        return this;
    }

    /// <inheritdoc />
    public EntityBuilder WithTag<T>() where T : struct, ITagComponent
    {
        var info = world.Components.GetOrRegister<T>(isTag: true);
        components.Add((info, default(T)!));
        return this;
    }

    /// <summary>
    /// Adds a string tag to be applied to the entity when built.
    /// </summary>
    /// <param name="tag">The string tag to add. Cannot be null, empty, or whitespace.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tag"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tag"/> is empty or whitespace.</exception>
    /// <remarks>
    /// <para>
    /// String tags provide runtime-flexible tagging that complements type-safe tag components.
    /// Use <see cref="WithTag{T}()"/> for compile-time type-safe tags, or this method for
    /// runtime-determined tags from data files, editors, or dynamic content.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var enemy = world.Spawn()
    ///     .With(new Position { X = 0, Y = 0 })
    ///     .WithTag("Enemy")
    ///     .WithTag("Hostile")
    ///     .Build();
    ///
    /// // Check tags later
    /// if (world.HasTag(enemy, "Hostile"))
    /// {
    ///     // Handle hostile entity
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="WithTag{T}()"/>
    /// <seealso cref="World.AddTag(Entity, string)"/>
    public EntityBuilder WithTag(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);

        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new ArgumentException("Tag cannot be empty or whitespace.", nameof(tag));
        }

        stringTags.Add(tag);
        return this;
    }

    /// <summary>
    /// Builds the entity and adds it to the world.
    /// </summary>
    /// <returns>The created entity.</returns>
    public Entity Build()
    {
        var entity = world.CreateEntity(components, name);

        // Apply string tags after entity creation
        foreach (var tag in stringTags)
        {
            world.AddTag(entity, tag);
        }

        return entity;
    }

    // Explicit interface implementations for non-generic interface
    IEntityBuilder IEntityBuilder.With<T>(T component) => With(component);
    IEntityBuilder IEntityBuilder.WithTag<T>() => WithTag<T>();

    internal void Reset()
    {
        components.Clear();
        stringTags.Clear();
        name = null;
    }

    internal void Reset(string? entityName)
    {
        components.Clear();
        stringTags.Clear();
        name = entityName;
    }
}
