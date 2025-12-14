using System.Text.Json;

namespace KeenEyes.Serialization;

/// <summary>
/// Applies delta snapshots to restore a world to a specific state.
/// </summary>
/// <remarks>
/// <para>
/// The DeltaRestorer applies incremental changes captured in a <see cref="DeltaSnapshot"/>
/// to a world that has been restored from a baseline. Deltas must be applied in sequence
/// order (1, 2, 3...) after the baseline is restored.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // First, restore the baseline
/// var entityMap = SnapshotManager.RestoreSnapshot(world, baseline, serializer);
///
/// // Then apply each delta in sequence
/// foreach (var delta in deltas.OrderBy(d => d.SequenceNumber))
/// {
///     entityMap = DeltaRestorer.ApplyDelta(world, delta, serializer, entityMap);
/// }
/// </code>
/// </example>
public static class DeltaRestorer
{
    /// <summary>
    /// Applies a delta snapshot to a world.
    /// </summary>
    /// <param name="world">The world to apply changes to.</param>
    /// <param name="delta">The delta snapshot containing changes to apply.</param>
    /// <param name="serializer">The component serializer for AOT-compatible deserialization.</param>
    /// <param name="entityMap">
    /// The entity ID mapping from the baseline restoration. Maps original IDs to current entities.
    /// </param>
    /// <returns>
    /// An updated entity ID mapping reflecting any new entities created by this delta.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The delta is applied in the following order:
    /// <list type="number">
    /// <item><description>Destroy removed entities</description></item>
    /// <item><description>Create new entities with their components</description></item>
    /// <item><description>Apply modifications to existing entities</description></item>
    /// <item><description>Update singletons</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The returned entity map includes all entities from the input map (minus destroyed ones)
    /// plus any newly created entities.
    /// </para>
    /// </remarks>
    public static Dictionary<int, Entity> ApplyDelta(
        World world,
        DeltaSnapshot delta,
        IComponentSerializer serializer,
        Dictionary<int, Entity> entityMap)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(delta);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(entityMap);

        // Create a new map to avoid modifying the input
        var newEntityMap = new Dictionary<int, Entity>(entityMap);

        // 1. Destroy removed entities
        foreach (var destroyedId in delta.DestroyedEntityIds)
        {
            if (newEntityMap.TryGetValue(destroyedId, out var entity))
            {
                if (world.IsAlive(entity))
                {
                    world.Despawn(entity);
                }
                newEntityMap.Remove(destroyedId);
            }
        }

        // 2. Create new entities
        foreach (var created in delta.CreatedEntities)
        {
            var entity = CreateEntity(world, created, serializer);
            newEntityMap[created.Id] = entity;
        }

        // 3. Restore parent relationships for new entities
        foreach (var created in delta.CreatedEntities)
        {
            if (!created.ParentId.HasValue)
            {
                continue;
            }

            if (newEntityMap.TryGetValue(created.Id, out var child) &&
                newEntityMap.TryGetValue(created.ParentId.Value, out var parent))
            {
                world.SetParent(child, parent);
            }
        }

        // 4. Apply modifications to existing entities
        foreach (var modification in delta.ModifiedEntities)
        {
            if (!newEntityMap.TryGetValue(modification.EntityId, out var entity))
            {
                continue; // Entity not found - skip
            }

            if (!world.IsAlive(entity))
            {
                continue; // Entity was destroyed - skip
            }

            ApplyEntityDelta(world, entity, modification, serializer, newEntityMap);
        }

        // 5. Update singletons
        foreach (var singleton in delta.ModifiedSingletons)
        {
            var type = serializer.GetType(singleton.TypeName);
            if (type is null)
            {
                continue;
            }

            var value = serializer.Deserialize(singleton.TypeName, singleton.Data);
            if (value is not null)
            {
                serializer.SetSingleton(world, singleton.TypeName, value);
            }
        }

        // 6. Remove deleted singletons
        foreach (var removedType in delta.RemovedSingletonTypes)
        {
            var type = serializer.GetType(removedType);
            if (type is not null)
            {
                world.RemoveSingleton(type);
            }
        }

        return newEntityMap;
    }

    /// <summary>
    /// Creates a new entity from serialized data.
    /// </summary>
    private static Entity CreateEntity(World world, SerializedEntity serialized, IComponentSerializer serializer)
    {
        var builder = world.Spawn(serialized.Name);

        foreach (var component in serialized.Components)
        {
            var type = serializer.GetType(component.TypeName);
            if (type is null)
            {
                continue;
            }

            var info = world.Components.Get(type);
            if (info is null)
            {
                // Try to register
                info = serializer.RegisterComponent(world, component.TypeName, component.IsTag);
                if (info is null)
                {
                    continue;
                }
            }

            if (component.IsTag || !component.Data.HasValue)
            {
                // Tag component - use default value
                var defaultValue = serializer.CreateDefault(component.TypeName);
                if (defaultValue is not null)
                {
                    builder.WithBoxed(info, defaultValue);
                }
            }
            else
            {
                var value = serializer.Deserialize(component.TypeName, component.Data.Value);
                if (value is not null)
                {
                    builder.WithBoxed(info, value);
                }
            }
        }

        return builder.Build();
    }

    /// <summary>
    /// Applies a delta to a single entity.
    /// </summary>
    private static void ApplyEntityDelta(
        World world,
        Entity entity,
        EntityDelta delta,
        IComponentSerializer serializer,
        Dictionary<int, Entity> entityMap)
    {
        // Apply name change
        if (delta.NewName is not null)
        {
            world.SetName(entity, delta.NewName);
        }

        // Apply parent change
        if (delta.ParentRemoved)
        {
            var currentParent = world.GetParent(entity);
            if (currentParent.IsValid)
            {
                world.SetParent(entity, Entity.Null);
            }
        }
        else if (delta.NewParentId.HasValue)
        {
            if (entityMap.TryGetValue(delta.NewParentId.Value, out var newParent))
            {
                world.SetParent(entity, newParent);
            }
        }

        // Remove components
        foreach (var removedType in delta.RemovedComponentTypes)
        {
            var type = serializer.GetType(removedType);
            if (type is not null)
            {
                world.RemoveComponent(entity, type);
            }
        }

        // Add new components
        foreach (var added in delta.AddedComponents)
        {
            var type = serializer.GetType(added.TypeName);
            if (type is null)
            {
                continue;
            }

            var info = world.Components.Get(type);
            if (info is null)
            {
                info = serializer.RegisterComponent(world, added.TypeName, added.IsTag);
                if (info is null)
                {
                    continue;
                }
            }

            object? value;
            if (added.IsTag || !added.Data.HasValue)
            {
                value = serializer.CreateDefault(added.TypeName);
                if (value is null)
                {
                    continue;
                }
            }
            else
            {
                value = serializer.Deserialize(added.TypeName, added.Data.Value);
                if (value is null)
                {
                    continue;
                }
            }

            world.SetComponent(entity, info, value);
        }

        // Update modified components
        foreach (var modified in delta.ModifiedComponents)
        {
            var type = serializer.GetType(modified.TypeName);
            if (type is null || !modified.Data.HasValue)
            {
                continue;
            }

            var info = world.Components.Get(type);
            if (info is null)
            {
                continue;
            }

            var value = serializer.Deserialize(modified.TypeName, modified.Data.Value);
            if (value is not null)
            {
                world.SetComponent(entity, info, value);
            }
        }
    }
}
