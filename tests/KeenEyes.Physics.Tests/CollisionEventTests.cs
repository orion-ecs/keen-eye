using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Physics.Components;
using KeenEyes.Physics.Core;
using KeenEyes.Physics.Events;

namespace KeenEyes.Physics.Tests;

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
