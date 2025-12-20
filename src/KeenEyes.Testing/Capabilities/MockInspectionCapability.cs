using KeenEyes.Capabilities;

namespace KeenEyes.Testing.Capabilities;

/// <summary>
/// Mock implementation of <see cref="IInspectionCapability"/> for testing.
/// </summary>
/// <remarks>
/// <para>
/// This mock allows setting up entity names, component types, and registered components
/// for testing inspection functionality without a real World.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var mock = new MockInspectionCapability();
/// mock.SetName(entity, "Player");
/// mock.RegisterComponent&lt;Position&gt;();
/// mock.AddComponentToEntity(entity, typeof(Position));
///
/// Assert.Equal("Player", mock.GetName(entity));
/// Assert.True(mock.HasComponent(entity, typeof(Position)));
/// </code>
/// </example>
public sealed class MockInspectionCapability : IInspectionCapability
{
    private readonly Dictionary<Entity, string?> entityNames = [];
    private readonly Dictionary<Entity, HashSet<Type>> entityComponents = [];
    private readonly List<RegisteredComponentInfo> registeredComponents = [];

    /// <summary>
    /// Sets the name for an entity.
    /// </summary>
    /// <param name="entity">The entity to name.</param>
    /// <param name="name">The name to assign.</param>
    public void SetName(Entity entity, string? name)
    {
        entityNames[entity] = name;
    }

    /// <inheritdoc />
    public string? GetName(Entity entity)
    {
        return entityNames.TryGetValue(entity, out var name) ? name : null;
    }

    /// <summary>
    /// Adds a component type to an entity's component set.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="componentType">The component type to add.</param>
    public void AddComponentToEntity(Entity entity, Type componentType)
    {
        if (!entityComponents.TryGetValue(entity, out var components))
        {
            components = [];
            entityComponents[entity] = components;
        }
        components.Add(componentType);
    }

    /// <summary>
    /// Adds a component type to an entity's component set.
    /// </summary>
    /// <typeparam name="T">The component type to add.</typeparam>
    /// <param name="entity">The entity.</param>
    public void AddComponentToEntity<T>(Entity entity) where T : struct, IComponent
    {
        AddComponentToEntity(entity, typeof(T));
    }

    /// <inheritdoc />
    public bool HasComponent(Entity entity, Type componentType)
    {
        return entityComponents.TryGetValue(entity, out var components) &&
               components.Contains(componentType);
    }

    /// <summary>
    /// Registers a component type.
    /// </summary>
    /// <typeparam name="T">The component type to register.</typeparam>
    /// <param name="isTag">Whether this is a tag component.</param>
    public void RegisterComponent<T>(bool isTag = false) where T : struct
    {
        var type = typeof(T);
        RegisterComponent(type, isTag);
    }

    /// <summary>
    /// Registers a component type.
    /// </summary>
    /// <param name="type">The component type to register.</param>
    /// <param name="isTag">Whether this is a tag component.</param>
    public void RegisterComponent(Type type, bool isTag = false)
    {
        // Calculate size (0 for tags, otherwise use Marshal.SizeOf or estimate)
        int size = isTag ? 0 : 4; // Default size estimate for testing

        registeredComponents.Add(new RegisteredComponentInfo(
            Type: type,
            Name: type.Name,
            Size: size,
            IsTag: isTag));
    }

    /// <inheritdoc />
    public IEnumerable<RegisteredComponentInfo> GetRegisteredComponents()
    {
        return registeredComponents;
    }

    /// <summary>
    /// Clears all mock state.
    /// </summary>
    public void Clear()
    {
        entityNames.Clear();
        entityComponents.Clear();
        registeredComponents.Clear();
    }
}
