using KeenEyes.Capabilities;

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
    private readonly IWorld world;
    private readonly IInspectionCapability inspectionCapability;
    private readonly IHierarchyCapability? hierarchyCapability;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityInspector"/> class.
    /// </summary>
    /// <param name="world">The world to inspect entities in.</param>
    /// <param name="inspectionCapability">The inspection capability.</param>
    /// <param name="hierarchyCapability">Optional hierarchy capability for parent/child relationships.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    public EntityInspector(
        IWorld world,
        IInspectionCapability inspectionCapability,
        IHierarchyCapability? hierarchyCapability = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(inspectionCapability);

        this.world = world;
        this.inspectionCapability = inspectionCapability;
        this.hierarchyCapability = hierarchyCapability;
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

        var name = inspectionCapability.GetName(entity);

        Entity parent = Entity.Null;
        List<Entity> children = [];

        if (hierarchyCapability is not null)
        {
            parent = hierarchyCapability.GetParent(entity);
            children = hierarchyCapability.GetChildren(entity).ToList();
        }

        // Get component types by iterating through all registered components
        // and checking which ones the entity has
        var components = new List<ComponentInfo>();
        foreach (var registeredComponent in inspectionCapability.GetRegisteredComponents())
        {
            if (inspectionCapability.HasComponent(entity, registeredComponent.Type))
            {
                components.Add(new ComponentInfo
                {
                    TypeName = registeredComponent.Name,
                    Type = registeredComponent.Type,
                    SizeInBytes = registeredComponent.Size
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
        return inspectionCapability.HasComponent(entity, componentType);
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
