using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeenEyes.Serialization;

/// <summary>
/// Provides functionality for creating, serializing, and restoring world snapshots.
/// </summary>
/// <remarks>
/// <para>
/// The SnapshotManager is the central handler for world persistence. It captures
/// the complete state of a world including all entities, components, hierarchy
/// relationships, and singletons.
/// </para>
/// <para>
/// Snapshots can be serialized to JSON format for storage and later restored.
/// The serialization uses <see cref="System.Text.Json"/> for efficient processing.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a snapshot of the current world state
/// var snapshot = SnapshotManager.CreateSnapshot(world);
///
/// // Serialize to JSON
/// var json = SnapshotManager.ToJson(snapshot);
///
/// // Save to file
/// File.WriteAllText("save.json", json);
///
/// // Later, load and restore
/// var loadedJson = File.ReadAllText("save.json");
/// var loadedSnapshot = SnapshotManager.FromJson(loadedJson);
/// SnapshotManager.RestoreSnapshot(world, loadedSnapshot, typeResolver);
/// </code>
/// </example>
public static class SnapshotManager
{
    private static readonly JsonSerializerOptions defaultJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        IncludeFields = true  // Required to serialize/deserialize struct fields
    };

    /// <summary>
    /// Creates a snapshot of the current world state.
    /// </summary>
    /// <param name="world">The world to capture.</param>
    /// <param name="metadata">Optional metadata to include in the snapshot.</param>
    /// <returns>A snapshot containing all entities, components, hierarchy, and singletons.</returns>
    /// <remarks>
    /// <para>
    /// The snapshot captures:
    /// <list type="bullet">
    /// <item><description>All entities and their IDs</description></item>
    /// <item><description>All components attached to each entity</description></item>
    /// <item><description>Entity names (if assigned)</description></item>
    /// <item><description>Parent-child hierarchy relationships</description></item>
    /// <item><description>All world singletons</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The operation iterates through all archetypes and entities, boxing component
    /// values for serialization. This is not intended for use in performance-critical
    /// hot paths.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="world"/> is null.</exception>
    public static WorldSnapshot CreateSnapshot(World world, IReadOnlyDictionary<string, object>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(world);

        var entities = new List<SerializedEntity>();

        // Collect all entities with their components and hierarchy info
        foreach (var entity in world.GetAllEntities())
        {
            var components = new List<SerializedComponent>();

            foreach (var (type, value) in world.GetComponents(entity))
            {
                var info = world.Components.Get(type);
                components.Add(new SerializedComponent
                {
                    TypeName = type.AssemblyQualifiedName ?? type.FullName ?? type.Name,
                    Data = value,
                    IsTag = info?.IsTag ?? false
                });
            }

            var parent = world.GetParent(entity);

            entities.Add(new SerializedEntity
            {
                Id = entity.Id,
                Name = world.GetName(entity),
                Components = components,
                ParentId = parent.IsValid ? parent.Id : null
            });
        }

        // Collect singletons
        var singletons = new List<SerializedSingleton>();
        foreach (var (type, value) in world.GetAllSingletons())
        {
            singletons.Add(new SerializedSingleton
            {
                TypeName = type.AssemblyQualifiedName ?? type.FullName ?? type.Name,
                Data = value
            });
        }

        return new WorldSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            Entities = entities,
            Singletons = singletons,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Restores a world from a snapshot using AOT-compatible deserialization.
    /// </summary>
    /// <param name="world">The world to restore into. Will be cleared before restoration.</param>
    /// <param name="snapshot">The snapshot to restore from.</param>
    /// <param name="serializer">
    /// Component serializer for AOT-compatible deserialization. Pass an instance of
    /// the generated <c>ComponentSerializationRegistry</c> which implements this interface
    /// for components marked with <c>[Component(Serializable = true)]</c>.
    /// </param>
    /// <returns>
    /// A dictionary mapping original entity IDs from the snapshot to newly created entities.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method first clears the world using <see cref="World.Clear"/>, then
    /// recreates all entities with their components. Entity IDs in the restored
    /// world may differ from the original IDs in the snapshot.
    /// </para>
    /// <para>
    /// Hierarchy relationships are reconstructed after all entities are created.
    /// </para>
    /// <para>
    /// The source generator creates <c>ComponentSerializationRegistry</c> which implements
    /// <see cref="IComponentSerializer"/> for components marked with <c>[Component(Serializable = true)]</c>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="world"/>, <paramref name="snapshot"/>, or <paramref name="serializer"/> is null.
    /// </exception>
    public static Dictionary<int, Entity> RestoreSnapshot(
        World world,
        WorldSnapshot snapshot,
        IComponentSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(serializer);

        // Use serializer's type resolver
        Func<string, Type?> resolveType = typeName =>
        {
            return serializer.GetType(typeName);
        };

        // Clear the world before restoration
        world.Clear();

        // Map from snapshot entity ID to new entity
        var entityMap = new Dictionary<int, Entity>();

        // First pass: Create all entities with their components
        foreach (var serializedEntity in snapshot.Entities)
        {
            var builder = world.Spawn(serializedEntity.Name);

            foreach (var component in serializedEntity.Components)
            {
                var type = resolveType(component.TypeName);
                if (type is null)
                {
                    // Type not found - skip this component
                    continue;
                }

                // Ensure type is registered
                var info = world.Components.Get(type)
                    ?? RegisterComponent(world, type, component.TypeName, component.IsTag, serializer);

                // Convert the data to the correct type if needed
                var value = ConvertComponentData(component.Data, type, serializer);
                if (value is not null)
                {
                    builder.WithBoxed(info, value);
                }
            }

            var entity = builder.Build();
            entityMap[serializedEntity.Id] = entity;
        }

        // Second pass: Restore hierarchy relationships
        foreach (var serializedEntity in snapshot.Entities.Where(e => e.ParentId.HasValue))
        {
            if (entityMap.TryGetValue(serializedEntity.Id, out var child) &&
                entityMap.TryGetValue(serializedEntity.ParentId!.Value, out var parent))
            {
                world.SetParent(child, parent);
            }
        }

        // Restore singletons
        foreach (var singleton in snapshot.Singletons)
        {
            var type = resolveType(singleton.TypeName);
            if (type is null)
            {
                continue;
            }

            var value = ConvertComponentData(singleton.Data, type, serializer);
            if (value is not null)
            {
                SetSingleton(world, type, singleton.TypeName, value, serializer);
            }
        }

        return entityMap;
    }

    /// <summary>
    /// Serializes a snapshot to JSON format.
    /// </summary>
    /// <param name="snapshot">The snapshot to serialize.</param>
    /// <param name="options">Optional JSON serializer options. If null, uses default options.</param>
    /// <returns>A JSON string representing the snapshot.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="snapshot"/> is null.</exception>
    public static string ToJson(WorldSnapshot snapshot, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return JsonSerializer.Serialize(snapshot, options ?? defaultJsonOptions);
    }

    /// <summary>
    /// Deserializes a snapshot from JSON format.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="options">Optional JSON serializer options. If null, uses default options.</param>
    /// <returns>The deserialized snapshot.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid.</exception>
    public static WorldSnapshot? FromJson(string json, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize<WorldSnapshot>(json, options ?? defaultJsonOptions);
    }

    /// <summary>
    /// Gets the default JSON serializer options used by the snapshot manager.
    /// </summary>
    /// <returns>A copy of the default options that can be customized.</returns>
    public static JsonSerializerOptions GetDefaultJsonOptions()
    {
        return new JsonSerializerOptions(defaultJsonOptions);
    }

    /// <summary>
    /// Registers a component type using the serializer's AOT-compatible method.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the serializer cannot register the component type.
    /// </exception>
    private static ComponentInfo RegisterComponent(World world, Type type, string typeName, bool isTag, IComponentSerializer serializer)
    {
        var info = serializer.RegisterComponent(world, typeName, isTag);
        if (info is not null)
        {
            return info;
        }

        // Also try with full name
        if (type.FullName is not null)
        {
            info = serializer.RegisterComponent(world, type.FullName, isTag);
            if (info is not null)
            {
                return info;
            }
        }

        throw new InvalidOperationException(
            $"Component type '{typeName}' is not registered in the serializer. " +
            $"Ensure all component types are marked with [Component(Serializable = true)].");
    }

    /// <summary>
    /// Sets a singleton value using the serializer's AOT-compatible method.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the serializer cannot set the singleton value.
    /// </exception>
    private static void SetSingleton(World world, Type type, string typeName, object value, IComponentSerializer serializer)
    {
        if (serializer.SetSingleton(world, typeName, value))
        {
            return;
        }

        // Also try with full name
        if (type.FullName is not null && serializer.SetSingleton(world, type.FullName, value))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Singleton type '{typeName}' is not registered in the serializer. " +
            $"Ensure all singleton types are marked with [Component(Serializable = true)].");
    }

    /// <summary>
    /// Converts component data to the target type using AOT-compatible deserialization.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the serializer cannot deserialize the component data.
    /// </exception>
    private static object? ConvertComponentData(object data, Type targetType, IComponentSerializer serializer)
    {
        // If the data is already the correct type, return it (direct restore without JSON round-trip)
        if (targetType.IsInstanceOfType(data))
        {
            return data;
        }

        // If the data is a JsonElement (from deserialization), use the AOT serializer
        if (data is JsonElement jsonElement)
        {
            var typeName = targetType.AssemblyQualifiedName ?? targetType.FullName ?? targetType.Name;
            var result = serializer.Deserialize(typeName, jsonElement);
            if (result is not null)
            {
                return result;
            }

            // Also try with full name
            if (targetType.FullName is not null)
            {
                result = serializer.Deserialize(targetType.FullName, jsonElement);
                if (result is not null)
                {
                    return result;
                }
            }

            throw new InvalidOperationException(
                $"Cannot deserialize component type '{typeName}'. " +
                $"Ensure the type is marked with [Component(Serializable = true)].");
        }

        // Data is neither the target type nor JsonElement
        // This can only happen with manually constructed snapshots using unsupported data types
        throw new InvalidOperationException(
            $"Cannot convert data of type '{data.GetType().FullName}' to '{targetType.FullName}'. " +
            "Data must be either the target type or a JsonElement from JSON deserialization.");
    }
}
