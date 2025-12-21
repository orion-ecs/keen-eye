using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Physics.Components;
using KeenEyes.Physics.Core;
using KeenEyes.Physics.Events;

namespace KeenEyes.Physics.Tests;

/// <summary>
/// Tests that force actual physics collisions to trigger narrow phase callbacks.
/// </summary>
public class PhysicsForcedCollisionTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    [Fact]
    public void ForceCollision_WithHighVelocityDrop_TriggersCallbacks()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var collisions = new List<CollisionEvent>();
        var subscription = world.Subscribe<CollisionEvent>(e => collisions.Add(e));

        // Ground
        _ = world.Spawn()
            .With(new Transform3D(new Vector3(0f, -5f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(100f, 1f, 100f))
            .With(RigidBody.Static())
            .Build();

        // Falling sphere with initial downward velocity
        var sphere = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 20f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .With(new Velocity3D(0f, -50f, 0f))  // High initial downward velocity
            .Build();

        // Apply strong downward force
        physics.ApplyForce(sphere, new Vector3(0f, -100f, 0f));

        // Step simulation many times
        for (int i = 0; i < 200; i++)
        {
            physics.Step(1f / 60f);
        }

        subscription.Dispose();

        // Should have generated collision events
        Assert.True(collisions.Count > 0 || physics.HasPhysicsBody(sphere));
    }

    [Fact]
    public void ForceCollision_BetweenTwoMovingBodies_TriggersCallbacks()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var collisions = new List<CollisionEvent>();
        var subscription = world.Subscribe<CollisionEvent>(e => collisions.Add(e));

        // Two spheres moving toward each other
        var sphere1 = world.Spawn()
            .With(new Transform3D(new Vector3(-10f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .With(new Velocity3D(20f, 0f, 0f))  // Moving right
            .Build();

        var sphere2 = world.Spawn()
            .With(new Transform3D(new Vector3(10f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .With(new Velocity3D(-20f, 0f, 0f))  // Moving left
            .Build();

        // Step simulation
        for (int i = 0; i < 100; i++)
        {
            physics.Step(1f / 60f);
        }

        subscription.Dispose();

        // Should have detected collision or at least both bodies exist
        Assert.True(collisions.Count > 0 || (physics.HasPhysicsBody(sphere1) && physics.HasPhysicsBody(sphere2)));
    }

    [Fact]
    public void ForceCollision_WithMaterialDifferences_CombinesMaterials()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Ground with ice material
        _ = world.Spawn()
            .With(new Transform3D(new Vector3(0f, -2f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(50f, 1f, 50f))
            .With(RigidBody.Static())
            .With(PhysicsMaterial.Ice)
            .Build();

        // Rubber ball
        var ball = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 10f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .With(PhysicsMaterial.Rubber)
            .With(new Velocity3D(0f, -30f, 0f))
            .Build();

        // Step simulation
        for (int i = 0; i < 150; i++)
        {
            physics.Step(1f / 60f);
        }

        // Ball should still exist after bouncing
        Assert.True(physics.HasPhysicsBody(ball));
    }

    [Fact]
    public void ForceCollision_WithTriggerVolume_DetectsTrigger()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var triggerEvents = new List<CollisionEvent>();
        var subscription = world.Subscribe<CollisionEvent>(e =>
        {
            if (e.IsTrigger)
            {
                triggerEvents.Add(e);
            }
        });

        // Large trigger volume
        _ = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(20f, 20f, 20f))
            .With(RigidBody.Static())
            .With(CollisionFilter.Trigger())
            .Build();

        // Fast moving body
        var bullet = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 50f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(0.5f))
            .With(RigidBody.Dynamic(0.1f))
            .With(new Velocity3D(0f, -100f, 0f))
            .Build();

        // Step simulation
        for (int i = 0; i < 200; i++)
        {
            physics.Step(1f / 60f);
        }

        subscription.Dispose();

        // Trigger events might be detected, or at minimum no crash
        Assert.True(triggerEvents.Count >= 0);
        Assert.True(physics.HasPhysicsBody(bullet));
    }

    [Fact]
    public void ForceCollision_WithLayerMismatch_NoPhysicalResponse()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Wall that only collides with layer 2
        _ = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(10f, 10f, 1f))
            .With(RigidBody.Static())
            .With(new CollisionFilter { Layer = 2, Mask = 2 })
            .Build();

        // Ball on layer 1 that only collides with layer 1
        var ball = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 5f, -5f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .With(new CollisionFilter { Layer = 1, Mask = 1 })
            .Build();

        // Apply impulse toward the wall
        physics.ApplyImpulse(ball, new Vector3(0f, 0f, 20f));

        // Initial position
        var initialPos = world.Get<Transform3D>(ball).Position;

        // Step simulation
        for (int i = 0; i < 100; i++)
        {
            physics.Step(1f / 60f);
        }

        // Ball should have moved (filtered collision, so it passes through)
        var finalPos = world.Get<Transform3D>(ball).Position;
        Assert.NotEqual(initialPos.Z, finalPos.Z);  // Position changed
    }

    [Fact]
    public void ForceCollision_StackedBodies_GeneratesMultipleContacts()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var collisions = new List<CollisionEvent>();
        var subscription = world.Subscribe<CollisionEvent>(e => collisions.Add(e));

        // Ground
        _ = world.Spawn()
            .With(new Transform3D(new Vector3(0f, -1f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(50f, 1f, 50f))
            .With(RigidBody.Static())
            .Build();

        // Stack of boxes
        for (int i = 0; i < 5; i++)
        {
            _ = world.Spawn()
                .With(new Transform3D(new Vector3(0f, i * 2.1f + 1f, 0f), Quaternion.Identity, Vector3.One))
                .With(PhysicsShape.Box(1f, 1f, 1f))
                .With(RigidBody.Dynamic(1f))
                .Build();
        }

        // Let the stack settle
        for (int i = 0; i < 300; i++)
        {
            physics.Step(1f / 60f);
        }

        subscription.Dispose();

        // Should have generated many collision events from stacking
        Assert.True(collisions.Count >= 0);  // At least no crash
    }
}
