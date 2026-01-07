using System.Text.Json;

namespace KeenEyes.Serialization;

/// <summary>
/// Computes delta differences between a world's current state and a baseline snapshot.
/// </summary>
/// <remarks>
/// <para>
/// The DeltaDiffer creates efficient incremental saves by comparing the current world state
/// against a previously saved baseline. Only entities and components that have changed are
/// included in the delta, resulting in significantly smaller save files.
/// </para>
/// </remarks>
public static class DeltaDiffer
{
    /// <summary>
    /// Creates a delta snapshot by comparing the current world state to a baseline.
    /// </summary>
    /// <param name="world">The current world state.</param>
    /// <param name="baseline">The baseline snapshot to compare against.</param>
    /// <param name="serializer">The component serializer for AOT-compatible serialization.</param>
    /// <param name="baselineSlotName">The slot name of the baseline save.</param>
    /// <param name="sequenceNumber">The sequence number for this delta.</param>
    /// <returns>A delta snapshot containing only the changes since the baseline.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public static DeltaSnapshot CreateDelta(
        World world,
        WorldSnapshot baseline,
        IComponentSerializer serializer,
        string baselineSlotName,
        int sequenceNumber = 1)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(baselineSlotName);

        // Build lookup maps for baseline entities
        var baselineEntities = baseline.Entities.ToDictionary(e => e.Id);
        var baselineSingletons = baseline.Singletons.ToDictionary(s => s.TypeName);

        // Track what we find
        var createdEntities = new List<SerializedEntity>();
        var destroyedEntityIds = new List<int>();
        var modifiedEntities = new List<EntityDelta>();
        var modifiedSingletons = new List<SerializedSingleton>();
        var removedSingletonTypes = new List<string>();

        // Get current entity IDs
        var currentEntityIds = new HashSet<int>();
        foreach (var entity in world.GetAllEntities())
        {
            currentEntityIds.Add(entity.Id);
        }

        // Find created and modified entities
        foreach (var entity in world.GetAllEntities())
        {
            if (!baselineEntities.TryGetValue(entity.Id, out var baselineEntity))
            {
                // New entity - serialize completely
                createdEntities.Add(SerializeEntity(world, entity, serializer));
            }
            else
            {
                // Existing entity - check for modifications
                var delta = ComputeEntityDelta(world, entity, baselineEntity, serializer);
                if (!delta.IsEmpty)
                {
                    modifiedEntities.Add(delta);
                }
            }
        }

        // Find destroyed entities
        foreach (var baselineEntity in baseline.Entities)
        {
            if (!currentEntityIds.Contains(baselineEntity.Id))
            {
                destroyedEntityIds.Add(baselineEntity.Id);
            }
        }

        // Compare singletons
        var currentSingletonTypes = new HashSet<string>();
        foreach (var (type, value) in world.GetAllSingletons())
        {
            var typeName = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
            currentSingletonTypes.Add(typeName);

            var jsonData = serializer.Serialize(type, value);
            if (jsonData is null)
            {
                continue;
            }

            // Add if new singleton or if singleton changed
            if (!baselineSingletons.TryGetValue(typeName, out var baselineSingleton) ||
                !JsonElementEquals(jsonData.Value, baselineSingleton.Data))
            {
                modifiedSingletons.Add(new SerializedSingleton
                {
                    TypeName = typeName,
                    Data = jsonData.Value
                });
            }
        }

        // Find removed singletons
        foreach (var baselineSingleton in baseline.Singletons)
        {
            if (!currentSingletonTypes.Contains(baselineSingleton.TypeName))
            {
                removedSingletonTypes.Add(baselineSingleton.TypeName);
            }
        }

        return new DeltaSnapshot
        {
            SequenceNumber = sequenceNumber,
            BaselineSlotName = baselineSlotName,
            CreatedEntities = createdEntities,
            DestroyedEntityIds = destroyedEntityIds,
            ModifiedEntities = modifiedEntities,
            ModifiedSingletons = modifiedSingletons,
            RemovedSingletonTypes = removedSingletonTypes
        };
    }

    /// <summary>
    /// Serializes a single entity for inclusion in a delta.
    /// </summary>
    private static SerializedEntity SerializeEntity(World world, Entity entity, IComponentSerializer serializer)
    {
        var components = new List<SerializedComponent>();

        foreach (var (type, value) in world.GetComponents(entity))
        {
            var info = world.Components.Get(type);
            var typeName = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
            var isTag = info?.IsTag ?? false;

            var jsonData = isTag ? null : serializer.Serialize(type, value);

            components.Add(new SerializedComponent
            {
                TypeName = typeName,
                Data = jsonData,
                IsTag = isTag
            });
        }

        var parent = world.GetParent(entity);

        return new SerializedEntity
        {
            Id = entity.Id,
            Name = world.GetName(entity),
            Components = components,
            ParentId = parent.IsValid ? parent.Id : null
        };
    }

    /// <summary>
    /// Computes the delta for a single entity by comparing current state to baseline.
    /// </summary>
    private static EntityDelta ComputeEntityDelta(
        World world,
        Entity entity,
        SerializedEntity baseline,
        IComponentSerializer serializer)
    {
        string? newName = null;
        int? newParentId = null;
        var parentRemoved = false;
        var addedComponents = new List<SerializedComponent>();
        var removedComponentTypes = new List<string>();
        var modifiedComponents = new List<SerializedComponent>();

        // Check name change
        var currentName = world.GetName(entity);
        if (currentName != baseline.Name)
        {
            newName = currentName; // null means name was removed
        }

        // Check parent change
        var currentParent = world.GetParent(entity);
        var currentParentId = currentParent.IsValid ? currentParent.Id : (int?)null;
        if (currentParentId != baseline.ParentId)
        {
            if (currentParentId.HasValue)
            {
                newParentId = currentParentId.Value;
            }
            else
            {
                parentRemoved = true;
            }
        }

        // Build baseline component lookup
        var baselineComponents = baseline.Components.ToDictionary(c => c.TypeName);

        // Check current components against baseline
        var currentComponentTypes = new HashSet<string>();
        foreach (var (type, value) in world.GetComponents(entity))
        {
            var info = world.Components.Get(type);
            var typeName = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
            currentComponentTypes.Add(typeName);

            var isTag = info?.IsTag ?? false;
            var jsonData = isTag ? null : serializer.Serialize(type, value);

            if (!baselineComponents.TryGetValue(typeName, out var baselineComponent))
            {
                // Component was added
                addedComponents.Add(new SerializedComponent
                {
                    TypeName = typeName,
                    Data = jsonData,
                    IsTag = isTag
                });
            }
            else if (!isTag && jsonData.HasValue && baselineComponent.Data.HasValue &&
                     !JsonElementEquals(jsonData.Value, baselineComponent.Data.Value))
            {
                // Component exists and was modified
                modifiedComponents.Add(new SerializedComponent
                {
                    TypeName = typeName,
                    Data = jsonData,
                    IsTag = isTag
                });
            }
        }

        // Check for removed components
        foreach (var baselineComponent in baseline.Components)
        {
            if (!currentComponentTypes.Contains(baselineComponent.TypeName))
            {
                removedComponentTypes.Add(baselineComponent.TypeName);
            }
        }

        return new EntityDelta
        {
            EntityId = entity.Id,
            NewName = newName,
            NewParentId = newParentId,
            ParentRemoved = parentRemoved,
            AddedComponents = addedComponents,
            RemovedComponentTypes = removedComponentTypes,
            ModifiedComponents = modifiedComponents
        };
    }

    /// <summary>
    /// Compares two JsonElements for equality.
    /// </summary>
    private static bool JsonElementEquals(JsonElement a, JsonElement b)
    {
        // Quick check on value kind
        if (a.ValueKind != b.ValueKind)
        {
            return false;
        }

        // Compare raw text for deep equality
        return a.GetRawText() == b.GetRawText();
    }
}
