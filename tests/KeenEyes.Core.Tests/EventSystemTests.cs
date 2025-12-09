using KeenEyes.Events;

namespace KeenEyes.Tests;

/// <summary>
/// Tests for the event system including component lifecycle events,
/// entity lifecycle events, and the generic event bus.
/// </summary>
public class EventSystemTests
{
    #region Component Added Events

    [Fact]
    public void OnComponentAdded_FiresWhenComponentAddedViaAdd()
    {
        using var world = new World();

        var firedEvents = new List<(Entity Entity, TestPosition Component)>();

        var subscription = world.OnComponentAdded<TestPosition>((entity, component) =>
        {
            firedEvents.Add((entity, component));
        });

        var entity = world.Spawn().Build();
        world.Add(entity, new TestPosition { X = 10f, Y = 20f });

        Assert.Single(firedEvents);
        Assert.Equal(entity, firedEvents[0].Entity);
        Assert.Equal(10f, firedEvents[0].Component.X);
        Assert.Equal(20f, firedEvents[0].Component.Y);

        subscription.Dispose();
    }

    [Fact]
    public void OnComponentAdded_FiresForMultipleEntities()
    {
        using var world = new World();

        var firedEvents = new List<Entity>();

        using var subscription = world.OnComponentAdded<TestPosition>((entity, _) =>
        {
            firedEvents.Add(entity);
        });

        var entity1 = world.Spawn().Build();
        var entity2 = world.Spawn().Build();

        world.Add(entity1, new TestPosition { X = 1f, Y = 1f });
        world.Add(entity2, new TestPosition { X = 2f, Y = 2f });

        Assert.Equal(2, firedEvents.Count);
        Assert.Contains(entity1, firedEvents);
        Assert.Contains(entity2, firedEvents);
    }

    [Fact]
    public void OnComponentAdded_OnlyFiresForSpecificComponentType()
    {
        using var world = new World();

        var positionEvents = new List<Entity>();
        var velocityEvents = new List<Entity>();

        using var posSub = world.OnComponentAdded<TestPosition>((entity, _) =>
        {
            positionEvents.Add(entity);
        });

        using var velSub = world.OnComponentAdded<TestVelocity>((entity, _) =>
        {
            velocityEvents.Add(entity);
        });

        var entity = world.Spawn().Build();
        world.Add(entity, new TestPosition { X = 1f, Y = 1f });

        Assert.Single(positionEvents);
        Assert.Empty(velocityEvents);
    }

    [Fact]
    public void OnComponentAdded_DisposePreventsSubsequentEvents()
    {
        using var world = new World();

        var eventCount = 0;

        var subscription = world.OnComponentAdded<TestPosition>((_, _) =>
        {
            eventCount++;
        });

        var entity1 = world.Spawn().Build();
        world.Add(entity1, new TestPosition { X = 1f, Y = 1f });

        Assert.Equal(1, eventCount);

        subscription.Dispose();

        var entity2 = world.Spawn().Build();
        world.Add(entity2, new TestPosition { X = 2f, Y = 2f });

        Assert.Equal(1, eventCount); // Should not have increased
    }

    [Fact]
    public void OnComponentAdded_MultipleHandlersReceiveEvents()
    {
        using var world = new World();

        var handler1Count = 0;
        var handler2Count = 0;

        using var sub1 = world.OnComponentAdded<TestPosition>((_, _) => handler1Count++);
        using var sub2 = world.OnComponentAdded<TestPosition>((_, _) => handler2Count++);

        var entity = world.Spawn().Build();
        world.Add(entity, new TestPosition { X = 1f, Y = 1f });

        Assert.Equal(1, handler1Count);
        Assert.Equal(1, handler2Count);
    }

    #endregion

    #region Component Removed Events

    [Fact]
    public void OnComponentRemoved_FiresWhenComponentRemoved()
    {
        using var world = new World();

        var removedEntities = new List<Entity>();

        using var subscription = world.OnComponentRemoved<TestPosition>(entity =>
        {
            removedEntities.Add(entity);
        });

        var entity = world.Spawn().With(new TestPosition { X = 1f, Y = 1f }).Build();

        world.Remove<TestPosition>(entity);

        Assert.Single(removedEntities);
        Assert.Equal(entity, removedEntities[0]);
    }

    [Fact]
    public void OnComponentRemoved_DoesNotFireIfComponentNotPresent()
    {
        using var world = new World();

        var eventCount = 0;

        using var subscription = world.OnComponentRemoved<TestPosition>(_ =>
        {
            eventCount++;
        });

        var entity = world.Spawn().Build();

        // Entity doesn't have TestPosition, so Remove should return false and no event
        var removed = world.Remove<TestPosition>(entity);

        Assert.False(removed);
        Assert.Equal(0, eventCount);
    }

    [Fact]
    public void OnComponentRemoved_OnlyFiresForSpecificComponentType()
    {
        using var world = new World();

        var positionRemoveCount = 0;
        var velocityRemoveCount = 0;

        using var posSub = world.OnComponentRemoved<TestPosition>(_ => positionRemoveCount++);
        using var velSub = world.OnComponentRemoved<TestVelocity>(_ => velocityRemoveCount++);

        var entity = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 2f, Y = 2f })
            .Build();

        world.Remove<TestPosition>(entity);

        Assert.Equal(1, positionRemoveCount);
        Assert.Equal(0, velocityRemoveCount);
    }

    [Fact]
    public void OnComponentRemoved_DisposePreventsSubsequentEvents()
    {
        using var world = new World();

        var eventCount = 0;

        var subscription = world.OnComponentRemoved<TestPosition>(_ => eventCount++);

        var entity1 = world.Spawn().With(new TestPosition { X = 1f, Y = 1f }).Build();
        world.Remove<TestPosition>(entity1);

        Assert.Equal(1, eventCount);

        subscription.Dispose();

        var entity2 = world.Spawn().With(new TestPosition { X = 2f, Y = 2f }).Build();
        world.Remove<TestPosition>(entity2);

        Assert.Equal(1, eventCount); // Should not have increased
    }

    #endregion

    #region Component Changed Events

    [Fact]
    public void OnComponentChanged_FiresWhenComponentSetViaSet()
    {
        using var world = new World();

        var changes = new List<(Entity Entity, TestPosition OldValue, TestPosition NewValue)>();

        using var subscription = world.OnComponentChanged<TestPosition>((entity, oldVal, newVal) =>
        {
            changes.Add((entity, oldVal, newVal));
        });

        var entity = world.Spawn().With(new TestPosition { X = 1f, Y = 2f }).Build();

        world.Set(entity, new TestPosition { X = 10f, Y = 20f });

        Assert.Single(changes);
        Assert.Equal(entity, changes[0].Entity);
        Assert.Equal(1f, changes[0].OldValue.X);
        Assert.Equal(2f, changes[0].OldValue.Y);
        Assert.Equal(10f, changes[0].NewValue.X);
        Assert.Equal(20f, changes[0].NewValue.Y);
    }

    [Fact]
    public void OnComponentChanged_FiresMultipleTimesForMultipleSets()
    {
        using var world = new World();

        var changeCount = 0;

        using var subscription = world.OnComponentChanged<TestPosition>((_, _, _) =>
        {
            changeCount++;
        });

        var entity = world.Spawn().With(new TestPosition { X = 0f, Y = 0f }).Build();

        world.Set(entity, new TestPosition { X = 1f, Y = 1f });
        world.Set(entity, new TestPosition { X = 2f, Y = 2f });
        world.Set(entity, new TestPosition { X = 3f, Y = 3f });

        Assert.Equal(3, changeCount);
    }

    [Fact]
    public void OnComponentChanged_DoesNotFireForAdd()
    {
        using var world = new World();

        var changeCount = 0;

        using var subscription = world.OnComponentChanged<TestPosition>((_, _, _) =>
        {
            changeCount++;
        });

        var entity = world.Spawn().Build();
        world.Add(entity, new TestPosition { X = 1f, Y = 1f });

        Assert.Equal(0, changeCount); // Add should not trigger change event
    }

    [Fact]
    public void OnComponentChanged_DisposePreventsSubsequentEvents()
    {
        using var world = new World();

        var changeCount = 0;

        var subscription = world.OnComponentChanged<TestPosition>((_, _, _) =>
        {
            changeCount++;
        });

        var entity = world.Spawn().With(new TestPosition { X = 0f, Y = 0f }).Build();

        world.Set(entity, new TestPosition { X = 1f, Y = 1f });
        Assert.Equal(1, changeCount);

        subscription.Dispose();

        world.Set(entity, new TestPosition { X = 2f, Y = 2f });
        Assert.Equal(1, changeCount); // Should not have increased
    }

    #endregion

    #region Entity Created Events

    [Fact]
    public void OnEntityCreated_FiresWhenEntitySpawned()
    {
        using var world = new World();

        var createdEntities = new List<(Entity Entity, string? Name)>();

        using var subscription = world.OnEntityCreated((entity, name) =>
        {
            createdEntities.Add((entity, name));
        });

        var entity = world.Spawn().Build();

        Assert.Single(createdEntities);
        Assert.Equal(entity, createdEntities[0].Entity);
        Assert.Null(createdEntities[0].Name);
    }

    [Fact]
    public void OnEntityCreated_IncludesEntityName()
    {
        using var world = new World();

        var createdEntities = new List<(Entity Entity, string? Name)>();

        using var subscription = world.OnEntityCreated((entity, name) =>
        {
            createdEntities.Add((entity, name));
        });

        var entity = world.Spawn("Player").Build();

        Assert.Single(createdEntities);
        Assert.Equal(entity, createdEntities[0].Entity);
        Assert.Equal("Player", createdEntities[0].Name);
    }

    [Fact]
    public void OnEntityCreated_FiresForEachEntity()
    {
        using var world = new World();

        var createCount = 0;

        using var subscription = world.OnEntityCreated((_, _) =>
        {
            createCount++;
        });

        world.Spawn().Build();
        world.Spawn().Build();
        world.Spawn().Build();

        Assert.Equal(3, createCount);
    }

    [Fact]
    public void OnEntityCreated_DisposePreventsSubsequentEvents()
    {
        using var world = new World();

        var createCount = 0;

        var subscription = world.OnEntityCreated((_, _) =>
        {
            createCount++;
        });

        world.Spawn().Build();
        Assert.Equal(1, createCount);

        subscription.Dispose();

        world.Spawn().Build();
        Assert.Equal(1, createCount); // Should not have increased
    }

    [Fact]
    public void OnEntityCreated_EntityHasComponentsWhenHandlerCalled()
    {
        using var world = new World();

        var hadPosition = false;
        var positionValue = default(TestPosition);

        using var subscription = world.OnEntityCreated((entity, _) =>
        {
            hadPosition = world.Has<TestPosition>(entity);
            if (hadPosition)
            {
                positionValue = world.Get<TestPosition>(entity);
            }
        });

        world.Spawn().With(new TestPosition { X = 42f, Y = 24f }).Build();

        Assert.True(hadPosition);
        Assert.Equal(42f, positionValue.X);
        Assert.Equal(24f, positionValue.Y);
    }

    #endregion

    #region Entity Destroyed Events

    [Fact]
    public void OnEntityDestroyed_FiresWhenEntityDespawned()
    {
        using var world = new World();

        var destroyedEntities = new List<Entity>();

        using var subscription = world.OnEntityDestroyed(entity =>
        {
            destroyedEntities.Add(entity);
        });

        var entity = world.Spawn().Build();
        world.Despawn(entity);

        Assert.Single(destroyedEntities);
        Assert.Equal(entity, destroyedEntities[0]);
    }

    [Fact]
    public void OnEntityDestroyed_EntityIsStillAliveWhenHandlerCalled()
    {
        using var world = new World();

        var wasAlive = false;

        using var subscription = world.OnEntityDestroyed(entity =>
        {
            wasAlive = world.IsAlive(entity);
        });

        var entity = world.Spawn().Build();
        world.Despawn(entity);

        Assert.True(wasAlive);
    }

    [Fact]
    public void OnEntityDestroyed_CanAccessComponentsDuringHandler()
    {
        using var world = new World();

        var hadPosition = false;
        var positionValue = default(TestPosition);

        using var subscription = world.OnEntityDestroyed(entity =>
        {
            hadPosition = world.Has<TestPosition>(entity);
            if (hadPosition)
            {
                positionValue = world.Get<TestPosition>(entity);
            }
        });

        var entity = world.Spawn().With(new TestPosition { X = 99f, Y = 88f }).Build();
        world.Despawn(entity);

        Assert.True(hadPosition);
        Assert.Equal(99f, positionValue.X);
        Assert.Equal(88f, positionValue.Y);
    }

    [Fact]
    public void OnEntityDestroyed_FiresForEachDespawn()
    {
        using var world = new World();

        var destroyCount = 0;

        using var subscription = world.OnEntityDestroyed(_ =>
        {
            destroyCount++;
        });

        var entity1 = world.Spawn().Build();
        var entity2 = world.Spawn().Build();
        var entity3 = world.Spawn().Build();

        world.Despawn(entity1);
        world.Despawn(entity2);

        Assert.Equal(2, destroyCount);
    }

    [Fact]
    public void OnEntityDestroyed_DisposePreventsSubsequentEvents()
    {
        using var world = new World();

        var destroyCount = 0;

        var subscription = world.OnEntityDestroyed(_ =>
        {
            destroyCount++;
        });

        var entity1 = world.Spawn().Build();
        world.Despawn(entity1);
        Assert.Equal(1, destroyCount);

        subscription.Dispose();

        var entity2 = world.Spawn().Build();
        world.Despawn(entity2);
        Assert.Equal(1, destroyCount); // Should not have increased
    }

    [Fact]
    public void OnEntityDestroyed_FiresForDespawnRecursive()
    {
        using var world = new World();

        var destroyedEntities = new List<Entity>();

        using var subscription = world.OnEntityDestroyed(entity =>
        {
            destroyedEntities.Add(entity);
        });

        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();
        var grandchild = world.Spawn().Build();

        world.SetParent(child, parent);
        world.SetParent(grandchild, child);

        world.DespawnRecursive(parent);

        Assert.Equal(3, destroyedEntities.Count);
    }

    #endregion

    #region EventBus Tests

    [Fact]
    public void EventBus_SubscribeAndPublish()
    {
        using var world = new World();

        var receivedEvents = new List<CustomEvent>();

        using var subscription = world.Events.Subscribe<CustomEvent>(evt =>
        {
            receivedEvents.Add(evt);
        });

        world.Events.Publish(new CustomEvent { Value = 42 });

        Assert.Single(receivedEvents);
        Assert.Equal(42, receivedEvents[0].Value);
    }

    [Fact]
    public void EventBus_MultipleSubscribers()
    {
        using var world = new World();

        var sub1Count = 0;
        var sub2Count = 0;

        using var sub1 = world.Events.Subscribe<CustomEvent>(_ => sub1Count++);
        using var sub2 = world.Events.Subscribe<CustomEvent>(_ => sub2Count++);

        world.Events.Publish(new CustomEvent { Value = 1 });

        Assert.Equal(1, sub1Count);
        Assert.Equal(1, sub2Count);
    }

    [Fact]
    public void EventBus_UnsubscribePreventsEvents()
    {
        using var world = new World();

        var eventCount = 0;

        var subscription = world.Events.Subscribe<CustomEvent>(_ => eventCount++);

        world.Events.Publish(new CustomEvent { Value = 1 });
        Assert.Equal(1, eventCount);

        subscription.Dispose();

        world.Events.Publish(new CustomEvent { Value = 2 });
        Assert.Equal(1, eventCount); // Should not have increased
    }

    [Fact]
    public void EventBus_DifferentEventTypes()
    {
        using var world = new World();

        var customEventCount = 0;
        var otherEventCount = 0;

        using var sub1 = world.Events.Subscribe<CustomEvent>(_ => customEventCount++);
        using var sub2 = world.Events.Subscribe<OtherEvent>(_ => otherEventCount++);

        world.Events.Publish(new CustomEvent { Value = 1 });
        world.Events.Publish(new CustomEvent { Value = 2 });
        world.Events.Publish(new OtherEvent { Name = "test" });

        Assert.Equal(2, customEventCount);
        Assert.Equal(1, otherEventCount);
    }

    [Fact]
    public void EventBus_HasHandlersReturnsCorrectValue()
    {
        using var world = new World();

        Assert.False(world.Events.HasHandlers<CustomEvent>());

        using var subscription = world.Events.Subscribe<CustomEvent>(_ => { });

        Assert.True(world.Events.HasHandlers<CustomEvent>());
        Assert.False(world.Events.HasHandlers<OtherEvent>());
    }

    [Fact]
    public void EventBus_GetHandlerCountReturnsCorrectValue()
    {
        using var world = new World();

        Assert.Equal(0, world.Events.GetHandlerCount<CustomEvent>());

        using var sub1 = world.Events.Subscribe<CustomEvent>(_ => { });
        Assert.Equal(1, world.Events.GetHandlerCount<CustomEvent>());

        using var sub2 = world.Events.Subscribe<CustomEvent>(_ => { });
        Assert.Equal(2, world.Events.GetHandlerCount<CustomEvent>());
    }

    [Fact]
    public void EventBus_PublishWithNoSubscribers_DoesNotThrow()
    {
        using var world = new World();

        // Should not throw
        world.Events.Publish(new CustomEvent { Value = 42 });
    }

    [Fact]
    public void EventBus_SubscribeThrowsOnNullHandler()
    {
        using var world = new World();

        Assert.Throws<ArgumentNullException>(() =>
            world.Events.Subscribe<CustomEvent>(null!));
    }

    #endregion

    #region EventSubscription Tests

    [Fact]
    public void EventSubscription_DisposeIsIdempotent()
    {
        using var world = new World();

        var eventCount = 0;

        var subscription = world.OnEntityCreated((_, _) => eventCount++);

        world.Spawn().Build();
        Assert.Equal(1, eventCount);

        // Dispose multiple times - should not throw
        subscription.Dispose();
        subscription.Dispose();
        subscription.Dispose();

        world.Spawn().Build();
        Assert.Equal(1, eventCount); // Still should not increase
    }

    [Fact]
    public void EventSubscription_CanUnsubscribeDuringEvent()
    {
        using var world = new World();

        EventSubscription? subscription = null;
        var eventCount = 0;

        subscription = world.OnEntityCreated((_, _) =>
        {
            eventCount++;
            subscription?.Dispose();
        });

        world.Spawn().Build();
        world.Spawn().Build();
        world.Spawn().Build();

        // Only the first event should have been processed
        Assert.Equal(1, eventCount);
    }

    #endregion

    #region World Dispose Clears Events

    [Fact]
    public void WorldDispose_ClearsAllEventHandlers()
    {
        var world = new World();

        var createCount = 0;
        var destroyCount = 0;
        var addCount = 0;

        world.OnEntityCreated((_, _) => createCount++);
        world.OnEntityDestroyed(_ => destroyCount++);
        world.OnComponentAdded<TestPosition>((_, _) => addCount++);

        var entity = world.Spawn().With(new TestPosition { X = 1f, Y = 1f }).Build();
        world.Despawn(entity);

        Assert.Equal(1, createCount);
        Assert.Equal(1, destroyCount);

        world.Dispose();

        // After dispose, event handlers should be cleared
        // But we can't add entities to a disposed world to test
        // This just ensures no exceptions during cleanup
    }

    [Fact]
    public void WorldDispose_ClearsEventBusSubscriptions()
    {
        var world = new World();

        // Subscribe to custom event
        var eventCount = 0;
        var subscription = world.Events.Subscribe<CustomEvent>(_ => eventCount++);

        // Verify subscription works before disposal
        world.Events.Publish(new CustomEvent { Value = 42 });
        Assert.Equal(1, eventCount);

        // Verify handler is registered
        Assert.True(world.Events.HasHandlers<CustomEvent>());
        Assert.Equal(1, world.Events.GetHandlerCount<CustomEvent>());

        // Dispose world
        world.Dispose();

        // Verify handlers are cleared
        Assert.False(world.Events.HasHandlers<CustomEvent>());
        Assert.Equal(0, world.Events.GetHandlerCount<CustomEvent>());

        // Disposing subscription after world disposal should not throw
        subscription.Dispose();
    }

    [Fact]
    public void WorldDispose_ClearsComponentLifecycleSubscriptions()
    {
        var world = new World();

        // Track if handlers are still holding references
        var addedCount = 0;
        var removedCount = 0;
        var changedCount = 0;

        var addSub = world.OnComponentAdded<TestPosition>((_, _) => addedCount++);
        var removeSub = world.OnComponentRemoved<TestPosition>(_ => removedCount++);
        var changeSub = world.OnComponentChanged<TestPosition>((_, _, _) => changedCount++);

        // Verify subscriptions work before disposal
        var entity = world.Spawn().Build();
        world.Add(entity, new TestPosition { X = 1f, Y = 1f });
        world.Set(entity, new TestPosition { X = 2f, Y = 2f });
        world.Remove<TestPosition>(entity);

        Assert.Equal(1, addedCount);
        Assert.Equal(1, removedCount);
        Assert.Equal(1, changedCount);

        // Dispose world - this should clear all handlers
        world.Dispose();

        // Disposing subscriptions after world disposal should not throw
        // and should be no-ops since handlers are already cleared
        addSub.Dispose();
        removeSub.Dispose();
        changeSub.Dispose();
    }

    [Fact]
    public void WorldDispose_ClearsEntityLifecycleSubscriptions()
    {
        var world = new World();

        var createdCount = 0;
        var destroyedCount = 0;

        var createSub = world.OnEntityCreated((_, _) => createdCount++);
        var destroySub = world.OnEntityDestroyed(_ => destroyedCount++);

        // Verify subscriptions work before disposal
        var entity = world.Spawn().Build();
        world.Despawn(entity);

        Assert.Equal(1, createdCount);
        Assert.Equal(1, destroyedCount);

        // Dispose world - this should clear all handlers
        world.Dispose();

        // Disposing subscriptions after world disposal should not throw
        createSub.Dispose();
        destroySub.Dispose();
    }

    [Fact]
    public void WorldDispose_PreventsMemoryLeaksFromLongLivedSubscribers()
    {
        // This test verifies the pattern where a long-lived object subscribes to events
        // and the world gets disposed - the subscriber should no longer hold references
        // to the world or its data

        WeakReference worldRef;
        EventSubscription? subscription = null;

        // Create scope where world lives
        {
            var world = new World();
            worldRef = new WeakReference(world);

            // Long-lived subscriber (simulated by keeping subscription reference)
            var eventCount = 0;
            subscription = world.Events.Subscribe<CustomEvent>(_ => eventCount++);

            // Verify world is alive
            Assert.True(worldRef.IsAlive);

            // Dispose world - should clear all subscriptions
            world.Dispose();
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // World should be collectible even though we still hold the subscription
        // because the subscription's handler list was cleared
        Assert.False(worldRef.IsAlive);

        // Disposing the subscription should not throw
        subscription?.Dispose();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Events_WorkWithEntityBuilder()
    {
        using var world = new World();

        var createdCount = 0;
        string? createdName = null;

        using var subscription = world.OnEntityCreated((entity, name) =>
        {
            createdCount++;
            createdName = name;
        });

        world.Spawn("TestEntity")
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 2f, Y = 2f })
            .Build();

        Assert.Equal(1, createdCount);
        Assert.Equal("TestEntity", createdName);
    }

    [Fact]
    public void Events_MultipleComponentTypesHandledIndependently()
    {
        using var world = new World();

        var posAddCount = 0;
        var velAddCount = 0;
        var healthAddCount = 0;

        using var sub1 = world.OnComponentAdded<TestPosition>((_, _) => posAddCount++);
        using var sub2 = world.OnComponentAdded<TestVelocity>((_, _) => velAddCount++);
        using var sub3 = world.OnComponentAdded<TestHealth>((_, _) => healthAddCount++);

        var entity = world.Spawn().Build();

        world.Add(entity, new TestPosition { X = 1f, Y = 1f });
        Assert.Equal(1, posAddCount);
        Assert.Equal(0, velAddCount);
        Assert.Equal(0, healthAddCount);

        world.Add(entity, new TestVelocity { X = 2f, Y = 2f });
        Assert.Equal(1, posAddCount);
        Assert.Equal(1, velAddCount);
        Assert.Equal(0, healthAddCount);

        world.Add(entity, new TestHealth { Current = 100, Max = 100 });
        Assert.Equal(1, posAddCount);
        Assert.Equal(1, velAddCount);
        Assert.Equal(1, healthAddCount);
    }

    [Fact]
    public void Events_NoEventsFiredForDeadEntity()
    {
        using var world = new World();

        var addCount = 0;

        using var subscription = world.OnComponentAdded<TestPosition>((_, _) => addCount++);

        var entity = world.Spawn().Build();
        world.Despawn(entity);

        // Entity is dead, Add should throw
        Assert.Throws<InvalidOperationException>(() =>
            world.Add(entity, new TestPosition { X = 1f, Y = 1f }));

        Assert.Equal(0, addCount);
    }

    [Fact]
    public void Events_ComponentChangedCapturesCorrectOldValue()
    {
        using var world = new World();

        var changes = new List<(float OldX, float NewX)>();

        using var subscription = world.OnComponentChanged<TestPosition>((entity, oldVal, newVal) =>
        {
            changes.Add((oldVal.X, newVal.X));
        });

        var entity = world.Spawn().With(new TestPosition { X = 1f, Y = 0f }).Build();

        world.Set(entity, new TestPosition { X = 2f, Y = 0f });
        world.Set(entity, new TestPosition { X = 3f, Y = 0f });
        world.Set(entity, new TestPosition { X = 4f, Y = 0f });

        Assert.Equal(3, changes.Count);
        Assert.Equal((1f, 2f), changes[0]);
        Assert.Equal((2f, 3f), changes[1]);
        Assert.Equal((3f, 4f), changes[2]);
    }

    #endregion

    #region Has Handlers Tests (Coverage)

    [Fact]
    public void EventBus_HasHandlers_ReturnsFalseAfterDispose()
    {
        using var world = new World();

        var subscription = world.Events.Subscribe<CustomEvent>(_ => { });

        Assert.True(world.Events.HasHandlers<CustomEvent>());

        subscription.Dispose();

        Assert.False(world.Events.HasHandlers<CustomEvent>());
    }

    [Fact]
    public void EventBus_GetHandlerCount_DecreasesAfterDispose()
    {
        using var world = new World();

        var sub1 = world.Events.Subscribe<CustomEvent>(_ => { });
        var sub2 = world.Events.Subscribe<CustomEvent>(_ => { });

        Assert.Equal(2, world.Events.GetHandlerCount<CustomEvent>());

        sub1.Dispose();
        Assert.Equal(1, world.Events.GetHandlerCount<CustomEvent>());

        sub2.Dispose();
        Assert.Equal(0, world.Events.GetHandlerCount<CustomEvent>());
    }

    #endregion

    #region Test Event Types

    private struct CustomEvent
    {
        public int Value;
    }

    private struct OtherEvent
    {
        public string Name;
    }

    #endregion
}
