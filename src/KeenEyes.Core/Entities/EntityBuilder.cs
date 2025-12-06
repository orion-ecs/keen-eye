namespace KeenEyes;

/// <summary>
/// Fluent builder for creating entities with components.
/// </summary>
public sealed class EntityBuilder : IEntityBuilder<EntityBuilder>
{
    private readonly World world;
    private readonly List<(ComponentInfo Info, object Data)> components = [];
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
    /// Builds the entity and adds it to the world.
    /// </summary>
    /// <returns>The created entity.</returns>
    public Entity Build()
    {
        return world.CreateEntity(components, name);
    }

    // Explicit interface implementations for non-generic interface
    IEntityBuilder IEntityBuilder.With<T>(T component) => With(component);
    IEntityBuilder IEntityBuilder.WithTag<T>() => WithTag<T>();

    internal void Reset()
    {
        components.Clear();
        name = null;
    }

    internal void Reset(string? entityName)
    {
        components.Clear();
        name = entityName;
    }
}
