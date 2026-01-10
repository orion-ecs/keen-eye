using KeenEyes.Capabilities;

namespace KeenEyes.Editor.Common.Serialization;

/// <summary>
/// Provides entity serialization for editor operations like clipboard and undo/redo.
/// </summary>
/// <remarks>
/// <para>
/// This class captures and restores entity state for in-memory editor operations.
/// Unlike the world snapshot system which serializes to JSON/binary for persistence,
/// this serializer stores component data as boxed objects for fast in-process operations.
/// </para>
/// <para>
/// The serializer captures the complete state of an entity including:
/// <list type="bullet">
/// <item><description>All components and their values</description></item>
/// <item><description>Entity name (if assigned)</description></item>
/// <item><description>Child entities (capturing the complete subtree)</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Capture entity state for clipboard
/// var snapshot = EntitySerializer.CaptureEntity(world, selectedEntity);
///
/// // Later, restore to create a copy
/// var duplicatedEntity = EntitySerializer.RestoreEntity(world, snapshot);
/// </code>
/// </example>
public static class EntitySerializer
{
    /// <summary>
    /// Captures the complete state of an entity, including all children.
    /// </summary>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="entity">The entity to capture.</param>
    /// <param name="includeChildren">
    /// Whether to recursively capture child entities. Default is true.
    /// </param>
    /// <returns>A snapshot containing the entity's complete state.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="world"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the entity is not alive.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Component values are boxed and stored as copies. Since components are structs,
    /// this creates independent snapshots that won't be affected by subsequent changes
    /// to the original entity.
    /// </para>
    /// <para>
    /// For deep hierarchies, consider setting <paramref name="includeChildren"/> to false
    /// and capturing children selectively to avoid performance issues.
    /// </para>
    /// </remarks>
    public static EntitySnapshot CaptureEntity(IWorld world, Entity entity, bool includeChildren = true)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!world.IsAlive(entity))
        {
            throw new InvalidOperationException($"Cannot capture entity {entity.Id}: entity is not alive.");
        }

        // Get entity name and component metadata if available
        string? name = null;
        Dictionary<Type, bool>? tagLookup = null;

        if (world is IInspectionCapability inspection)
        {
            name = inspection.GetName(entity);

            // Build a lookup for tag components
            tagLookup = inspection.GetRegisteredComponents()
                .ToDictionary(c => c.Type, c => c.IsTag);
        }

        // Capture all components using snapshot capability
        var components = new List<ComponentSnapshot>();
        if (world is ISnapshotCapability snapshotCapability)
        {
            foreach (var (type, value) in snapshotCapability.GetComponents(entity))
            {
                // Determine if it's a tag component
                var isTag = tagLookup?.TryGetValue(type, out var tag) == true && tag;

                components.Add(new ComponentSnapshot
                {
                    ComponentType = type,
                    Value = isTag ? null : CloneValue(value),
                    IsTag = isTag
                });
            }
        }

        // Capture children recursively if requested
        var children = new List<EntitySnapshot>();
        if (includeChildren && world is IHierarchyCapability hierarchy)
        {
            foreach (var child in hierarchy.GetChildren(entity))
            {
                children.Add(CaptureEntity(world, child, includeChildren: true));
            }
        }

        return new EntitySnapshot
        {
            OriginalId = entity.Id,
            Name = name,
            Components = components,
            Children = children,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Captures the state of multiple entities.
    /// </summary>
    /// <param name="world">The world containing the entities.</param>
    /// <param name="entities">The entities to capture.</param>
    /// <param name="includeChildren">
    /// Whether to recursively capture child entities for each entity.
    /// </param>
    /// <returns>A list of snapshots, one for each captured entity.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="world"/> or <paramref name="entities"/> is null.
    /// </exception>
    /// <remarks>
    /// Dead entities in the input are silently skipped.
    /// </remarks>
    public static IReadOnlyList<EntitySnapshot> CaptureEntities(
        IWorld world,
        IEnumerable<Entity> entities,
        bool includeChildren = true)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(entities);

        var snapshots = new List<EntitySnapshot>();
        foreach (var entity in entities)
        {
            if (world.IsAlive(entity))
            {
                snapshots.Add(CaptureEntity(world, entity, includeChildren));
            }
        }
        return snapshots;
    }

    /// <summary>
    /// Restores an entity from a snapshot.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="snapshot">The snapshot to restore from.</param>
    /// <param name="parent">
    /// Optional parent entity for the restored entity. If null, the entity
    /// will be created as a root entity.
    /// </param>
    /// <returns>The newly created entity.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="world"/> or <paramref name="snapshot"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The restored entity will have a new ID assigned by the world. The original ID
    /// from the snapshot is not preserved.
    /// </para>
    /// <para>
    /// If the snapshot's name is already taken in the world, a unique suffix will
    /// be appended to avoid name collisions.
    /// </para>
    /// <para>
    /// Child entities from the snapshot are also restored, maintaining the original
    /// parent-child relationships within the restored subtree.
    /// </para>
    /// </remarks>
    public static Entity RestoreEntity(IWorld world, EntitySnapshot snapshot, Entity? parent = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(snapshot);

        // Generate a unique name if the original is taken
        var name = GenerateUniqueName(world, snapshot.Name);

        // Create the entity
        var builder = world.Spawn(name);
        var entity = builder.Build();

        // Add all components
        foreach (var componentSnapshot in snapshot.Components)
        {
            if (componentSnapshot.IsTag)
            {
                // For tag components, we need to add them without data
                // Use the world's AddTag method if available, or SetComponent with default value
                var defaultValue = Activator.CreateInstance(componentSnapshot.ComponentType);
                if (defaultValue is not null)
                {
                    world.SetComponent(entity, componentSnapshot.ComponentType, defaultValue);
                }
            }
            else if (componentSnapshot.Value is not null)
            {
                world.SetComponent(entity, componentSnapshot.ComponentType, componentSnapshot.Value);
            }
        }

        // Set parent if specified
        if (parent.HasValue && parent.Value.IsValid && world is IHierarchyCapability hierarchy)
        {
            hierarchy.SetParent(entity, parent.Value);
        }

        // Restore children
        if (snapshot.Children.Count > 0 && world is IHierarchyCapability)
        {
            foreach (var childSnapshot in snapshot.Children)
            {
                RestoreEntity(world, childSnapshot, entity);
            }
        }

        return entity;
    }

    /// <summary>
    /// Restores multiple entities from snapshots.
    /// </summary>
    /// <param name="world">The world to create the entities in.</param>
    /// <param name="snapshots">The snapshots to restore from.</param>
    /// <param name="parent">
    /// Optional parent entity for all restored entities.
    /// </param>
    /// <returns>A list of newly created entities.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="world"/> or <paramref name="snapshots"/> is null.
    /// </exception>
    public static IReadOnlyList<Entity> RestoreEntities(
        IWorld world,
        IEnumerable<EntitySnapshot> snapshots,
        Entity? parent = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(snapshots);

        var entities = new List<Entity>();
        foreach (var snapshot in snapshots)
        {
            entities.Add(RestoreEntity(world, snapshot, parent));
        }
        return entities;
    }

    /// <summary>
    /// Duplicates an entity in the same world.
    /// </summary>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="entity">The entity to duplicate.</param>
    /// <param name="includeChildren">Whether to also duplicate child entities.</param>
    /// <returns>The newly created duplicate entity.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="world"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the entity is not alive.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The duplicate will have the same components and values as the original,
    /// but a new entity ID. If the original has a name, the duplicate will
    /// receive a unique name with a numeric suffix.
    /// </para>
    /// <para>
    /// If <paramref name="includeChildren"/> is true, all descendants are also
    /// duplicated, preserving the hierarchy structure.
    /// </para>
    /// </remarks>
    public static Entity DuplicateEntity(IWorld world, Entity entity, bool includeChildren = true)
    {
        var snapshot = CaptureEntity(world, entity, includeChildren);

        // Get the original entity's parent to place duplicate as sibling
        Entity? parent = null;
        if (world is IHierarchyCapability hierarchy)
        {
            var originalParent = hierarchy.GetParent(entity);
            if (originalParent.IsValid)
            {
                parent = originalParent;
            }
        }

        return RestoreEntity(world, snapshot, parent);
    }

    /// <summary>
    /// Generates a unique name based on the provided name.
    /// </summary>
    /// <param name="world">The world to check for name uniqueness.</param>
    /// <param name="baseName">The base name to make unique. If null, returns null.</param>
    /// <returns>A unique name, or null if baseName was null.</returns>
    private static string? GenerateUniqueName(IWorld world, string? baseName)
    {
        if (baseName is null)
        {
            return null;
        }

        // Check if the name is already unique
        if (!NameExists(world, baseName))
        {
            return baseName;
        }

        // Try adding numeric suffixes until we find a unique name
        var suffix = 1;
        string newName;
        do
        {
            newName = $"{baseName} ({suffix})";
            suffix++;
        } while (NameExists(world, newName) && suffix < 10000);

        return newName;
    }

    /// <summary>
    /// Checks if an entity name already exists in the world.
    /// </summary>
    private static bool NameExists(IWorld world, string name)
    {
        // We need to check all entities for this name
        // This could be optimized with a name lookup capability
        if (world is not IInspectionCapability inspection)
        {
            return false;
        }

        foreach (var entity in world.GetAllEntities())
        {
            if (inspection.GetName(entity) == name)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Creates a deep copy of a value for snapshot storage.
    /// </summary>
    /// <remarks>
    /// For simple value types (structs), boxing creates a copy.
    /// For reference types inside structs, this is a shallow copy.
    /// </remarks>
    private static object CloneValue(object value)
    {
        // For value types (which components should be), boxing creates a copy
        // This is sufficient for most ECS component use cases
        return value;
    }
}
