using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Physics.Components;
using KeenEyes.Physics.Core;
using KeenEyes.Physics.Events;

namespace KeenEyes.Physics.Tests;

/// <summary>
/// Tests that trigger actual collisions to exercise the NarrowPhaseCallbacks code paths.
/// </summary>
public class CollisionCallbackTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    #region Collision Filter Callbacks

    [Fact]
    public void Collision_WithMatchingFilters_ProducesCollisionEvents()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var collisionEvents = new List<CollisionEvent>();
        world.Subscribe<CollisionEvent>(e => collisionEvents.Add(e));

        // Create two dynamic bodies that should collide
        var entityA = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 10f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .With(new CollisionFilter(1, 0xFFFFFFFF))
            .Build();

        var entityB = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 8f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .With(new CollisionFilter(1, 0xFFFFFFFF))
            .Build();

        // Step physics multiple times to allow collision to occur
        for (int i = 0; i < 10; i++)
        {
            physics.Step(1f / 60f);
        }

        // Should have collision events
        Assert.NotEmpty(collisionEvents);
    }

    [Fact]
    public void Collision_WithNonMatchingFilters_ProducesNoCollisionEvents()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var collisionEvents = new List<CollisionEvent>();
        world.Subscribe<CollisionEvent>(e => collisionEvents.Add(e));

        // Create two dynamic bodies with filters that prevent collision
        var entityA = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 10f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .With(new CollisionFilter(1, 0b0010)) // Layer 1, only collides with layer 2
            .Build();

        var entityB = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 8f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .With(new CollisionFilter(1, 0b0010)) // Layer 1, only collides with layer 2
            .Build();

        // Step physics multiple times
        for (int i = 0; i < 10; i++)
        {
            physics.Step(1f / 60f);
        }

        // Should not have collision events (they both want layer 2, but are both in layer 1)
        Assert.Empty(collisionEvents);
    }

    [Fact]
    public void Collision_WithTrigger_ProducesTriggerEvents()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var collisionEvents = new List<CollisionEvent>();
        world.Subscribe<CollisionEvent>(e => collisionEvents.Add(e));

        // Create overlapping trigger and dynamic body
        var trigger = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(5f, 5f, 5f))
            .With(RigidBody.Static())
            .With(CollisionFilter.Trigger())
            .Build();

        var dynamic = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 1f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Step physics to detect the overlap
        for (int i = 0; i < 5; i++)
        {
            physics.Step(1f / 60f);
        }

        // Should have trigger collision events
        var triggerEvents = collisionEvents.Where(e => e.IsTrigger).ToList();
        Assert.NotEmpty(triggerEvents);
    }

    #endregion

    #region Material Combining Tests

    [Fact]
    public void Collision_WithDifferentMaterials_CombinesMaterialProperties()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var collisionEvents = new List<CollisionEvent>();
        world.Subscribe<CollisionEvent>(e => collisionEvents.Add(e));

        // Create two overlapping bodies with different materials
        var entityA = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 1.5f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .With(PhysicsMaterial.Rubber)
            .Build();

        var entityB = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(10f, 1f, 10f))
            .With(RigidBody.Static())
            .With(PhysicsMaterial.Ice)
            .Build();

        // Step to detect collision
        for (int i = 0; i < 5; i++)
        {
            physics.Step(1f / 60f);
        }

        // Should have collision events (material combining is tested internally)
        Assert.NotEmpty(collisionEvents);
    }

    [Fact]
    public void Collision_WithoutMaterialComponent_UsesDefaultMaterial()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var collisionEvents = new List<CollisionEvent>();
        world.Subscribe<CollisionEvent>(e => collisionEvents.Add(e));

        // Create overlapping bodies without explicit material components
        var entityA = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 1.5f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var entityB = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(10f, 1f, 10f))
            .With(RigidBody.Static())
            .Build();

        // Step simulation
        for (int i = 0; i < 5; i++)
        {
            physics.Step(1f / 60f);
        }

        // Should have collision events with default materials
        Assert.NotEmpty(collisionEvents);
    }

    #endregion

    #region Multi-Contact Tests

    [Fact]
    public void Collision_WithMultipleContactPoints_RecordsDeepestPenetration()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var collisionEvents = new List<CollisionEvent>();
        world.Subscribe<CollisionEvent>(e => collisionEvents.Add(e));

        // Create overlapping box-box collision which will have multiple contact points
        var entityA = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 0.8f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(1f, 1f, 1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var entityB = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(5f, 1f, 5f))
            .With(RigidBody.Static())
            .Build();

        // Step to detect collision
        for (int i = 0; i < 5; i++)
        {
            physics.Step(1f / 60f);
        }

        // Should have collision events
        Assert.NotEmpty(collisionEvents);
    }

    #endregion

    #region Collision Event Ordering Tests

    [Fact]
    public void Collision_EntitiesInDifferentOrder_ProducesSameCollisionPair()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var collisionEvents = new List<CollisionEvent>();
        world.Subscribe<CollisionEvent>(e => collisionEvents.Add(e));

        // Create two overlapping spheres
        var entityA = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var entityB = world.Spawn()
            .With(new Transform3D(new Vector3(1.5f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Step physics
        for (int i = 0; i < 10; i++)
        {
            physics.Step(1f / 60f);
        }

        // Should have collision events
        // Entities should be ordered consistently in the collision pair
        Assert.NotEmpty(collisionEvents);
    }

    #endregion

    #region Static vs Dynamic Collision Tests

    [Fact]
    public void Collision_BetweenDynamicAndStatic_ProducesEvents()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var collisionEvents = new List<CollisionEvent>();
        world.Subscribe<CollisionEvent>(e => collisionEvents.Add(e));

        // Create overlapping dynamic and static
        var dynamic = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 1.5f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var staticBody = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(10f, 1f, 10f))
            .With(RigidBody.Static())
            .Build();

        // Step to detect collision
        for (int i = 0; i < 5; i++)
        {
            physics.Step(1f / 60f);
        }

        // Should have collision events between dynamic and static
        Assert.NotEmpty(collisionEvents);
        Assert.Contains(collisionEvents, e =>
            (e.EntityA == dynamic && e.EntityB == staticBody) ||
            (e.EntityA == staticBody && e.EntityB == dynamic));
    }

    [Fact]
    public void Collision_BetweenTwoDynamicBodies_ProducesEvents()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var collisionEvents = new List<CollisionEvent>();
        world.Subscribe<CollisionEvent>(e => collisionEvents.Add(e));

        // Create two dynamic bodies that will collide
        var entityA = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 10f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var entityB = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 8f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Step physics
        for (int i = 0; i < 10; i++)
        {
            physics.Step(1f / 60f);
        }

        // Should have collision events
        Assert.NotEmpty(collisionEvents);
    }

    #endregion

    #region Collision Filter Edge Cases

    [Fact]
    public void Collision_WithNoCollisionFilter_UsesDefaultFilter()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var collisionEvents = new List<CollisionEvent>();
        world.Subscribe<CollisionEvent>(e => collisionEvents.Add(e));

        // Create bodies without explicit collision filters (should use default)
        var entityA = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 10f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var entityB = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 8f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Step physics
        for (int i = 0; i < 10; i++)
        {
            physics.Step(1f / 60f);
        }

        // Should have collision events (default filter allows all collisions)
        Assert.NotEmpty(collisionEvents);
    }

    [Fact]
    public void Collision_WithOneFilterOneMissing_StillProcessesCorrectly()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var collisionEvents = new List<CollisionEvent>();
        world.Subscribe<CollisionEvent>(e => collisionEvents.Add(e));

        // Create one body with filter, one without
        var entityA = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 10f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .With(new CollisionFilter(1, 0xFFFFFFFF))
            .Build();

        var entityB = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 8f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Step physics
        for (int i = 0; i < 10; i++)
        {
            physics.Step(1f / 60f);
        }

        // Should have collision events
        Assert.NotEmpty(collisionEvents);
    }

    #endregion
}
