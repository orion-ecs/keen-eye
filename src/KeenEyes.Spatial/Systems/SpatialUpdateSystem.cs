using KeenEyes.Common;
using KeenEyes.Spatial.Partitioning;

namespace KeenEyes.Spatial.Systems;

/// <summary>
/// System that maintains the spatial index by tracking entity position changes.
/// </summary>
/// <remarks>
/// <para>
/// This system leverages the World's change tracking to detect which entities
/// have moved since the last frame, updating only those entities in the spatial
/// index. This provides O(dirty_entities) update cost instead of O(all_entities).
/// </para>
/// <para>
/// Only entities with both <see cref="Transform3D"/> and <see cref="SpatialIndexed"/>
/// components are indexed. Entities without <see cref="SpatialIndexed"/> are ignored.
/// </para>
/// <para>
/// This system runs in the LateUpdate phase after movement systems have updated
/// positions, ensuring the spatial index is up-to-date before the next frame's queries.
/// </para>
/// </remarks>
internal sealed class SpatialUpdateSystem : SystemBase
{
    private ISpatialPartitioner? partitioner;
    private EventSubscription? spatialIndexedAddedSubscription;
    private EventSubscription? spatialIndexedRemovedSubscription;
    private EventSubscription? entityDestroyedSubscription;

    protected override void OnInitialize()
    {
        // Get the spatial API extension
        if (!World.TryGetExtension<SpatialQueryApi>(out var spatialApi))
        {
            throw new InvalidOperationException("SpatialUpdateSystem requires SpatialQueryApi extension");
        }

        partitioner = spatialApi!.Partitioner;

        // Enable automatic tracking of Transform3D changes
        World.EnableAutoTracking<Transform3D>();

        // Subscribe to SpatialIndexed tag additions - index entities immediately when tagged
        spatialIndexedAddedSubscription = World.OnComponentAdded<SpatialIndexed>((entity, _) =>
        {
            if (World.Has<Transform3D>(entity))
            {
                IndexEntity(entity);
            }
        });

        // Subscribe to SpatialIndexed tag removals - remove from index immediately
        spatialIndexedRemovedSubscription = World.OnComponentRemoved<SpatialIndexed>(entity =>
        {
            partitioner?.Remove(entity);
        });

        // Subscribe to entity destroyed events - remove from index when entity is despawned
        entityDestroyedSubscription = World.OnEntityDestroyed(entity =>
        {
            // Only remove if it was spatially indexed
            if (World.Has<SpatialIndexed>(entity))
            {
                partitioner?.Remove(entity);
            }
        });

        // Initial indexing pass - index all existing entities with SpatialIndexed + Transform3D
        foreach (var entity in World.Query<Transform3D, SpatialIndexed>())
        {
            IndexEntity(entity);
        }
    }

    public override void Update(float deltaTime)
    {
        if (partitioner is null)
        {
            return;
        }

        // Get all entities with modified Transform3D components
        foreach (var entity in World.GetDirtyEntities<Transform3D>())
        {
            // Only process entities that are spatially indexed
            if (!World.Has<SpatialIndexed>(entity))
            {
                continue;
            }

            // Entity has moved - update its position in the spatial index
            IndexEntity(entity);
        }

        // Clear dirty flags after processing
        World.ClearDirtyFlags<Transform3D>();
    }

    private void IndexEntity(Entity entity)
    {
        if (partitioner is null || !World.IsAlive(entity))
        {
            return;
        }

        ref readonly var transform = ref World.Get<Transform3D>(entity);

        if (World.Has<SpatialBounds>(entity))
        {
            // Entity has bounds - use AABB-based indexing
            ref readonly var bounds = ref World.Get<SpatialBounds>(entity);
            partitioner.Update(entity, transform.Position, bounds);
        }
        else
        {
            // Entity is a point - use point-based indexing
            partitioner.Update(entity, transform.Position);
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Unsubscribe from events
            spatialIndexedAddedSubscription?.Dispose();
            spatialIndexedRemovedSubscription?.Dispose();
            entityDestroyedSubscription?.Dispose();

            // Disable auto-tracking when system is disposed
            World.DisableAutoTracking<Transform3D>();
        }
        base.Dispose(disposing);
    }
}
