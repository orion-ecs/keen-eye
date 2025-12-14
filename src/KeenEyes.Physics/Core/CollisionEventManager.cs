using System.Numerics;
using System.Runtime.CompilerServices;
using KeenEyes.Physics.Components;
using KeenEyes.Physics.Events;

namespace KeenEyes.Physics.Core;

/// <summary>
/// Represents a unique pair of colliding entities.
/// </summary>
internal readonly struct CollisionPairKey : IEquatable<CollisionPairKey>
{
    public readonly Entity EntityA;
    public readonly Entity EntityB;

    public CollisionPairKey(Entity a, Entity b)
    {
        // Always store entities in consistent order (lower ID first)
        // to ensure (A,B) and (B,A) hash to the same key
        if (a.Id <= b.Id)
        {
            EntityA = a;
            EntityB = b;
        }
        else
        {
            EntityA = b;
            EntityB = a;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(CollisionPairKey other)
        => EntityA.Equals(other.EntityA) && EntityB.Equals(other.EntityB);

    public override bool Equals(object? obj)
        => obj is CollisionPairKey other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(EntityA, EntityB);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool InvolvesEntity(Entity entity)
        => EntityA.Equals(entity) || EntityB.Equals(entity);
}

/// <summary>
/// Data about an active collision, used for tracking collision state.
/// </summary>
internal struct CollisionData
{
    public Vector3 ContactNormal;
    public Vector3 ContactPoint;
    public float PenetrationDepth;
    public bool IsTrigger;
}

/// <summary>
/// Manages collision event tracking and publishing for the physics system.
/// </summary>
/// <remarks>
/// <para>
/// This manager tracks active collision pairs across physics frames to determine
/// when collisions start, continue, and end. It uses a concurrent collection
/// to safely collect collision data from BepuPhysics narrow phase callbacks,
/// which may execute on multiple threads.
/// </para>
/// <para>
/// At the end of each physics step, call <see cref="PublishEvents"/> to:
/// </para>
/// <list type="bullet">
/// <item><description>Publish <see cref="CollisionStartedEvent"/> for new collisions</description></item>
/// <item><description>Publish <see cref="CollisionEvent"/> for all active collisions</description></item>
/// <item><description>Publish <see cref="CollisionEndedEvent"/> for collisions that ended</description></item>
/// </list>
/// <para>
/// This implementation is designed for zero allocations in hot paths (RecordCollision, PublishEvents).
/// </para>
/// </remarks>
internal sealed class CollisionEventManager(IWorld world)
{
    // Current frame's collision data (collected during narrow phase)
    // Using regular Dictionary + lock since we need to update existing values atomically
    private readonly Dictionary<CollisionPairKey, CollisionData> currentFrameCollisions = [];
    private readonly Lock currentFrameLock = new();

    // Previous frame's collision pairs (for detecting ended collisions)
    private readonly HashSet<CollisionPairKey> previousFramePairs = [];

    // Tracks trigger status for collision pairs (needed for CollisionEndedEvent)
    private readonly Dictionary<CollisionPairKey, bool> pairTriggerStatus = [];

    // Reusable list for entity removal (avoids allocation during RemoveEntity)
    private readonly List<CollisionPairKey> removalBuffer = new(16);

    /// <summary>
    /// Records a collision from the narrow phase callback.
    /// </summary>
    /// <remarks>
    /// This method is thread-safe and can be called from BepuPhysics worker threads.
    /// Zero allocations in this hot path.
    /// </remarks>
    /// <param name="entityA">The first entity in the collision.</param>
    /// <param name="entityB">The second entity in the collision.</param>
    /// <param name="contactNormal">The contact normal pointing from A to B.</param>
    /// <param name="contactPoint">The world-space contact point.</param>
    /// <param name="penetrationDepth">The penetration depth.</param>
    /// <param name="isTrigger">Whether this is a trigger collision.</param>
    public void RecordCollision(
        Entity entityA,
        Entity entityB,
        Vector3 contactNormal,
        Vector3 contactPoint,
        float penetrationDepth,
        bool isTrigger)
    {
        var key = new CollisionPairKey(entityA, entityB);

        // If the key ordering swapped the entities, flip the normal
        var normalToStore = key.EntityA.Equals(entityA) ? contactNormal : -contactNormal;

        var newData = new CollisionData
        {
            ContactNormal = normalToStore,
            ContactPoint = contactPoint,
            PenetrationDepth = penetrationDepth,
            IsTrigger = isTrigger
        };

        lock (currentFrameLock)
        {
            if (currentFrameCollisions.TryGetValue(key, out var existing))
            {
                // If multiple contacts exist, keep the deepest penetration
                if (penetrationDepth > existing.PenetrationDepth)
                {
                    newData.IsTrigger = isTrigger || existing.IsTrigger;
                    currentFrameCollisions[key] = newData;
                }
                else if (isTrigger && !existing.IsTrigger)
                {
                    // Preserve trigger flag even if this contact is shallower
                    existing.IsTrigger = true;
                    currentFrameCollisions[key] = existing;
                }
            }
            else
            {
                currentFrameCollisions[key] = newData;
            }
        }
    }

    /// <summary>
    /// Publishes all collision events for this frame and prepares for the next frame.
    /// </summary>
    /// <remarks>
    /// Call this method after the physics step completes. It will:
    /// <list type="bullet">
    /// <item><description>Publish <see cref="CollisionStartedEvent"/> for new collision pairs</description></item>
    /// <item><description>Publish <see cref="CollisionEvent"/> for all current collisions</description></item>
    /// <item><description>Publish <see cref="CollisionEndedEvent"/> for pairs no longer colliding</description></item>
    /// </list>
    /// This method must be called from the main thread. Zero allocations in steady state.
    /// </remarks>
    public void PublishEvents()
    {
        // Process current frame collisions
        foreach (var kvp in currentFrameCollisions)
        {
            var key = kvp.Key;
            var data = kvp.Value;
            bool isNewCollision = !previousFramePairs.Contains(key);

            // Publish collision started event for new pairs
            if (isNewCollision)
            {
                world.Send(new CollisionStartedEvent(
                    key.EntityA,
                    key.EntityB,
                    data.ContactNormal,
                    data.ContactPoint,
                    data.PenetrationDepth,
                    data.IsTrigger));

                // Track trigger status for when collision ends
                pairTriggerStatus[key] = data.IsTrigger;
            }

            // Publish collision event for all current collisions
            world.Send(new CollisionEvent(
                key.EntityA,
                key.EntityB,
                data.ContactNormal,
                data.ContactPoint,
                data.PenetrationDepth,
                data.IsTrigger));
        }

        // Find collisions that ended (were in previous frame but not current)
        foreach (var key in previousFramePairs)
        {
            if (!currentFrameCollisions.ContainsKey(key))
            {
                // Collision ended
                pairTriggerStatus.TryGetValue(key, out bool wasTrigger);

                world.Send(new CollisionEndedEvent(
                    key.EntityA,
                    key.EntityB,
                    wasTrigger));

                pairTriggerStatus.Remove(key);
            }
        }

        // Swap buffers: current becomes previous
        previousFramePairs.Clear();
        foreach (var key in currentFrameCollisions.Keys)
        {
            previousFramePairs.Add(key);
        }

        // Clear current frame for next step
        currentFrameCollisions.Clear();
    }

    /// <summary>
    /// Clears all collision tracking state.
    /// </summary>
    /// <remarks>
    /// Call this when resetting the physics world or removing entities in bulk.
    /// </remarks>
    public void Clear()
    {
        lock (currentFrameLock)
        {
            currentFrameCollisions.Clear();
        }
        previousFramePairs.Clear();
        pairTriggerStatus.Clear();
    }

    /// <summary>
    /// Removes tracking for a specific entity (when it's despawned).
    /// </summary>
    /// <param name="entity">The entity being removed.</param>
    public void RemoveEntity(Entity entity)
    {
        // Remove from current frame using reusable buffer
        lock (currentFrameLock)
        {
            removalBuffer.Clear();
            foreach (var key in currentFrameCollisions.Keys)
            {
                if (key.InvolvesEntity(entity))
                {
                    removalBuffer.Add(key);
                }
            }

            foreach (var key in removalBuffer)
            {
                currentFrameCollisions.Remove(key);
            }
        }

        // Remove from previous frame tracking
        previousFramePairs.RemoveWhere(k => k.InvolvesEntity(entity));

        // Remove from trigger status tracking using the same buffer
        removalBuffer.Clear();
        foreach (var key in pairTriggerStatus.Keys)
        {
            if (key.InvolvesEntity(entity))
            {
                removalBuffer.Add(key);
            }
        }

        foreach (var key in removalBuffer)
        {
            pairTriggerStatus.Remove(key);
        }
    }

    /// <summary>
    /// Gets the number of currently tracked collision pairs.
    /// </summary>
    public int ActiveCollisionCount
    {
        get
        {
            lock (currentFrameLock)
            {
                return currentFrameCollisions.Count;
            }
        }
    }
}
