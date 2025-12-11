namespace KeenEyes.Debugging;

/// <summary>
/// Provides inspection capabilities for entities, allowing examination of entity state,
/// components, hierarchy, and metadata.
/// </summary>
/// <remarks>
/// <para>
/// The EntityInspector allows you to query detailed information about entities at runtime,
/// which is useful for debugging, tooling, and runtime introspection. It provides access to
/// component data, entity names, parent-child relationships, and metadata.
/// </para>
/// <para>
/// This class requires a concrete <see cref="World"/> instance to access inspection APIs
/// that are not part of the core <see cref="IWorld"/> abstraction.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var inspector = world.GetExtension&lt;EntityInspector&gt;();
/// var info = inspector.Inspect(entity);
///
/// Console.WriteLine($"Entity: {info.Entity.Id} ({info.Name ?? "unnamed"})");
/// Console.WriteLine("Components:");
/// foreach (var component in info.Components)
/// {
///     Console.WriteLine($"  - {component.TypeName} ({component.SizeInBytes} bytes)");
/// }
/// </code>
/// </example>
public sealed class EntityInspector
{
    private readonly World world;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityInspector"/> class.
    /// </summary>
    /// <param name="world">The world to inspect entities in.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="world"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="world"/> is not a concrete <see cref="World"/> instance.</exception>
    public EntityInspector(IWorld world)
    {
        if (world == null)
            throw new ArgumentNullException(nameof(world));

        if (world is not World concreteWorld)
            throw new ArgumentException("EntityInspector requires a concrete World instance", nameof(world));

        this.world = concreteWorld;
    }

    /// <summary>
    /// Inspects an entity and returns detailed information about it.
    /// </summary>
    /// <param name="entity">The entity to inspect.</param>
    /// <returns>Detailed information about the entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the entity is dead or invalid.</exception>
    /// <remarks>
    /// This method reads the current state of the entity. Component values reflect the state
    /// at the time of inspection and may change if the entity is modified.
    /// </remarks>
    public EntityInfo Inspect(Entity entity)
    {
        if (!world.IsAlive(entity))
        {
            throw new InvalidOperationException($"Cannot inspect dead or invalid entity {entity.Id}");
        }

        var name = world.GetName(entity);
        var parent = world.GetParent(entity);
        var children = world.GetChildren(entity).ToList();

        // Get component types by iterating through all registered components
        // and checking which ones the entity has
        var components = new List<ComponentInfo>();
        foreach (var componentInfo in world.Components.All)
        {
            if (world.HasComponent(entity, componentInfo.Type))
            {
                components.Add(new ComponentInfo
                {
                    TypeName = componentInfo.Type.Name,
                    Type = componentInfo.Type,
                    SizeInBytes = componentInfo.Size
                });
            }
        }

        return new EntityInfo
        {
            Entity = entity,
            Name = name,
            Components = components,
            Parent = parent.IsValid ? parent : null,
            Children = children
        };
    }

    /// <summary>
    /// Gets all entities in the world.
    /// </summary>
    /// <returns>A collection of all living entities.</returns>
    /// <remarks>
    /// This method returns a snapshot of entities at the time of the call. Entities may be
    /// created or destroyed after this method returns.
    /// </remarks>
    public IReadOnlyList<Entity> GetAllEntities()
    {
        return world.GetAllEntities().ToList();
    }

    /// <summary>
    /// Checks if an entity has a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to check for.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity has the component; otherwise, false.</returns>
    public bool HasComponent<T>(Entity entity) where T : struct, IComponent
    {
        return world.Has<T>(entity);
    }

    /// <summary>
    /// Checks if an entity has a component of the specified type.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="componentType">The component type to check for.</param>
    /// <returns>True if the entity has the component; otherwise, false.</returns>
    public bool HasComponent(Entity entity, Type componentType)
    {
        return world.HasComponent(entity, componentType);
    }
}

/// <summary>
/// Represents detailed information about an entity.
/// </summary>
/// <remarks>
/// This record contains a snapshot of entity state at a point in time, including
/// components, hierarchy relationships, and metadata.
/// </remarks>
public readonly record struct EntityInfo
{
    /// <summary>
    /// Gets the entity handle.
    /// </summary>
    public required Entity Entity { get; init; }

    /// <summary>
    /// Gets the entity's name, if it has one.
    /// </summary>
    public required string? Name { get; init; }

    /// <summary>
    /// Gets the list of components on this entity.
    /// </summary>
    public required IReadOnlyList<ComponentInfo> Components { get; init; }

    /// <summary>
    /// Gets the entity's parent, if it has one.
    /// </summary>
    public required Entity? Parent { get; init; }

    /// <summary>
    /// Gets the entity's children.
    /// </summary>
    public required IReadOnlyList<Entity> Children { get; init; }
}

/// <summary>
/// Represents information about a component on an entity.
/// </summary>
/// <remarks>
/// This record contains metadata about a component, including its type and size.
/// It does not contain the component's actual data values.
/// </remarks>
public readonly record struct ComponentInfo
{
    /// <summary>
    /// Gets the name of the component type.
    /// </summary>
    public required string TypeName { get; init; }

    /// <summary>
    /// Gets the runtime type of the component.
    /// </summary>
    public required Type Type { get; init; }

    /// <summary>
    /// Gets the size of the component in bytes.
    /// </summary>
    public required int SizeInBytes { get; init; }
}
