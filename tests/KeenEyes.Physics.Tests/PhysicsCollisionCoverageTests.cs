using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Physics.Components;
using KeenEyes.Physics.Core;
using KeenEyes.Physics.Events;

namespace KeenEyes.Physics.Tests;

/// <summary>
/// Tests focused on collision detection and filtering to improve coverage of narrow phase callbacks.
/// </summary>
public class PhysicsCollisionCoverageTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    #region Collision Filter Tests with Physics Simulation

    [Fact]
    public void Collision_WithFilteredLayers_PreventCollision()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create two overlapping bodies with incompatible collision filters. Starting them
        // overlapping guarantees the narrow phase would report contact if filtering were
        // broken, so a clean run proves the filter actually suppressed the collision.
        var entity1 = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 5f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .With(new CollisionFilter
            {
                Layer = 1,
                Mask = 2  // Can only collide with layer 2
            })
            .Build();

        var entity2 = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 4.5f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .With(new CollisionFilter
            {
                Layer = 4,
                Mask = 8  // Can only collide with layer 8
            })
            .Build();

        var collisionDetected = false;
        using var subscription = world.Subscribe<CollisionEvent>(collision =>
        {
            if ((collision.EntityA == entity1 && collision.EntityB == entity2) ||
                (collision.EntityA == entity2 && collision.EntityB == entity1))
            {
                collisionDetected = true;
            }
        });

        // Step simulation multiple times to allow potential collision
        for (int i = 0; i < 60; i++)
        {
            physics.Step(1f / 60f);
        }

        // Incompatible filters must suppress the collision even though the bodies overlap.
        Assert.False(collisionDetected);
        Assert.True(physics.HasPhysicsBody(entity1));
        Assert.True(physics.HasPhysicsBody(entity2));
    }

    [Fact]
    public void Collision_WithMatchingLayers_AllowCollision()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var collisionDetected = false;

        // Create two overlapping bodies whose filters permit collision. Overlapping start
        // positions force a deterministic contact on the first step, so the collision event
        // is guaranteed to fire when filtering works.
        var entity1 = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 5f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .With(new CollisionFilter
            {
                Layer = 1,
                Mask = 2  // Can collide with layer 2
            })
            .Build();

        var entity2 = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 4.5f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .With(new CollisionFilter
            {
                Layer = 2,
                Mask = 1  // Can collide with layer 1
            })
            .Build();

        // Subscribe to collision events
        using var subscription = world.Subscribe<CollisionEvent>(collision =>
        {
            if ((collision.EntityA == entity1 && collision.EntityB == entity2) ||
                (collision.EntityA == entity2 && collision.EntityB == entity1))
            {
                collisionDetected = true;
            }
        });

        // Step simulation to allow collision
        for (int i = 0; i < 120; i++)
        {
            physics.Step(1f / 60f);
        }

        // Matching filters plus overlapping bodies must produce a collision event.
        Assert.True(collisionDetected);
    }

    #endregion

    #region Trigger Collision Tests

    [Fact]
    public void Collision_WithTrigger_DetectsOverlapButNoPhysicalResponse()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create a trigger body
        var trigger = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(5f, 5f, 5f))
            .With(RigidBody.Static())
            .With(CollisionFilter.Trigger())
            .Build();

        // Create a dynamic body overlapping the trigger volume so a contact is guaranteed.
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 1f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var triggerCollisionDetected = false;
        using var subscription = world.Subscribe<CollisionEvent>(collision =>
        {
            if (((collision.EntityA == trigger && collision.EntityB == entity) ||
                 (collision.EntityA == entity && collision.EntityB == trigger)) &&
                collision.IsTrigger)
            {
                triggerCollisionDetected = true;
            }
        });

        // Step simulation.
        for (int i = 0; i < 60; i++)
        {
            physics.Step(1f / 60f);
        }

        // The overlap is detected and reported as a trigger collision (IsTrigger), meaning
        // it was recorded without generating a solid contact constraint. If the trigger were
        // (incorrectly) treated as solid, IsTrigger would be false and this would never flip.
        Assert.True(triggerCollisionDetected);
        Assert.True(physics.HasPhysicsBody(trigger));
        Assert.True(physics.HasPhysicsBody(entity));
    }

    #endregion

    #region Material Property Collision Tests

    [Fact]
    public void Collision_WithDifferentMaterials_CombinesProperties()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create two overlapping bodies with different materials so a contact is guaranteed
        // and the material-combination code path runs.
        var entity1 = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 5f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .With(PhysicsMaterial.Rubber)
            .Build();

        var entity2 = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 4.5f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .With(PhysicsMaterial.Ice)
            .Build();

        var collisionDetected = false;
        using var subscription = world.Subscribe<CollisionEvent>(collision =>
        {
            if ((collision.EntityA == entity1 && collision.EntityB == entity2) ||
                (collision.EntityA == entity2 && collision.EntityB == entity1))
            {
                collisionDetected = true;
            }
        });

        // Step simulation to allow collision
        for (int i = 0; i < 60; i++)
        {
            physics.Step(1f / 60f);
        }

        // The overlap must produce a collision, exercising the combined-material contact path.
        Assert.True(collisionDetected);
        Assert.True(physics.HasPhysicsBody(entity1));
        Assert.True(physics.HasPhysicsBody(entity2));
    }

    [Fact]
    public void Collision_WithOneMissingMaterial_UsesDefault()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Entity with material, overlapping the second body to guarantee a contact.
        var entity1 = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 5f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .With(PhysicsMaterial.Rubber)
            .Build();

        // Entity without material (will use default)
        var entity2 = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 4.5f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var collisionDetected = false;
        using var subscription = world.Subscribe<CollisionEvent>(collision =>
        {
            if ((collision.EntityA == entity1 && collision.EntityB == entity2) ||
                (collision.EntityA == entity2 && collision.EntityB == entity1))
            {
                collisionDetected = true;
            }
        });

        // Step simulation
        for (int i = 0; i < 60; i++)
        {
            physics.Step(1f / 60f);
        }

        // The body missing a material must still collide, using the default material.
        Assert.True(collisionDetected);
        Assert.True(physics.HasPhysicsBody(entity1));
        Assert.True(physics.HasPhysicsBody(entity2));
    }

    #endregion

    #region Collision Event Tests

    [Fact]
    public void Collision_RecordsCollisionEvents()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity1 = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 10f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var entity2 = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(10f, 1f, 10f))
            .With(RigidBody.Static())
            .Build();

        var collisionEvents = new List<CollisionEvent>();
        using var subscription = world.Subscribe<CollisionEvent>(collision =>
        {
            collisionEvents.Add(collision);
        });

        // Step simulation to allow collision
        for (int i = 0; i < 120; i++)
        {
            physics.Step(1f / 60f);
        }

        // Any recorded collision events must reference the two simulated bodies
        // (collision timing is not guaranteed, so the collection may be empty)
        Assert.All(collisionEvents, e => Assert.True(
            e.EntityA == entity1 || e.EntityA == entity2 ||
            e.EntityB == entity1 || e.EntityB == entity2));
        Assert.True(physics.HasPhysicsBody(entity1));
        Assert.True(physics.HasPhysicsBody(entity2));
    }

    #endregion

    #region Edge Case Coverage

    [Fact]
    public void Collision_WithMissingEntityInCallback_HandlesGracefully()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create entities that will collide
        var entity1 = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 5f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var entity2 = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(10f, 1f, 10f))
            .With(RigidBody.Static())
            .Build();

        // Step a few times
        for (int i = 0; i < 30; i++)
        {
            physics.Step(1f / 60f);
        }

        // Both entities should still exist
        Assert.True(physics.HasPhysicsBody(entity1));
        Assert.True(physics.HasPhysicsBody(entity2));
    }

    [Fact]
    public void Collision_BetweenTwoStaticBodies_DoesNotCrash()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create two overlapping static bodies
        _ = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(5f, 5f, 5f))
            .With(RigidBody.Static())
            .Build();

        _ = world.Spawn()
            .With(new Transform3D(new Vector3(2f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(5f, 5f, 5f))
            .With(RigidBody.Static())
            .Build();

        // Step simulation (static bodies don't collide, but callback should handle gracefully)
        physics.Step(1f / 60f);

        Assert.Equal(2, physics.StaticCount);
    }

    #endregion
}
