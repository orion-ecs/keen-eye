using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Physics.Components;
using KeenEyes.Physics.Core;
using KeenEyes.Physics.Events;

namespace KeenEyes.Physics.Tests;

#region CollisionPairKey Tests

/// <summary>
/// Tests for the CollisionPairKey internal struct.
/// </summary>
public class CollisionPairKeyTests
{
    [Fact]
    public void Constructor_LowerIdFirst_PreservesOrder()
    {
        var entityA = new Entity(1, 0);
        var entityB = new Entity(2, 0);

        var key = new CollisionPairKey(entityA, entityB);

        Assert.Equal(entityA, key.EntityA);
        Assert.Equal(entityB, key.EntityB);
    }

    [Fact]
    public void Constructor_HigherIdFirst_SwapsOrder()
    {
        var entityA = new Entity(5, 0);
        var entityB = new Entity(2, 0);

        var key = new CollisionPairKey(entityA, entityB);

        // Should be swapped so lower ID is first
        Assert.Equal(entityB, key.EntityA);
        Assert.Equal(entityA, key.EntityB);
    }

    [Fact]
    public void Constructor_SameId_PreservesOrder()
    {
        var entityA = new Entity(3, 0);
        var entityB = new Entity(3, 1);

        var key = new CollisionPairKey(entityA, entityB);

        Assert.Equal(entityA, key.EntityA);
        Assert.Equal(entityB, key.EntityB);
    }

    [Fact]
    public void Equals_SameEntitiesSameOrder_ReturnsTrue()
    {
        var entityA = new Entity(1, 0);
        var entityB = new Entity(2, 0);

        var key1 = new CollisionPairKey(entityA, entityB);
        var key2 = new CollisionPairKey(entityA, entityB);

        Assert.True(key1.Equals(key2));
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void Equals_SameEntitiesReversedOrder_ReturnsTrue()
    {
        var entityA = new Entity(1, 0);
        var entityB = new Entity(2, 0);

        var key1 = new CollisionPairKey(entityA, entityB);
        var key2 = new CollisionPairKey(entityB, entityA);

        Assert.True(key1.Equals(key2));
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void Equals_DifferentEntities_ReturnsFalse()
    {
        var entityA = new Entity(1, 0);
        var entityB = new Entity(2, 0);
        var entityC = new Entity(3, 0);

        var key1 = new CollisionPairKey(entityA, entityB);
        var key2 = new CollisionPairKey(entityA, entityC);

        Assert.False(key1.Equals(key2));
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void Equals_Object_WorksCorrectly()
    {
        var entityA = new Entity(1, 0);
        var entityB = new Entity(2, 0);

        var key1 = new CollisionPairKey(entityA, entityB);
        object key2 = new CollisionPairKey(entityA, entityB);

        Assert.True(key1.Equals(key2));
    }

    [Fact]
    public void Equals_NullObject_ReturnsFalse()
    {
        var entityA = new Entity(1, 0);
        var entityB = new Entity(2, 0);

        var key = new CollisionPairKey(entityA, entityB);

        Assert.False(key.Equals(null));
    }

    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        var entityA = new Entity(1, 0);
        var entityB = new Entity(2, 0);

        var key = new CollisionPairKey(entityA, entityB);

        Assert.False(key.Equals("not a key"));
    }

    [Fact]
    public void GetHashCode_SameEntitiesSameOrder_SameHash()
    {
        var entityA = new Entity(1, 0);
        var entityB = new Entity(2, 0);

        var key1 = new CollisionPairKey(entityA, entityB);
        var key2 = new CollisionPairKey(entityA, entityB);

        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_SameEntitiesReversedOrder_SameHash()
    {
        var entityA = new Entity(1, 0);
        var entityB = new Entity(2, 0);

        var key1 = new CollisionPairKey(entityA, entityB);
        var key2 = new CollisionPairKey(entityB, entityA);

        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentEntities_DifferentHash()
    {
        var entityA = new Entity(1, 0);
        var entityB = new Entity(2, 0);
        var entityC = new Entity(3, 0);

        var key1 = new CollisionPairKey(entityA, entityB);
        var key2 = new CollisionPairKey(entityA, entityC);

        // Hash codes could theoretically collide, but for these values they shouldn't
        Assert.NotEqual(key1.GetHashCode(), key2.GetHashCode());
    }

    [Fact]
    public void CollisionPairKey_CanBeUsedAsDictionaryKey()
    {
        var entityA = new Entity(1, 0);
        var entityB = new Entity(2, 0);

        var dict = new Dictionary<CollisionPairKey, string>();
        var key = new CollisionPairKey(entityA, entityB);

        dict[key] = "test";

        // Should find with same key
        Assert.Equal("test", dict[key]);

        // Should find with reversed key
        var reversedKey = new CollisionPairKey(entityB, entityA);
        Assert.Equal("test", dict[reversedKey]);
    }
}

#endregion

#region CollisionEventManager Tests

/// <summary>
/// Tests for the CollisionEventManager internal class.
/// </summary>
public class CollisionEventManagerTests : IDisposable
{
    private readonly World world;
    private readonly CollisionEventManager manager;

    public CollisionEventManagerTests()
    {
        world = new World();
        manager = new CollisionEventManager(world);
    }

    public void Dispose()
    {
        world.Dispose();
    }

    [Fact]
    public void RecordCollision_SingleCollision_IncrementsActiveCount()
    {
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();

        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.1f, false);

        Assert.Equal(1, manager.ActiveCollisionCount);
    }

    [Fact]
    public void RecordCollision_SamePairTwice_CountsAsOne()
    {
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();

        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.1f, false);
        manager.RecordCollision(entityA, entityB, Vector3.UnitX, Vector3.One, 0.2f, false);

        Assert.Equal(1, manager.ActiveCollisionCount);
    }

    [Fact]
    public void RecordCollision_ReversedPair_CountsAsOne()
    {
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();

        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.1f, false);
        manager.RecordCollision(entityB, entityA, Vector3.UnitX, Vector3.One, 0.2f, false);

        Assert.Equal(1, manager.ActiveCollisionCount);
    }

    [Fact]
    public void RecordCollision_DifferentPairs_CountsSeparately()
    {
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();
        var entityC = world.Spawn().Build();

        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.1f, false);
        manager.RecordCollision(entityA, entityC, Vector3.UnitX, Vector3.One, 0.2f, false);

        Assert.Equal(2, manager.ActiveCollisionCount);
    }

    [Fact]
    public void RecordCollision_DeeperPenetration_UpdatesData()
    {
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();
        var collisionEvents = new List<CollisionEvent>();
        world.Subscribe<CollisionEvent>(e => collisionEvents.Add(e));

        // First collision with shallow penetration
        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.1f, false);
        // Second collision with deeper penetration should update
        manager.RecordCollision(entityA, entityB, Vector3.UnitX, Vector3.One, 0.5f, false);

        manager.PublishEvents();

        Assert.Single(collisionEvents);
        Assert.Equal(0.5f, collisionEvents[0].PenetrationDepth);
        Assert.Equal(Vector3.One, collisionEvents[0].ContactPoint);
    }

    [Fact]
    public void RecordCollision_ShallowerPenetration_KeepsOriginalData()
    {
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();
        var collisionEvents = new List<CollisionEvent>();
        world.Subscribe<CollisionEvent>(e => collisionEvents.Add(e));

        // First collision with deep penetration
        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.One, 0.5f, false);
        // Second collision with shallow penetration should not update
        manager.RecordCollision(entityA, entityB, Vector3.UnitX, Vector3.Zero, 0.1f, false);

        manager.PublishEvents();

        Assert.Single(collisionEvents);
        Assert.Equal(0.5f, collisionEvents[0].PenetrationDepth);
        Assert.Equal(Vector3.One, collisionEvents[0].ContactPoint);
    }

    [Fact]
    public void RecordCollision_TriggerCollision_PreservesTriggerFlag()
    {
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();
        var collisionEvents = new List<CollisionEvent>();
        world.Subscribe<CollisionEvent>(e => collisionEvents.Add(e));

        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.1f, true);
        manager.PublishEvents();

        Assert.Single(collisionEvents);
        Assert.True(collisionEvents[0].IsTrigger);
    }

    [Fact]
    public void RecordCollision_TriggerThenNonTrigger_PreservesTrigger()
    {
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();
        var collisionEvents = new List<CollisionEvent>();
        world.Subscribe<CollisionEvent>(e => collisionEvents.Add(e));

        // First with trigger, deeper penetration
        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.5f, true);
        // Second without trigger, shallower penetration (keeps original trigger flag)
        manager.RecordCollision(entityA, entityB, Vector3.UnitX, Vector3.One, 0.1f, false);

        manager.PublishEvents();

        Assert.Single(collisionEvents);
        Assert.True(collisionEvents[0].IsTrigger);
    }

    [Fact]
    public void PublishEvents_NewCollision_PublishesStartedAndCollision()
    {
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();
        var startedEvents = new List<CollisionStartedEvent>();
        var collisionEvents = new List<CollisionEvent>();

        world.Subscribe<CollisionStartedEvent>(e => startedEvents.Add(e));
        world.Subscribe<CollisionEvent>(e => collisionEvents.Add(e));

        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.1f, false);
        manager.PublishEvents();

        Assert.Single(startedEvents);
        Assert.Single(collisionEvents);
    }

    [Fact]
    public void PublishEvents_ContinuingCollision_OnlyPublishesCollision()
    {
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();
        var startedEvents = new List<CollisionStartedEvent>();
        var collisionEvents = new List<CollisionEvent>();

        world.Subscribe<CollisionStartedEvent>(e => startedEvents.Add(e));
        world.Subscribe<CollisionEvent>(e => collisionEvents.Add(e));

        // First frame
        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.1f, false);
        manager.PublishEvents();

        startedEvents.Clear();
        collisionEvents.Clear();

        // Second frame - same collision continues
        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.1f, false);
        manager.PublishEvents();

        Assert.Empty(startedEvents); // No new started event
        Assert.Single(collisionEvents); // Still publishes collision event
    }

    [Fact]
    public void PublishEvents_EndingCollision_PublishesEnded()
    {
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();
        var endedEvents = new List<CollisionEndedEvent>();

        world.Subscribe<CollisionEndedEvent>(e => endedEvents.Add(e));

        // First frame - collision occurs
        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.1f, false);
        manager.PublishEvents();

        // Second frame - no collision recorded
        manager.PublishEvents();

        Assert.Single(endedEvents);
    }

    [Fact]
    public void PublishEvents_TriggerCollisionEnds_WasTriggerIsTrue()
    {
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();
        var endedEvents = new List<CollisionEndedEvent>();

        world.Subscribe<CollisionEndedEvent>(e => endedEvents.Add(e));

        // Trigger collision
        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.1f, true);
        manager.PublishEvents();

        // Collision ends
        manager.PublishEvents();

        Assert.Single(endedEvents);
        Assert.True(endedEvents[0].WasTrigger);
    }

    [Fact]
    public void PublishEvents_NonTriggerCollisionEnds_WasTriggerIsFalse()
    {
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();
        var endedEvents = new List<CollisionEndedEvent>();

        world.Subscribe<CollisionEndedEvent>(e => endedEvents.Add(e));

        // Non-trigger collision
        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.1f, false);
        manager.PublishEvents();

        // Collision ends
        manager.PublishEvents();

        Assert.Single(endedEvents);
        Assert.False(endedEvents[0].WasTrigger);
    }

    [Fact]
    public void PublishEvents_ClearsCurrentFrameCollisions()
    {
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();

        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.1f, false);
        Assert.Equal(1, manager.ActiveCollisionCount);

        manager.PublishEvents();

        Assert.Equal(0, manager.ActiveCollisionCount);
    }

    [Fact]
    public void PublishEvents_MultipleCollisions_PublishesAllEvents()
    {
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();
        var entityC = world.Spawn().Build();
        var collisionEvents = new List<CollisionEvent>();

        world.Subscribe<CollisionEvent>(e => collisionEvents.Add(e));

        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.1f, false);
        manager.RecordCollision(entityA, entityC, Vector3.UnitX, Vector3.One, 0.2f, false);
        manager.RecordCollision(entityB, entityC, Vector3.UnitZ, Vector3.UnitY, 0.3f, false);
        manager.PublishEvents();

        Assert.Equal(3, collisionEvents.Count);
    }

    [Fact]
    public void PublishEvents_CollisionRestartsAfterEnding_PublishesStartedAgain()
    {
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();
        var startedEvents = new List<CollisionStartedEvent>();

        world.Subscribe<CollisionStartedEvent>(e => startedEvents.Add(e));

        // Frame 1: Collision starts
        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.1f, false);
        manager.PublishEvents();

        // Frame 2: No collision
        manager.PublishEvents();

        // Frame 3: Collision starts again
        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.1f, false);
        manager.PublishEvents();

        Assert.Equal(2, startedEvents.Count); // Started twice
    }

    [Fact]
    public void Clear_RemovesAllTrackingState()
    {
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();
        var endedEvents = new List<CollisionEndedEvent>();

        world.Subscribe<CollisionEndedEvent>(e => endedEvents.Add(e));

        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.1f, false);
        manager.PublishEvents();

        manager.Clear();

        // After clear, publishing should not trigger ended event
        manager.PublishEvents();

        Assert.Empty(endedEvents);
    }

    [Fact]
    public void Clear_ResetsActiveCollisionCount()
    {
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();

        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.1f, false);
        Assert.Equal(1, manager.ActiveCollisionCount);

        manager.Clear();

        Assert.Equal(0, manager.ActiveCollisionCount);
    }

    [Fact]
    public void RemoveEntity_RemovesFromCurrentFrame()
    {
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();
        var entityC = world.Spawn().Build();

        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.1f, false);
        manager.RecordCollision(entityA, entityC, Vector3.UnitX, Vector3.One, 0.2f, false);

        Assert.Equal(2, manager.ActiveCollisionCount);

        manager.RemoveEntity(entityA);

        Assert.Equal(0, manager.ActiveCollisionCount);
    }

    [Fact]
    public void RemoveEntity_RemovesFromPreviousFrameTracking()
    {
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();
        var endedEvents = new List<CollisionEndedEvent>();

        world.Subscribe<CollisionEndedEvent>(e => endedEvents.Add(e));

        // Collision in frame 1
        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.1f, false);
        manager.PublishEvents();

        // Remove entity before frame 2
        manager.RemoveEntity(entityA);

        // Frame 2 - should not publish ended event since entity was explicitly removed
        manager.PublishEvents();

        Assert.Empty(endedEvents);
    }

    [Fact]
    public void RemoveEntity_OnlyRemovesInvolvingThatEntity()
    {
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();
        var entityC = world.Spawn().Build();

        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.1f, false);
        manager.RecordCollision(entityB, entityC, Vector3.UnitX, Vector3.One, 0.2f, false);

        manager.RemoveEntity(entityA);

        Assert.Equal(1, manager.ActiveCollisionCount);
    }

    [Fact]
    public void PublishEvents_EntityOrderPreserved_NormalFromAToB()
    {
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();
        var collisionEvents = new List<CollisionEvent>();

        world.Subscribe<CollisionEvent>(e => collisionEvents.Add(e));

        // Record with A first (A has lower ID)
        var normal = new Vector3(1, 0, 0);
        manager.RecordCollision(entityA, entityB, normal, Vector3.Zero, 0.1f, false);
        manager.PublishEvents();

        Assert.Single(collisionEvents);
        Assert.Equal(normal, collisionEvents[0].ContactNormal);
    }

    [Fact]
    public void PublishEvents_EntityOrderReversed_NormalIsFlipped()
    {
        var entityA = world.Spawn().Build(); // Lower ID
        var entityB = world.Spawn().Build(); // Higher ID
        var collisionEvents = new List<CollisionEvent>();

        world.Subscribe<CollisionEvent>(e => collisionEvents.Add(e));

        // Record with B first (but A has lower ID, so key will swap and flip normal)
        var normal = new Vector3(1, 0, 0);
        manager.RecordCollision(entityB, entityA, normal, Vector3.Zero, 0.1f, false);
        manager.PublishEvents();

        Assert.Single(collisionEvents);
        Assert.Equal(-normal, collisionEvents[0].ContactNormal); // Normal should be flipped
    }

    [Fact]
    public void PublishEvents_FullCollisionLifecycle_CorrectEventSequence()
    {
        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();
        var startedEvents = new List<CollisionStartedEvent>();
        var collisionEvents = new List<CollisionEvent>();
        var endedEvents = new List<CollisionEndedEvent>();

        world.Subscribe<CollisionStartedEvent>(e => startedEvents.Add(e));
        world.Subscribe<CollisionEvent>(e => collisionEvents.Add(e));
        world.Subscribe<CollisionEndedEvent>(e => endedEvents.Add(e));

        // Frame 1: Collision starts
        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.1f, false);
        manager.PublishEvents();

        Assert.Single(startedEvents);
        Assert.Single(collisionEvents);
        Assert.Empty(endedEvents);

        // Frame 2: Collision continues
        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.1f, false);
        manager.PublishEvents();

        Assert.Single(startedEvents); // Still 1
        Assert.Equal(2, collisionEvents.Count); // Now 2
        Assert.Empty(endedEvents);

        // Frame 3: Collision continues
        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.1f, false);
        manager.PublishEvents();

        Assert.Single(startedEvents); // Still 1
        Assert.Equal(3, collisionEvents.Count); // Now 3
        Assert.Empty(endedEvents);

        // Frame 4: Collision ends
        manager.PublishEvents();

        Assert.Single(startedEvents); // Still 1
        Assert.Equal(3, collisionEvents.Count); // Still 3
        Assert.Single(endedEvents); // Now 1
    }
}

#endregion

/// <summary>
/// Tests for the collision event system.
/// </summary>
public class CollisionEventTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    #region CollisionFilter Tests

    [Fact]
    public void CollisionFilter_Default_CollideWithEverything()
    {
        var filter = CollisionFilter.Default;

        Assert.Equal(1u, filter.Layer);
        Assert.Equal(0xFFFFFFFF, filter.Mask);
        Assert.False(filter.IsTrigger);
    }

    [Fact]
    public void CollisionFilter_CanCollideWith_SameLayer_ReturnsTrue()
    {
        var filterA = new CollisionFilter(layer: 1, mask: 0xFFFFFFFF);
        var filterB = new CollisionFilter(layer: 1, mask: 0xFFFFFFFF);

        Assert.True(filterA.CanCollideWith(in filterB));
    }

    [Fact]
    public void CollisionFilter_CanCollideWith_DifferentLayerInMask_ReturnsTrue()
    {
        var filterA = new CollisionFilter(layer: 1, mask: 0b0011);
        var filterB = new CollisionFilter(layer: 2, mask: 0b0011);

        Assert.True(filterA.CanCollideWith(in filterB));
    }

    [Fact]
    public void CollisionFilter_CanCollideWith_LayerNotInMask_ReturnsFalse()
    {
        var filterA = new CollisionFilter(layer: 1, mask: 0b0001); // Only collide with layer 1
        var filterB = new CollisionFilter(layer: 2, mask: 0b0011); // Layer 2

        Assert.False(filterA.CanCollideWith(in filterB));
    }

    [Fact]
    public void CollisionFilter_CanCollideWith_AsymmetricMasks_ReturnsFalse()
    {
        var filterA = new CollisionFilter(layer: 1, mask: 0b0011); // Wants to collide with 1,2
        var filterB = new CollisionFilter(layer: 2, mask: 0b0010); // Only wants to collide with 2

        // A wants to collide with B (layer 2 & mask 0011 = 2)
        // But B doesn't want to collide with A (layer 1 & mask 0010 = 0)
        Assert.False(filterA.CanCollideWith(in filterB));
    }

    [Fact]
    public void CollisionFilter_Trigger_CreatesTriggerFilter()
    {
        var trigger = CollisionFilter.Trigger(layer: 4, mask: 0xFF);

        Assert.Equal(4u, trigger.Layer);
        Assert.Equal(0xFFu, trigger.Mask);
        Assert.True(trigger.IsTrigger);
    }

    [Fact]
    public void CollisionFilter_IsTriggerCollision_WhenEitherIsTrigger_ReturnsTrue()
    {
        var normal = new CollisionFilter(layer: 1, mask: 0xFF);
        var trigger = CollisionFilter.Trigger(layer: 1, mask: 0xFF);

        Assert.True(normal.IsTriggerCollision(in trigger));
        Assert.True(trigger.IsTriggerCollision(in normal));
    }

    [Fact]
    public void CollisionFilter_IsTriggerCollision_WhenNeitherIsTrigger_ReturnsFalse()
    {
        var filterA = new CollisionFilter(layer: 1, mask: 0xFF);
        var filterB = new CollisionFilter(layer: 2, mask: 0xFF);

        Assert.False(filterA.IsTriggerCollision(in filterB));
    }

    [Fact]
    public void CollisionFilter_IsTriggerCollision_WhenBothAreTrigger_ReturnsTrue()
    {
        var triggerA = CollisionFilter.Trigger(layer: 1, mask: 0xFF);
        var triggerB = CollisionFilter.Trigger(layer: 2, mask: 0xFF);

        Assert.True(triggerA.IsTriggerCollision(in triggerB));
    }

    [Fact]
    public void CollisionFilter_ParameterlessConstructor_SetsDefaults()
    {
        var filter = new CollisionFilter();

        Assert.Equal(1u, filter.Layer);
        Assert.Equal(0xFFFFFFFF, filter.Mask);
        Assert.False(filter.IsTrigger);
    }

    [Fact]
    public void CollisionFilter_CanCollideWith_ZeroLayer_ReturnsFalse()
    {
        var filterA = new CollisionFilter(layer: 0, mask: 0xFFFFFFFF);
        var filterB = new CollisionFilter(layer: 1, mask: 0xFFFFFFFF);

        Assert.False(filterA.CanCollideWith(in filterB));
    }

    [Fact]
    public void CollisionFilter_CanCollideWith_ZeroMask_ReturnsFalse()
    {
        var filterA = new CollisionFilter(layer: 1, mask: 0);
        var filterB = new CollisionFilter(layer: 1, mask: 0xFFFFFFFF);

        Assert.False(filterA.CanCollideWith(in filterB));
    }

    [Fact]
    public void CollisionFilter_CanCollideWith_BothZeroLayer_ReturnsFalse()
    {
        var filterA = new CollisionFilter(layer: 0, mask: 0xFFFFFFFF);
        var filterB = new CollisionFilter(layer: 0, mask: 0xFFFFFFFF);

        Assert.False(filterA.CanCollideWith(in filterB));
    }

    [Fact]
    public void CollisionFilter_CanCollideWith_MultipleLayers_ReturnsTrue()
    {
        // Entity in multiple layers
        var filterA = new CollisionFilter(layer: 0b0101, mask: 0xFFFFFFFF); // Layers 1 and 3
        var filterB = new CollisionFilter(layer: 0b0100, mask: 0xFFFFFFFF); // Layer 3 only

        Assert.True(filterA.CanCollideWith(in filterB));
    }

    [Fact]
    public void CollisionFilter_CanCollideWith_MultipleLayers_NoOverlap_ReturnsFalse()
    {
        var filterA = new CollisionFilter(layer: 0b0101, mask: 0b0101); // Layers 1 and 3, wants 1 and 3
        var filterB = new CollisionFilter(layer: 0b1010, mask: 0b1010); // Layers 2 and 4, wants 2 and 4

        Assert.False(filterA.CanCollideWith(in filterB));
    }

    [Fact]
    public void CollisionFilter_CanCollideWith_HighBitLayers_ReturnsTrue()
    {
        var filterA = new CollisionFilter(layer: 0x80000000, mask: 0x80000000); // Highest bit
        var filterB = new CollisionFilter(layer: 0x80000000, mask: 0x80000000);

        Assert.True(filterA.CanCollideWith(in filterB));
    }

    [Fact]
    public void CollisionFilter_CanCollideWith_AllBitsSet_ReturnsTrue()
    {
        var filterA = new CollisionFilter(layer: 0xFFFFFFFF, mask: 0xFFFFFFFF);
        var filterB = new CollisionFilter(layer: 0xFFFFFFFF, mask: 0xFFFFFFFF);

        Assert.True(filterA.CanCollideWith(in filterB));
    }

    [Fact]
    public void CollisionFilter_Trigger_DefaultParameters_SetsCorrectValues()
    {
        var trigger = CollisionFilter.Trigger();

        Assert.Equal(1u, trigger.Layer);
        Assert.Equal(0xFFFFFFFF, trigger.Mask);
        Assert.True(trigger.IsTrigger);
    }

    [Fact]
    public void CollisionFilter_Constructor_WithIsTrigger_SetsAllValues()
    {
        var filter = new CollisionFilter(layer: 4, mask: 0xFF, isTrigger: true);

        Assert.Equal(4u, filter.Layer);
        Assert.Equal(0xFFu, filter.Mask);
        Assert.True(filter.IsTrigger);
    }

    [Fact]
    public void CollisionFilter_CanCollideWith_IsSymmetric()
    {
        var filterA = new CollisionFilter(layer: 1, mask: 0b0011);
        var filterB = new CollisionFilter(layer: 2, mask: 0b0011);

        // Both directions should return the same result
        Assert.Equal(filterA.CanCollideWith(in filterB), filterB.CanCollideWith(in filterA));
    }

    [Fact]
    public void CollisionFilter_CanCollideWith_SelfCollision_ReturnsTrue()
    {
        var filter = new CollisionFilter(layer: 1, mask: 0xFFFFFFFF);

        // Entity should be able to collide with an identical filter
        Assert.True(filter.CanCollideWith(in filter));
    }

    [Fact]
    public void CollisionFilter_CanCollideWith_PlayerEnemyBulletScenario()
    {
        // Define collision layers
        const uint PlayerLayer = 1 << 0;  // 0x00000001
        const uint EnemyLayer = 1 << 1;   // 0x00000002
        const uint BulletLayer = 1 << 2;  // 0x00000004
        const uint WallLayer = 1 << 3;    // 0x00000008

        // Player collides with enemies and walls
        var playerFilter = new CollisionFilter(PlayerLayer, EnemyLayer | WallLayer);

        // Enemy collides with player, enemy bullets, and walls
        var enemyFilter = new CollisionFilter(EnemyLayer, PlayerLayer | BulletLayer | WallLayer);

        // Player bullet collides with enemies and walls
        var playerBulletFilter = new CollisionFilter(BulletLayer, EnemyLayer | WallLayer);

        // Wall collides with everything
        var wallFilter = new CollisionFilter(WallLayer, PlayerLayer | EnemyLayer | BulletLayer | WallLayer);

        // Player vs Enemy: Player wants enemy (yes), Enemy wants player (yes)
        Assert.True(playerFilter.CanCollideWith(in enemyFilter));

        // Player vs Wall: Player wants wall (yes), Wall wants player (yes)
        Assert.True(playerFilter.CanCollideWith(in wallFilter));

        // Player vs PlayerBullet: Player doesn't want bullets (no)
        Assert.False(playerFilter.CanCollideWith(in playerBulletFilter));

        // PlayerBullet vs Enemy: Bullet wants enemy (yes), Enemy wants bullets (yes)
        Assert.True(playerBulletFilter.CanCollideWith(in enemyFilter));

        // PlayerBullet vs Wall: Bullet wants wall (yes), Wall wants bullet (yes)
        Assert.True(playerBulletFilter.CanCollideWith(in wallFilter));
    }

    #endregion

    #region Collision Event Integration Tests

    [Fact]
    public void CollisionEvent_SubscriptionWorks()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var collisionEvents = new List<CollisionEvent>();
        var subscription = world.Subscribe<CollisionEvent>(e => collisionEvents.Add(e));

        // Verify subscription was created
        Assert.NotNull(subscription);

        // Manually send a collision event to verify messaging works
        var entity1 = world.Spawn().Build();
        var entity2 = world.Spawn().Build();
        world.Send(new CollisionEvent(entity1, entity2, Vector3.UnitY, Vector3.Zero, 0.1f, false));

        // Should have received the event
        Assert.Single(collisionEvents);
        Assert.Equal(entity1, collisionEvents[0].EntityA);
        Assert.Equal(entity2, collisionEvents[0].EntityB);

        subscription.Dispose();
    }

    [Fact]
    public void CollisionStartedEvent_SubscriptionWorks()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var startedEvents = new List<CollisionStartedEvent>();
        var subscription = world.Subscribe<CollisionStartedEvent>(e => startedEvents.Add(e));

        // Manually send a started event to verify messaging works
        var entity1 = world.Spawn().Build();
        var entity2 = world.Spawn().Build();
        world.Send(new CollisionStartedEvent(entity1, entity2, Vector3.UnitY, Vector3.Zero, 0.1f, false));

        // Should have received the event
        Assert.Single(startedEvents);

        subscription.Dispose();
    }

    [Fact]
    public void CollisionEndedEvent_SubscriptionWorks()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var endedEvents = new List<CollisionEndedEvent>();
        var subscription = world.Subscribe<CollisionEndedEvent>(e => endedEvents.Add(e));

        // Manually send an ended event to verify messaging works
        var entity1 = world.Spawn().Build();
        var entity2 = world.Spawn().Build();
        world.Send(new CollisionEndedEvent(entity1, entity2, false));

        // Should have received the event
        Assert.Single(endedEvents);

        subscription.Dispose();
    }

    [Fact]
    public void CollisionFilter_BodiesCreatedWithFilter()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var physics = world.GetExtension<PhysicsWorld>();

        const uint PlayerLayer = 1;
        const uint EnemyLayer = 2;

        // Create entities with collision filters
        var player = world.Spawn()
            .With(new Transform3D(new Vector3(0, 0, 0), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(0.5f))
            .With(RigidBody.Dynamic(1f))
            .With(new CollisionFilter(PlayerLayer, PlayerLayer | EnemyLayer))
            .Build();

        var enemy = world.Spawn()
            .With(new Transform3D(new Vector3(5, 0, 0), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(0.5f))
            .With(RigidBody.Dynamic(1f))
            .With(new CollisionFilter(EnemyLayer, 0xFFFFFFFF))
            .Build();

        // Verify bodies were created
        Assert.True(physics.HasPhysicsBody(player));
        Assert.True(physics.HasPhysicsBody(enemy));

        // Verify collision filter components are accessible
        Assert.True(world.Has<CollisionFilter>(player));
        Assert.True(world.Has<CollisionFilter>(enemy));

        var playerFilter = world.Get<CollisionFilter>(player);
        var enemyFilter = world.Get<CollisionFilter>(enemy);

        Assert.Equal(PlayerLayer, playerFilter.Layer);
        Assert.Equal(EnemyLayer, enemyFilter.Layer);
    }

    [Fact]
    public void CollisionFilter_TriggerComponentCreated()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var physics = world.GetExtension<PhysicsWorld>();

        // Create a trigger entity
        var trigger = world.Spawn()
            .With(new Transform3D(new Vector3(0, 0, 0), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(2f, 2f, 2f))
            .With(RigidBody.Static())
            .With(CollisionFilter.Trigger())
            .Build();

        // Verify body was created
        Assert.True(physics.HasPhysicsBody(trigger));

        // Verify trigger filter
        Assert.True(world.Has<CollisionFilter>(trigger));
        var filter = world.Get<CollisionFilter>(trigger);
        Assert.True(filter.IsTrigger);
    }

    [Fact]
    public void PhysicsWorld_BodiesFallWithGravity()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin(new PhysicsConfig
        {
            FixedTimestep = 1f / 60f,
            Gravity = new Vector3(0, -10f, 0)
        }));

        var physics = world.GetExtension<PhysicsWorld>();

        // Create a falling ball
        var ball = world.Spawn()
            .With(new Transform3D(new Vector3(0, 10, 0), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(0.5f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var initialY = world.Get<Transform3D>(ball).Position.Y;

        // Run physics
        for (int i = 0; i < 60; i++)
        {
            physics.Step(1f / 60f);
        }

        var finalY = world.Get<Transform3D>(ball).Position.Y;

        // Ball should have fallen
        Assert.True(finalY < initialY, $"Ball should have fallen: initial Y={initialY}, final Y={finalY}");
    }

    #endregion

    #region Entity Cleanup Tests

    [Fact]
    public void DespawnEntity_RemovesPhysicsBody()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());

        var physics = world.GetExtension<PhysicsWorld>();

        // Create entity with physics
        var ball = world.Spawn()
            .With(new Transform3D(new Vector3(0, 0, 0), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(0.5f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        Assert.True(physics.HasPhysicsBody(ball));
        Assert.Equal(1, physics.BodyCount);

        // Despawn entity
        world.Despawn(ball);

        // Physics body should be removed
        Assert.Equal(0, physics.BodyCount);
    }

    #endregion

    #region Event Data Tests

    [Fact]
    public void CollisionEvent_RecordStructHasCorrectData()
    {
        // Test that CollisionEvent record struct has correct fields
        var entityA = new Entity(1, 0);
        var entityB = new Entity(2, 0);
        var normal = Vector3.UnitY;
        var point = new Vector3(1, 2, 3);
        float depth = 0.5f;
        bool isTrigger = true;

        var evt = new CollisionEvent(entityA, entityB, normal, point, depth, isTrigger);

        Assert.Equal(entityA, evt.EntityA);
        Assert.Equal(entityB, evt.EntityB);
        Assert.Equal(normal, evt.ContactNormal);
        Assert.Equal(point, evt.ContactPoint);
        Assert.Equal(depth, evt.PenetrationDepth);
        Assert.True(evt.IsTrigger);
    }

    [Fact]
    public void CollisionStartedEvent_RecordStructHasCorrectData()
    {
        var entityA = new Entity(1, 0);
        var entityB = new Entity(2, 0);
        var normal = Vector3.UnitX;
        var point = Vector3.Zero;
        float depth = 0.1f;
        bool isTrigger = false;

        var evt = new CollisionStartedEvent(entityA, entityB, normal, point, depth, isTrigger);

        Assert.Equal(entityA, evt.EntityA);
        Assert.Equal(entityB, evt.EntityB);
        Assert.Equal(normal, evt.ContactNormal);
        Assert.Equal(point, evt.ContactPoint);
        Assert.Equal(depth, evt.PenetrationDepth);
        Assert.False(evt.IsTrigger);
    }

    [Fact]
    public void CollisionEndedEvent_RecordStructHasCorrectData()
    {
        var entityA = new Entity(1, 0);
        var entityB = new Entity(2, 0);
        bool wasTrigger = true;

        var evt = new CollisionEndedEvent(entityA, entityB, wasTrigger);

        Assert.Equal(entityA, evt.EntityA);
        Assert.Equal(entityB, evt.EntityB);
        Assert.True(evt.WasTrigger);
    }

    #endregion
}
