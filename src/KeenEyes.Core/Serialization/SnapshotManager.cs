using System.Diagnostics.CodeAnalysis;
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
    /// <summary>
    /// Creates a snapshot of the current world state using AOT-compatible serialization.
    /// </summary>
    /// <param name="world">The world to capture.</param>
    /// <param name="serializer">
    /// Component serializer for AOT-compatible serialization. Pass an instance of
    /// the generated <c>ComponentSerializationRegistry</c> which implements this interface
    /// for components marked with <c>[Component(Serializable = true)]</c>.
    /// </param>
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
    /// Component and singleton data is pre-serialized to JSON using the provided serializer
    /// for Native AOT compatibility. This eliminates the need for reflection during JSON serialization.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="world"/> or <paramref name="serializer"/> is null.
    /// </exception>
    public static WorldSnapshot CreateSnapshot(
        World world,
        IComponentSerializer serializer,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(serializer);

        var entities = new List<SerializedEntity>();

        // Collect all entities with their components and hierarchy info
        foreach (var entity in world.GetAllEntities())
        {
            var components = new List<SerializedComponent>();

            foreach (var (type, value) in world.GetComponents(entity))
            {
                var info = world.Components.Get(type);
                var typeName = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
                var isTag = info?.IsTag ?? false;

                // Serialize component data using IComponentSerializer for AOT compatibility
                var jsonData = isTag ? null : serializer.Serialize(type, value);

                components.Add(new SerializedComponent
                {
                    TypeName = typeName,
                    Data = jsonData,
                    IsTag = isTag
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
            var typeName = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;

            // Serialize singleton data using IComponentSerializer for AOT compatibility
            var jsonData = serializer.Serialize(type, value)
                ?? throw new InvalidOperationException(
                    $"Failed to serialize singleton of type '{typeName}'. " +
                    $"Ensure the type is marked with [Component(Serializable = true)].");

            singletons.Add(new SerializedSingleton
            {
                TypeName = typeName,
                Data = jsonData
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

            // Deserialize singleton data from JSON
            var typeName = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
            var value = serializer.Deserialize(typeName, singleton.Data)
                ?? (type.FullName is not null ? serializer.Deserialize(type.FullName, singleton.Data) : null);

            if (value is not null)
            {
                SetSingleton(world, type, singleton.TypeName, value, serializer);
            }
        }

        return entityMap;
    }

    /// <summary>
    /// Serializes a snapshot to JSON format using AOT-compatible source generation.
    /// </summary>
    /// <param name="snapshot">The snapshot to serialize.</param>
    /// <returns>A JSON string representing the snapshot.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="snapshot"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// This method serializes the WorldSnapshot envelope (metadata, entity list, etc.), not component data.
    /// Component data serialization is handled by IComponentSerializer for AOT compatibility.
    /// </para>
    /// <para>
    /// Uses source-generated JSON serialization which is fully Native AOT compatible.
    /// The serialization uses camelCase naming, includes fields, and omits null values.
    /// </para>
    /// </remarks>
    public static string ToJson(WorldSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return JsonSerializer.Serialize(snapshot, SnapshotJsonContext.Default.WorldSnapshot);
    }

    /// <summary>
    /// Deserializes a snapshot from JSON format using AOT-compatible source generation.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized snapshot.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid.</exception>
    /// <remarks>
    /// <para>
    /// This method deserializes the WorldSnapshot envelope (metadata, entity list, etc.), not component data.
    /// Component data deserialization is handled by IComponentSerializer for AOT compatibility.
    /// </para>
    /// <para>
    /// Uses source-generated JSON serialization which is fully Native AOT compatible.
    /// The deserialization expects camelCase naming and supports fields.
    /// </para>
    /// </remarks>
    public static WorldSnapshot? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize(json, SnapshotJsonContext.Default.WorldSnapshot);
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
    /// Converts component data from JSON to the target type using AOT-compatible deserialization.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the serializer cannot deserialize the component data.
    /// </exception>
    private static object? ConvertComponentData(JsonElement? data, Type targetType, IComponentSerializer serializer)
    {
        // Null data (tag components)
        if (data is null)
        {
            return null;
        }

        var jsonElement = data.Value;
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
}
