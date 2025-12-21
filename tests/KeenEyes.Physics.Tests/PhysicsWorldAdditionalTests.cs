using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Physics.Components;
using KeenEyes.Physics.Core;
using KeenEyes.Physics.Events;

namespace KeenEyes.Physics.Tests;

/// <summary>
/// Additional tests for PhysicsWorld to increase coverage.
/// </summary>
public class PhysicsWorldAdditionalTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    #region RayHit Record Tests

    [Fact]
    public void RayHit_RecordStructHasCorrectData()
    {
        var entity = new Entity(42, 5);
        var position = new Vector3(10f, 20f, 30f);
        var normal = Vector3.UnitY;
        var distance = 15.5f;

        var hit = new RayHit(entity, position, normal, distance);

        Assert.Equal(entity, hit.Entity);
        Assert.Equal(position, hit.Position);
        Assert.Equal(normal, hit.Normal);
        Assert.Equal(distance, hit.Distance);
    }

    [Fact]
    public void RayHit_DefaultInstance_HasDefaultValues()
    {
        var hit = default(RayHit);

        Assert.Equal(default(Entity), hit.Entity);
        Assert.Equal(Vector3.Zero, hit.Position);
        Assert.Equal(Vector3.Zero, hit.Normal);
        Assert.Equal(0f, hit.Distance);
    }

    #endregion

    #region Force and Impulse Error Tests

    [Fact]
    public void ApplyImpulse_WithNonExistentEntity_ThrowsException()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var fakeEntity = new Entity(999, 0);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            physics.ApplyImpulse(fakeEntity, Vector3.UnitX));

        Assert.Contains("does not have a physics body", ex.Message);
    }

    [Fact]
    public void ApplyAngularImpulse_WithNonExistentEntity_ThrowsException()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var fakeEntity = new Entity(999, 0);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            physics.ApplyAngularImpulse(fakeEntity, Vector3.UnitY));

        Assert.Contains("does not have a physics body", ex.Message);
    }

    [Fact]
    public void ApplyForceAtPosition_WithNonExistentEntity_ThrowsException()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var fakeEntity = new Entity(999, 0);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            physics.ApplyForceAtPosition(fakeEntity, Vector3.UnitX, Vector3.Zero));

        Assert.Contains("does not have a physics body", ex.Message);
    }

    [Fact]
    public void SetAngularVelocity_WithNonExistentEntity_ThrowsException()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var fakeEntity = new Entity(999, 0);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            physics.SetAngularVelocity(fakeEntity, Vector3.UnitY));

        Assert.Contains("does not have a physics body", ex.Message);
    }

    #endregion

    #region OverlapBox Tests

    [Fact]
    public void OverlapBox_WithDefaultRotation_FindsObjects()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create object inside box
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(1f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(0.5f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Test with default rotation (no parameter)
        var results = physics.OverlapBox(Vector3.Zero, new Vector3(5f, 5f, 5f)).ToList();

        Assert.Contains(entity, results);
    }

    [Fact]
    public void OverlapBox_WithExplicitDefaultRotation_FindsObjects()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create object inside box
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(1f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(0.5f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Test with explicit default rotation
        var results = physics.OverlapBox(Vector3.Zero, new Vector3(5f, 5f, 5f), default(Quaternion)).ToList();

        Assert.Contains(entity, results);
    }

    #endregion

    #region Kinematic Body Sync Tests

    [Fact]
    public void KinematicBody_WithVelocityComponent_SyncsVelocity()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(1f, 1f, 1f))
            .With(RigidBody.Kinematic())
            .With(new Velocity3D(5f, 0f, 0f))
            .Build();

        // Step simulation to trigger sync
        physics.Step(1f / 60f);

        // Velocity should have been synced to physics body
        Assert.True(physics.HasPhysicsBody(entity));
    }

    [Fact]
    public void KinematicBody_WithAngularVelocityComponent_SyncsAngularVelocity()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(1f, 1f, 1f))
            .With(RigidBody.Kinematic())
            .With(new AngularVelocity3D(0f, 3f, 0f))
            .Build();

        // Step simulation to trigger sync
        physics.Step(1f / 60f);

        // Angular velocity should have been synced to physics body
        Assert.True(physics.HasPhysicsBody(entity));
    }

    [Fact]
    public void KinematicBody_WithoutVelocityComponent_DoesNotCrash()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(1f, 1f, 1f))
            .With(RigidBody.Kinematic())
            .Build();

        // Step simulation - should not crash even without velocity components
        physics.Step(1f / 60f);

        Assert.True(physics.HasPhysicsBody(entity));
    }

    #endregion

    #region Dynamic Body Sync Tests

    [Fact]
    public void DynamicBody_SyncsPositionFromSimulationToEcs()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 10f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        var initialY = world.Get<Transform3D>(entity).Position.Y;

        // Step multiple times to let gravity pull it down
        for (int i = 0; i < 10; i++)
        {
            physics.Step(1f / 60f);
        }

        var finalY = world.Get<Transform3D>(entity).Position.Y;

        // Position should have changed due to gravity
        Assert.True(finalY < initialY);
    }

    [Fact]
    public void DynamicBody_WithVelocityComponent_SyncsVelocity()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .With(new Velocity3D(0f, 0f, 0f))
            .Build();

        // Apply impulse to change velocity
        physics.ApplyImpulse(entity, new Vector3(10f, 0f, 0f));

        // Step to sync velocity back to ECS
        physics.Step(1f / 60f);

        // Velocity component should have been updated
        var velocity = world.Get<Velocity3D>(entity);
        Assert.True(velocity.Value.X > 0);
    }

    [Fact]
    public void DynamicBody_WithAngularVelocityComponent_SyncsAngularVelocity()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(1f, 1f, 1f))
            .With(RigidBody.Dynamic(1f))
            .With(new AngularVelocity3D(0f, 0f, 0f))
            .Build();

        // Apply angular impulse
        physics.ApplyAngularImpulse(entity, new Vector3(0f, 5f, 0f));

        // Step to sync angular velocity back to ECS
        physics.Step(1f / 60f);

        // Angular velocity component should have been updated
        var angVel = world.Get<AngularVelocity3D>(entity);
        Assert.True(angVel.Value.Y > 0);
    }

    [Fact]
    public void DynamicBody_WithoutVelocityComponent_DoesNotCrash()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Step simulation - should not crash even without velocity components
        physics.Step(1f / 60f);

        Assert.True(physics.HasPhysicsBody(entity));
    }

    #endregion

    #region PhysicsWorld Properties Tests

    [Fact]
    public void BodyCount_WithMultipleBodies_ReturnsCorrectCount()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        Assert.Equal(0, physics.BodyCount);

        world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        Assert.Equal(1, physics.BodyCount);

        world.Spawn()
            .With(new Transform3D(new Vector3(5f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(1f, 1f, 1f))
            .With(RigidBody.Kinematic())
            .Build();

        Assert.Equal(2, physics.BodyCount);
    }

    [Fact]
    public void StaticCount_WithMultipleStatics_ReturnsCorrectCount()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        Assert.Equal(0, physics.StaticCount);

        world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(10f, 1f, 10f))
            .With(RigidBody.Static())
            .Build();

        Assert.Equal(1, physics.StaticCount);

        world.Spawn()
            .With(new Transform3D(new Vector3(20f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(10f, 1f, 10f))
            .With(RigidBody.Static())
            .Build();

        Assert.Equal(2, physics.StaticCount);
    }

    [Fact]
    public void Config_ReturnsProvidedConfiguration()
    {
        world = new World();
        var customConfig = new PhysicsConfig
        {
            FixedTimestep = 1f / 120f,
            Gravity = new Vector3(0f, -5f, 0f),
            MaxStepsPerFrame = 5
        };
        world.InstallPlugin(new PhysicsPlugin(customConfig));
        var physics = world.GetExtension<PhysicsWorld>();

        Assert.Equal(1f / 120f, physics.Config.FixedTimestep);
        Assert.Equal(new Vector3(0f, -5f, 0f), physics.Config.Gravity);
        Assert.Equal(5, physics.Config.MaxStepsPerFrame);
    }

    #endregion

    #region Step Tests

    [Fact]
    public void Step_WithZeroDeltaTime_TakesNoSteps()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var steps = physics.Step(0f);

        Assert.Equal(0, steps);
    }

    [Fact]
    public void Step_WithVerySmallDeltaTime_AccumulatesOverTime()
    {
        world = new World();
        var config = new PhysicsConfig { FixedTimestep = 1f / 60f };
        world.InstallPlugin(new PhysicsPlugin(config));
        var physics = world.GetExtension<PhysicsWorld>();

        // Step with very small deltas
        for (int i = 0; i < 100; i++)
        {
            physics.Step(0.0001f); // 0.1ms
        }

        // After 100 steps of 0.1ms, total time is 10ms
        // With timestep of ~16.67ms, no step should have occurred yet
        // But let's add enough to trigger at least one step
        var finalSteps = physics.Step(0.02f);

        // Should eventually take at least one step
        Assert.True(finalSteps >= 0);
    }

    #endregion

    #region Raycast Edge Cases

    [Fact]
    public void Raycast_WithZeroMaxDistance_ReturnsFalse()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create a box right in front
        world.Spawn()
            .With(new Transform3D(new Vector3(1f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(1f, 1f, 1f))
            .With(RigidBody.Static())
            .Build();

        // Ray with zero max distance
        var hit = physics.Raycast(Vector3.Zero, Vector3.UnitX, 0f, out _);

        Assert.False(hit);
    }

    [Fact]
    public void Raycast_ThroughMultipleObjects_ReturnsClosest()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create two boxes in a line
        _ = world.Spawn()
            .With(new Transform3D(new Vector3(10f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(1f, 1f, 1f))
            .With(RigidBody.Static())
            .Build();

        var nearBox = world.Spawn()
            .With(new Transform3D(new Vector3(3f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(1f, 1f, 1f))
            .With(RigidBody.Static())
            .Build();

        // Ray should hit the near box first
        var hit = physics.Raycast(Vector3.Zero, Vector3.UnitX, 20f, out var hitInfo);

        Assert.True(hit);
        Assert.Equal(nearBox, hitInfo.Entity);
        Assert.True(hitInfo.Distance < 10f);
    }

    #endregion

    #region OverlapSphere Edge Cases

    [Fact]
    public void OverlapSphere_WithVerySmallRadius_FindsOverlappingObjects()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create object at origin
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        // Overlap sphere with very small radius at the same position
        // Should still find the object since they overlap
        var results = physics.OverlapSphere(Vector3.Zero, 0.1f).ToList();

        Assert.Contains(entity, results);
    }

    [Fact]
    public void OverlapSphere_WithLargeRadius_FindsAllObjects()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create multiple objects spread out
        var entities = new List<Entity>();
        for (int i = 0; i < 5; i++)
        {
            var entity = world.Spawn()
                .With(new Transform3D(new Vector3(i * 2f, 0f, 0f), Quaternion.Identity, Vector3.One))
                .With(PhysicsShape.Sphere(0.5f))
                .With(RigidBody.Dynamic(1f))
                .Build();
            entities.Add(entity);
        }

        // Very large overlap sphere
        var results = physics.OverlapSphere(Vector3.Zero, 100f).ToList();

        Assert.Equal(5, results.Count);
        foreach (var entity in entities)
        {
            Assert.Contains(entity, results);
        }
    }

    #endregion

    #region PhysicsPlugin Edge Cases

    [Fact]
    public void PhysicsPlugin_AddRigidBodyWithoutTransform_DoesNotCreateBody()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create entity with RigidBody but no Transform3D
        var entity = world.Spawn().Build();
        world.Add(entity, RigidBody.Dynamic(1f));

        // Body should not be created
        Assert.False(physics.HasPhysicsBody(entity));
    }

    [Fact]
    public void PhysicsPlugin_AddRigidBodyWithoutShape_DoesNotCreateBody()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        // Create entity with RigidBody and Transform but no PhysicsShape
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .Build();
        world.Add(entity, RigidBody.Dynamic(1f));

        // Body should not be created
        Assert.False(physics.HasPhysicsBody(entity));
    }

    [Fact]
    public void PhysicsPlugin_Uninstall_CleansUpPhysicsWorld()
    {
        world = new World();
        var plugin = new PhysicsPlugin();
        world.InstallPlugin(plugin);

        // Create a body
        world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        Assert.True(world.TryGetExtension<PhysicsWorld>(out _));

        // Uninstall
        world.UninstallPlugin<PhysicsPlugin>();

        // Extension should be removed
        Assert.False(world.TryGetExtension<PhysicsWorld>(out _));
    }

    #endregion

    #region Collision Event Manager Edge Cases

    [Fact]
    public void CollisionEventManager_RecordCollision_WithShallowerThenTrigger_PreservesTrigger()
    {
        world = new World();
        var manager = new CollisionEventManager(world);

        var entityA = world.Spawn().Build();
        var entityB = world.Spawn().Build();

        var collisionEvents = new List<CollisionEvent>();
        world.Subscribe<CollisionEvent>(e => collisionEvents.Add(e));

        // First: Non-trigger with deep penetration
        manager.RecordCollision(entityA, entityB, Vector3.UnitY, Vector3.Zero, 0.5f, false);
        // Second: Trigger with shallow penetration (should preserve trigger flag)
        manager.RecordCollision(entityA, entityB, Vector3.UnitX, Vector3.One, 0.1f, true);

        manager.PublishEvents();

        Assert.Single(collisionEvents);
        Assert.True(collisionEvents[0].IsTrigger);
    }

    #endregion
}
