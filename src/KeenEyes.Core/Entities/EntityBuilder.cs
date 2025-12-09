namespace KeenEyes;

/// <summary>
/// Fluent builder for creating entities with components.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Performance Note:</strong> Components are temporarily boxed during entity construction.
/// This incurs one heap allocation per component added via <see cref="With{T}(T)"/>. However,
/// entity creation is typically not a performance-critical path in ECS applications. Once built,
/// components are stored unboxed in archetype arrays for zero-copy access during iteration.
/// </para>
/// <para>
/// The boxing overhead only affects the entity creation path. Query iteration and system processing
/// (the hot paths in ECS) operate directly on unboxed component data in contiguous archetype storage
/// with zero allocations.
/// </para>
/// <para>
/// If batch entity creation becomes a bottleneck, consider:
/// </para>
/// <list type="bullet">
/// <item><description>Creating entities in larger batches during initialization rather than per-frame</description></item>
/// <item><description>Using object pooling for frequently created/destroyed entity types</description></item>
/// <item><description>Profiling to confirm entity creation is actually the bottleneck (not queries/systems)</description></item>
/// </list>
/// <para>
/// This design trade-off prioritizes API ergonomics and simplicity while maintaining excellent
/// performance where it matters most: query iteration and system execution.
/// </para>
/// </remarks>
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

    /// <summary>
    /// Adds a component to the entity being built.
    /// </summary>
    /// <typeparam name="T">The component type to add.</typeparam>
    /// <param name="component">The component data to add.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method temporarily boxes the struct component during entity construction. The component
    /// is unboxed when copied to archetype storage during <see cref="Build()"/>, where it is stored
    /// in contiguous unboxed arrays for efficient iteration.
    /// </para>
    /// <para>
    /// If called multiple times with the same component type, only the last value is used.
    /// This allows overriding component values during builder configuration.
    /// </para>
    /// </remarks>
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

    /// <summary>
    /// Adds a tag component to the entity being built.
    /// </summary>
    /// <typeparam name="T">The tag component type to add.</typeparam>
    /// <returns>This builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Tag components are marker types with no data. They are used for filtering entities in queries
    /// (e.g., <c>world.Query&lt;Position&gt;().With&lt;EnemyTag&gt;()</c>).
    /// </para>
    /// <para>
    /// Like <see cref="With{T}(T)"/>, this method temporarily boxes a default struct value during
    /// entity construction. For tag components, this is typically a zero-byte struct, making the
    /// allocation overhead minimal.
    /// </para>
    /// </remarks>
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
    /// Adds a component to the entity being built using a boxed value.
    /// </summary>
    /// <param name="info">The component info from the component registry.</param>
    /// <param name="value">The boxed component value.</param>
    /// <returns>This builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method is primarily intended for serialization and deserialization scenarios
    /// where the component type is not known at compile time. For normal usage, prefer
    /// the generic <see cref="With{T}(T)"/> method.
    /// </para>
    /// <para>
    /// The value must be assignable to the component type specified in the info.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="info"/> or <paramref name="value"/> is null.
    /// </exception>
    public EntityBuilder WithBoxed(ComponentInfo info, object value)
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(value);
        components.Add((info, value));
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
