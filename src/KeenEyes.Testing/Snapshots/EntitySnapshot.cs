namespace KeenEyes.Testing.Snapshots;

/// <summary>
/// Represents a snapshot of a single entity's state at a point in time.
/// </summary>
/// <remarks>
/// <para>
/// EntitySnapshot captures the complete state of an entity including its ID, version,
/// and all component data. This is useful for verifying entity state in tests.
/// </para>
/// <para>
/// Component data is stored as a dictionary keyed by component type name, with
/// values containing the component's field data.
/// </para>
/// </remarks>
public sealed class EntitySnapshot
{
    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public int EntityId { get; init; }

    /// <summary>
    /// Gets the entity version.
    /// </summary>
    public int Version { get; init; }

    /// <summary>
    /// Gets the entity's name, if any.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the component data dictionary.
    /// </summary>
    /// <remarks>
    /// Keys are component type names (without namespace), values are dictionaries
    /// of field names to their values.
    /// </remarks>
    public Dictionary<string, Dictionary<string, object?>> Components { get; init; } = [];

    /// <summary>
    /// Gets the component type names present on this entity.
    /// </summary>
    public IReadOnlyList<string> ComponentTypes => [.. Components.Keys];

    /// <summary>
    /// Gets the number of components on this entity.
    /// </summary>
    public int ComponentCount => Components.Count;

    /// <summary>
    /// Checks if the entity has a component of the specified type.
    /// </summary>
    /// <param name="componentTypeName">The component type name.</param>
    /// <returns>True if the entity has the component; otherwise, false.</returns>
    public bool HasComponent(string componentTypeName) => Components.ContainsKey(componentTypeName);

    /// <summary>
    /// Checks if the entity has a component of the specified type.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <returns>True if the entity has the component; otherwise, false.</returns>
    public bool HasComponent<T>() where T : IComponent => Components.ContainsKey(typeof(T).Name);

    /// <summary>
    /// Gets the component data for the specified type.
    /// </summary>
    /// <param name="componentTypeName">The component type name.</param>
    /// <returns>The component field data, or null if not present.</returns>
    public Dictionary<string, object?>? GetComponent(string componentTypeName)
    {
        return Components.TryGetValue(componentTypeName, out var data) ? data : null;
    }

    /// <summary>
    /// Gets a specific field value from a component.
    /// </summary>
    /// <typeparam name="T">The expected field type.</typeparam>
    /// <param name="componentTypeName">The component type name.</param>
    /// <param name="fieldName">The field name.</param>
    /// <returns>The field value, or default if not found.</returns>
    public T? GetFieldValue<T>(string componentTypeName, string fieldName)
    {
        if (!Components.TryGetValue(componentTypeName, out var componentData))
        {
            return default;
        }

        if (!componentData.TryGetValue(fieldName, out var value))
        {
            return default;
        }

        if (value is T typedValue)
        {
            return typedValue;
        }

        // Handle numeric conversions
        if (value != null && typeof(T).IsAssignableFrom(value.GetType()))
        {
            return (T)value;
        }

        try
        {
            return (T)Convert.ChangeType(value!, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Creates a snapshot of an entity from the specified world.
    /// </summary>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="entity">The entity to snapshot.</param>
    /// <returns>A snapshot of the entity's state.</returns>
    /// <exception cref="ArgumentException">Thrown when the entity is not alive.</exception>
    public static EntitySnapshot Create(World world, Entity entity)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!world.IsAlive(entity))
        {
            throw new ArgumentException($"Entity {entity} is not alive.", nameof(entity));
        }

        var snapshot = new EntitySnapshot
        {
            EntityId = entity.Id,
            Version = entity.Version,
            Name = world.GetName(entity)
        };

        // Get all components for this entity using GetComponents()
        foreach (var (componentType, componentValue) in world.GetComponents(entity))
        {
            var componentData = CaptureComponentData(componentType, componentValue);
            snapshot.Components[componentType.Name] = componentData;
        }

        return snapshot;
    }

    private static Dictionary<string, object?> CaptureComponentData(Type componentType, object componentValue)
    {
        var data = new Dictionary<string, object?>();

        // Use reflection to capture field values for the snapshot
        // Note: This is acceptable for test utilities, not production code
        var fields = componentType.GetFields(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance);

        foreach (var field in fields)
        {
            data[field.Name] = field.GetValue(componentValue);
        }

        return data;
    }

    /// <summary>
    /// Returns a string representation of this snapshot.
    /// </summary>
    public override string ToString()
    {
        var components = string.Join(", ", Components.Keys);
        return $"EntitySnapshot {{ Id = {EntityId}, Version = {Version}, Name = {Name ?? "(none)"}, Components = [{components}] }}";
    }
}
