namespace KeenEyes.Serialization;

/// <summary>
/// Reconstructs a full <see cref="WorldSnapshot"/> by applying a <see cref="DeltaSnapshot"/>
/// to a baseline snapshot, without requiring a live <see cref="World"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DeltaRestorer"/> mutates a live world in place; this applier is its
/// world-free counterpart. It operates purely on the serialized representation
/// (<see cref="WorldSnapshot"/>, <see cref="SerializedEntity"/>, <see cref="SerializedComponent"/>)
/// and never deserializes component payloads, so it needs no component serializer and
/// performs no reflection.
/// </para>
/// <para>
/// This makes it suitable for tools that must reconstruct historical state offline -
/// for example extracting ghost trails from a delta-compressed replay - where spinning
/// up a world purely to read back state would be wasteful and would require a serializer.
/// </para>
/// <para>
/// To reconstruct the state at a delta marker, restore the governing keyframe's
/// <see cref="WorldSnapshot"/> and apply each intervening delta in order:
/// <code>
/// var state = keyframe.Snapshot!;
/// foreach (var marker in deltaMarkersInOrder)
/// {
///     state = DeltaSnapshotApplier.Apply(state, marker.Delta!);
/// }
/// </code>
/// </para>
/// </remarks>
public static class DeltaSnapshotApplier
{
    /// <summary>
    /// Applies a delta snapshot to a baseline snapshot and returns the reconstructed state.
    /// </summary>
    /// <param name="baseline">
    /// The snapshot the delta is relative to - either a keyframe or a previously
    /// reconstructed state.
    /// </param>
    /// <param name="delta">The incremental changes to apply.</param>
    /// <returns>
    /// A new <see cref="WorldSnapshot"/> representing the world state after the delta
    /// is applied. The input snapshots are never mutated.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="baseline"/> or <paramref name="delta"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The delta is applied in the same conceptual order as <see cref="DeltaRestorer.ApplyDelta"/>:
    /// destroyed entities are removed, created entities are appended, then per-entity
    /// modifications (name, parent, and added/removed/modified components) are applied,
    /// followed by singleton additions, replacements, and removals.
    /// </para>
    /// <para>
    /// Baseline entity order is preserved and newly created entities are appended, keeping
    /// parents ahead of children as required by <see cref="WorldSnapshot.Entities"/>.
    /// References that cannot be resolved (a modification targeting an entity absent from
    /// the baseline, a removal of a component that is not present) are skipped, mirroring
    /// the lenient behavior of <see cref="DeltaRestorer"/>.
    /// </para>
    /// </remarks>
    public static WorldSnapshot Apply(WorldSnapshot baseline, DeltaSnapshot delta)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(delta);

        // Rebuild the entity list: baseline entities (minus destroyed ones) keep their
        // order, then created entities are appended. indexById tracks each surviving
        // entity's slot so modifications can be applied in place.
        var destroyed = new HashSet<int>(delta.DestroyedEntityIds);
        var entities = new List<SerializedEntity>(baseline.Entities.Count);
        var indexById = new Dictionary<int, int>(baseline.Entities.Count);

        foreach (var entity in baseline.Entities)
        {
            if (destroyed.Contains(entity.Id))
            {
                continue;
            }

            indexById[entity.Id] = entities.Count;
            entities.Add(entity);
        }

        foreach (var created in delta.CreatedEntities)
        {
            if (indexById.TryGetValue(created.Id, out var existingIndex))
            {
                // An id collision with a surviving entity is unexpected; treat the created
                // record as authoritative to match a live re-spawn's last-writer-wins result.
                entities[existingIndex] = created;
            }
            else
            {
                indexById[created.Id] = entities.Count;
                entities.Add(created);
            }
        }

        foreach (var modification in delta.ModifiedEntities)
        {
            if (!indexById.TryGetValue(modification.EntityId, out var index))
            {
                continue; // Entity not present in the baseline - skip.
            }

            entities[index] = ApplyEntityDelta(entities[index], modification);
        }

        var singletons = ApplySingletonChanges(baseline.Singletons, delta);

        return new WorldSnapshot
        {
            Version = baseline.Version,
            Timestamp = delta.Timestamp,
            Entities = entities,
            Singletons = singletons,
            Metadata = baseline.Metadata,
        };
    }

    /// <summary>
    /// Produces a new <see cref="SerializedEntity"/> with the entity delta applied.
    /// </summary>
    private static SerializedEntity ApplyEntityDelta(SerializedEntity entity, EntityDelta delta)
    {
        var name = delta.NameRemoved ? null : (delta.NewName ?? entity.Name);

        int? parentId = entity.ParentId;
        if (delta.ParentRemoved)
        {
            parentId = null;
        }
        else if (delta.NewParentId.HasValue)
        {
            parentId = delta.NewParentId;
        }

        var components = new List<SerializedComponent>(entity.Components);

        if (delta.RemovedComponentTypes.Count > 0)
        {
            var removed = new HashSet<string>(delta.RemovedComponentTypes, StringComparer.Ordinal);
            components.RemoveAll(c => removed.Contains(c.TypeName));
        }

        foreach (var added in delta.AddedComponents)
        {
            ReplaceOrAppendComponent(components, added);
        }

        foreach (var modified in delta.ModifiedComponents)
        {
            ReplaceOrAppendComponent(components, modified);
        }

        return entity with
        {
            Name = name,
            ParentId = parentId,
            Components = components,
        };
    }

    /// <summary>
    /// Replaces the component with a matching type name, or appends it when absent.
    /// </summary>
    private static void ReplaceOrAppendComponent(List<SerializedComponent> components, SerializedComponent component)
    {
        var index = components.FindIndex(c => string.Equals(c.TypeName, component.TypeName, StringComparison.Ordinal));
        if (index >= 0)
        {
            components[index] = component;
        }
        else
        {
            components.Add(component);
        }
    }

    /// <summary>
    /// Applies singleton additions, replacements, and removals from the delta.
    /// </summary>
    private static List<SerializedSingleton> ApplySingletonChanges(
        IReadOnlyList<SerializedSingleton> baseline,
        DeltaSnapshot delta)
    {
        var singletons = new List<SerializedSingleton>(baseline);

        if (delta.RemovedSingletonTypes.Count > 0)
        {
            var removed = new HashSet<string>(delta.RemovedSingletonTypes, StringComparer.Ordinal);
            singletons.RemoveAll(s => removed.Contains(s.TypeName));
        }

        foreach (var modified in delta.ModifiedSingletons)
        {
            var index = singletons.FindIndex(s => string.Equals(s.TypeName, modified.TypeName, StringComparison.Ordinal));
            if (index >= 0)
            {
                singletons[index] = modified;
            }
            else
            {
                singletons.Add(modified);
            }
        }

        return singletons;
    }
}
